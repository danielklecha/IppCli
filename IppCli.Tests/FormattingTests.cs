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
using Xunit;

namespace IppCli.Tests;

public class FormattingTests
{
    [Fact]
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
        Assert.NotNull(json);
        Assert.Contains("\"statusCode\": \"SuccessfulOk\"", json);
        Assert.Contains("\"printerName\": \"OfficeLaserJet\"", json);
        Assert.Contains("\"printerState\": \"Idle\"", json);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("SuccessfulOk", doc.RootElement.GetProperty("statusCode").GetString());
    }

    [Theory]
    [InlineData(OutputFormat.Tree)]
    [InlineData(OutputFormat.Json)]
    public void OutputFormatter_FormatResponse_ExecutesWithoutError(OutputFormat format)
    {
        var response = new PausePrinterResponse
        {
            Version = new IppVersion(2, 0),
            RequestId = 1,
            StatusCode = IppStatusCode.SuccessfulOk
        };

        var exception = Record.Exception(() => OutputFormatter.FormatResponse("Pause-Printer", response, format));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(OutputFormat.Tree, typeof(ConsoleTreeRenderer))]
    [InlineData(OutputFormat.Json, typeof(JsonOutputRenderer))]
    public void OutputFormatter_GetRenderer_ReturnsExpectedRendererType(OutputFormat format, Type expectedType)
    {
        var renderer = OutputFormatter.GetRenderer(format);
        Assert.NotNull(renderer);
        Assert.IsType(expectedType, renderer);
    }

    [Fact]
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
        Assert.Contains("Get-Printer-Attributes", output);
        Assert.Contains("PrinterAttributes", output);
        Assert.Contains("PrinterName: OfficeLaserJet", output);
        Assert.Contains("PrinterState: Idle", output);
        Assert.Contains("PrinterIsAcceptingJobs: true", output);
        Assert.Contains("OperationAttributes", output);
        Assert.Contains("AttributesCharset: utf-8", output);
        Assert.Contains("RequestId: 42", output);
        Assert.Contains("StatusCode: SuccessfulOk", output);
        Assert.Contains("Version: 2.0", output);
    }

    [Fact]
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
        Assert.Contains("Get-Jobs", output);
        Assert.Contains("JobsAttributes", output);
        Assert.Contains("JobId: 5", output);
        Assert.Contains("JobName: Report.pdf", output);
        Assert.Contains("JobState: Completed", output);
        Assert.Contains("JobOriginatingUserName: bob", output);
        Assert.Contains("JobMediaSheetsCompleted: 10", output);
    }

    [Fact]
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
        Assert.Contains("Sample-Tree", output);
        Assert.Contains("Title: Test Model", output);
        Assert.Contains("Count: 42", output);
        Assert.Contains("Nested", output);
        Assert.Contains("Description: Deeply nested", output);
        Assert.Contains("Tags", output);
        Assert.Contains("tag1", output);
        Assert.Contains("tag2", output);
    }

    [Fact]
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

        Assert.Contains("Custom", outputWithout);
        Assert.Contains("SubA: Hello", outputWithout);
        Assert.Contains("SubB: 100", outputWithout);

        // 2. With leafTypes including CustomTypeWithSubproperties: rendered via ToString()
        var consoleWith = new TestConsole();
        consoleWith.Profile.Width = 200;

        ConsoleTreeRenderer.Render(model, "WithLeafTypes", new[] { typeof(CustomTypeWithSubproperties) }, consoleWith);
        var outputWith = consoleWith.Output;

        Assert.Contains("Custom: [CustomType: Hello-100]", outputWith);
        Assert.DoesNotContain("SubA: Hello", outputWith);
        Assert.DoesNotContain("SubB: 100", outputWith);
    }

    [Fact]
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
        Assert.Contains("Version: 2.1", output);
        // IppVersion should not be expanded to Major/Minor
        Assert.DoesNotContain("Major:", output);
        Assert.DoesNotContain("Minor:", output);
    }

    [Fact]
    public void ConsoleTreeRenderer_HandlesCircularReferenceGracefully()
    {
        var nodeA = new CircularNode { Name = "NodeA" };
        var nodeB = new CircularNode { Name = "NodeB", Next = nodeA };
        nodeA.Next = nodeB;

        var console = new TestConsole();
        console.Profile.Width = 200;

        var ex = Record.Exception(() => ConsoleTreeRenderer.Render(nodeA, "CircularTest", null, console));
        Assert.Null(ex);

        var output = console.Output;
        Assert.Contains("circular reference", output);
    }

    [Fact]
    public void ConsoleTreeRenderer_HandlesNullObjectGracefully()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render<SampleModel>(null, "NullTest", null, console);

        var output = console.Output;
        Assert.Contains("NullTest", output);
        Assert.Contains("null", output);
    }

    [Fact]
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
        Assert.Contains("Pause-Printer", output);
        Assert.Contains("RequestId: 7", output);
        Assert.Contains("StatusCode: SuccessfulOk", output);
        Assert.Contains("Version: 2.0", output);
    }

    [Fact]
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

        var ex = Record.Exception(() => ConsoleTreeRenderer.Render(model, "EmptyCollectionTest", null, console));
        Assert.Null(ex);

        var output = console.Output;
        Assert.Contains("EmptyList: []", output);
        Assert.Contains("EmptyArray: []", output);
    }

    [Fact]
    public void BaseRenderer_IsComplexType_IdentifiesTypesCorrectly()
    {
        Assert.False(BaseRenderer.IsComplexType(typeof(int)));
        Assert.False(BaseRenderer.IsComplexType(typeof(string)));
        Assert.False(BaseRenderer.IsComplexType(typeof(Uri)));
        Assert.False(BaseRenderer.IsComplexType(typeof(IppVersion)));
        Assert.False(BaseRenderer.IsComplexType(typeof(IppStatusCode)));
        Assert.False(BaseRenderer.IsComplexType(typeof(JobState)));
        Assert.False(BaseRenderer.IsComplexType(typeof(DateTime)));
        Assert.False(BaseRenderer.IsComplexType(typeof(byte[])));

        Assert.True(BaseRenderer.IsComplexType(typeof(JobDescriptionAttributes)));
        Assert.True(BaseRenderer.IsComplexType(typeof(TestItem)));
    }

    [Fact]
    public void ConsoleTreeRenderer_IsLeaf_SmartEnumRecognizedAsLeaf()
    {
        // Types
        Assert.True(ConsoleTreeRenderer.IsLeaf(typeof(JobState)));
        Assert.True(ConsoleTreeRenderer.IsLeaf(typeof(JobHoldUntil)));
        Assert.True(ConsoleTreeRenderer.IsLeaf(typeof(PrinterState)));

        // Instances
        Assert.True(ConsoleTreeRenderer.IsLeaf(JobState.Completed));
        Assert.True(ConsoleTreeRenderer.IsLeaf(JobHoldUntil.Indefinite));
        Assert.True(ConsoleTreeRenderer.IsLeaf(new JobHoldUntil("no-value", false)));
        Assert.True(ConsoleTreeRenderer.IsLeaf(new TestNoValueClass(false, "NoVal")));
        Assert.False(ConsoleTreeRenderer.IsLeaf(new TestNoValueClass(true, "HasVal")));
        Assert.True(ConsoleTreeRenderer.IsLeaf(null));
        Assert.True(ConsoleTreeRenderer.IsLeaf("string"));
        Assert.True(ConsoleTreeRenderer.IsLeaf(123));
    }

    [Fact]
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
        Assert.Contains("HoldUntil: no value", output);
        Assert.Contains("HoldUntilValid: indefinite", output);
        Assert.Contains("ResolutionNoVal: no value", output);
        Assert.Contains("CustomNoValue: no value", output);
        Assert.Contains("CustomWithValue", output);
        Assert.Contains("Content: Actual Value", output);
    }

    [Fact]
    public void ConsoleTreeRenderer_NoValue_RootObjectDisplaysGreyNoValue()
    {
        var root = new TestNoValueClass(false, "Secret");

        var console = new TestConsole();
        console.Profile.Width = 200;

        ConsoleTreeRenderer.Render(root, "RootNoValueTest", null, console);

        var output = console.Output;
        Assert.Contains("no value", output);
        Assert.DoesNotContain("Secret", output);
    }

    [Fact]
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
        Assert.Contains("Items (4): no value, no value, no value, indefinite", output);
    }

    [Fact]
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
        Assert.Contains("DocumentFormatSupported (3): application/pdf, image/urf, image/jpeg", output);
        Assert.Contains("SidesSupported (2): one-sided, two-sided-long-edge", output);
        Assert.Contains("PrinterStateReasons (1): none", output);
        Assert.DoesNotContain("[0]", output);
    }

    [Fact]
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
        Assert.Contains("MediaColReady (2)", output);
        Assert.Contains("[0]", output);
        Assert.Contains("[1]", output);
        Assert.Contains("MediaBottomMargin: 432", output);
        Assert.Contains("MediaBottomMargin: 200", output);
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
