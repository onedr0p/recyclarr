using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Common;
using Recyclarr.Compatibility;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public class SyncProcessor(
    IAnsiConsole console,
    ILogger log,
    IConfigurationRegistry configRegistry,
    SyncPipelineExecutor pipelineExecutor,
    ServiceAgnosticCapabilityEnforcer capabilityEnforcer,
    ConsoleExceptionHandler exceptionHandler,
    NotificationService notify)
    : ISyncProcessor
{
    public async Task<ExitStatus> Process(ISyncSettings settings)
    {
        notify.BeginWatchEvents();
        var result = await ProcessConfigs(settings);
        await notify.SendNotification(result != ExitStatus.Failed);
        return result;
    }

    private async Task<ExitStatus> ProcessConfigs(ISyncSettings settings)
    {
        bool failureDetected;
        try
        {
            var configs = configRegistry.FindAndLoadConfigs(new ConfigFilterCriteria
            {
                ManualConfigFiles = settings.Configs,
                Instances = settings.Instances,
                Service = settings.Service
            });

            failureDetected = await ProcessService(settings, configs);
        }
        catch (Exception e)
        {
            if (!await exceptionHandler.HandleException(e))
            {
                // This means we didn't handle the exception; rethrow it.
                throw;
            }

            failureDetected = true;
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }

    private async Task<bool> ProcessService(ISyncSettings settings, IEnumerable<IServiceConfiguration> configs)
    {
        var failureDetected = false;

        foreach (var config in configs)
        {
            try
            {
                notify.SetInstanceName(config.InstanceName);
                PrintProcessingHeader(config.ServiceType, config);
                await capabilityEnforcer.Check(config);
                await pipelineExecutor.Process(settings, config);
            }
            catch (Exception e)
            {
                if (!await exceptionHandler.HandleException(e))
                {
                    // This means we didn't handle the exception; rethrow it.
                    throw;
                }

                failureDetected = true;
            }
        }

        return failureDetected;
    }

    private void PrintProcessingHeader(SupportedServices serviceType, IServiceConfiguration config)
    {
        var instanceName = config.InstanceName;

        console.WriteLine(
            $"""

             ===========================================
             Processing {serviceType} Server: [{instanceName}]
             ===========================================

             """);

        log.Debug("Processing {Server} server {Name}", serviceType, instanceName);
    }
}
