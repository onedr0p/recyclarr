using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Platform;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

internal static class Program
{
    private static ILifetimeScope? _scope;
    private static IBaseCommandSetupTask[] _tasks = Array.Empty<IBaseCommandSetupTask>();
    private static ILogger? _log;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public static int Main(string[] args)
    {
        var builder = new ContainerBuilder();
        CompositionRoot.Setup(builder);

        var logLevelSwitch = new LoggingLevelSwitch();
        var appDataPathProvider = new AppDataPathProvider();
        CompositionRoot.RegisterExternal(builder, logLevelSwitch, appDataPathProvider);

        var app = new CommandApp(new AutofacTypeRegistrar(builder, s => _scope = s));
        app.Configure(config =>
        {
        #if DEBUG
            config.PropagateExceptions();
            config.ValidateExamples();
        #endif

            config.Settings.PropagateExceptions = true;
            config.Settings.StrictParsing = true;

            config.SetApplicationName("recyclarr");
            config.SetApplicationVersion(
                $"v{GitVersionInformation.SemVer} ({GitVersionInformation.FullBuildMetaData})");

            var interceptor = new CliInterceptor(logLevelSwitch, appDataPathProvider);
            interceptor.OnIntercepted.Subscribe(_ => OnAppInitialized());
            config.SetInterceptor(interceptor);

            CliSetup.Commands(config);
        });

        var result = 1;
        try
        {
            result = app.Run(args);
        }
        catch (Exception ex)
        {
            if (_log is null)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            }
            else
            {
                _log.Error(ex, "Non-recoverable Exception");
            }
        }
        finally
        {
            OnAppCleanup();
        }

        return result;
    }

    private static void OnAppInitialized()
    {
        if (_scope is null)
        {
            throw new InvalidProgramException("Composition root is not initialized");
        }

        _log = _scope.Resolve<ILogger>();
        _log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);

        var paths = _scope.Resolve<IAppPaths>();
        _log.Debug("App Data Dir: {AppData}", paths.AppDataDirectory);

        _tasks = _scope.Resolve<IOrderedEnumerable<IBaseCommandSetupTask>>().ToArray();
        _tasks.ForEach(x => x.OnStart());
    }

    private static void OnAppCleanup()
    {
        _tasks.Reverse().ForEach(x => x.OnFinish());
    }
}
