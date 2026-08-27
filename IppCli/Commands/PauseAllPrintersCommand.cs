using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class PauseAllPrintersCommand : AsyncCommand<PauseAllPrintersCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<PauseAllPrintersRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.PauseAllPrintersAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Pause-All-Printers", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
