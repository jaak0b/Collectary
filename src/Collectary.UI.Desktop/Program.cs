using System;
using System.IO;
using Avalonia;
using Avalonia.Logging;
using Collectary.Infrastructure.Cloud;
using Collectary.Presentation.Services;

namespace Collectary.UI.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var logPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Collectary", "logs");
        AppLogger.Initialize(logPath);
        try
        {
            var builder = BuildAvaloniaApp();
            Avalonia.Logging.Logger.Sink = new AvaloniaLogSink(AppLogger.Log);
            builder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            AppLogger.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .AfterSetup(RegisterCloudModule)
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace(LogEventLevel.Warning)
            .LogToTrace(LogEventLevel.Verbose, LogArea.Binding);

    private static void RegisterCloudModule(AppBuilder builder)
    {
        if (builder.Instance is not App app) return;

        var cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Collectary");

        app.PlatformModules = new Autofac.Core.IModule[]
        {
            new CloudModule(cacheDirectory, () => AppPreferences.Load().OneDriveRootFolderId),
        };
    }
}
