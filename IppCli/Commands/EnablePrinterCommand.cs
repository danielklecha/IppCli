using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class EnablePrinterCommand : AsyncCommand<EnablePrinterCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<EnablePrinterRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var client = IppClientFactory.Instance.CreateClient(settings);

        var response = await client.EnablePrinterAsync(settings.Request, cancellationToken);
        OutputFormatter.FormatResponse("Enable-Printer", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
