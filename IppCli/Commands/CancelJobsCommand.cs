using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class CancelJobsCommand : AsyncCommand<CancelJobsCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<CancelJobsRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.CancelJobsAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Cancel-Jobs", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
