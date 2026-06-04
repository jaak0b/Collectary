using System.Text.Json;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Sync;

public class SyncSerializer : ISyncSerializer
{
    private readonly JsonSerializerOptions _options;

    public SyncSerializer()
    {
        _options = new JsonSerializerOptions
        {
            TypeInfoResolver = new PolymorphicFieldResolver(),
            WriteIndented = true,
        };
    }

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);

    public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options)!;
}
