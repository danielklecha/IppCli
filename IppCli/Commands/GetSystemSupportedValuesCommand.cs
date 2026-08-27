using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class GetSystemSupportedValuesCommand : AsyncCommand<GetSystemSupportedValuesCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<GetSystemSupportedValuesRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.GetSystemSupportedValuesAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Get-System-Supported-Values", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
