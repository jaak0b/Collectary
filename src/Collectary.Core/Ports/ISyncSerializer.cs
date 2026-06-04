namespace Collectary.Core.Ports;

public interface ISyncSerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string content);
}
