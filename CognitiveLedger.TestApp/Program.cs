using System.Net.Http.Json;
using System.Text.Json;

namespace CognitiveLedger.TestApp;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task Main(string[] args)
    {
        var settings = LoadSettings();
        var pdfFolder = ResolvePdfFolder(args.FirstOrDefault(), settings.PdfFolder);
        var importerUrl = settings.Importer.Url;

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(importerUrl.TrimEnd('/') + "/");
        httpClient.Timeout = TimeSpan.FromSeconds(settings.Importer.TimeoutSeconds);

        Console.WriteLine("CognitiveLedger PDF Importer");
        Console.WriteLine($"PDF folder: {pdfFolder}");
        Console.WriteLine($"Importer:   {httpClient.BaseAddress}");

        while (true)
        {
            var pdfFiles = GetPdfFiles(pdfFolder);
            DisplayMenu(pdfFiles);

            Console.Write("Select a file to import (or Q to quit): ");
            var selection = Console.ReadLine()?.Trim();

            if (string.Equals(selection, "q", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!int.TryParse(selection, out var selectedNumber) ||
                selectedNumber < 1 ||
                selectedNumber > pdfFiles.Length)
            {
                Console.WriteLine("Invalid selection. Please enter one of the listed numbers or Q.\n");
                continue;
            }

            await ImportPdfAsync(
                httpClient,
                pdfFiles[selectedNumber - 1],
                settings.PiiToRedact);
            Console.WriteLine();
        }
    }

    private static string ResolvePdfFolder(string? commandLineFolder, string configuredFolder)
    {
        if (!string.IsNullOrWhiteSpace(commandLineFolder))
        {
            return Path.GetFullPath(commandLineFolder);
        }

        if (Path.IsPathRooted(configuredFolder))
        {
            return Path.GetFullPath(configuredFolder);
        }

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), configuredFolder),
            Path.Combine(Directory.GetCurrentDirectory(), "..", configuredFolder),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", configuredFolder)
        };

        return Path.GetFullPath(candidates.FirstOrDefault(Directory.Exists) ?? candidates[0]);
    }

    private static AppSettings LoadSettings()
    {
        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var appSettings = JsonSerializer.Deserialize<AppSettings>(
            File.ReadAllText(appSettingsPath),
            JsonOptions);

        var developmentSettingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "appsettings.Development.json");

        if (appSettings is not null && File.Exists(developmentSettingsPath))
        {
            var developmentSettings = JsonSerializer.Deserialize<AppSettingsOverride>(
                File.ReadAllText(developmentSettingsPath),
                JsonOptions);

            if (developmentSettings is not null)
            {
                appSettings = new AppSettings(
                    Importer: developmentSettings.Importer ?? appSettings.Importer,
                    PdfFolder: developmentSettings.PdfFolder ?? appSettings.PdfFolder,
                    PiiToRedact: developmentSettings.PiiToRedact ?? appSettings.PiiToRedact);
            }
        }

        if (string.IsNullOrWhiteSpace(appSettings?.Importer?.Url))
        {
            throw new InvalidOperationException(
                "A non-empty Importer:Url value is required in appsettings.json.");
        }

        if (appSettings.Importer.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Importer:TimeoutSeconds must be a positive integer in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(appSettings.PdfFolder))
        {
            throw new InvalidOperationException(
                "A non-empty PdfFolder value is required in appsettings.json.");
        }

        if (appSettings.PiiToRedact is null || !appSettings.PiiToRedact.All(string.IsNullOrEmpty))
        {
            throw new InvalidOperationException(
                "At least one PiiToRedact value is required in appsettings.json.");
        }

        return appSettings;
    }

    private static string[] GetPdfFiles(string pdfFolder)
    {
        if (!Directory.Exists(pdfFolder))
        {
            return [];
        }

        return [.. Directory
            .EnumerateFiles(pdfFolder, "*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static void DisplayMenu(IReadOnlyList<string> pdfFiles)
    {
        Console.WriteLine();

        if (pdfFiles.Count == 0)
        {
            Console.WriteLine("No PDF files were found.");
        }
        else
        {
            for (var index = 0; index < pdfFiles.Count; index++)
            {
                Console.WriteLine($"{index + 1}. {Path.GetFileName(pdfFiles[index])}");
            }
        }

        Console.WriteLine("Q. Quit");
    }

    private static async Task ImportPdfAsync(
        HttpClient httpClient,
        string pdfPath,
        string[] piiToRedact)
    {
        try
        {
            Console.WriteLine($"\nImporting {Path.GetFileName(pdfPath)}...");

            var request = new ImportPdfRequest(
                UserId: 1,
                Base64PdfData: Convert.ToBase64String(await File.ReadAllBytesAsync(pdfPath)),
                FileName: Path.GetFileName(pdfPath),
                BankName: "Unknown",
                StatementType: "CreditCard",
                PiiToRedact: piiToRedact);

            using var response = await httpClient.PostAsJsonAsync("api/parse/pdf", request, JsonOptions);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            Console.WriteLine(FormatJson(responseBody));
        }
        catch (HttpRequestException exception)
        {
            Console.WriteLine($"Unable to reach the importer service: {exception.Message}");
        }
        catch (TaskCanceledException exception)
        {
            Console.WriteLine($"The import request timed out: {exception.Message}");
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Unable to read the PDF: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Unable to read the PDF: {exception.Message}");
        }
    }

    private static string FormatJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty response)";
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            return JsonSerializer.Serialize(document.RootElement, JsonOptions);
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private sealed record ImportPdfRequest(
        int UserId,
        string Base64PdfData,
        string FileName,
        string BankName,
        string StatementType,
        string[] PiiToRedact);

    private sealed record AppSettings(
        ImporterSettings Importer,
        string PdfFolder,
        string[] PiiToRedact);

    private sealed record AppSettingsOverride(
        ImporterSettings? Importer,
        string? PdfFolder,
        string[]? PiiToRedact);

    private sealed record ImporterSettings(string Url, int TimeoutSeconds);
}
