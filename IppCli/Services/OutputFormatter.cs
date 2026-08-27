using IppCli.Models;
using SharpIpp.Protocol;

namespace IppCli.Services;

public static class OutputFormatter
{
    public static IOutputRenderer GetRenderer(OutputFormat format) => format switch
    {
        OutputFormat.Json => new JsonOutputRenderer(),
        OutputFormat.Tree => new ConsoleTreeRenderer(),
        _ => new ConsoleTreeRenderer()
    };

    public static void FormatResponse(string operationName, IIppResponse response, IIppSettings settings)
    {
        FormatResponse(operationName, response, settings.Output);
    }

    public static void FormatResponse(string operationName, IIppResponse response, OutputFormat format)
    {
        var renderer = GetRenderer(format);
        renderer.RenderResponse(operationName, response);
    }
}

