using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using IppCli.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IppCli.Tests;

[TestClass]
public class GeneratorDiagnosticTests
{
    private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<GeneratedSourceResult> GeneratedSources) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(SharpIpp.Protocol.Models.IppVersion).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SharpIpp.Models.Requests.PrintJobRequest).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Spectre.Console.Cli.CommandSettings).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestCompilation",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new CliSettingsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        var runResult = driver.GetRunResult();
        var allDiagnostics = runResult.Diagnostics;
        var generatedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();

        return (allDiagnostics, generatedSources);
    }

    [TestMethod]
    public void ClassNotPartial_EmitsIPPCLI001()
    {
        var source = @"
using IppCli.Attributes;
using SharpIpp.Models.Requests;

namespace TestApp;

[GenerateCliSettings]
public class NonPartialSettings
{
    public PrintJobRequest Request { get; set; } = new();
}
";

        var (diagnostics, _) = RunGenerator(source);

        var warning = diagnostics.FirstOrDefault(d => d.Id == "IPPCLI001");
        Assert.IsNotNull(warning, "Expected IPPCLI001 diagnostic for non-partial class.");
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        StringAssert.Contains(warning.GetMessage(), "NonPartialSettings");
    }

    [TestMethod]
    public void PropertyNotFound_EmitsIPPCLI002()
    {
        var source = @"
using IppCli.Attributes;

namespace TestApp;

[GenerateCliSettings(""NonExistentRequest"")]
public partial class MissingPropSettings
{
}
";

        var (diagnostics, _) = RunGenerator(source);

        var warning = diagnostics.FirstOrDefault(d => d.Id == "IPPCLI002");
        Assert.IsNotNull(warning, "Expected IPPCLI002 diagnostic for missing property.");
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        StringAssert.Contains(warning.GetMessage(), "NonExistentRequest");
    }

    [TestMethod]
    public void InvalidMaxNestingDepth_EmitsIPPCLI004()
    {
        var source = @"
using IppCli.Attributes;
using SharpIpp.Models.Requests;

namespace TestApp;

[GenerateCliSettings(maxNestingDepth: -1)]
public partial class NegativeDepthSettings
{
    public PrintJobRequest Request { get; set; } = new();
}
";

        var (diagnostics, _) = RunGenerator(source);

        var warning = diagnostics.FirstOrDefault(d => d.Id == "IPPCLI004");
        Assert.IsNotNull(warning, "Expected IPPCLI004 diagnostic for negative nesting depth.");
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        StringAssert.Contains(warning.GetMessage(), "-1");
    }

    [TestMethod]
    public void ValidPartialSettings_GeneratesCodeWithoutDiagnostics()
    {
        var source = @"
using IppCli.Attributes;
using SharpIpp.Models.Requests;

namespace TestApp;

[GenerateCliSettings]
public partial class ValidSettings
{
    public PrintJobRequest Request { get; set; } = new();
}
";

        var (diagnostics, generatedSources) = RunGenerator(source);

        var generatorWarnings = diagnostics.Where(d => d.Id.StartsWith("IPPCLI")).ToList();
        Assert.AreEqual(0, generatorWarnings.Count, $"Expected 0 IPPCLI diagnostics but found: {string.Join(", ", generatorWarnings.Select(d => d.GetMessage()))}");
        Assert.IsTrue(generatedSources.Any(s => s.HintName.Contains("ValidSettings.g.cs")), "Expected ValidSettings.g.cs to be generated.");
    }
}
