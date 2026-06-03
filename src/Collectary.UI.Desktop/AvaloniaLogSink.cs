using Avalonia.Logging;
using Serilog;
using AvaloniaLevel = Avalonia.Logging.LogEventLevel;
using SerilogLevel = Serilog.Events.LogEventLevel;

namespace Collectary.UI.Desktop;

public class AvaloniaLogSink : ILogSink
{
    private readonly ILogger _logger;

    public AvaloniaLogSink(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsEnabled(AvaloniaLevel level, string area) =>
        level >= AvaloniaLevel.Warning ||
        (area == LogArea.Binding && level >= AvaloniaLevel.Verbose);

    public void Log(AvaloniaLevel level, string area, object? source, string messageTemplate)
        => WriteToLog(level, area, source, messageTemplate);

    public void Log(AvaloniaLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        => WriteToLog(level, area, source, messageTemplate, propertyValues);

    private void WriteToLog(AvaloniaLevel avaloniaLevel, string area, object? source, string messageTemplate, object?[]? values = null)
    {
        var serilogLevel = avaloniaLevel switch
        {
            AvaloniaLevel.Verbose => SerilogLevel.Debug,
            AvaloniaLevel.Debug => SerilogLevel.Debug,
            AvaloniaLevel.Information => SerilogLevel.Information,
            AvaloniaLevel.Warning => SerilogLevel.Warning,
            AvaloniaLevel.Error => SerilogLevel.Error,
            _ => SerilogLevel.Fatal
        };
        var prefix = source is not null
            ? $"[Avalonia:{area}] ({source.GetType().Name}) "
            : $"[Avalonia:{area}] ";
        _logger.Write(serilogLevel, prefix + messageTemplate, values ?? []);
    }
}
