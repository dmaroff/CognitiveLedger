namespace CognitiveLedger.Common;

public interface IAppConfiguration
{
    string AiProvider { get; }
    string AiApiKey { get; }
    int AiRequestTimeoutSeconds { get; }
    string AiModel { get; }
    string ConnectionString { get; }
    string AiEndpoint { get; }
}