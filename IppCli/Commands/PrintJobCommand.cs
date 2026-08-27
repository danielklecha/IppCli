using IppCli.Attributes;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Commands;

public partial class PrintJobCommand : AsyncCommand<PrintJobCommand.Settings>
{
    [GenerateCliSettings(nameof(Settings.Request))]
    public partial class Settings : BaseSettings<PrintJobRequest>
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Document) || !File.Exists(settings.Document))
        {
            throw new FileNotFoundException($"The specified document file '{settings.Document}' does not exist.");
        }

        using var client = IppClientFactory.Instance.CreateClient(settings);
        await using var fileStream = File.OpenRead(settings.Document);
        settings.Request.Document = fileStream;

        if (string.IsNullOrEmpty(settings.Request.OperationAttributes?.JobName))
        {
            settings.Request.OperationAttributes ??= new();
            settings.Request.OperationAttributes.JobName = Path.GetFileName(settings.Document);
        }

        if (string.IsNullOrEmpty(settings.Request.OperationAttributes?.DocumentName))
        {
            settings.Request.OperationAttributes ??= new();
            settings.Request.OperationAttributes.DocumentName = Path.GetFileName(settings.Document);
        }

        var response = await client.PrintJobAsync(settings.Request, cancellationToken);

        OutputFormatter.FormatResponse("Print-Job", response, settings);

        return (short)response.StatusCode <= 0x00FF ? 0 : 1;
    }
}
