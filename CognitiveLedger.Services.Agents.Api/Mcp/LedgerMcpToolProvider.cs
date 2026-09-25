using CognitiveLedger.Agents;
using CognitiveLedger.Common;
using CognitiveLedger.Services.Agents.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace CognitiveLedger.Services.Agents.Api.Mcp;

public sealed class LedgerMcpToolProvider : IAgentToolProvider, IAsyncDisposable
{
    private readonly IOptions<LedgerMcpOptions> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AppLog<LedgerMcpToolProvider> _logger;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private McpClient? _client;
    private IReadOnlyList<AITool>? _tools;

    public LedgerMcpToolProvider(
        IOptions<LedgerMcpOptions> options,
        ILoggerFactory loggerFactory,
        AppLog<LedgerMcpToolProvider> logger)
    {
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async ValueTask<IReadOnlyList<AITool>> GetToolsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogMethodStart();
        if (_tools is not null)
        {
            return _tools;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_tools is not null)
            {
                _logger.LogInfo("Tools already initialized, returning cached tools.");
                return _tools;
            }

            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = _options.Value.Endpoint,
                    Name = "CognitiveLedger Ledger MCP"
                },
                _loggerFactory);

            _client = await McpClient.CreateAsync(
                transport,
                loggerFactory: _loggerFactory,
                cancellationToken: cancellationToken);

            _logger.LogInfo("Fetching tools from MCP server ...");
            var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);
            _logger.LogInfo("Tools list retrieved successfully.");
            _tools = [.. tools];
            return _tools;
        }
        finally
        {
            _initializationLock.Release();
            _logger.LogMethodEnd();
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