using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IppCli.Models;
using IppCli.Services;
using SharpIpp.Models.Responses;
using SharpIpp.Protocol;
using SharpIpp.Protocol.Models;
using Spectre.Console;

namespace IppCli.Tests;

[TestClass]
public class FormattingTests
{
    [TestMethod]
    public void JsonOutputRenderer_GetJsonString_ProducesValidIndentedJson()
    {
        var response = new GetPrinterAttributesResponse
        {
            Version = new IppVersion(2, 0),
            RequestId = 123,
            StatusCode = IppStatusCode.SuccessfulOk,
            PrinterAttributes = new PrinterDescriptionAttributes
            {
                PrinterName = "OfficeLaserJet",
                PrinterState = PrinterState.Idle,
                PrinterIsAcceptingJobs = true
            }
        };

        var json = JsonOutputRenderer.ToJsonString(response);
        Assert.IsNotNull(json);
        StringAssert.Contains(json, "\"statusCode\": \"SuccessfulOk\"");
        StringAssert.Contains(json, "\"printerName\": \"OfficeLaserJet\"");
        StringAssert.Contains(json, "\"printerState\": \"Idle\"");

        using var doc = JsonDocument.Parse(json);
        Assert.AreEqual("SuccessfulOk", doc.RootElement.GetProperty("statusCode").GetString());
    }

    [TestMethod]
    [DataRow(OutputFormat.Tree)]
    [DataRow(OutputFormat.Json)]
    public void OutputFormatter_FormatResponse_ExecutesWithoutError(OutputFormat format)
    {
        var response = new PausePrinterResponse
        {
            Version = new IppVersion(2, 0),
            RequestId = 1,
            StatusCode = IppStatusCode.SuccessfulOk
        };

        OutputFormatter.FormatResponse("Pause-Printer", response, format);
    }

    [TestMethod]
    [DataRow(OutputFormat.Tree, typeof(ConsoleTreeRenderer))]
    [DataRow(OutputFormat.Json, typeof(JsonOutputRenderer))]
    public void OutputFormatter_GetRenderer_ReturnsExpectedRendererType(OutputFormat format, Type expectedType)
    {
        var renderer = OutputFormatter.GetRenderer(format);
        Assert.IsNotNull(renderer);
        Assert.IsInstanceOfType(renderer, expectedType);
    }

