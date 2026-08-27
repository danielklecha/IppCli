using System.ComponentModel;
using IppCli.Serialization;
using SharpIpp.Models.Requests;
using Spectre.Console.Cli;

namespace IppCli.Models;

public abstract class BaseSettings<TRequest> : CommandSettings, IIppSettings
    where TRequest : class, IIppRequest, new()
{
    public TRequest Request { get; set; } = new();

    [CommandOption("-r|--request <JSON>")]
    [Description("Full IPP request JSON string or @file.json")]
    public string? RequestJson
    {
        get => Request != null ? IppJsonHelper.Serialize(Request) : null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var parsed = IppJsonHelper.Deserialize<TRequest>(value);
                if (parsed != null)
                {
                    Request = parsed;
                }
            }
        }
    }

    [CommandOption("-o|--output <FORMAT>")]
    [Description("Output format: Tree or Json. Default is Tree")]
    [DefaultValue(OutputFormat.Tree)]
    public OutputFormat Output { get; set; } = OutputFormat.Tree;

    [CommandOption("-k|--insecure|--ignore-ssl-errors")]
    [Description("Ignore SSL/TLS certificate validation errors (useful for self-signed printer certificates)")]
    public bool IgnoreSslErrors { get; set; }

    [CommandOption("-t|--timeout <SECONDS>")]
    [Description("HTTP request timeout in seconds. Default is 30 seconds")]
    [DefaultValue(30)]
    public int TimeoutSeconds { get; set; } = 30;
}
