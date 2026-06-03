namespace Collectary.Core.Ports;

public interface IAppLogger
{
    void Verbose(string messageTemplate, params object?[] propertyValues);
    void Debug(string messageTemplate, params object?[] propertyValues);
    void Information(string messageTemplate, params object?[] propertyValues);
    void Warning(string messageTemplate, params object?[] propertyValues);
    void Error(Exception exception, string messageTemplate, params object?[] propertyValues);
}
