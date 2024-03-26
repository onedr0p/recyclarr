using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Notifications;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatLogPhase(ILogger log, NotificationEmitter notify)
    : ILogPipelinePhase<CustomFormatPipelineContext>
{
    // Returning 'true' means to exit. 'false' means to proceed.
    public bool LogConfigPhaseAndExitIfNeeded(CustomFormatPipelineContext context)
    {
        if (context.InvalidFormats.Count != 0)
        {
            log.Warning("These Custom Formats do not exist in the guide and will be skipped: {Cfs}",
                context.InvalidFormats);
            notify.NotifyError($"Invalid Custom Formats: {context.InvalidFormats}");
        }

        if (context.ConfigOutput.Count == 0)
        {
            log.Debug("No custom formats to process");
            return true;
        }

        return false;
    }

    public void LogTransactionNotices(CustomFormatPipelineContext context)
    {
    }

    public void LogPersistenceResults(CustomFormatPipelineContext context)
    {
        context.LogTransactions(log);

        var totalCount = context.TransactionOutput.TotalCustomFormatChanges;
        if (totalCount > 0)
        {
            notify.NotifyStatistic("Total custom formats synced", totalCount.ToString());
        }
    }
}
