using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using CognitiveLedger.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable All


namespace CognitiveLedger.AI.Services.Ollama;

/// <summary>
/// Owns the lifecycle of a private, per-application Ollama server: downloading the
/// platform-appropriate binary if needed, starting it as a supervised child process,
/// waiting for it to become healthy, and ensuring the configured model is pulled.
/// </summary>
public sealed class OllamaService : IHostedService, IDisposable
{
    private readonly OllamaServiceOptions _options;
    private readonly ILogger<OllamaService> _logger;
    // Binary downloads and model pulls can take well beyond HttpClient's 100s default
    // Timeout; cancellation is instead governed per-call by the tokens passed in.
    private readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly CancellationTokenSource _stoppingCts = new();
    // Serializes RunSetupAsync (boot-time) and DownloadAndStartAsync (user-triggered) so
    // they can never race and spawn two "ollama serve" processes against the same port.
    private readonly SemaphoreSlim _setupLock = new(1, 1);
    private Process? _process;

    public OllamaService(IOptions<OllamaServiceOptions> options, ILogger<OllamaService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ModelName))
        {
            throw new OllamaSetupException($"{nameof(OllamaServiceOptions.ModelName)} must be configured.");
        }
    }

    /// <summary>
    /// True once the Ollama binary has been found (or downloaded) on this machine.
    /// False means the user must call <see cref="DownloadAndStartAsync"/> to install it.
    /// </summary>
    public bool IsInstalled { get; private set; }

    /// <summary>
    /// True once the private Ollama server is running, healthy, and the configured model
    /// has been pulled.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Raised whenever <see cref="IsInstalled"/> or <see cref="IsRunning"/> changes, so
    /// consumers (e.g. a view model) can reflect current state instead of polling it.
    /// </summary>
    public event EventHandler? StateChanged;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Setup can take minutes on first run, so it must not block IHostedService.StartAsync
        // / host.Start() — run it in the background instead and let callers observe state via
        // IsInstalled/IsRunning/StateChanged.
        _ = AppStartupRunSetupAsync(_stoppingCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Ollama service...");
        await _stoppingCts.CancelAsync();

        if (_process is null || _process.HasExited)
        {
            _logger.LogInformation("Ollama process already stopped.");
            return;
        }

        try
        {
            _process.Kill(entireProcessTree: true);

            // Bound the wait ourselves rather than relying solely on the host's overall
            // ShutdownTimeout (30s by default) - if the child process misbehaves, that
            // would make the whole app appear frozen on close instead of just this step.
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await _process.WaitForExitAsync(linkedCts.Token);
            _logger.LogInformation("Ollama process stopped (pid {Pid}).", _process.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timed out waiting for the Ollama process to exit; continuing shutdown anyway.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop the Ollama process cleanly.");
        }
    }

    /// <summary>
    /// User-triggered install: downloads the Ollama binary if it isn't present, starts it,
    /// and pulls the configured model, reporting progress along the way.
    /// </summary>
    public async Task DownloadAndStartAsync(Action<ProgressStep>? progress, CancellationToken cancellationToken)
    {
        await _setupLock.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetBinary(out var binaryPath))
            {
                binaryPath = await DownloadOllamaModel(binaryPath, cancellationToken);
                SetInstalled(true);
            }

            await StartAndWaitAsync(progress, binaryPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama service failed to start.");
            throw;
        }
        finally
        {
            _setupLock.Release();
        }
    }

    /// <summary>
    /// Boot-time check: if Ollama is already installed, starts it automatically; if not,
    /// returns immediately and leaves <see cref="IsInstalled"/> false so the UI can prompt
    /// the user to run <see cref="DownloadAndStartAsync"/> themselves.
    /// </summary>
    private async Task AppStartupRunSetupAsync(CancellationToken cancellationToken)
    {
        await _setupLock.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetBinary(out var binaryPath))
            {
                _logger.LogInformation("Ollama not found on system; user action required to install.");
                SetInstalled(false);
                return;
            }

            SetInstalled(true);
            await StartAndWaitAsync(null, binaryPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama service failed to start.");
            throw;
        }
        finally
        {
            _setupLock.Release();
        }
    }

    private async Task StartAndWaitAsync(
        Action<ProgressStep>? progress,
        string binaryPath,
        CancellationToken cancellationToken)
    {
        var pStep = new ProgressStep(4);
        progress?.Invoke(pStep.NextStep("Starting Ollama server ..."));
        StartProcess(binaryPath);

        progress?.Invoke(pStep.NextStep("Waiting for Ollama server ..."));
        await WaitUntilHealthyAsync(cancellationToken);

        progress?.Invoke(pStep.NextStep("Downloading model ..."));
        await EnsureModelAsync(cancellationToken);

        progress?.Invoke(pStep.NextStep("Ollama ready"));
        SetRunning(true);
        _logger.LogInformation("Ollama service ready.");
    }

    private void SetInstalled(bool installed)
    {
        IsInstalled = installed;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetRunning(bool running)
    {
        IsRunning = running;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool TryGetBinary(out string binaryPath)
    {
        Directory.CreateDirectory(_options.InstallDirectory);
        binaryPath = Path.Combine(_options.InstallDirectory, BinaryFileName);
        return File.Exists(binaryPath);
    }

    private async Task<string> DownloadOllamaModel(string binaryPath, CancellationToken cancellationToken)
    {
        var releaseAssetName = ReleaseAssetName;
        _logger.LogInformation("Ollama binary not found at {Path}; downloading release for {Platform}.", binaryPath, releaseAssetName);

        var downloadUrl = $"{_options.ReleaseBaseUrl}/{releaseAssetName}";
        var archivePath = Path.Combine(_options.InstallDirectory, releaseAssetName);

        await DownloadWithRetryAsync(downloadUrl, archivePath, cancellationToken);
        ExtractArchive(archivePath, _options.InstallDirectory);
        File.Delete(archivePath);

        if (!File.Exists(binaryPath))
        {
            throw new OllamaSetupException($"Extracted Ollama release did not contain the expected binary at '{binaryPath}'.");
        }

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(binaryPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        return binaryPath;
    }

    private async Task DownloadWithRetryAsync(string downloadUrl, string archivePath, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var response = await _httpClient.GetStreamAsync(downloadUrl, cancellationToken);
                await using var fileStream = File.Create(archivePath);
                await response.CopyToAsync(fileStream, cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException && attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Download attempt {Attempt}/{MaxAttempts} of '{Url}' failed; retrying.", attempt, maxAttempts, downloadUrl);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }
    }

    private static void ExtractArchive(string archivePath, string destinationDirectory)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, destinationDirectory, overwriteFiles: true);
            return;
        }

        // System.Formats.Tar's PAX extended-header parser rejects some vendor-specific
        // records that GNU tar (used to build Ollama's releases) legitimately emits,
        // throwing "The extended header contains invalid records." Shell out to the
        // system's own tar instead, which handles these archives correctly.
        var startInfo = new ProcessStartInfo
        {
            FileName = "tar",
            ArgumentList = { "-xzf", archivePath, "-C", destinationDirectory },
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new OllamaSetupException("Failed to start 'tar' to extract the Ollama archive.");

        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new OllamaSetupException($"'tar' exited with code {process.ExitCode} extracting '{archivePath}': {stderr}");
        }
    }

    private void StartProcess(string binaryPath)
    {
        if (_process is { HasExited: false })
        {
            _logger.LogInformation("Ollama process already running (pid {Pid}); reusing it.", _process.Id);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = "serve",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment =
            {
                ["OLLAMA_HOST"] = $"{_options.Host}:{_options.Port}",
                ["OLLAMA_MODELS"] = Path.Combine(_options.InstallDirectory, "models")
            }
        };

        _process = Process.Start(startInfo)
                   ?? throw new OllamaSetupException($"Failed to start the Ollama process from '{binaryPath}'.");

        _logger.LogInformation("Started Ollama server (pid {Pid}) on {Host}:{Port}.", _process.Id, _options.Host, _options.Port);
    }

    private async Task WaitUntilHealthyAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_options.HealthCheckTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        while (true)
        {
            linkedCts.Token.ThrowIfCancellationRequested();

            try
            {
                var response = await _httpClient.GetAsync(BaseUri("/api/tags"), linkedCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or SocketException)
            {
                // Server not accepting connections yet; keep polling until the timeout elapses.
            }

            try
            {
                await Task.Delay(_options.HealthCheckPollInterval, linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new OllamaSetupException(
                    $"Ollama server did not become healthy within {_options.HealthCheckTimeout}.");
            }
        }
    }

    private async Task EnsureModelAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await PullModelAsync(cancellationToken);
                return;
            }
            // Ollama's own pull from its remote registry can drop mid-stream on a flaky
            // connection and surface as a {"error":"EOF"} message; retry rather than fail.
            catch (OllamaSetupException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Pull attempt {Attempt}/{MaxAttempts} of '{Model}' failed; retrying.", attempt, maxAttempts, _options.ModelName);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }
    }

    private async Task PullModelAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ensuring model '{Model}' is pulled.", _options.ModelName);

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUri("/api/pull"))
        {
            Content = JsonContent.Create(new { model = _options.ModelName, stream = true }),
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(line);
            var status = doc.RootElement.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
            _logger.LogDebug("Pull '{Model}': {Status}", _options.ModelName, status);

            if (doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                throw new OllamaSetupException($"Failed to pull model '{_options.ModelName}': {errorProp.GetString()}");
            }
        }
    }

    private Uri BaseUri(string path) => new($"http://{_options.Host}:{_options.Port}{path}");

    private static string BinaryFileName => OperatingSystem.IsWindows() ? "ollama.exe" : "ollama";

    private static string ReleaseAssetName
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return "ollama-windows-amd64.zip";
            }

            if (OperatingSystem.IsMacOS())
            {
                return "ollama-darwin.tgz";
            }

            if (OperatingSystem.IsLinux())
            {
                return RuntimeInformation.OSArchitecture == Architecture.Arm64
                    ? "ollama-linux-arm64.tgz"
                    : "ollama-linux-amd64.tgz";
            }

            throw new OllamaSetupException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _stoppingCts.Dispose();
        _setupLock.Dispose();
    }
}
