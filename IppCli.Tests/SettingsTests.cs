using System.Reflection;
using IppCli.Attributes;
using SharpIpp.Protocol.Models;

namespace IppCli.Tests;

[TestClass]
public class SettingsTests
{
    [TestMethod]
    public void BaseSettings_DefaultsArePopulated()
    {
        var settings = new IppCli.Commands.GetPrinterAttributesCommand.Settings();

        Assert.AreEqual("1.1", settings.Version);
        Assert.AreEqual(1, settings.RequestId);
        Assert.AreEqual(OutputFormat.Tree, settings.Output);
        Assert.AreEqual(30, settings.TimeoutSeconds);
        Assert.IsFalse(settings.IgnoreSslErrors);
        Assert.IsNotNull(settings.Request.OperationAttributes);
        Assert.AreEqual(Environment.UserName, settings.Request.OperationAttributes.RequestingUserName);
    }

    [TestMethod]
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

        Assert.AreEqual(new IppVersion(2, 0), settings.Request.Version);
        Assert.AreEqual(99, settings.Request.RequestId);
        Assert.IsNotNull(settings.Request.OperationAttributes);
        Assert.AreEqual(new Uri("ipp://192.168.1.50/ipp/system"), (Uri?)settings.Request.OperationAttributes.PrinterUri);
        Assert.AreEqual(1234, (int?)settings.Request.OperationAttributes.ResourceId);
        CollectionAssert.AreEqual(new[] { "resource-name", "resource-state" }, settings.Request.OperationAttributes.RequestedAttributes.Unwrap());
    }

    [TestMethod]
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

        Assert.AreEqual(new Uri("ipp://192.168.1.100/ipp/print"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
        Assert.AreEqual("My Document", settings.Request.OperationAttributes?.JobName?.ToString());
        Assert.AreEqual(5, (int?)settings.Request.JobTemplateAttributes?.Copies);
        Assert.AreEqual(Sides.TwoSidedLongEdge, (Sides?)settings.Request.JobTemplateAttributes?.Sides);
        Assert.AreEqual(PrintColorMode.Color, (PrintColorMode?)settings.Request.JobTemplateAttributes?.PrintColorMode);
    }

    [TestMethod]
    public void GeneratedSettings_SetPrinterAttributes_MutatesDescriptionAttributes()
    {
        var settings = new IppCli.Commands.SetPrinterAttributesCommand.Settings
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            PaPrinterInfo = "Office Color Laser",
            PaPrinterLocation = "Room 302"
        };

        Assert.AreEqual(new Uri("ipp://192.168.1.100/ipp/print"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
        Assert.AreEqual("Office Color Laser", settings.Request.PrinterAttributes?.PrinterInfo?.ToString());
        Assert.AreEqual("Room 302", settings.Request.PrinterAttributes?.PrinterLocation?.ToString());
    }

    [TestMethod]
    public void GeneratedSettings_GetJobs_MutatesWhichJobsAndMyJobs()
    {
        var settings = new IppCli.Commands.GetJobsCommand.Settings
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            OpWhichJobs = WhichJobs.Completed,
            OpMyJobs = true,
            OpLimit = 10
        };

        Assert.AreEqual(WhichJobs.Completed, (WhichJobs?)settings.Request.OperationAttributes?.WhichJobs);
        Assert.IsTrue((bool?)settings.Request.OperationAttributes?.MyJobs == true);
        Assert.AreEqual(10, (int?)settings.Request.OperationAttributes?.Limit);
    }

    [TestMethod]
    public void GeneratedSettings_OpJson_ParsesInlineJson()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            OpJson = """{"printerUri": "ipp://10.0.0.5/ipp/print", "jobName": "Invoice #42", "documentFormat": "application/pdf"}"""
        };

        Assert.AreEqual(new Uri("ipp://10.0.0.5/ipp/print"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
        Assert.AreEqual("Invoice #42", settings.Request.OperationAttributes?.JobName?.ToString());
        Assert.AreEqual(new DocumentFormat("application/pdf"), (DocumentFormat?)settings.Request.OperationAttributes?.DocumentFormat);
    }

    [TestMethod]
    public void GeneratedSettings_JtaJson_ParsesComplexNestedAttributes()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            JtaJson = """{"copies": 3, "sides": "TwoSidedLongEdge", "printColorMode": "Color"}"""
        };

        Assert.AreEqual(3, (int?)settings.Request.JobTemplateAttributes?.Copies);
        Assert.AreEqual(Sides.TwoSidedLongEdge, (Sides?)settings.Request.JobTemplateAttributes?.Sides);
        Assert.AreEqual(PrintColorMode.Color, (PrintColorMode?)settings.Request.JobTemplateAttributes?.PrintColorMode);
    }

    [TestMethod]
    public void GeneratedSettings_PaJson_ParsesPrinterAttributes()
    {
        var settings = new IppCli.Commands.SetPrinterAttributesCommand.Settings
        {
            PaJson = """{"printerInfo": "Main Office", "printerLocation": "Floor 2"}"""
        };

        Assert.AreEqual("Main Office", settings.Request.PrinterAttributes?.PrinterInfo?.ToString());
        Assert.AreEqual("Floor 2", settings.Request.PrinterAttributes?.PrinterLocation?.ToString());
    }

    [TestMethod]
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

            Assert.AreEqual(new Uri("ipp://printer.local/ipp/print"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
            Assert.AreEqual("Test from File", settings.Request.OperationAttributes?.JobName?.ToString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [TestMethod]
    public void GeneratedSettings_JsonAndIndividualProperty_OverridesProperty()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            OpJson = """{"printerUri": "ipp://10.0.0.1/ipp/print", "jobName": "Original Name"}""",
            OpJobName = "Overridden Name"
        };

        Assert.AreEqual(new Uri("ipp://10.0.0.1/ipp/print"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
        Assert.AreEqual("Overridden Name", settings.Request.OperationAttributes?.JobName?.ToString());
    }

    [TestMethod]
    public void GeneratedSettings_JtaJson_SupportsKebabCaseEnumAndStructValues()
    {
        var settings = new IppCli.Commands.PrintJobCommand.Settings
        {
            JtaJson = """{"copies": 2, "sides": "two-sided-long-edge", "print-color-mode": "color", "orientationRequested": "landscape"}"""
        };

        Assert.AreEqual(2, (int?)settings.Request.JobTemplateAttributes?.Copies);
        Assert.AreEqual(Sides.TwoSidedLongEdge, (Sides?)settings.Request.JobTemplateAttributes?.Sides);
        Assert.AreEqual(PrintColorMode.Color, (PrintColorMode?)settings.Request.JobTemplateAttributes?.PrintColorMode);
        Assert.AreEqual(Orientation.Landscape, (Orientation?)settings.Request.JobTemplateAttributes?.OrientationRequested);
    }

    [TestMethod]
    public void GeneratedSettings_OpJson_SupportsArrayAttributes()
    {
        var settings = new IppCli.Commands.GetResourceAttributesCommand.Settings
        {
            OpJson = """{"printerUri": "ipp://192.168.1.50/ipp/system", "resourceId": 1234, "requestedAttributes": ["resource-name", "resource-state"]}"""
        };

        Assert.AreEqual(new Uri("ipp://192.168.1.50/ipp/system"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
        Assert.AreEqual(1234, (int?)settings.Request.OperationAttributes?.ResourceId);
        CollectionAssert.AreEqual(new[] { "resource-name", "resource-state" }, settings.Request.OperationAttributes?.RequestedAttributes.Unwrap());
    }

    [TestMethod]
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

        Assert.AreEqual(new IppVersion(2, 0), settings.Request.Version);
        Assert.AreEqual(77, settings.Request.RequestId);
        Assert.AreEqual(new Uri("ipp://192.168.1.150/ipp/print"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
        Assert.AreEqual("Full Request JSON Test", settings.Request.OperationAttributes?.JobName?.ToString());
        Assert.AreEqual(4, (int?)settings.Request.JobTemplateAttributes?.Copies);
        Assert.AreEqual(Sides.TwoSidedLongEdge, (Sides?)settings.Request.JobTemplateAttributes?.Sides);
    }

    [TestMethod]
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

            Assert.AreEqual(new IppVersion(2, 1), settings.Request.Version);
            Assert.AreEqual(88, settings.Request.RequestId);
            Assert.AreEqual(new Uri("ipp://printer.local/ipp/print"), (Uri?)settings.Request.OperationAttributes?.PrinterUri);
            Assert.AreEqual("Test from Request File", settings.Request.OperationAttributes?.JobName?.ToString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [TestMethod]
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
        Assert.IsNotNull(json);
        StringAssert.Contains(json, "ipp://10.0.0.1/ipp/print");
        StringAssert.Contains(json, "Serialized Job");
    }

    [TestMethod]
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

        Assert.IsNotNull(settings.Request.DocumentTemplateAttributes.Unwrap()?.MediaCol.Unwrap());
        Assert.AreEqual(MediaColor.White, (MediaColor?)settings.Request.DocumentTemplateAttributes.Unwrap()?.MediaCol.Unwrap()?.MediaColor.Unwrap());
        Assert.AreEqual(21000, settings.Request.DocumentTemplateAttributes.Unwrap()!.MediaCol.Unwrap()!.MediaSize.Unwrap()!.XDimension!.Value.Value.Lower);
        Assert.AreEqual(29700, settings.Request.DocumentTemplateAttributes.Unwrap()!.MediaCol.Unwrap()!.MediaSize.Unwrap()!.YDimension!.Value.Value.Lower);
    }

    [TestMethod]
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

        Assert.IsNotNull(settings.Request.JobTemplateAttributes?.MediaCol.Unwrap());
        Assert.AreEqual(MediaColor.Blue, (MediaColor?)settings.Request.JobTemplateAttributes?.MediaCol.Unwrap()?.MediaColor.Unwrap());
        Assert.AreEqual(21590, settings.Request.JobTemplateAttributes!.MediaCol.Unwrap()!.MediaSize.Unwrap()!.XDimension!.Value.Value.Lower);
        Assert.AreEqual(27940, settings.Request.JobTemplateAttributes!.MediaCol.Unwrap()!.MediaSize.Unwrap()!.YDimension!.Value.Value.Lower);

        var serialized = settings.JtaMediaCol;
        Assert.IsNotNull(serialized);
        StringAssert.Contains(serialized, "21590");
    }

    [TestMethod]
    public void GeneratedSettings_MediaCol_CommandOptionAttributesExist()
    {
        var docProp = typeof(IppCli.Commands.SendDocumentCommand.Settings).GetProperty("DtaMediaCol");
        Assert.IsNotNull(docProp);
        var docAttr = docProp.GetCustomAttribute<Spectre.Console.Cli.CommandOptionAttribute>();
        Assert.IsNotNull(docAttr);
        Assert.IsTrue(docAttr.LongNames.Contains("dta-media-col"));
        Assert.IsFalse(docAttr.LongNames.Contains("dta-mediacol"));

        var jbProp = typeof(IppCli.Commands.PrintJobCommand.Settings).GetProperty("JtaMediaCol");
        Assert.IsNotNull(jbProp);
        var jbAttr = jbProp.GetCustomAttribute<Spectre.Console.Cli.CommandOptionAttribute>();
        Assert.IsNotNull(jbAttr);
        Assert.IsTrue(jbAttr.LongNames.Contains("jta-media-col"));
        Assert.IsFalse(jbAttr.LongNames.Contains("jta-mediacol"));
    }

    [TestMethod]
    public void GeneratedSettings_Level2ComplexTypes_NotExpandedToFlatOptions()
    {
        var settingsType = typeof(IppCli.Commands.PrintJobCommand.Settings);
        
        // Level 2 complex JSON option exists
        var mediaColProp = settingsType.GetProperty("JtaMediaCol");
        Assert.IsNotNull(mediaColProp);
        var mediaColAttr = mediaColProp.GetCustomAttribute<Spectre.Console.Cli.CommandOptionAttribute>();
        Assert.IsNotNull(mediaColAttr);
        Assert.IsTrue(mediaColAttr.LongNames.Contains("jta-media-col"));

        // Level 3+ properties are NOT expanded into separate flat properties
        Assert.IsNull(settingsType.GetProperty("JtaMediaColMediaColor"));
        Assert.IsNull(settingsType.GetProperty("JtaMediaColMediaSizeXDimension"));
        Assert.IsNull(settingsType.GetProperty("JtaMediaColMediaType"));
    }

    [TestMethod]
    public void GeneratedSettings_SetJobAttributes_DtaJson_ParsesJobTemplateAttributes()
    {
        var settings = new IppCli.Commands.SetJobAttributesCommand.Settings
        {
            JtaJson = """{"copies": 10, "sides": "TwoSidedLongEdge"}"""
        };

        Assert.AreEqual(10, (int?)settings.Request.JobTemplateAttributes?.Copies);
        Assert.AreEqual(Sides.TwoSidedLongEdge, (Sides?)settings.Request.JobTemplateAttributes?.Sides);
    }

    [TestMethod]
    public void GeneratedSettings_SetDocumentAttributes_DtaJson_ParsesDocumentTemplateAttributes()
    {
        var settings = new IppCli.Commands.SetDocumentAttributesCommand.Settings
        {
            DtaJson = """{"copies": 7}"""
        };

        Assert.AreEqual(7, (int?)settings.Request.DocumentTemplateAttributes.Unwrap()?.Copies);
    }

    [TestMethod]
    public void GeneratedSettings_CustomSettingsWithDepth_CompilesAndWorks()
    {
        var settings = new PositionalDepthSettingsTestClass
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            OpJobName = "Positional Depth Test"
        };

        Assert.AreEqual(new Uri("ipp://192.168.1.100/ipp/print"), (Uri?)settings.CustomRequest.OperationAttributes?.PrinterUri);
        Assert.AreEqual("Positional Depth Test", settings.CustomRequest.OperationAttributes?.JobName?.ToString());
    }

    [TestMethod]
    public void GeneratedSettings_NamedMaxNestingDepth_CompilesAndWorks()
    {
        var settings = new NamedDepthSettingsTestClass
        {
            OpPrinterUri = "ipp://192.168.1.100/ipp/print",
            OpJobName = "Named Depth Test"
        };

        Assert.AreEqual(new Uri("ipp://192.168.1.100/ipp/print"), (Uri?)settings.CustomRequest.OperationAttributes?.PrinterUri);
        Assert.AreEqual("Named Depth Test", settings.CustomRequest.OperationAttributes?.JobName?.ToString());
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
