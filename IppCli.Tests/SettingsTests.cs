using System.Reflection;
using IppCli.Attributes;
using SharpIpp.Protocol.Models;
using Xunit;

namespace IppCli.Tests;

public class SettingsTests
{
    [Fact]
    public void BaseSettings_DefaultsArePopulated()
    {
        var settings = new IppCli.Commands.GetPrinterAttributesCommand.Settings();

        Assert.Equal("1.1", settings.Version);
        Assert.Equal(1, settings.RequestId);
        Assert.Equal(OutputFormat.Tree, settings.Output);
        Assert.Equal(30, settings.TimeoutSeconds);
        Assert.False(settings.IgnoreSslErrors);
        Assert.NotNull(settings.Request.OperationAttributes);
        Assert.Equal(Environment.UserName, settings.Request.OperationAttributes.RequestingUserName);
    }

    [Fact]
    public void GeneratedSettings_MutatesRequestDirectly()
    {
        var settings = new IppCli.Commands.GetResourceAttributesCommand.Settings
        {
            Version = "2.0",
            RequestId = 99,
            OpPrinterUri = "ipp://192.168.1.50/ipp/system",
            OpResourceId = 1234,
            OpRequestedAttributes = "resource-name,resource-state"
        };

        Assert.Equal(new IppVersion(2, 0), settings.Request.Version);
        Assert.Equal(99, settings.Request.RequestId);
        Assert.NotNull(settings.Request.OperationAttributes);
        Assert.Equal(new Uri("ipp://192.168.1.50/ipp/system"), settings.Request.OperationAttributes.PrinterUri);
        Assert.Equal(1234, settings.Request.OperationAttributes.ResourceId);
        Assert.Equal(new[] { "resource-name", "resource-state" }, settings.Request.OperationAttributes.RequestedAttributes);
    }

