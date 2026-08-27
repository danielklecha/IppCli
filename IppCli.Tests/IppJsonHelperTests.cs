using System;
using System.IO;
using IppCli.Serialization;
using SharpIpp.Protocol.Models;
using Xunit;

namespace IppCli.Tests;

public class IppJsonHelperTests
{
    [Fact]
    public void Uri_DeserializesAndSerializesCorrectly()
    {
        var uri = IppJsonHelper.Deserialize<Uri>("\"ipp://printer.local/ipp/print\"");
        Assert.Equal(new Uri("ipp://printer.local/ipp/print"), uri);

        var serialized = IppJsonHelper.Serialize(uri);
        Assert.Equal("\"ipp://printer.local/ipp/print\"", serialized);

        var relative = IppJsonHelper.Deserialize<Uri>("\"/printer/ipp\"");
        Assert.Equal(new Uri("/printer/ipp", UriKind.Relative), relative);

        var nullUri = IppJsonHelper.Deserialize<Uri>("null");
        Assert.Null(nullUri);
    }

    [Fact]
    public void IppVersion_DeserializesStringAndNumber()
    {
        var v1 = IppJsonHelper.Deserialize<IppVersion>("\"2.0\"");
        Assert.Equal(new IppVersion(2, 0), v1);

        var v2 = IppJsonHelper.Deserialize<IppVersion>("1.1");
        Assert.Equal(new IppVersion(1, 1), v2);

        var serialized = IppJsonHelper.Serialize(new IppVersion(2, 0));
        Assert.Equal("\"2.0\"", serialized);
    }

    [Fact]
    public void DocumentFormat_DeserializesPredefinedAndCustomStrings()
    {
        var df1 = IppJsonHelper.Deserialize<DocumentFormat>("\"application/pdf\"");
        Assert.Equal(DocumentFormat.ApplicationPdf, df1);

        var df2 = IppJsonHelper.Deserialize<DocumentFormat>("\"application/vnd.custom-ipp\"");
        Assert.Equal("application/vnd.custom-ipp", df2.ToString());

        var serialized = IppJsonHelper.Serialize(DocumentFormat.ApplicationPdf);
        Assert.Equal("\"application/pdf\"", serialized);
    }

    [Fact]
    public void IdentifyAction_DeserializesPredefinedAndCustomStrings()
    {
        var ia1 = IppJsonHelper.Deserialize<IdentifyAction>("\"flash\"");
        Assert.Equal(IdentifyAction.Flash, ia1);

        var ia2 = IppJsonHelper.Deserialize<IdentifyAction>("\"custom-chime\"");
        Assert.Equal("custom-chime", ia2.ToString());

        var serialized = IppJsonHelper.Serialize(IdentifyAction.Sound);
        Assert.Equal("\"sound\"", serialized);
    }

    [Fact]
    public void Range_DeserializesStringNumberAndArray()
    {
        var r1 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("\"1-100\"");
        Assert.Equal(1, r1.Lower);
        Assert.Equal(100, r1.Upper);

        var r2 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("5");
        Assert.Equal(5, r2.Lower);
        Assert.Equal(5, r2.Upper);

        var r3 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("[10, 50]");
        Assert.Equal(10, r3.Lower);
        Assert.Equal(50, r3.Upper);

        var serialized = IppJsonHelper.Serialize(new SharpIpp.Protocol.Models.Range(1, 100));
        Assert.Equal("\"1 - 100\"", serialized);
    }

    [Fact]
    public void SharpIppStructs_SupportsKebabCaseAndStaticFields()
    {
        var sides = IppJsonHelper.Deserialize<Sides>("\"two-sided-long-edge\"");
        Assert.Equal(Sides.TwoSidedLongEdge, sides);

        var colorMode = IppJsonHelper.Deserialize<PrintColorMode>("\"color\"");
        Assert.Equal(PrintColorMode.Color, colorMode);

        var whichJobs = IppJsonHelper.Deserialize<WhichJobs>("\"not-completed\"");
        Assert.Equal(WhichJobs.NotCompleted, whichJobs);
    }

