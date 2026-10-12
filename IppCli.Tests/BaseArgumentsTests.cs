using IppCli.Models;

namespace IppCli.Tests;

[TestClass]
public class BaseArgumentsTests
{
    [TestMethod]
    [DataRow(new[] { "-o", "json" }, OutputFormat.Json)]
    [DataRow(new[] { "-o", "JSON" }, OutputFormat.Json)]
    [DataRow(new[] { "-o", "Json" }, OutputFormat.Json)]
    [DataRow(new[] { "--output", "json" }, OutputFormat.Json)]
    [DataRow(new[] { "--output", "JSON" }, OutputFormat.Json)]
    [DataRow(new[] { "--output", "Json" }, OutputFormat.Json)]
    [DataRow(new[] { "get-printer-attributes", "ipp://localhost", "-o", "json" }, OutputFormat.Json)]
    [DataRow(new[] { "get-printer-attributes", "-o", "json", "ipp://localhost" }, OutputFormat.Json)]
    [DataRow(new[] { "get-printer-attributes", "--output", "json", "ipp://localhost" }, OutputFormat.Json)]
    public void Parse_JsonVariations_ReturnsJson(string[] args, OutputFormat expected)
    {
        var result = BaseArguments.Parse(args);
        Assert.AreEqual(expected, result.Output);
        Assert.AreEqual(expected, BaseArguments.GetOutputFormat(args));
    }

    [TestMethod]
    [DataRow(new[] { "-o", "tree" }, OutputFormat.Tree)]
    [DataRow(new[] { "-o", "TREE" }, OutputFormat.Tree)]
    [DataRow(new[] { "-o", "Tree" }, OutputFormat.Tree)]
    [DataRow(new[] { "--output", "tree" }, OutputFormat.Tree)]
    [DataRow(new[] { "--output", "TREE" }, OutputFormat.Tree)]
    [DataRow(new[] { "--output", "Tree" }, OutputFormat.Tree)]
    [DataRow(new[] { "get-printer-attributes", "ipp://localhost", "-o", "tree" }, OutputFormat.Tree)]
    [DataRow(new[] { "get-printer-attributes", "--output", "tree", "ipp://localhost" }, OutputFormat.Tree)]
    public void Parse_TreeVariations_ReturnsTree(string[] args, OutputFormat expected)
    {
        var result = BaseArguments.Parse(args);
        Assert.AreEqual(expected, result.Output);
        Assert.AreEqual(expected, BaseArguments.GetOutputFormat(args));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(new string[0])]
    [DataRow(new[] { "get-printer-attributes", "ipp://localhost" })]
    [DataRow(new[] { "-o" })]
    [DataRow(new[] { "--output" })]
    [DataRow(new[] { "-o", "--insecure" })]
    [DataRow(new[] { "--output", "--insecure" })]
    [DataRow(new[] { "-o", "unknown" })]
    [DataRow(new[] { "--output", "unknown" })]
    [DataRow(new[] { "-o=json" })]
    [DataRow(new[] { "-o:json" })]
    [DataRow(new[] { "--output=json" })]
    [DataRow(new[] { "--output:json" })]
    [DataRow(new[] { "-o=tree" })]
    [DataRow(new[] { "--output=tree" })]
    [DataRow(new[] { "--output=custom" })]
    public void Parse_DefaultOrInvalid_ReturnsTree(string[]? args)
    {
        var result = BaseArguments.Parse(args);
        Assert.AreEqual(OutputFormat.Tree, result.Output);
        Assert.AreEqual(OutputFormat.Tree, BaseArguments.GetOutputFormat(args));
    }
}
