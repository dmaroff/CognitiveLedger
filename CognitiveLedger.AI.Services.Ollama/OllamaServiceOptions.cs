namespace CognitiveLedger.AI.Services.Ollama;

public sealed class OllamaServiceOptions
{
    /// <summary>
    /// Directory where the private Ollama binary, models, and related files are installed.
    /// Defaults to a CognitiveLedger folder under the current user's local application data.
    /// </summary>
    public string InstallDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CognitiveLedger", "ollama");

    /// <summary>
    /// Loopback port the private Ollama server should listen on.
    /// </summary>
    public int Port { get; init; } = 11555;

    /// <summary>
    /// Name of the model to ensure is pulled and available (e.g. "llama3.1").
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Base URL used to download Ollama release archives when no local binary is found.
    /// </summary>
    public string ReleaseBaseUrl { get; init; } = "https://github.com/ollama/ollama/releases/latest/download";

    public TimeSpan HealthCheckTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan HealthCheckPollInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    public string Host => "127.0.0.1";
}
