using Xunit;

namespace IppCli.Tests;

public class CliAppExecutionTests
{
    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    // Printer commands
    [InlineData("get-printer-attributes", "--help")]
    [InlineData("get-printer-supported-values", "--help")]
    [InlineData("pause-printer", "--help")]
    [InlineData("resume-printer", "--help")]
    [InlineData("purge-jobs", "--help")]
    [InlineData("identify-printer", "--help")]
    [InlineData("enable-printer", "--help")]
    [InlineData("disable-printer", "--help")]
    [InlineData("activate-printer", "--help")]
    [InlineData("deactivate-printer", "--help")]
    [InlineData("restart-printer", "--help")]
    [InlineData("shutdown-printer", "--help")]
    [InlineData("startup-printer", "--help")]
    [InlineData("get-user-printer-attributes", "--help")]
    [InlineData("cups-get-printers", "--help")]
    [InlineData("set-printer-attributes", "--help")]
    // Job commands
    [InlineData("print-job", "--help")]
    [InlineData("create-job", "--help")]
    [InlineData("get-job-attributes", "--help")]
    [InlineData("get-jobs", "--help")]
    [InlineData("cancel-job", "--help")]
    [InlineData("cancel-jobs", "--help")]
    [InlineData("cancel-my-jobs", "--help")]
    [InlineData("cancel-current-job", "--help")]
    [InlineData("hold-job", "--help")]
    [InlineData("hold-new-jobs", "--help")]
    [InlineData("release-job", "--help")]
    [InlineData("release-held-new-jobs", "--help")]
    [InlineData("restart-job", "--help")]
    [InlineData("resubmit-job", "--help")]
    [InlineData("validate-job", "--help")]
    [InlineData("close-job", "--help")]
    [InlineData("promote-job", "--help")]
    [InlineData("resume-job", "--help")]
    [InlineData("suspend-current-job", "--help")]
    [InlineData("set-job-attributes", "--help")]
    // Document commands
    [InlineData("send-document", "--help")]
    [InlineData("get-document-attributes", "--help")]
    [InlineData("get-documents", "--help")]
    [InlineData("cancel-document", "--help")]
    [InlineData("validate-document", "--help")]
    [InlineData("set-document-attributes", "--help")]
    // Subscription commands
    [InlineData("create-printer-subscriptions", "--help")]
    [InlineData("create-job-subscriptions", "--help")]
    [InlineData("get-subscriptions", "--help")]
    [InlineData("get-subscription-attributes", "--help")]
    [InlineData("cancel-subscription", "--help")]
    [InlineData("renew-subscription", "--help")]
    [InlineData("get-notifications", "--help")]
    // System commands
    [InlineData("get-system-attributes", "--help")]
    [InlineData("get-system-supported-values", "--help")]
    [InlineData("get-printers", "--help")]
    [InlineData("pause-all-printers", "--help")]
    [InlineData("resume-all-printers", "--help")]
    [InlineData("enable-all-printers", "--help")]
    [InlineData("disable-all-printers", "--help")]
    [InlineData("shutdown-all-printers", "--help")]
    [InlineData("startup-all-printers", "--help")]
    [InlineData("restart-system", "--help")]
    [InlineData("get-resources", "--help")]
    [InlineData("get-resource-attributes", "--help")]
    public async Task Program_Main_WithHelpArgs_ReturnsSuccess(params string[] args)
    {
        var result = await Program.Main(args);
        Assert.Equal(0, result);
    }
}
