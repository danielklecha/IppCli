namespace IppCli.Tests;

[TestClass]
public class CliAppExecutionTests
{
    [TestMethod]
    [DataRow("--help")]
    [DataRow("--version")]
    // Printer commands
    [DataRow("get-printer-attributes", "--help")]
    [DataRow("get-printer-supported-values", "--help")]
    [DataRow("pause-printer", "--help")]
    [DataRow("resume-printer", "--help")]
    [DataRow("purge-jobs", "--help")]
    [DataRow("identify-printer", "--help")]
    [DataRow("enable-printer", "--help")]
    [DataRow("disable-printer", "--help")]
    [DataRow("activate-printer", "--help")]
    [DataRow("deactivate-printer", "--help")]
    [DataRow("restart-printer", "--help")]
    [DataRow("shutdown-printer", "--help")]
    [DataRow("startup-printer", "--help")]
    [DataRow("get-user-printer-attributes", "--help")]
    [DataRow("cups-get-printers", "--help")]
    [DataRow("set-printer-attributes", "--help")]
    // Job commands
    [DataRow("print-job", "--help")]
    [DataRow("create-job", "--help")]
    [DataRow("get-job-attributes", "--help")]
    [DataRow("get-jobs", "--help")]
    [DataRow("cancel-job", "--help")]
    [DataRow("cancel-jobs", "--help")]
    [DataRow("cancel-my-jobs", "--help")]
    [DataRow("cancel-current-job", "--help")]
    [DataRow("hold-job", "--help")]
    [DataRow("hold-new-jobs", "--help")]
    [DataRow("release-job", "--help")]
    [DataRow("release-held-new-jobs", "--help")]
    [DataRow("restart-job", "--help")]
    [DataRow("resubmit-job", "--help")]
    [DataRow("validate-job", "--help")]
    [DataRow("close-job", "--help")]
    [DataRow("promote-job", "--help")]
    [DataRow("resume-job", "--help")]
    [DataRow("suspend-current-job", "--help")]
    [DataRow("set-job-attributes", "--help")]
    // Document commands
    [DataRow("send-document", "--help")]
    [DataRow("get-document-attributes", "--help")]
    [DataRow("get-documents", "--help")]
    [DataRow("cancel-document", "--help")]
    [DataRow("validate-document", "--help")]
    [DataRow("set-document-attributes", "--help")]
    // Subscription commands
    [DataRow("create-printer-subscriptions", "--help")]
    [DataRow("create-job-subscriptions", "--help")]
    [DataRow("get-subscriptions", "--help")]
    [DataRow("get-subscription-attributes", "--help")]
    [DataRow("cancel-subscription", "--help")]
    [DataRow("renew-subscription", "--help")]
    [DataRow("get-notifications", "--help")]
    // System commands
    [DataRow("get-system-attributes", "--help")]
    [DataRow("get-system-supported-values", "--help")]
    [DataRow("get-printers", "--help")]
    [DataRow("pause-all-printers", "--help")]
    [DataRow("resume-all-printers", "--help")]
    [DataRow("enable-all-printers", "--help")]
    [DataRow("disable-all-printers", "--help")]
    [DataRow("shutdown-all-printers", "--help")]
    [DataRow("startup-all-printers", "--help")]
    [DataRow("restart-system", "--help")]
    [DataRow("get-resources", "--help")]
    [DataRow("get-resource-attributes", "--help")]
    public async Task Program_Main_WithHelpArgs_ReturnsSuccess(string arg1, string? arg2 = null)
    {
        var args = arg2 == null ? new[] { arg1 } : new[] { arg1, arg2 };
        var result = await Program.Main(args);
        Assert.AreEqual(0, result);
    }
}
