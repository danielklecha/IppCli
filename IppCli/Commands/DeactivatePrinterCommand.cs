using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class DeactivatePrinterCommand : AsyncCommand<DeactivatePrinterCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<DeactivatePrinterRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.DeactivatePrinterAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Deactivate-Printer", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
