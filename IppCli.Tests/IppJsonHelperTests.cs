using System;
using System.IO;
using IppCli.Serialization;
using SharpIpp.Protocol.Models;

namespace IppCli.Tests;

[TestClass]
public class IppJsonHelperTests
{
    [TestMethod]
    public void Uri_DeserializesAndSerializesCorrectly()
    {
        var uri = IppJsonHelper.Deserialize<Uri>("\"ipp://printer.local/ipp/print\"");
        Assert.AreEqual(new Uri("ipp://printer.local/ipp/print"), uri);

        var serialized = IppJsonHelper.Serialize(uri);
        Assert.AreEqual("\"ipp://printer.local/ipp/print\"", serialized);

        var relative = IppJsonHelper.Deserialize<Uri>("\"/printer/ipp\"");
        Assert.AreEqual(new Uri("/printer/ipp", UriKind.Relative), relative);

        var nullUri = IppJsonHelper.Deserialize<Uri>("null");
        Assert.IsNull(nullUri);
    }

    [TestMethod]
    public void IppVersion_DeserializesStringAndNumber()
    {
        var v1 = IppJsonHelper.Deserialize<IppVersion>("\"2.0\"");
        Assert.AreEqual(new IppVersion(2, 0), v1);

        var v2 = IppJsonHelper.Deserialize<IppVersion>("1.1");
        Assert.AreEqual(new IppVersion(1, 1), v2);

        var serialized = IppJsonHelper.Serialize(new IppVersion(2, 0));
        Assert.AreEqual("\"2.0\"", serialized);
    }

    [TestMethod]
    public void DocumentFormat_DeserializesPredefinedAndCustomStrings()
    {
        var df1 = IppJsonHelper.Deserialize<DocumentFormat>("\"application/pdf\"");
        Assert.AreEqual(DocumentFormat.ApplicationPdf, df1);

        var df2 = IppJsonHelper.Deserialize<DocumentFormat>("\"application/vnd.custom-ipp\"");
        Assert.AreEqual("application/vnd.custom-ipp", df2.ToString());

        var serialized = IppJsonHelper.Serialize(DocumentFormat.ApplicationPdf);
        Assert.AreEqual("\"application/pdf\"", serialized);
    }

    [TestMethod]
    public void IdentifyAction_DeserializesPredefinedAndCustomStrings()
    {
        var ia1 = IppJsonHelper.Deserialize<IdentifyAction>("\"flash\"");
        Assert.AreEqual(IdentifyAction.Flash, ia1);

        var ia2 = IppJsonHelper.Deserialize<IdentifyAction>("\"custom-chime\"");
        Assert.AreEqual("custom-chime", ia2.ToString());

        var serialized = IppJsonHelper.Serialize(IdentifyAction.Sound);
        Assert.AreEqual("\"sound\"", serialized);
    }

    [TestMethod]
    public void Range_DeserializesStringNumberAndArray()
    {
        var r1 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("\"1-100\"");
        Assert.AreEqual(1, r1.Lower);
        Assert.AreEqual(100, r1.Upper);

        var r2 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("5");
        Assert.AreEqual(5, r2.Lower);
        Assert.AreEqual(5, r2.Upper);

        var r3 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("[10, 50]");
        Assert.AreEqual(10, r3.Lower);
        Assert.AreEqual(50, r3.Upper);

        var serialized = IppJsonHelper.Serialize(new SharpIpp.Protocol.Models.Range(1, 100));
        Assert.AreEqual("\"1 - 100\"", serialized);
    }

    [TestMethod]
    public void SharpIppStructs_SupportsKebabCaseAndStaticFields()
    {
        var sides = IppJsonHelper.Deserialize<Sides>("\"two-sided-long-edge\"");
        Assert.AreEqual(Sides.TwoSidedLongEdge, sides);

        var colorMode = IppJsonHelper.Deserialize<PrintColorMode>("\"color\"");
        Assert.AreEqual(PrintColorMode.Color, colorMode);

        var whichJobs = IppJsonHelper.Deserialize<WhichJobs>("\"not-completed\"");
        Assert.AreEqual(WhichJobs.NotCompleted, whichJobs);
    }

    [TestMethod]
    public void Enums_SupportsCamelCaseAndCaseInsensitive()
    {
        var status = IppJsonHelper.Deserialize<IppStatusCode>("\"successfulOk\"");
        Assert.AreEqual(IppStatusCode.SuccessfulOk, status);

        var orientation = IppJsonHelper.Deserialize<Orientation>("\"landscape\"");
        Assert.AreEqual(Orientation.Landscape, orientation);
    }