    [Fact]
    public void Enums_SupportsCamelCaseAndCaseInsensitive()
    {
        var status = IppJsonHelper.Deserialize<IppStatusCode>("\"successfulOk\"");
        Assert.Equal(IppStatusCode.SuccessfulOk, status);

        var orientation = IppJsonHelper.Deserialize<Orientation>("\"landscape\"");
        Assert.Equal(Orientation.Landscape, orientation);
    }

    [Fact]
    public void FileReference_LoadsAndNormalizesContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{\"printer-uri\": \"ipp://10.0.0.1/ipp/print\", \"job-name\": \"TestJob\"}");
            var parsed = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.JobTemplateAttributes>($"@{tempFile}");
            Assert.NotNull(parsed);
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
    public void FileReference_NonExistentFile_ThrowsFileNotFoundExceptionWithResolvedPath()
    {
        var ex = Assert.Throws<FileNotFoundException>(() =>
            IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.JobTemplateAttributes>("@non_existent_attributes_file.json"));

        Assert.Contains("non_existent_attributes_file.json", ex.Message);
        Assert.Contains("resolved to", ex.Message);
    }

    [Fact]
    public void FileReference_RelativeAndQuotedPath_LoadsCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var relativeFileName = "test-attrs.json";
            File.WriteAllText(Path.Combine(tempDir, relativeFileName), "{\"copies\": 5, \"sides\": \"two-sided-long-edge\"}");

            var parsed = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.JobTemplateAttributes>($"@\"{relativeFileName}\"");
            Assert.NotNull(parsed);
            Assert.Equal(5, parsed.Copies);
            Assert.Equal(Sides.TwoSidedLongEdge, parsed.Sides);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Range_DeserializesObjectAndSpacedStrings()
    {
        var r1 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("{\"lower\": 5, \"upper\": 25}");
        Assert.Equal(5, r1.Lower);
        Assert.Equal(25, r1.Upper);

        var r2 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("{\"from\": 10, \"to\": 50}");
        Assert.Equal(10, r2.Lower);
        Assert.Equal(50, r2.Upper);

        var r3 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("\" 15 - 30 \"");
        Assert.Equal(15, r3.Lower);
        Assert.Equal(30, r3.Upper);

        var r4 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("{\"min\": 2, \"max\": 8}");
        Assert.Equal(2, r4.Lower);
        Assert.Equal(8, r4.Upper);
    }

    [Fact]
    public void SharpIppStructs_SupportsSnakeCaseAndPascalCase()
    {
        var sidesSnake = IppJsonHelper.Deserialize<Sides>("\"two_sided_long_edge\"");
        Assert.Equal(Sides.TwoSidedLongEdge, sidesSnake);

        var sidesPascal = IppJsonHelper.Deserialize<Sides>("\"TwoSidedLongEdge\"");
        Assert.Equal(Sides.TwoSidedLongEdge, sidesPascal);

        var sidesOne = IppJsonHelper.Deserialize<Sides>("\"one-sided\"");
        Assert.Equal(Sides.OneSided, sidesOne);
    }

    [Fact]
    public void JsonNormalization_NormalizesNestedKebabCaseKeys()
    {
        var jsonFlat = "{\"job-priority\": 50, \"copies\": 2}";
        var parsedFlat = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.JobTemplateAttributes>(jsonFlat);
        Assert.NotNull(parsedFlat);
        Assert.Equal(2, parsedFlat.Copies);
        Assert.Equal(50, parsedFlat.JobPriority);

        var jsonNested = "{\"job-template-attributes\": {\"job-priority\": 30, \"copies\": 5}}";
        var parsedNested = IppJsonHelper.Deserialize<SharpIpp.Models.Requests.PrintJobRequest>(jsonNested);
        Assert.NotNull(parsedNested);
        Assert.NotNull(parsedNested.JobTemplateAttributes);
        Assert.Equal(5, parsedNested.JobTemplateAttributes.Copies);
        Assert.Equal(30, parsedNested.JobTemplateAttributes.JobPriority);
    }
}