    [Fact]
    public void GeneratedSettings_PrintJob_MutatesNestedAttributes()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            Version = "1.1",
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            OpJobName = "My Document",
            JtaCopies = 5,
            JtaSides = Sides.TwoSidedLongEdge,
            JtaPrintColorMode = PrintColorMode.Color
        };

        Assert.Equal(new Uri("ipp://192.168.1.100/ipp/print"), settings.Request.OperationAttributes?.PrinterUri);
        Assert.Equal("My Document", settings.Request.OperationAttributes?.JobName);
        Assert.Equal(5, settings.Request.JobTemplateAttributes?.Copies);
        Assert.Equal(Sides.TwoSidedLongEdge, settings.Request.JobTemplateAttributes?.Sides);
        Assert.Equal(PrintColorMode.Color, settings.Request.JobTemplateAttributes?.PrintColorMode);
    }

    [Fact]
    public void GeneratedSettings_SetPrinterAttributes_MutatesDescriptionAttributes()
    {
        var settings = new IppCli.Commands.SetPrinterAttributesCommand.Settings
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            PaPrinterInfo = "Office Color Laser",
            PaPrinterLocation = "Room 302"
        };

        Assert.Equal(new Uri("ipp://192.168.1.100/ipp/print"), settings.Request.OperationAttributes?.PrinterUri);
        Assert.Equal("Office Color Laser", settings.Request.PrinterAttributes?.PrinterInfo);
        Assert.Equal("Room 302", settings.Request.PrinterAttributes?.PrinterLocation);
    }

    [Fact]
    public void GeneratedSettings_GetJobs_MutatesWhichJobsAndMyJobs()
    {
        var settings = new IppCli.Commands.GetJobsCommand.Settings
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            OpWhichJobs = WhichJobs.Completed,
            OpMyJobs = true,
            OpLimit = 10
        };

        Assert.Equal(WhichJobs.Completed, settings.Request.OperationAttributes?.WhichJobs);
        Assert.True(settings.Request.OperationAttributes?.MyJobs);
        Assert.Equal(10, settings.Request.OperationAttributes?.Limit);
    }

    [Fact]
    public void GeneratedSettings_OpJson_ParsesInlineJson()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            OpJson = """{"printerUri": "ipp://10.0.0.5/ipp/print", "jobName": "Invoice #42", "documentFormat": "application/pdf"}"""
        };

        Assert.Equal(new Uri("ipp://10.0.0.5/ipp/print"), settings.Request.OperationAttributes?.PrinterUri);
        Assert.Equal("Invoice #42", settings.Request.OperationAttributes?.JobName);
        Assert.Equal(new DocumentFormat("application/pdf"), settings.Request.OperationAttributes?.DocumentFormat);
    }

    [Fact]
    public void GeneratedSettings_JtaJson_ParsesComplexNestedAttributes()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            JtaJson = """{"copies": 3, "sides": "TwoSidedLongEdge", "printColorMode": "Color"}"""
        };

        Assert.Equal(3, settings.Request.JobTemplateAttributes?.Copies);
        Assert.Equal(Sides.TwoSidedLongEdge, settings.Request.JobTemplateAttributes?.Sides);
        Assert.Equal(PrintColorMode.Color, settings.Request.JobTemplateAttributes?.PrintColorMode);
    }

    [Fact]
    public void GeneratedSettings_PaJson_ParsesPrinterAttributes()
    {
        var settings = new IppCli.Commands.SetPrinterAttributesCommand.Settings
        {
            PaJson = """{"printerInfo": "Main Office", "printerLocation": "Floor 2"}"""
        };

        Assert.Equal("Main Office", settings.Request.PrinterAttributes?.PrinterInfo);
        Assert.Equal("Floor 2", settings.Request.PrinterAttributes?.PrinterLocation);
    }

    [Fact]
    public void GeneratedSettings_OpJson_FileReference_LoadsFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """{"printerUri": "ipp://printer.local/ipp/print", "jobName": "Test from File"}""");

            var settings = new IppCli.Commands.PrintJobCommand.Settings
            {
                OpJson = $"@{tempFile}"
            };

            Assert.Equal(new Uri("ipp://printer.local/ipp/print"), settings.Request.OperationAttributes?.PrinterUri);
            Assert.Equal("Test from File", settings.Request.OperationAttributes?.JobName);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GeneratedSettings_JsonAndIndividualProperty_OverridesProperty()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            OpJson = """{"printerUri": "ipp://10.0.0.1/ipp/print", "jobName": "Original Name"}""",
            OpJobName = "Overridden Name"
        };

        Assert.Equal(new Uri("ipp://10.0.0.1/ipp/print"), settings.Request.OperationAttributes?.PrinterUri);
        Assert.Equal("Overridden Name", settings.Request.OperationAttributes?.JobName);
    }

    [Fact]
    public void GeneratedSettings_JtaJson_SupportsKebabCaseEnumAndStructValues()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            JtaJson = """{"copies": 2, "sides": "two-sided-long-edge", "print-color-mode": "color", "orientationRequested": "landscape"}"""
        };

        Assert.Equal(2, settings.Request.JobTemplateAttributes?.Copies);
        Assert.Equal(Sides.TwoSidedLongEdge, settings.Request.JobTemplateAttributes?.Sides);
        Assert.Equal(PrintColorMode.Color, settings.Request.JobTemplateAttributes?.PrintColorMode);
        Assert.Equal(Orientation.Landscape, settings.Request.JobTemplateAttributes?.OrientationRequested);
    }

    [Fact]
    public void GeneratedSettings_OpJson_SupportsArrayAttributes()
    {
        var settings = new IppCli.Commands.GetResourceAttributesCommand.Settings
        {
            OpJson = """{"printerUri": "ipp://192.168.1.50/ipp/system", "resourceId": 1234, "requestedAttributes": ["resource-name", "resource-state"]}"""
        };

        Assert.Equal(new Uri("ipp://192.168.1.50/ipp/system"), settings.Request.OperationAttributes?.PrinterUri);
        Assert.Equal(1234, settings.Request.OperationAttributes?.ResourceId);
        Assert.Equal(new[] { "resource-name", "resource-state" }, settings.Request.OperationAttributes?.RequestedAttributes);
    }

    [Fact]
    public void BaseSettings_RequestJson_ParsesInlineJson()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            RequestJson = """
            {
                "version": "2.0",
                "requestId": 77,
                "operationAttributes": {
                    "printerUri": "ipp://192.168.1.150/ipp/print",
                    "jobName": "Full Request JSON Test"
                },
                "jobTemplateAttributes": {
                    "copies": 4,
                    "sides": "TwoSidedLongEdge"
                }
            }
            """
        };

        Assert.Equal(new IppVersion(2, 0), settings.Request.Version);
        Assert.Equal(77, settings.Request.RequestId);
        Assert.Equal(new Uri("ipp://192.168.1.150/ipp/print"), settings.Request.OperationAttributes?.PrinterUri);
        Assert.Equal("Full Request JSON Test", settings.Request.OperationAttributes?.JobName);
        Assert.Equal(4, settings.Request.JobTemplateAttributes?.Copies);
        Assert.Equal(Sides.TwoSidedLongEdge, settings.Request.JobTemplateAttributes?.Sides);
    }

    [Fact]
    public void BaseSettings_RequestJson_LoadsFromFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
            {
                "version": "2.1",
                "requestId": 88,
                "operationAttributes": {
                    "printerUri": "ipp://printer.local/ipp/print",
                    "jobName": "Test from Request File"
                }
            }
            """);

            var settings = new IppCli.Commands.PrintJobCommand.Settings
            {
                RequestJson = $"@{tempFile}"
            };

            Assert.Equal(new IppVersion(2, 1), settings.Request.Version);
            Assert.Equal(88, settings.Request.RequestId);
            Assert.Equal(new Uri("ipp://printer.local/ipp/print"), settings.Request.OperationAttributes?.PrinterUri);
            Assert.Equal("Test from Request File", settings.Request.OperationAttributes?.JobName);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void BaseSettings_RequestJson_SerializesCurrentRequest()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            Version = "2.0",
            RequestId = 123,
            OpPrinterUri = "ipp://10.0.0.1/ipp/print",
            OpJobName = "Serialized Job"
        };

        var json = settings.RequestJson;
        Assert.NotNull(json);
        Assert.Contains("ipp://10.0.0.1/ipp/print", json);
        Assert.Contains("Serialized Job", json);
    }

    [Fact]
    public void GeneratedSettings_DtaMediaCol_ParsesJson()
    {
        var settings = new IppCli.Commands.SendDocumentCommand.Settings
        {
            DtaMediaCol = """
            {
                "media-color": "white",
                "media-size": {
                    "x-dimension": 21000,
                    "y-dimension": 29700
                }
            }
            """
        };

        Assert.NotNull(settings.Request.DocumentTemplateAttributes?.MediaCol);
        Assert.Equal(MediaColor.White, settings.Request.DocumentTemplateAttributes?.MediaCol?.MediaColor);
        Assert.Equal(21000, settings.Request.DocumentTemplateAttributes?.MediaCol?.MediaSize?.XDimension);
        Assert.Equal(29700, settings.Request.DocumentTemplateAttributes?.MediaCol?.MediaSize?.YDimension);
    }

    [Fact]
    public void GeneratedSettings_JtaMediaCol_ParsesJsonAndSerializes()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            JtaMediaCol = """
            {
                "mediaColor": "blue",
                "mediaSize": {
                    "xDimension": 21590,
                    "yDimension": 27940
                }
            }
            """
        };

        Assert.NotNull(settings.Request.JobTemplateAttributes?.MediaCol);
        Assert.Equal(MediaColor.Blue, settings.Request.JobTemplateAttributes?.MediaCol?.MediaColor);
        Assert.Equal(21590, settings.Request.JobTemplateAttributes?.MediaCol?.MediaSize?.XDimension);
        Assert.Equal(27940, settings.Request.JobTemplateAttributes?.MediaCol?.MediaSize?.YDimension);

        var serialized = settings.JtaMediaCol;
        Assert.NotNull(serialized);
        Assert.Contains("21590", serialized);
    }

    [Fact]
    public void GeneratedSettings_MediaCol_CommandOptionAttributesExist()
    {
        var docProp = typeof(IppCli.Commands.SendDocumentCommand.Settings).GetProperty("DtaMediaCol");
        Assert.NotNull(docProp);
        var docAttr = docProp.GetCustomAttribute<Spectre.Console.Cli.CommandOptionAttribute>();
        Assert.NotNull(docAttr);
        Assert.Contains("dta-media-col", docAttr.LongNames);
        Assert.DoesNotContain("dta-mediacol", docAttr.LongNames);

        var jbProp = typeof(IppCli.Commands.PrintJobCommand.Settings).GetProperty("JtaMediaCol");
        Assert.NotNull(jbProp);
        var jbAttr = jbProp.GetCustomAttribute<Spectre.Console.Cli.CommandOptionAttribute>();
        Assert.NotNull(jbAttr);
        Assert.Contains("jta-media-col", jbAttr.LongNames);
        Assert.DoesNotContain("jta-mediacol", jbAttr.LongNames);
    }

    [Fact]
    public void GeneratedSettings_Level2ComplexTypes_NotExpandedToFlatOptions()
    {
        var settingsType = typeof(IppCli.Commands.PrintJobCommand.Settings);
        
        // Level 2 complex JSON option exists
        var mediaColProp = settingsType.GetProperty("JtaMediaCol");
        Assert.NotNull(mediaColProp);
        var mediaColAttr = mediaColProp.GetCustomAttribute<Spectre.Console.Cli.CommandOptionAttribute>();
        Assert.NotNull(mediaColAttr);
        Assert.Contains("jta-media-col", mediaColAttr.LongNames);

        // Level 3+ properties are NOT expanded into separate flat properties
        Assert.Null(settingsType.GetProperty("JtaMediaColMediaColor"));
        Assert.Null(settingsType.GetProperty("JtaMediaColMediaSizeXDimension"));
        Assert.Null(settingsType.GetProperty("JtaMediaColMediaType"));
    }

    [Fact]
    public void GeneratedSettings_SetJobAttributes_DtaJson_ParsesJobTemplateAttributes()
    {
        var settings = new IppCli.Commands.SetJobAttributesCommand.Settings
        {
            JtaJson = """{"copies": 10, "sides": "TwoSidedLongEdge"}"""
        };

        Assert.Equal(10, settings.Request.JobTemplateAttributes?.Copies);
        Assert.Equal(Sides.TwoSidedLongEdge, settings.Request.JobTemplateAttributes?.Sides);
    }

    [Fact]
    public void GeneratedSettings_SetDocumentAttributes_DtaJson_ParsesDocumentTemplateAttributes()
    {
        var settings = new IppCli.Commands.SetDocumentAttributesCommand.Settings
        {
            DtaJson = """{"copies": 7}"""
        };

        Assert.Equal(7, settings.Request.DocumentTemplateAttributes?.Copies);
    }
    [Fact]
    public void GeneratedSettings_CustomSettingsWithDepth_CompilesAndWorks()
    {
        var settings = new PositionalDepthSettingsTestClass
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            OpJobName = "Positional Depth Test"
        };

        Assert.Equal(new Uri("ipp://192.168.1.100/ipp/print"), settings.CustomRequest.OperationAttributes?.PrinterUri);
        Assert.Equal("Positional Depth Test", settings.CustomRequest.OperationAttributes?.JobName);
    }

    [Fact]
    public void GeneratedSettings_NamedMaxNestingDepth_CompilesAndWorks()
    {
        var settings = new NamedDepthSettingsTestClass
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            OpJobName = "Named Depth Test"
        };

        Assert.Equal(new Uri("ipp://192.168.1.100/ipp/print"), settings.CustomRequest.OperationAttributes?.PrinterUri);
        Assert.Equal("Named Depth Test", settings.CustomRequest.OperationAttributes?.JobName);
    }
}

[GenerateCliSettings(nameof(CustomRequest))]
public partial class CustomSettingsTestClass : Spectre.Console.Cli.CommandSettings
{
    public SharpIpp.Models.Requests.PrintJobRequest CustomRequest { get; } = new();
}

[GenerateCliSettings(nameof(CustomRequest), 2)]
public partial class PositionalDepthSettingsTestClass : Spectre.Console.Cli.CommandSettings
{
    public SharpIpp.Models.Requests.PrintJobRequest CustomRequest { get; } = new();
}

[GenerateCliSettings(nameof(CustomRequest), MaxNestingDepth = 2)]
public partial class NamedDepthSettingsTestClass : Spectre.Console.Cli.CommandSettings
{
    public SharpIpp.Models.Requests.PrintJobRequest CustomRequest { get; } = new();
}



