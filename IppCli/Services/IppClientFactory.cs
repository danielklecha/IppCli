using System.Net.Http;
using IppCli.Models;
using SharpIpp;
using Spectre.Console;

namespace IppCli.Services;

public class IppClientFactory : IIppClientFactory
{
    public static IppClientFactory Instance { get; } = new();

    public ISharpIppClient CreateClient(IIppSettings settings)
    {
        var primaryHandler = new HttpClientHandler();

        if (settings.IgnoreSslErrors)
        {
            primaryHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var statusHandler = new StatusDelegatingHandler(primaryHandler);

        var httpClient = new HttpClient(statusHandler)
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(5, settings.TimeoutSeconds))
        };

        return new SharpIppClient(httpClient);
    }

    private sealed class StatusDelegatingHandler : DelegatingHandler
    {
        public StatusDelegatingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!AnsiConsole.Profile.Capabilities.Interactive)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var target = request.RequestUri != null ? $" to [cyan]{request.RequestUri.Host}[/]" : string.Empty;

            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Default)
                .StartAsync($"Sending request{target}...", async _ =>
                {
                    return await base.SendAsync(request, cancellationToken);
                });
        }
    }
}
