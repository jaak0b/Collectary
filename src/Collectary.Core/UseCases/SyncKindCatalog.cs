using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public sealed class SyncKindCatalog
{
    public IReadOnlyList<SyncKind> Describe(ISyncStore store, ISyncSerializer serializer)
    {
        SyncKind For<T>(
            SyncEntityKind kind, string wire,
            Func<Task<IReadOnlyList<T>>> getLocal,
            Func<T, string> label,
            Func<T, Task> apply,
            Func<DeviceSnapshot, List<T>> slot) where T : DomainObject, ISyncable => new(
            kind, wire,
            async () => (await getLocal()).Cast<ISyncable>().ToList(),
            e => label((T)e),
            e => serializer.Serialize((T)e),
            c => serializer.Deserialize<T>(c),
            e => apply((T)e),
            s => slot(s),
            (s, list) =>
            {
                var target = slot(s);
                target.Clear();
                target.AddRange(list.Cast<T>());
            });

        return new[]
        {
            For<User>(SyncEntityKind.User, SyncService.UserKind,
                store.GetAllUsersAsync, u => u.Username, store.ApplyUserAsync, s => s.Users),
            For<SharedField>(SyncEntityKind.SharedField, SyncService.SharedFieldKind,
                store.GetAllSharedFieldsAsync, sf => sf.Name, store.ApplySharedFieldAsync, s => s.SharedFields),
            For<Preset>(SyncEntityKind.Preset, SyncService.PresetKind,
                store.GetAllPresetsAsync, p => p.Name, store.ApplyPresetAsync, s => s.Presets),
            For<Item>(SyncEntityKind.Item, SyncService.ItemKind,
                store.GetAllItemsAsync, i => i.DisplayName, store.ApplyItemAsync, s => s.Items),
            For<CollectionShare>(SyncEntityKind.Share, SyncService.ShareKind,
                store.GetAllSharesAsync, sh => sh.PresetId.ToString(), store.ApplyShareAsync, s => s.Shares),
        };
    }
}
