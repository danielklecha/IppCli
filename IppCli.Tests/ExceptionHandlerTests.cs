using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using IppCli.Models;
using SharpIpp.Exceptions;
using SharpIpp.Protocol.Models;
using Spectre.Console;
using Spectre.Console.Testing;

namespace IppCli.Tests;

[TestClass]
public class ExceptionHandlerTests
{
    [TestMethod]
    public void WriteException_SingleException_RendersTypeAndMessage()
    {
        var testConsole = new TestConsole();
        var ex = new InvalidOperationException("Something went wrong");

        testConsole.WriteException(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(InvalidOperationException):");
        StringAssert.Contains(output, "Something went wrong");
    }

    [TestMethod]
    public void WriteException_IppRequestException_RendersRequestMessage()
    {
        var testConsole = new TestConsole();
        var requestMessage = new IppRequestMessage
        {
            Version = new IppVersion(2, 0),
            RequestId = 88,
            IppOperation = IppOperation.GetPrinterAttributes,
            OperationAttributes =
            {
                new(Tag.Charset, "attributes-charset", "utf-8")
            }
        };

        var ex = new IppRequestException("Invalid request", requestMessage, IppStatusCode.ClientErrorBadRequest);

        testConsole.WriteException(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(IppRequestException):");
        StringAssert.Contains(output, "Invalid request");
        StringAssert.Contains(output, "RequestMessage");
        StringAssert.Contains(output, "88");
        StringAssert.Contains(output, "GetPrinterAttributes");
        StringAssert.Contains(output, "attributes-charset");
    }


    [TestMethod]
    public void WriteException_ValidationException_RendersTypeAndMessage()
    {
        var testConsole = new TestConsole();
        var ex = new SharpIpp.Validation.ValidationException("Attribute is required");

        testConsole.WriteException(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(ValidationException):");
        StringAssert.Contains(output, "Attribute is required");
    }

    [TestMethod]
    public void WriteSharpIppDetails_UnrelatedException_RendersNothing()
    {
        var testConsole = new TestConsole();
        var ex = new InvalidOperationException("Not a SharpIpp exception");

        testConsole.WriteSharpIppDetails(ex);

        Assert.AreEqual(string.Empty, testConsole.Output);
    }

    [TestMethod]
    public void WriteException_NestedException_RendersInnerChainWithoutHints()
    {
        var testConsole = new TestConsole();
        var inner = new SocketException(10061);
        var ex = new HttpRequestException("HTTP request failed", inner);

        testConsole.WriteException(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(HttpRequestException):");
        StringAssert.Contains(output, "HTTP request failed");
        StringAssert.Contains(output, "(SocketException):");
        Assert.IsFalse(output.Contains("Caused by"));
        Assert.IsFalse(output.Contains("Hint:"));
    }

    [TestMethod]
    public void WriteException_AggregateExceptionWithSingleInner_RendersAggregateAndInnerWithoutHints()
    {
        var testConsole = new TestConsole();
        var inner = new TimeoutException("Operation timed out");
        var agg = new AggregateException(inner);

        testConsole.WriteException(agg);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(AggregateException):");
        StringAssert.Contains(output, "(TimeoutException):");
        StringAssert.Contains(output, "Operation timed out");
        Assert.IsFalse(output.Contains("Hint:"));
    }

    [TestMethod]
    public void WriteException_AggregateExceptionWithMultipleInners_RendersAll()
    {
        var testConsole = new TestConsole();
        var inner1 = new InvalidOperationException("First error");
        var inner2 = new ArgumentException("Second error");
        var agg = new AggregateException(inner1, inner2);

        testConsole.WriteException(agg);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(AggregateException):");
        StringAssert.Contains(output, "(InvalidOperationException):");
        StringAssert.Contains(output, "First error");
        StringAssert.Contains(output, "(ArgumentException):");
        StringAssert.Contains(output, "Second error");
    }

    [TestMethod]
    public void WriteException_SslCertificateError_DoesNotRenderHints()
    {
        var testConsole = new TestConsole();
        var authEx = new AuthenticationException("The remote certificate is invalid according to the validation procedure.");
        var httpEx = new HttpRequestException("SSL connection could not be established", authEx);

        testConsole.WriteException(httpEx);

        var output = testConsole.Output;
        Assert.IsFalse(output.Contains("Hint:"));
    }

    [TestMethod]
    public void WriteException_DebugModeEnabled_RendersStackTrace()
    {
        var testConsole = new TestConsole();
        var ex = new InvalidOperationException("Debug test exception");

        Environment.SetEnvironmentVariable("IPP_CLI_DEBUG", "1");
        try
        {
            testConsole.WriteException(ex);
        }
        finally
        {
            Environment.SetEnvironmentVariable("IPP_CLI_DEBUG", null);
        }

        var output = testConsole.Output;
        StringAssert.Contains(output, "Debug test exception");
        StringAssert.Contains(output, "InvalidOperationException");
    }

    [TestMethod]
    public void WriteException_IppResponseException_RendersResponseMessage()
    {
        var testConsole = new TestConsole();
        var responseMessage = new IppResponseMessage
        {
            Version = new IppVersion(2, 0),
            StatusCode = IppStatusCode.ClientErrorNotFound,
            RequestId = 42,
            OperationAttributes =
            {
                new List<IppAttribute>
                {
                    new(Tag.Charset, "attributes-charset", "utf-8"),
                    new(Tag.TextWithoutLanguage, "status-message", "Printer not found")
                }
            }
        };

        var ex = new IppResponseException("Printer not found", responseMessage);

        testConsole.WriteException(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(IppResponseException):");
        StringAssert.Contains(output, "Printer not found");
        StringAssert.Contains(output, "ResponseMessage");
        StringAssert.Contains(output, "StatusCode");
        StringAssert.Contains(output, "ClientErrorNotFound");
        StringAssert.Contains(output, "RequestId");
        StringAssert.Contains(output, "42");
        StringAssert.Contains(output, "attributes-charset");
        StringAssert.Contains(output, "status-message");
    }

    [TestMethod]
    public void WriteException_IppResponseException_WithNullResponseMessage_RendersWithoutError()
    {
        var testConsole = new TestConsole();
        var ex = new IppResponseException("An unexpected response error occurred");

        testConsole.WriteException(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(IppResponseException):");
        StringAssert.Contains(output, "An unexpected response error occurred");
    }

    [TestMethod]
    public void WriteException_NestedIppResponseException_RendersResponseMessage()
    {
        var testConsole = new TestConsole();
        var responseMessage = new IppResponseMessage
        {
            Version = new IppVersion(2, 0),
            StatusCode = IppStatusCode.ServerErrorInternalError,
            RequestId = 101,
            OperationAttributes =
            {
                new List<IppAttribute>
                {
                    new(Tag.TextWithoutLanguage, "status-message", "Internal server error")
                }
            }
        };

        var innerEx = new IppResponseException("Server failure", responseMessage);
        var outerEx = new HttpRequestException("HTTP request failed", innerEx);

        testConsole.WriteException(outerEx);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(HttpRequestException):");
        StringAssert.Contains(output, "(IppResponseException):");
        Assert.IsFalse(output.Contains("Caused by"));
        StringAssert.Contains(output, "Server failure");
        StringAssert.Contains(output, "ResponseMessage");
        StringAssert.Contains(output, "ServerErrorInternalError");
        StringAssert.Contains(output, "Internal server error");
    }

    [TestMethod]
    public void WriteException_AggregateExceptionWithIppResponseException_RendersResponseMessage()
    {
        var testConsole = new TestConsole();
        var responseMessage = new IppResponseMessage
        {
            Version = new IppVersion(2, 0),
            StatusCode = IppStatusCode.ClientErrorBadRequest,
            RequestId = 77
        };

        var ippEx = new IppResponseException("Bad request", responseMessage);
        var otherEx = new InvalidOperationException("Other failure");
        var agg = new AggregateException(ippEx, otherEx);

        testConsole.WriteException(agg);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(AggregateException):");
        StringAssert.Contains(output, "(IppResponseException):");
        StringAssert.Contains(output, "ResponseMessage");
        StringAssert.Contains(output, "ClientErrorBadRequest");
    }

    [TestMethod]
    public void WriteHints_SslCertificateKeywordInMessage_RendersHint()
    {
        var testConsole = new TestConsole();
        var ex = new InvalidOperationException("The SSL certificate expired");

        testConsole.WriteHints(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Hint: If the printer uses a self-signed certificate");
    }

    [TestMethod]
    public void WriteHints_TimeoutException_RendersTimeoutHint()
    {
        var testConsole = new TestConsole();
        var ex = new TimeoutException("Timed out waiting for response");

        testConsole.WriteHints(ex);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Hint: The request timed out. You can increase the timeout with -t");
    }

    [TestMethod]
    public void WriteHints_SocketExceptionDeeplyNested_RendersSocketHint()
    {
        var testConsole = new TestConsole();
        var socketEx = new SocketException(10060);
        var innerEx = new IOException("IO failed", socketEx);
        var outerEx = new HttpRequestException("HTTP failed", innerEx);

        testConsole.WriteHints(outerEx);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Hint: Check that the printer URI and port are correct");
    }

    [TestMethod]
    public void WriteHints_UnrelatedException_RendersNoHints()
    {
        var testConsole = new TestConsole();
        var ex = new FormatException("Invalid number format");

        testConsole.WriteHints(ex);

        Assert.AreEqual(string.Empty, testConsole.Output);
    }

    [TestMethod]
    public void WriteException_ThrowsArgumentNullException_WhenArgumentsNull()
    {
        var testConsole = new TestConsole();
        Assert.ThrowsException<ArgumentNullException>(() => ((Spectre.Console.IAnsiConsole)null!).WriteException(new Exception()));
        Assert.ThrowsException<ArgumentNullException>(() => testConsole.WriteException(null!));
        Assert.ThrowsException<ArgumentNullException>(() => ((Spectre.Console.IAnsiConsole)null!).WriteHints(new Exception()));
        Assert.ThrowsException<ArgumentNullException>(() => testConsole.WriteHints(null!));
        Assert.ThrowsException<ArgumentNullException>(() => ((Spectre.Console.IAnsiConsole)null!).WriteExceptions(new[] { new Exception() }));
        Assert.ThrowsException<ArgumentNullException>(() => testConsole.WriteExceptions((IEnumerable<Exception>)null!));
    }

    [TestMethod]
    public void WriteExceptions_MultipleExceptions_RendersAllWithBullets()
    {
        var testConsole = new TestConsole();
        var ex1 = new InvalidOperationException("First failure");
        var ex2 = new ArgumentNullException("arg", "Param failure");

        testConsole.WriteExceptions(new Exception[] { ex1, ex2 });

        var output = testConsole.Output;
        StringAssert.Contains(output, "(InvalidOperationException):");
        StringAssert.Contains(output, "First failure");
        StringAssert.Contains(output, "(ArgumentNullException):");
        StringAssert.Contains(output, "Param failure");
    }

    [TestMethod]
    public void WriteExceptions_WithNestedExceptions_RendersCausalChain()
    {
        var testConsole = new TestConsole();
        var inner = new SocketException(10061);
        var ex = new HttpRequestException("Network failure", inner);

        testConsole.WriteExceptions(new[] { ex });

        var output = testConsole.Output;
        StringAssert.Contains(output, "(HttpRequestException):");
        StringAssert.Contains(output, "Network failure");
        StringAssert.Contains(output, "(SocketException):");
        Assert.IsFalse(output.Contains("Caused by"));
        Assert.IsFalse(output.Contains("Hint:"));
    }

    [TestMethod]
    public void WriteExceptions_EmptyCollection_RendersNothing()
    {
        var testConsole = new TestConsole();

        testConsole.WriteExceptions(Array.Empty<Exception>());

        Assert.AreEqual(string.Empty, testConsole.Output);
    }

    [TestMethod]
    public void CreateExceptionTree_ReturnsRenderableTreeWithRoot()
    {
        var ex = new InvalidOperationException("Tree test error");
        var tree = ex.CreateExceptionTree();

        Assert.IsNotNull(tree);
        var testConsole = new TestConsole();
        testConsole.Write(tree);
        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(InvalidOperationException):");
        StringAssert.Contains(output, "Tree test error");
    }

    [TestMethod]
    public void CreateExceptionsTree_EmptyCollection_ReturnsNull()
    {
        var tree = Array.Empty<Exception>().CreateExceptionsTree();
        Assert.IsNull(tree);
    }

    [TestMethod]
    public void WriteException_JsonFormat_IppResponseException_RendersResponseMessageAsJson()
    {
        var testConsole = new TestConsole();
        testConsole.Profile.Width = 2000;
        var responseMessage = new IppResponseMessage
        {
            Version = new IppVersion(2, 0),
            StatusCode = IppStatusCode.ClientErrorNotFound,
            RequestId = 42,
            OperationAttributes =
            {
                new List<IppAttribute>
                {
                    new(Tag.Charset, "attributes-charset", "utf-8"),
                    new(Tag.TextWithoutLanguage, "status-message", "Printer not found")
                }
            }
        };

        var ex = new IppResponseException("Printer not found", responseMessage);

        testConsole.WriteException(ex, OutputFormat.Json);

        var output = testConsole.Output;
        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;
        Assert.AreEqual("ClientErrorNotFound", root.GetProperty("statusCode").GetString());
        Assert.AreEqual(42, root.GetProperty("requestId").GetInt32());
        Assert.IsFalse(output.Contains("Hint:"));
    }

    [TestMethod]
    public void WriteException_JsonFormat_StandardException_RendersJsonError()
    {
        var testConsole = new TestConsole();
        testConsole.Profile.Width = 2000;
        var ex = new InvalidOperationException("Something went wrong");

        testConsole.WriteException(ex, OutputFormat.Json);

        var output = testConsole.Output;
        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;
        Assert.AreEqual("InvalidOperationException", root.GetProperty("error").GetString());
        Assert.AreEqual("Something went wrong", root.GetProperty("message").GetString());
        Assert.IsFalse(output.Contains("Hint:"));
    }

    [TestMethod]
    public void WriteException_JsonFormat_NestedException_RendersJsonWithInnerException()
    {
        var testConsole = new TestConsole();
        testConsole.Profile.Width = 2000;
        var inner = new SocketException(10061);
        var ex = new HttpRequestException("HTTP failed", inner);

        testConsole.WriteException(ex, OutputFormat.Json);

        var output = testConsole.Output;
        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;
        Assert.AreEqual("HttpRequestException", root.GetProperty("error").GetString());
        Assert.AreEqual("HTTP failed", root.GetProperty("message").GetString());
        var innerProp = root.GetProperty("innerException");
        Assert.AreEqual("SocketException", innerProp.GetProperty("error").GetString());
    }

    [TestMethod]
    public void WriteException_JsonFormat_DeeplyNestedException_RendersRecursiveInnerException()
    {
        var testConsole = new TestConsole();
        testConsole.Profile.Width = 2000;
        var inner2 = new SocketException(10061);
        var inner1 = new IOException("IO error", inner2);
        var ex = new HttpRequestException("HTTP failed", inner1);

        testConsole.WriteException(ex, OutputFormat.Json);

        var output = testConsole.Output;
        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;
        Assert.AreEqual("HttpRequestException", root.GetProperty("error").GetString());
        var lvl1 = root.GetProperty("innerException");
        Assert.AreEqual("IOException", lvl1.GetProperty("error").GetString());
        Assert.AreEqual("IO error", lvl1.GetProperty("message").GetString());
        var lvl2 = lvl1.GetProperty("innerException");
        Assert.AreEqual("SocketException", lvl2.GetProperty("error").GetString());
    }

    [TestMethod]
    public void WriteException_JsonFormat_AggregateException_RendersJsonWithInnerExceptions()
    {
        var testConsole = new TestConsole();
        testConsole.Profile.Width = 2000;
        var inner1 = new InvalidOperationException("First error");
        var inner2 = new ArgumentException("Second error");
        var agg = new AggregateException(inner1, inner2);

        testConsole.WriteException(agg, OutputFormat.Json);

        var output = testConsole.Output;
        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;
        Assert.AreEqual("AggregateException", root.GetProperty("error").GetString());
        var errors = root.GetProperty("innerExceptions");
        Assert.AreEqual(2, errors.GetArrayLength());
        Assert.AreEqual("InvalidOperationException", errors[0].GetProperty("error").GetString());
        Assert.AreEqual("First error", errors[0].GetProperty("message").GetString());
    }

    [TestMethod]
    public void WriteExceptions_JsonFormat_MultipleExceptions_RendersJsonArray()
    {
        var testConsole = new TestConsole();
        testConsole.Profile.Width = 2000;
        var ex1 = new InvalidOperationException("First");
        var ex2 = new ArgumentException("Second");

        testConsole.WriteExceptions(new Exception[] { ex1, ex2 }, OutputFormat.Json);

        var output = testConsole.Output;
        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;
        Assert.AreEqual(2, root.GetArrayLength());
        Assert.AreEqual("InvalidOperationException", root[0].GetProperty("error").GetString());
        Assert.AreEqual("ArgumentException", root[1].GetProperty("error").GetString());
    }
}