    [TestMethod]
    public void ConsoleTreeRenderer_CreateTree_RendersResponseProperties()
    {
        var response = new GetPrinterAttributesResponse
        {
            Version = new IppVersion(2, 0),
            RequestId = 42,
            StatusCode = IppStatusCode.SuccessfulOk,
            PrinterAttributes = new PrinterDescriptionAttributes
            {
                PrinterName = "OfficeLaserJet",
                PrinterState = PrinterState.Idle,
                PrinterIsAcceptingJobs = true
            },
            OperationAttributes = new OperationAttributes
            {
                AttributesCharset = "utf-8"
            }
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(response, "Get-Printer-Attributes", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "Get-Printer-Attributes");
        StringAssert.Contains(output, "PrinterAttributes");
        StringAssert.Contains(output, "PrinterName: OfficeLaserJet");
        StringAssert.Contains(output, "PrinterState: Idle");
        StringAssert.Contains(output, "PrinterIsAcceptingJobs: true");
        StringAssert.Contains(output, "OperationAttributes");
        StringAssert.Contains(output, "AttributesCharset: utf-8");
        StringAssert.Contains(output, "RequestId: 42");
        StringAssert.Contains(output, "StatusCode: SuccessfulOk");
        StringAssert.Contains(output, "Version: 2.0");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_CreateTree_RendersJobsCollection()
    {
        var response = new GetJobsResponse
        {
            Version = new IppVersion(2, 0),
            RequestId = 10,
            StatusCode = IppStatusCode.SuccessfulOk,
            JobsAttributes = new[]
            {
                new JobDescriptionAttributes
                {
                    JobId = 5,
                    JobName = "Report.pdf",
                    JobState = JobState.Completed,
                    JobOriginatingUserName = "bob",
                    JobMediaSheetsCompleted = 10,
                    TimeAtCreation = 1000
                }
            }
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(response, "Get-Jobs", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "Get-Jobs");
        StringAssert.Contains(output, "JobsAttributes");
        StringAssert.Contains(output, "JobId: 5");
        StringAssert.Contains(output, "JobName: Report.pdf");
        StringAssert.Contains(output, "JobState: Completed");
        StringAssert.Contains(output, "JobOriginatingUserName: bob");
        StringAssert.Contains(output, "JobMediaSheetsCompleted: 10");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_CreateTree_GenericMethod_RendersAnyObject()
    {
        var sample = new SampleModel
        {
            Title = "Test Model",
            Count = 42,
            Nested = new NestedModel
            {
                Description = "Deeply nested",
                Tags = new[] { "tag1", "tag2" }
            }
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(sample, "Sample-Tree", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "Sample-Tree");
        StringAssert.Contains(output, "Title: Test Model");
        StringAssert.Contains(output, "Count: 42");
        StringAssert.Contains(output, "Nested");
        StringAssert.Contains(output, "Description: Deeply nested");
        StringAssert.Contains(output, "Tags");
        StringAssert.Contains(output, "tag1");
        StringAssert.Contains(output, "tag2");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_LeafTypesParameter_DisplaysUsingToStringWithoutExpandingSubproperties()
    {
        // CustomTypeWithSubproperties has properties SubA and SubB
        var model = new ModelWithCustomType
        {
            Name = "Root",
            Custom = new CustomTypeWithSubproperties { SubA = "Hello", SubB = 100 }
        };

        // 1. Without leafTypes: expands SubA and SubB
        var consoleWithout = new TestConsole();
        consoleWithout.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "WithoutLeafTypes", null, consoleWithout);
        var outputWithout = consoleWithout.Output;

        StringAssert.Contains(outputWithout, "Custom");
        StringAssert.Contains(outputWithout, "SubA: Hello");
        StringAssert.Contains(outputWithout, "SubB: 100");

        // 2. With leafTypes including CustomTypeWithSubproperties: rendered via ToString()
        var consoleWith = new TestConsole();
        consoleWith.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "WithLeafTypes", new[] { typeof(CustomTypeWithSubproperties) }, consoleWith);
        var outputWith = consoleWith.Output;

        StringAssert.Contains(outputWith, "Custom: [CustomType: Hello-100]");
        Assert.IsFalse(outputWith.Contains("SubA: Hello"));
        Assert.IsFalse(outputWith.Contains("SubB: 100"));
    }

    [TestMethod]
    public void ConsoleTreeRenderer_IppVersion_TreatedAsLeafByDefault()
    {
        var model = new
        {
            Name = "PrinterVersionCheck",
            Version = new IppVersion(2, 1)
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "VersionTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "Version: 2.1");
        // IppVersion should not be expanded to Major/Minor
        Assert.IsFalse(output.Contains("Major:"));
        Assert.IsFalse(output.Contains("Minor:"));
    }

    [TestMethod]
    public void ConsoleTreeRenderer_HandlesCircularReferenceGracefully()
    {
        var nodeA = new CircularNode { Name = "NodeA" };
        var nodeB = new CircularNode { Name = "NodeB", Next = nodeA };
        nodeA.Next = nodeB;

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(nodeA, "CircularTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "circular reference");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_HandlesNullObjectGracefully()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render<SampleModel>(null, "NullTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "NullTest");
        StringAssert.Contains(output, "null");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_RenderResponse_ViaStringWriter_RendersCorrectly()
    {
        var response = new PausePrinterResponse
        {
            Version = new IppVersion(2, 0),
            RequestId = 7,
            StatusCode = IppStatusCode.SuccessfulOk
        };

        using var sw = new StringWriter();
        var renderer = new ConsoleTreeRenderer();
        renderer.RenderResponse("Pause-Printer", response, sw);

        var output = sw.ToString();
        StringAssert.Contains(output, "Pause-Printer");
        StringAssert.Contains(output, "RequestId: 7");
        StringAssert.Contains(output, "StatusCode: SuccessfulOk");
        StringAssert.Contains(output, "Version: 2.0");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_EmptyCollections_RendersWithoutError()
    {
        var model = new
        {
            Name = "PrinterWithEmptyCollections",
            EmptyList = new List<string>(),
            EmptyArray = Array.Empty<int>()
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "EmptyCollectionTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "EmptyList: []");
        StringAssert.Contains(output, "EmptyArray: []");
    }

    [TestMethod]
    public void BaseRenderer_IsComplexType_IdentifiesTypesCorrectly()
    {
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(int)));
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(string)));
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(Uri)));
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(IppVersion)));
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(IppStatusCode)));
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(JobState)));
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(DateTime)));
        Assert.IsFalse(BaseRenderer.IsComplexType(typeof(byte[])));

        Assert.IsTrue(BaseRenderer.IsComplexType(typeof(JobDescriptionAttributes)));
        Assert.IsTrue(BaseRenderer.IsComplexType(typeof(TestItem)));
    }

    [TestMethod]
    public void ConsoleTreeRenderer_IsLeaf_SmartEnumRecognizedAsLeaf()
    {
        // Types
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(typeof(JobState)));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(typeof(JobHoldUntil)));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(typeof(PrinterState)));

        // Instances
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(JobState.Completed));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(JobHoldUntil.Indefinite));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(new JobHoldUntil("no-value", false)));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(new TestNoValueClass(false, "NoVal")));
        Assert.IsFalse(ConsoleTreeRenderer.IsLeaf(new TestNoValueClass(true, "HasVal")));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(null));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf("string"));
        Assert.IsTrue(ConsoleTreeRenderer.IsLeaf(123));
    }

    [TestMethod]
    public void ConsoleTreeRenderer_NoValue_PropertyDisplaysGreyNoValue()
    {
        var model = new
        {
            HoldUntil = new JobHoldUntil("no-value", false),
            HoldUntilValid = JobHoldUntil.Indefinite,
            ResolutionNoVal = new Resolution(300, 300, ResolutionUnit.DotsPerInch, false),
            CustomNoValue = new TestNoValueClass(false, "Should not be displayed"),
            CustomWithValue = new TestNoValueClass(true, "Actual Value")
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "NoValueTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "HoldUntil: no value");
        StringAssert.Contains(output, "HoldUntilValid: indefinite");
        StringAssert.Contains(output, "ResolutionNoVal: no value");
        StringAssert.Contains(output, "CustomNoValue: no value");
        StringAssert.Contains(output, "CustomWithValue");
        StringAssert.Contains(output, "Content: Actual Value");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_NoValue_RootObjectDisplaysGreyNoValue()
    {
        var root = new TestNoValueClass(false, "Secret");

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(root, "RootNoValueTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "no value");
        Assert.IsFalse(output.Contains("Secret"));
    }

    [TestMethod]
    public void ConsoleTreeRenderer_NoValue_CollectionDisplaysGreyNoValue()
    {
        var model = new
        {
            Items = new object[]
            {
                new JobHoldUntil("no-value", false),
                new Resolution(300, 300, ResolutionUnit.DotsPerInch, false),
                new TestNoValueClass(false, "NoVal"),
                JobHoldUntil.Indefinite
            }
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "CollectionNoValueTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "Items (4): no value, no value, no value, indefinite");
    }

    [TestMethod]
    public void ConsoleTreeRenderer_InlineCollection_RendersCommaSeparatedWithCount()
    {
        var model = new
        {
            DocumentFormatSupported = new[] { "application/pdf", "image/urf", "image/jpeg" },
            SidesSupported = new[] { Sides.OneSided, Sides.TwoSidedLongEdge },
            PrinterStateReasons = new[] { "none" }
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "InlineCollectionTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "DocumentFormatSupported (3): application/pdf, image/urf, image/jpeg");
        StringAssert.Contains(output, "SidesSupported (2): one-sided, two-sided-long-edge");
        StringAssert.Contains(output, "PrinterStateReasons (1): none");
        Assert.IsFalse(output.Contains("[0]"));
    }

    [TestMethod]
    public void ConsoleTreeRenderer_ComplexCollection_UsesZeroBasedIndexing()
    {
        var model = new
        {
            MediaColReady = new[]
            {
                new { MediaBottomMargin = 432, MediaLeftMargin = 432 },
                new { MediaBottomMargin = 200, MediaLeftMargin = 200 }
            }
        };

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "ComplexCollectionTest", null, console);

        var output = console.Output;
        StringAssert.Contains(output, "MediaColReady (2)");
        StringAssert.Contains(output, "[0]");
        StringAssert.Contains(output, "[1]");
        StringAssert.Contains(output, "MediaBottomMargin: 432");
        StringAssert.Contains(output, "MediaBottomMargin: 200");
    }

    private class TestNoValueClass : INoValue
    {
        public bool IsValue { get; }
        public string Content { get; }

        public TestNoValueClass(bool isValue, string content)
        {
            IsValue = isValue;
            Content = content;
        }

        public override string ToString() => Content;
    }

    private class SampleModel
    {
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
        public NestedModel? Nested { get; set; }
    }

    private class NestedModel
    {
        public string Description { get; set; } = string.Empty;
        public string[]? Tags { get; set; }
    }

    private class ModelWithCustomType
    {
        public string Name { get; set; } = string.Empty;
        public CustomTypeWithSubproperties? Custom { get; set; }
    }

    private class CustomTypeWithSubproperties
    {
        public string SubA { get; set; } = string.Empty;
        public int SubB { get; set; }

        public override string ToString() => $"[CustomType: {SubA}-{SubB}]";
    }

    private class CircularNode
    {
        public string Name { get; set; } = string.Empty;
        public CircularNode? Next { get; set; }
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
