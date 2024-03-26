using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;

namespace Recyclarr.Cli.Processors.Sync;

public class SyncPipelineExecutor(
    ILogger log,
    IOrderedEnumerable<ISyncPipeline> pipelines,
    IEnumerable<IPipelineCache> caches,
    NotificationEmitter emitter)
{
    public async Task Process(ISyncSettings settings, IServiceConfiguration config)
    {
        try
        {
            emitter.NotifyStatistic("Test statistic", "10");
            emitter.NotifyStatistic("Test statistic 2", "1060");
            emitter.NotifyError("Failure occurred");
            emitter.NotifyError("Another failure occurred");
            emitter.NotifyError("Another failure occurred 2");
            foreach (var cache in caches)
            {
                cache.Clear();
            }

            foreach (var pipeline in pipelines)
            {
                log.Debug("Executing Pipeline: {Pipeline}", pipeline.GetType().Name);
                await pipeline.Execute(settings, config);
            }

            log.Information("Completed at {Date}", DateTime.Now);
        }
        catch (Exception e)
        {
            emitter.NotifyError($"Exception: {e.Message}");
            throw;
        }
    }
}
