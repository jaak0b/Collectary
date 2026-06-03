using Collectary.Core.Ports;

namespace Collectary.Core.Logging;

public sealed class NullAppLogger : IAppLogger
{
    public void Verbose(string messageTemplate, params object?[] propertyValues) { }
    public void Debug(string messageTemplate, params object?[] propertyValues) { }
    public void Information(string messageTemplate, params object?[] propertyValues) { }
    public void Warning(string messageTemplate, params object?[] propertyValues) { }
    public void Error(Exception exception, string messageTemplate, params object?[] propertyValues) { }
}