    [TestMethod]
    public void FileReference_LoadsAndNormalizesContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{\"printer-uri\": \"ipp://10.0.0.1/ipp/print\", \"job-name\": \"TestJob\"}");
            var parsed = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.JobTemplateAttributes>($"@{tempFile}");
            Assert.IsNotNull(parsed);
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
    public void FileReference_NonExistentFile_ThrowsFileNotFoundExceptionWithResolvedPath()
    {
        var ex = Assert.ThrowsExactly<FileNotFoundException>(() =>
            IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.JobTemplateAttributes>("@non_existent_attributes_file.json"));

        StringAssert.Contains(ex.Message, "non_existent_attributes_file.json");
        StringAssert.Contains(ex.Message, "resolved to");
    }

    [TestMethod]
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
            Assert.IsNotNull(parsed);
            Assert.AreEqual(5, parsed.Copies);
            Assert.AreEqual(Sides.TwoSidedLongEdge, parsed.Sides);
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

    [TestMethod]
    public void Range_DeserializesObjectAndSpacedStrings()
    {
        var r1 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("{\"lower\": 5, \"upper\": 25}");
        Assert.AreEqual(5, r1.Lower);
        Assert.AreEqual(25, r1.Upper);

        var r2 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("{\"from\": 10, \"to\": 50}");
        Assert.AreEqual(10, r2.Lower);
        Assert.AreEqual(50, r2.Upper);

        var r3 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("\" 15 - 30 \"");
        Assert.AreEqual(15, r3.Lower);
        Assert.AreEqual(30, r3.Upper);

        var r4 = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.Range>("{\"min\": 2, \"max\": 8}");
        Assert.AreEqual(2, r4.Lower);
        Assert.AreEqual(8, r4.Upper);
    }

    [TestMethod]
    public void SharpIppStructs_SupportsSnakeCaseAndPascalCase()
    {
        var sidesSnake = IppJsonHelper.Deserialize<Sides>("\"two_sided_long_edge\"");
        Assert.AreEqual(Sides.TwoSidedLongEdge, sidesSnake);

        var sidesPascal = IppJsonHelper.Deserialize<Sides>("\"TwoSidedLongEdge\"");
        Assert.AreEqual(Sides.TwoSidedLongEdge, sidesPascal);

        var sidesOne = IppJsonHelper.Deserialize<Sides>("\"one-sided\"");
        Assert.AreEqual(Sides.OneSided, sidesOne);
    }

    [TestMethod]
    public void JsonNormalization_NormalizesNestedKebabCaseKeys()
    {
        var jsonFlat = "{\"job-priority\": 50, \"copies\": 2}";
        var parsedFlat = IppJsonHelper.Deserialize<SharpIpp.Protocol.Models.JobTemplateAttributes>(jsonFlat);
        Assert.IsNotNull(parsedFlat);
        Assert.AreEqual(2, parsedFlat.Copies);
        Assert.AreEqual(50, parsedFlat.JobPriority);

        var jsonNested = "{\"job-template-attributes\": {\"job-priority\": 30, \"copies\": 5}}";
        var parsedNested = IppJsonHelper.Deserialize<SharpIpp.Models.Requests.PrintJobRequest>(jsonNested);
        Assert.IsNotNull(parsedNested);
        Assert.IsNotNull(parsedNested.JobTemplateAttributes);
        Assert.AreEqual(5, parsedNested.JobTemplateAttributes.Copies);
        Assert.AreEqual(30, parsedNested.JobTemplateAttributes.JobPriority);
    }

    [TestMethod]
    public void OctetString_DeserializesAndSerializesCorrectly()
    {
        // 1. String token passes directly to constructor
        var os1 = IppJsonHelper.Deserialize<OctetString>("\"hello-world\"");
        Assert.IsTrue(os1.IsValue);
        Assert.AreEqual("hello-world", os1.ToString());

        // 2. Magic "no-value" string is NOT treated as null/default
        var osNoValStr = IppJsonHelper.Deserialize<OctetString>("\"no-value\"");
        Assert.IsTrue(osNoValStr.IsValue);
        Assert.AreEqual("no-value", osNoValStr.ToString());

        // 3. Null token returns default
        var osNull = IppJsonHelper.Deserialize<OctetString>("null");
        Assert.IsFalse(osNull.IsValue);

        // 4. JSON object with value property
        var osObj = IppJsonHelper.Deserialize<OctetString>("{\"value\": \"test-octet\"}");
        Assert.IsTrue(osObj.IsValue);
        Assert.AreEqual("test-octet", osObj.ToString());

        // 5. JSON object with isNoValue: true
        var osNoValObj = IppJsonHelper.Deserialize<OctetString>("{\"isNoValue\": true}");
        Assert.IsFalse(osNoValObj.IsValue);

        // 6. JSON object with isValue: false
        var osIsValFalseObj = IppJsonHelper.Deserialize<OctetString>("{\"isValue\": false}");
        Assert.IsFalse(osIsValFalseObj.IsValue);

        // 7. JSON object with isNoValue: false and value
        var osValidObj = IppJsonHelper.Deserialize<OctetString>("{\"isNoValue\": false, \"value\": \"abc\"}");
        Assert.IsTrue(osValidObj.IsValue);
        Assert.AreEqual("abc", osValidObj.ToString());

        // 8. Serializing OctetString with value
        var serialized = IppJsonHelper.Serialize(new OctetString("hello"));
        Assert.AreEqual("\"hello\"", serialized);

        // 9. Serializing default OctetString produces null
        var serializedDef = IppJsonHelper.Serialize(default(OctetString));
        Assert.AreEqual("null", serializedDef);
    }

