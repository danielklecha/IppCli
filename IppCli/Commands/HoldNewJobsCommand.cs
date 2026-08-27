using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class HoldNewJobsCommand : AsyncCommand<HoldNewJobsCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<HoldNewJobsRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.HoldNewJobsAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Hold-New-Jobs", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
