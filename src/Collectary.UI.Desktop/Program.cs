using System;
using Avalonia;
using Avalonia.Logging;
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
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace(LogEventLevel.Warning)
            .LogToTrace(LogEventLevel.Verbose, LogArea.Binding);
}