    [TestMethod]
    public void StringWithLanguage_DeserializesAndSerializesCorrectly()
    {
        // 1. String token passes directly to constructor
        var swl1 = IppJsonHelper.Deserialize<StringWithLanguage>("\"my-string\"");
        Assert.IsTrue(swl1.IsValue);
        Assert.IsFalse(swl1.HasLanguage);
        Assert.AreEqual("my-string", swl1.Value);

        // 2. Magic "no-value" string is NOT treated as null/default
        var swlNoValStr = IppJsonHelper.Deserialize<StringWithLanguage>("\"no-value\"");
        Assert.IsTrue(swlNoValStr.IsValue);
        Assert.AreEqual("no-value", swlNoValStr.Value);

        // 3. Null token returns default
        var swlNull = IppJsonHelper.Deserialize<StringWithLanguage>("null");
        Assert.IsFalse(swlNull.IsValue);

        // 4. JSON object with language and value
        var swlObj = IppJsonHelper.Deserialize<StringWithLanguage>("{\"language\": \"en-us\", \"value\": \"greeting\"}");
        Assert.IsTrue(swlObj.IsValue);
        Assert.IsTrue(swlObj.HasLanguage);
        Assert.AreEqual("en-us", swlObj.Language);
        Assert.AreEqual("greeting", swlObj.Value);

        // 5. JSON object with only value
        var swlValOnly = IppJsonHelper.Deserialize<StringWithLanguage>("{\"value\": \"plain\"}");
        Assert.IsTrue(swlValOnly.IsValue);
        Assert.IsFalse(swlValOnly.HasLanguage);
        Assert.AreEqual("plain", swlValOnly.Value);

        // 6. JSON object with isNoValue: true
        var swlNoValObj = IppJsonHelper.Deserialize<StringWithLanguage>("{\"isNoValue\": true}");
        Assert.IsFalse(swlNoValObj.IsValue);

        // 7. JSON object with isValue: false
        var swlIsValFalse = IppJsonHelper.Deserialize<StringWithLanguage>("{\"isValue\": false}");
        Assert.IsFalse(swlIsValFalse.IsValue);

        // 8. JSON object with isNoValue: false and language/value
        var swlExplicitVal = IppJsonHelper.Deserialize<StringWithLanguage>("{\"isNoValue\": false, \"language\": \"fr\", \"value\": \"bonjour\"}");
        Assert.IsTrue(swlExplicitVal.IsValue);
        Assert.IsTrue(swlExplicitVal.HasLanguage);
        Assert.AreEqual("fr", swlExplicitVal.Language);
        Assert.AreEqual("bonjour", swlExplicitVal.Value);

        // 9. Alternative property names like "lang" or "naturalLanguage" or "text" are NOT associated
        var swlAlt = IppJsonHelper.Deserialize<StringWithLanguage>("{\"lang\": \"fr\", \"text\": \"bonjour\"}");
        Assert.IsFalse(swlAlt.IsValue);

        // 10. Serializing StringWithLanguage with language produces object
        var serializedWithLang = IppJsonHelper.Serialize(new StringWithLanguage("en", "hello"));
        StringAssert.Contains(serializedWithLang, "\"language\": \"en\"");
        StringAssert.Contains(serializedWithLang, "\"value\": \"hello\"");

        // 11. Serializing StringWithLanguage without language produces string
        var serializedNoLang = IppJsonHelper.Serialize(new StringWithLanguage("hello"));
        Assert.AreEqual("\"hello\"", serializedNoLang);

        // 12. Serializing default StringWithLanguage produces null
        var serializedDef = IppJsonHelper.Serialize(default(StringWithLanguage));
        Assert.AreEqual("null", serializedDef);
    }
}
