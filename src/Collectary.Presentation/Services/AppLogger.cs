using Serilog;
using Serilog.Events;

namespace Collectary.UI.Services;

public static class AppLogger
{
    public static ILogger Log => Serilog.Log.Logger;

    public static void Initialize(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "Collectary-.log");

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
#if DEBUG
            .WriteTo.Debug(
                outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
#endif
            .CreateLogger();
    }

    public static void CloseAndFlush() => Serilog.Log.CloseAndFlush();
}
