using Microsoft.Extensions.Configuration;

namespace CognitiveLedger.Common;

public sealed class AppConfiguration : IAppConfiguration
{
    private const string AiApiKeyEnvVar = "AI:COGNITIVE_LEDGER_API_KEY";
    private const string AiRequestTimeoutSecondsEnvVar = "AI:RequestTimeoutSeconds";

    public AppConfiguration(IConfiguration configuration)
    {
        Set_AiProvider(configuration);
        Set_AiApiKey(configuration);
        Set_AiRequestTimeoutSeconds(configuration);
        Set_AiModel(configuration);
        Set_ConnectionString(configuration);
        Set_AiEndpoint(configuration);
    }

    private void Set_AiProvider(IConfiguration configuration)
    {
        AiProvider = configuration["AI:Provider"] ?? "OpenAI";
    }
    
    private void Set_AiApiKey(IConfiguration configuration)
    {
        AiApiKey = configuration["AI:COGNITIVE_LEDGER_API_KEY"]
                       ?? string.Empty;
    }

    private void Set_AiRequestTimeoutSeconds(IConfiguration configuration)
    {
        var timeoutSetting = configuration["AI:RequestTimeoutSeconds"];
        if (timeoutSetting is null)
            return;
        
        if (!int.TryParse(timeoutSetting, out var timeoutSeconds) ||
            timeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "AI:RequestTimeoutSeconds must be a positive integer.");
        }

        AiRequestTimeoutSeconds = timeoutSeconds;
    }
    
    private void Set_AiModel(IConfiguration configuration)
    {
        var modelSetting = configuration["AI:Model"];
        if (string.IsNullOrWhiteSpace(modelSetting))
        {
            throw new InvalidOperationException(
                "AI:Model must be specified in the configuration.");
        }
        AiModel = modelSetting;
    }
    
    private void Set_ConnectionString(IConfiguration configuration)
    {
        ConnectionString = configuration.GetConnectionString("CognitiveLedger") 
                           ?? throw new InvalidOperationException(
                               "Database connection string 'CognitiveLedger' is not configured.");
    }
    
    private void Set_AiEndpoint(IConfiguration configuration)
    {
        AiEndpoint = configuration["AI:Endpoint"] ?? string.Empty;
    }
    
    

    public string AiProvider { get; set; }
    public string AiApiKey { get; private set; }
    public int AiRequestTimeoutSeconds { get; private set; } = 300;
    public string AiModel { get; private set; }
    public string AiEndpoint { get; private set; }
    
    public string ConnectionString { get; private set; }
}
