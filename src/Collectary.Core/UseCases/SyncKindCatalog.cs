using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public sealed class SyncKindCatalog
{
    public IReadOnlyList<SyncKind> Describe(ISyncStore store, ISyncSerializer serializer) => new[]
    {
        new SyncKind(SyncEntityKind.User, SyncService.UserKind,
            async () => (await store.GetAllUsersAsync()).Cast<ISyncable>().ToList(),
            e => ((User)e).Username,
            e => serializer.Serialize((User)e),
            c => serializer.Deserialize<User>(c),
            e => store.ApplyUserAsync((User)e)),

        new SyncKind(SyncEntityKind.SharedField, SyncService.SharedFieldKind,
            async () => (await store.GetAllSharedFieldsAsync()).Cast<ISyncable>().ToList(),
            e => ((SharedField)e).Name,
            e => serializer.Serialize((SharedField)e),
            c => serializer.Deserialize<SharedField>(c),
            e => store.ApplySharedFieldAsync((SharedField)e)),

        new SyncKind(SyncEntityKind.Preset, SyncService.PresetKind,
            async () => (await store.GetAllPresetsAsync()).Cast<ISyncable>().ToList(),
            e => ((Preset)e).Name,
            e => serializer.Serialize((Preset)e),
            c => serializer.Deserialize<Preset>(c),
            e => store.ApplyPresetAsync((Preset)e)),

        new SyncKind(SyncEntityKind.Item, SyncService.ItemKind,
            async () => (await store.GetAllItemsAsync()).Cast<ISyncable>().ToList(),
            e => ((Item)e).DisplayName,
            e => serializer.Serialize((Item)e),
            c => serializer.Deserialize<Item>(c),
            e => store.ApplyItemAsync((Item)e)),

        new SyncKind(SyncEntityKind.Share, SyncService.ShareKind,
            async () => (await store.GetAllSharesAsync()).Cast<ISyncable>().ToList(),
            e => ((CollectionShare)e).PresetId.ToString(),
            e => serializer.Serialize((CollectionShare)e),
            c => serializer.Deserialize<CollectionShare>(c),
            e => store.ApplyShareAsync((CollectionShare)e)),
    };
}
