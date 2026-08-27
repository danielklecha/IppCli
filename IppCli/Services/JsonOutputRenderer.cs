using System;
using System.IO;
using IppCli.Serialization;
using SharpIpp.Protocol;
using Spectre.Console;

namespace IppCli.Services;

public class JsonOutputRenderer : IOutputRenderer
{
    private static readonly JsonOutputRenderer DefaultInstance = new();

    public static string ToJsonString(object data)
    {
        return IppJsonHelper.SerializeOutput(data);
    }

    public static void RenderJson(object data)
    {
        var json = ToJsonString(data);
        AnsiConsole.WriteLine(json);
    }

    public static void Render(string operationName, IIppResponse response, TextWriter? writer = null)
    {
        DefaultInstance.RenderResponse(operationName, response, writer);
    }

    public void RenderResponse(string operationName, IIppResponse response, TextWriter? writer = null)
    {
        var json = ToJsonString(response);
        if (writer != null && writer != Console.Out)
        {
            writer.WriteLine(json);
        }
        else
        {
            AnsiConsole.WriteLine(json);
        }
    }
}
