using System.Net.Http;
using IppCli.Models;
using SharpIpp;

namespace IppCli.Services;

public class IppClientFactory : IIppClientFactory
{
    public static IppClientFactory Instance { get; } = new();

    public ISharpIppClient CreateClient(IIppSettings settings)
    {
        var handler = new HttpClientHandler();

        if (settings.IgnoreSslErrors)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(5, settings.TimeoutSeconds))
        };

        return new SharpIppClient(httpClient);
    }
}
