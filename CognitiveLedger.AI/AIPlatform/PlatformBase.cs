namespace CognitiveLedger.AI.AIPlatform;
public abstract class PlatformBase
{
    protected readonly HttpClient HttpClient;

    protected PlatformBase(string host, int port)
    {
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{host}:{port}")
        };
    }
}