using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class ResumeJobCommand : AsyncCommand<ResumeJobCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<ResumeJobRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.ResumeJobAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Resume-Job", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
