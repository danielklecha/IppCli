using System.Net.Sockets;
using System.Security.Authentication;
using SharpIpp.Exceptions;
using SharpIpp.Protocol.Models;
using Spectre.Console.Testing;

namespace IppCli.Tests;

[TestClass]
public class ExceptionHandlerTests
{
    [TestMethod]
    public void HandleException_SingleException_RendersTypeAndMessage()
    {
        var testConsole = new TestConsole();
        var ex = new InvalidOperationException("Something went wrong");

        Program.HandleException(ex, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(InvalidOperationException):");
        StringAssert.Contains(output, "Something went wrong");
    }

    [TestMethod]
    public void HandleException_NestedException_RendersInnerChain()
    {
        var testConsole = new TestConsole();
        var inner = new SocketException(10061);
        var ex = new HttpRequestException("HTTP request failed", inner);

        Program.HandleException(ex, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(HttpRequestException):");
        StringAssert.Contains(output, "HTTP request failed");
        StringAssert.Contains(output, "Caused by (SocketException):");
        StringAssert.Contains(output, "Hint: Check that the printer URI and port are correct");
    }

    [TestMethod]
    public void HandleException_AggregateExceptionWithSingleInner_UnwrapsAndRenders()
    {
        var testConsole = new TestConsole();
        var inner = new TimeoutException("Operation timed out");
        var agg = new AggregateException(inner);

        Program.HandleException(agg, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(TimeoutException):");
        StringAssert.Contains(output, "Operation timed out");
        StringAssert.Contains(output, "Hint: The request timed out. You can increase the timeout with -t");
    }

    [TestMethod]
    public void HandleException_AggregateExceptionWithMultipleInners_RendersAll()
    {
        var testConsole = new TestConsole();
        var inner1 = new InvalidOperationException("First error");
        var inner2 = new ArgumentException("Second error");
        var agg = new AggregateException(inner1, inner2);

        Program.HandleException(agg, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(AggregateException):");
        StringAssert.Contains(output, "(InvalidOperationException):");
        StringAssert.Contains(output, "First error");
        StringAssert.Contains(output, "(ArgumentException):");
        StringAssert.Contains(output, "Second error");
    }

    [TestMethod]
    public void HandleException_SslCertificateError_RendersInsecureHint()
    {
        var testConsole = new TestConsole();
        var authEx = new AuthenticationException("The remote certificate is invalid according to the validation procedure.");
        var httpEx = new HttpRequestException("SSL connection could not be established", authEx);

        Program.HandleException(httpEx, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Hint: If the printer uses a self-signed certificate");
    }

    [TestMethod]
    public void HandleException_DebugModeEnabled_RendersStackTrace()
    {
        var testConsole = new TestConsole();
        var ex = new InvalidOperationException("Debug test exception");

        Environment.SetEnvironmentVariable("IPP_CLI_DEBUG", "1");
        try
        {
            Program.HandleException(ex, testConsole);
        }
        finally
        {
            Environment.SetEnvironmentVariable("IPP_CLI_DEBUG", null);
        }

        var output = testConsole.Output;
        StringAssert.Contains(output, "Debug test exception");
        // Spectre WriteException renders exception details
        StringAssert.Contains(output, "InvalidOperationException");
    }

    [TestMethod]
    public void HandleException_IppResponseException_RendersResponseMessage()
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

        Program.HandleException(ex, testConsole);

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
    public void HandleException_IppResponseException_WithNullResponseMessage_RendersWithoutError()
    {
        var testConsole = new TestConsole();
        var ex = new IppResponseException("An unexpected response error occurred");

        Program.HandleException(ex, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "(IppResponseException):");
        StringAssert.Contains(output, "An unexpected response error occurred");
    }

    [TestMethod]
    public void HandleException_NestedIppResponseException_RendersResponseMessage()
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

        Program.HandleException(outerEx, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(HttpRequestException):");
        StringAssert.Contains(output, "Caused by (IppResponseException):");
        StringAssert.Contains(output, "Server failure");
        StringAssert.Contains(output, "ResponseMessage");
        StringAssert.Contains(output, "ServerErrorInternalError");
        StringAssert.Contains(output, "Internal server error");
    }

    [TestMethod]
    public void HandleException_AggregateExceptionWithIppResponseException_RendersResponseMessage()
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

        Program.HandleException(agg, testConsole);

        var output = testConsole.Output;
        StringAssert.Contains(output, "(AggregateException):");
        StringAssert.Contains(output, "(IppResponseException):");
        StringAssert.Contains(output, "ResponseMessage");
        StringAssert.Contains(output, "ClientErrorBadRequest");
    }
}
