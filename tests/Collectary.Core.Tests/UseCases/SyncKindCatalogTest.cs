using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class SyncKindCatalogTest
{
    private IReadOnlyList<SyncKind> Describe() =>
        new SyncKindCatalog().Describe(A.Fake<ISyncStore>(), A.Fake<ISyncSerializer>());

    [Test]
    public void Describe_CoversEverySyncEntityKind()
    {
        var kinds = Describe();

        Assert.That(kinds.Select(k => k.Kind), Is.EquivalentTo(Enum.GetValues<SyncEntityKind>()),
            "every SyncEntityKind must have exactly one catalog entry — adding an enum value without a row must fail here");
    }

    [Test]
    public void Describe_ListsWireStringsInOwnerBeforeReferencerOrder()
    {
        var kinds = Describe();

        Assert.That(kinds.Select(k => k.WireString),
            Is.EqualTo(new[] { "users", "sharedfields", "presets", "items", "shares" }),
            "owners (users) reconcile before presets; presets before items and shares");
    }

    [Test]
    public async Task Describe_EachKind_RoutesApplyToTheMatchingStoreMethod()
    {
        var store = A.Fake<ISyncStore>();
        var kinds = new SyncKindCatalog().Describe(store, A.Fake<ISyncSerializer>()).ToDictionary(k => k.Kind);
        var user = new User();
        var field = new SharedField { Definition = new TextFieldDefinition() };
        var preset = new Preset();
        var item = new Item();
        var share = new CollectionShare();

        await kinds[SyncEntityKind.User].Apply(user);
        await kinds[SyncEntityKind.SharedField].Apply(field);
        await kinds[SyncEntityKind.Preset].Apply(preset);
        await kinds[SyncEntityKind.Item].Apply(item);
        await kinds[SyncEntityKind.Share].Apply(share);

        A.CallTo(() => store.ApplyUserAsync(user)).MustHaveHappenedOnceExactly();
        A.CallTo(() => store.ApplySharedFieldAsync(field)).MustHaveHappenedOnceExactly();
        A.CallTo(() => store.ApplyPresetAsync(preset)).MustHaveHappenedOnceExactly();
        A.CallTo(() => store.ApplyItemAsync(item)).MustHaveHappenedOnceExactly();
        A.CallTo(() => store.ApplyShareAsync(share)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void Describe_EachKind_LabelsTheEntityWithItsOwnField()
    {
        var kinds = new SyncKindCatalog().Describe(A.Fake<ISyncStore>(), A.Fake<ISyncSerializer>()).ToDictionary(k => k.Kind);
        var presetId = Guid.NewGuid();

        Assert.Multiple(() =>
        {
            Assert.That(kinds[SyncEntityKind.User].Label(new User { Username = "alice" }), Is.EqualTo("alice"));
            Assert.That(kinds[SyncEntityKind.SharedField].Label(new SharedField { Name = "field", Definition = new TextFieldDefinition() }), Is.EqualTo("field"));
            Assert.That(kinds[SyncEntityKind.Preset].Label(new Preset { Name = "coll" }), Is.EqualTo("coll"));
            Assert.That(kinds[SyncEntityKind.Item].Label(new Item { DisplayName = "thing" }), Is.EqualTo("thing"));
            Assert.That(kinds[SyncEntityKind.Share].Label(new CollectionShare { PresetId = presetId }), Is.EqualTo(presetId.ToString()));
        });
    }

    [Test]
    public async Task Describe_EachKind_RoutesGetLocalSerializeAndDeserializeThroughItsType()
    {
        var store = A.Fake<ISyncStore>();
        var serializer = A.Fake<ISyncSerializer>();
        var user = new User();
        var share = new CollectionShare();
        A.CallTo(() => store.GetAllUsersAsync()).Returns(new[] { user });
        A.CallTo(() => store.GetAllSharesAsync()).Returns(new[] { share });
        A.CallTo(() => serializer.Serialize(user)).Returns("U");
        A.CallTo(() => serializer.Deserialize<CollectionShare>("S")).Returns(share);
        var kinds = new SyncKindCatalog().Describe(store, serializer).ToDictionary(k => k.Kind);

        var loadedUsers = await kinds[SyncEntityKind.User].GetLocal();
        Assert.Multiple(() =>
        {
            Assert.That(loadedUsers, Has.One.SameAs(user));
            Assert.That(kinds[SyncEntityKind.User].Serialize(user), Is.EqualTo("U"));
            Assert.That(kinds[SyncEntityKind.Share].Deserialize("S"), Is.SameAs(share));
        });
    }
}
