using Collectary.Core.Domain;

namespace Collectary.Core.UseCases;

public sealed class DeviceSnapshot
{
    public int FormatVersion { get; set; } = 1;
    public Guid DeviceId { get; set; }
    public List<User> Users { get; set; } = new();
    public List<SharedField> SharedFields { get; set; } = new();
    public List<Preset> Presets { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<CollectionShare> Shares { get; set; } = new();
    public List<Guid> Tombstones { get; set; } = new();
}
