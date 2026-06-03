using Collectary.Core.Ports;

namespace Collectary.UI.Services;

public sealed class SerilogAppLogger : IAppLogger
{
    public void Verbose(string messageTemplate, params object?[] propertyValues) =>
        AppLogger.Log.Verbose(messageTemplate, propertyValues);

    public void Debug(string messageTemplate, params object?[] propertyValues) =>
        AppLogger.Log.Debug(messageTemplate, propertyValues);

    public void Information(string messageTemplate, params object?[] propertyValues) =>
        AppLogger.Log.Information(messageTemplate, propertyValues);

    public void Warning(string messageTemplate, params object?[] propertyValues) =>
        AppLogger.Log.Warning(messageTemplate, propertyValues);

    public void Error(Exception exception, string messageTemplate, params object?[] propertyValues) =>
        AppLogger.Log.Error(exception, messageTemplate, propertyValues);
}
