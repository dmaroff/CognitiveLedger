using CognitiveLedger.Agents;
using CognitiveLedger.Services.Agents.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace CognitiveLedger.Services.Agents.Api.Mcp;

public sealed class LedgerMcpToolProvider(
    IOptions<LedgerMcpOptions> options,
    ILoggerFactory loggerFactory) : IAgentToolProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private McpClient? _client;
    private IReadOnlyList<AITool>? _tools;

    public async ValueTask<IReadOnlyList<AITool>> GetToolsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_tools is not null)
        {
            return _tools;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_tools is not null)
            {
                return _tools;
            }

            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = options.Value.Endpoint,
                    Name = "CognitiveLedger Ledger MCP"
                },
                loggerFactory);

            _client = await McpClient.CreateAsync(
                transport,
                loggerFactory: loggerFactory,
                cancellationToken: cancellationToken);

            var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);
            _tools = [.. tools.Cast<AITool>()];
            return _tools;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        _initializationLock.Dispose();
    }
}
