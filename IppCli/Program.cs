using IppCli.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IppCli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("ipp-cli");
            config.SetApplicationVersion(typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");

            config.SetExceptionHandler((ex, _) =>
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] [red]{Markup.Escape(ex.Message)}[/]");
                if (ex.InnerException != null)
                {
                    AnsiConsole.MarkupLine($"[dim red]Details: {Markup.Escape(ex.InnerException.Message)}[/]");
                }
            });

            // ==========================================
            // Top-Level Standard Operation Commands
            // ==========================================

            // Printer operations
            config.AddCommand<GetPrinterAttributesCommand>("get-printer-attributes").WithDescription("Get attributes of the target printer");
            config.AddCommand<GetPrinterSupportedValuesCommand>("get-printer-supported-values").WithDescription("Get supported values for printer attributes");
            config.AddCommand<PausePrinterCommand>("pause-printer").WithDescription("Pause printer scheduling and processing");
            config.AddCommand<ResumePrinterCommand>("resume-printer").WithDescription("Resume printer scheduling and processing");
            config.AddCommand<PurgeJobsCommand>("purge-jobs").WithDescription("Purge all jobs from the printer queue");
            config.AddCommand<IdentifyPrinterCommand>("identify-printer").WithDescription("Identify physical printer");
            config.AddCommand<EnablePrinterCommand>("enable-printer").WithDescription("Enable printer to accept new jobs");
            config.AddCommand<DisablePrinterCommand>("disable-printer").WithDescription("Disable printer from accepting new jobs");
            config.AddCommand<ActivatePrinterCommand>("activate-printer").WithDescription("Activate printer");
            config.AddCommand<DeactivatePrinterCommand>("deactivate-printer").WithDescription("Deactivate printer");
            config.AddCommand<RestartPrinterCommand>("restart-printer").WithDescription("Restart the printer");
            config.AddCommand<ShutdownPrinterCommand>("shutdown-printer").WithDescription("Shutdown the printer");
            config.AddCommand<StartupPrinterCommand>("startup-printer").WithDescription("Startup the printer");
            config.AddCommand<GetUserPrinterAttributesCommand>("get-user-printer-attributes").WithDescription("Get user printer attributes");
            config.AddCommand<CupsGetPrintersCommand>("cups-get-printers").WithDescription("Get all printers known to CUPS server");
            config.AddCommand<SetPrinterAttributesCommand>("set-printer-attributes").WithDescription("Set printer attributes");

            // Job operations
            config.AddCommand<PrintJobCommand>("print-job").WithDescription("Submit a print job with document data");
            config.AddCommand<CreateJobCommand>("create-job").WithDescription("Create an empty multi-document print job");
            config.AddCommand<GetJobAttributesCommand>("get-job-attributes").WithDescription("Get attributes of a specific job");
            config.AddCommand<GetJobsCommand>("get-jobs").WithDescription("List print jobs from the printer queue");
            config.AddCommand<CancelJobCommand>("cancel-job").WithDescription("Cancel a specific print job");
            config.AddCommand<CancelJobsCommand>("cancel-jobs").WithDescription("Cancel multiple print jobs");
            config.AddCommand<CancelMyJobsCommand>("cancel-my-jobs").WithDescription("Cancel all jobs submitted by current user");
            config.AddCommand<CancelCurrentJobCommand>("cancel-current-job").WithDescription("Cancel the currently printing job");
            config.AddCommand<HoldJobCommand>("hold-job").WithDescription("Hold a pending job from scheduling");
            config.AddCommand<HoldNewJobsCommand>("hold-new-jobs").WithDescription("Hold all newly submitted jobs");
            config.AddCommand<ReleaseJobCommand>("release-job").WithDescription("Release a previously held job");
            config.AddCommand<ReleaseHeldNewJobsCommand>("release-held-new-jobs").WithDescription("Release all held new jobs");
            config.AddCommand<RestartJobCommand>("restart-job").WithDescription("Restart a completed or retained job");
            config.AddCommand<ResubmitJobCommand>("resubmit-job").WithDescription("Resubmit an existing job");
            config.AddCommand<ValidateJobCommand>("validate-job").WithDescription("Validate if job attributes would be accepted");
            config.AddCommand<CloseJobCommand>("close-job").WithDescription("Close a multi-document job");
            config.AddCommand<PromoteJobCommand>("promote-job").WithDescription("Promote a job to the front of the queue");
            config.AddCommand<ResumeJobCommand>("resume-job").WithDescription("Resume a previously suspended job");
            config.AddCommand<SuspendCurrentJobCommand>("suspend-current-job").WithDescription("Suspend current printing job");
            config.AddCommand<SetJobAttributesCommand>("set-job-attributes").WithDescription("Set settable job attributes");

            // Document operations
            config.AddCommand<SendDocumentCommand>("send-document").WithDescription("Send a document stream to a created job");
            config.AddCommand<GetDocumentAttributesCommand>("get-document-attributes").WithDescription("Get document attributes");
            config.AddCommand<GetDocumentsCommand>("get-documents").WithDescription("List documents belonging to a job");
            config.AddCommand<CancelDocumentCommand>("cancel-document").WithDescription("Cancel a document");
            config.AddCommand<ValidateDocumentCommand>("validate-document").WithDescription("Validate document template attributes");
            config.AddCommand<SetDocumentAttributesCommand>("set-document-attributes").WithDescription("Set document attributes");

            // Subscription operations
            config.AddCommand<CreatePrinterSubscriptionsCommand>("create-printer-subscriptions").WithDescription("Create printer subscriptions");
            config.AddCommand<CreateJobSubscriptionsCommand>("create-job-subscriptions").WithDescription("Create job subscriptions");
            config.AddCommand<GetSubscriptionsCommand>("get-subscriptions").WithDescription("List active subscriptions");
            config.AddCommand<GetSubscriptionAttributesCommand>("get-subscription-attributes").WithDescription("Get subscription attributes");
            config.AddCommand<CancelSubscriptionCommand>("cancel-subscription").WithDescription("Cancel subscription");
            config.AddCommand<RenewSubscriptionCommand>("renew-subscription").WithDescription("Renew subscription");
            config.AddCommand<GetNotificationsCommand>("get-notifications").WithDescription("Get notifications for a subscription");

            // System operations
            config.AddCommand<GetSystemAttributesCommand>("get-system-attributes").WithDescription("Get system attributes");
            config.AddCommand<GetSystemSupportedValuesCommand>("get-system-supported-values").WithDescription("Get system supported values");
            config.AddCommand<GetPrintersCommand>("get-printers").WithDescription("Get printers from system");
            config.AddCommand<PauseAllPrintersCommand>("pause-all-printers").WithDescription("Pause all printers on the system");
            config.AddCommand<ResumeAllPrintersCommand>("resume-all-printers").WithDescription("Resume all printers on the system");
            config.AddCommand<EnableAllPrintersCommand>("enable-all-printers").WithDescription("Enable all printers on the system");
            config.AddCommand<DisableAllPrintersCommand>("disable-all-printers").WithDescription("Disable all printers on the system");
            config.AddCommand<ShutdownAllPrintersCommand>("shutdown-all-printers").WithDescription("Shutdown all printers on the system");
            config.AddCommand<StartupAllPrintersCommand>("startup-all-printers").WithDescription("Startup all printers on the system");
            config.AddCommand<RestartSystemCommand>("restart-system").WithDescription("Restart the IPP system");
            config.AddCommand<GetResourcesCommand>("get-resources").WithDescription("Get resources on the system");
            config.AddCommand<GetResourceAttributesCommand>("get-resource-attributes").WithDescription("Get resource attributes");
        });

        return await app.RunAsync(args);
    }
}
