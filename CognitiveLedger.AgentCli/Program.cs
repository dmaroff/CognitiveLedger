using System.Net.Http.Json;
using System.Text.Json;

namespace CognitiveLedger.AgentCli;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task Main(string[] args)
    {
        var settings = LoadSettings();
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(settings.AgentsApi.Url.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(settings.AgentsApi.TimeoutSeconds)
        };

        Console.WriteLine("CognitiveLedger Agent CLI");
        Console.WriteLine($"Agents API: {httpClient.BaseAddress}");
        Console.WriteLine("Enter a financial question, or /exit to quit.");

        if (args.Length > 0)
        {
            await AskAsync(httpClient, string.Join(' ', args));
            return;
        }

        while (true)
        {
            Console.Write("\nYou> ");
            var question = Console.ReadLine()?.Trim();

            if (string.Equals(question, "/exit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(question, "/quit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                continue;
            }

            await AskAsync(httpClient, question);
        }
    }

    private static async Task AskAsync(HttpClient httpClient, string question)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/agent/messages",
                new AgentMessageRequest(question),
                JsonOptions);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"\nAgents API returned HTTP {(int)response.StatusCode} " +
                    $"{response.ReasonPhrase}.");
                Console.WriteLine(FormatJson(responseBody));
                return;
            }

            var agentResponse = JsonSerializer.Deserialize<AgentResponse>(
                responseBody,
                JsonOptions);

            if (agentResponse is null)
            {
                Console.WriteLine("\nThe Agents API returned an empty or invalid response.");
                return;
            }

            Console.WriteLine($"\nAgent> {agentResponse.Answer}");
            DisplayToolExecutions(agentResponse.ToolExecutions);
        }
        catch (HttpRequestException exception)
        {
            Console.WriteLine($"\nUnable to reach Agents API: {exception.Message}");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("\nThe agent request timed out.");
        }
        catch (JsonException exception)
        {
            Console.WriteLine($"\nUnable to read the Agents API response: {exception.Message}");
        }
    }

    private static void DisplayToolExecutions(
        IReadOnlyList<AgentToolExecution> toolExecutions)
    {
        if (toolExecutions.Count == 0)
        {
            Console.WriteLine("Tools> none");
            return;
        }

        Console.WriteLine("Tools>");
        foreach (var execution in toolExecutions)
        {
            var status = execution.Succeeded switch
            {
                true => "succeeded",
                false => "failed",
                null => "unknown"
            };

            Console.WriteLine(
                $"  - {execution.Name}: {status}; iteration {execution.Iteration}; " +
                $"{execution.Duration.TotalMilliseconds:N0} ms");

            if (execution.Arguments is { Count: > 0 })
            {
                var arguments = string.Join(
                    ", ",
                    execution.Arguments
                        .OrderBy(argument => argument.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(argument => $"{argument.Key}={argument.Value ?? "null"}"));
                Console.WriteLine($"    arguments: {arguments}");
            }

            if (!string.IsNullOrWhiteSpace(execution.ErrorMessage))
            {
                Console.WriteLine($"    {execution.ErrorMessage}");
            }
        }
    }

    private static AppSettings LoadSettings()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var settings = JsonSerializer.Deserialize<AppSettings>(
            File.ReadAllText(settingsPath),
            JsonOptions);

        var developmentSettingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "appsettings.Development.json");

        if (settings is not null && File.Exists(developmentSettingsPath))
        {
            var developmentSettings = JsonSerializer.Deserialize<AppSettingsOverride>(
                File.ReadAllText(developmentSettingsPath),
                JsonOptions);

            if (developmentSettings?.AgentsApi is not null)
            {
                settings = new AppSettings(developmentSettings.AgentsApi);
            }
        }

        if (string.IsNullOrWhiteSpace(settings?.AgentsApi?.Url))
        {
            throw new InvalidOperationException(
                "A non-empty AgentsApi:Url value is required in appsettings.json.");
        }

        if (settings.AgentsApi.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "AgentsApi:TimeoutSeconds must be a positive integer in appsettings.json.");
        }

        return settings;
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

    private sealed record AgentMessageRequest(string Message, string? ConversationId = null);

    private sealed record AgentResponse(
        string Answer,
        string? ConversationId,
        IReadOnlyList<AgentToolExecution> ToolExecutions);

    private sealed record AgentToolExecution(
        string Name,
        IReadOnlyDictionary<string, string?>? Arguments,
        string? CallId,
        bool? Succeeded,
        string? ErrorMessage,
        int Iteration,
        DateTimeOffset StartedAtUtc,
        TimeSpan Duration);

    private sealed record AppSettings(AgentsApiSettings AgentsApi);

    private sealed record AppSettingsOverride(AgentsApiSettings? AgentsApi);

    private sealed record AgentsApiSettings(string Url, int TimeoutSeconds);
}
