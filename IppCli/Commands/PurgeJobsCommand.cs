using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class PurgeJobsCommand : AsyncCommand<PurgeJobsCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<PurgeJobsRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.PurgeJobsAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Purge-Jobs", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
