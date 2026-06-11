using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public sealed record SyncKind(
    SyncEntityKind Kind,
    string WireString,
    Func<Task<IReadOnlyList<ISyncable>>> GetLocal,
    Func<ISyncable, string> Label,
    Func<ISyncable, string> Serialize,
    Func<string, ISyncable> Deserialize,
    Func<ISyncable, Task> Apply,
    Func<DeviceSnapshot, IEnumerable<ISyncable>> FromSnapshot,
    Action<DeviceSnapshot, IReadOnlyList<ISyncable>> IntoSnapshot);
