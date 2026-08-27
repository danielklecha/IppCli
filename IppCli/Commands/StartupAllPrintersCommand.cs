using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class StartupAllPrintersCommand : AsyncCommand<StartupAllPrintersCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<StartupAllPrintersRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.StartupAllPrintersAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Startup-All-Printers", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
