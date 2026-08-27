namespace IppCli.Models;

public interface IIppSettings
{
    OutputFormat Output { get; set; }
    bool IgnoreSslErrors { get; set; }
    int TimeoutSeconds { get; set; }
}

