using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Persistence;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.Mapping;
using Collectary.Presentation.ViewModels.SystemFields;
using Microsoft.EntityFrameworkCore;

namespace Collectary.UI.Tests.Infrastructure;

[TestFixture]
public abstract class FlowTestBase
{
    private string _dbPath = null!;
    private DbContextOptions<InventoryDbContext> _options = null!;

    protected IPresetRepository PresetRepo { get; private set; } = null!;
    protected IItemRepository ItemRepo { get; private set; } = null!;
    protected ISystemFieldRepository SystemFieldRepo { get; private set; } = null!;
    protected IPresetUseCase PresetUseCase { get; private set; } = null!;
    protected IItemUseCase ItemUseCase { get; private set; } = null!;
    protected ISystemFieldUseCase SystemFieldUseCase { get; private set; } = null!;
    protected IFieldEditorMapper Mapper { get; private set; } = null!;
    protected TestFieldEditorRegistry EditorRegistry { get; private set; } = null!;
    protected IListCellBuilder CellBuilder { get; private set; } = null!;

    [SetUp]
    public void SetUpFlow()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"collectary-test-{Guid.NewGuid()}.db");
        _options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_dbPath};Pooling=False")
            .Options;

        using (var db = CreateDb())
            db.Database.EnsureCreated();

        var merger = new FieldDefinitionMerger();
        PresetRepo = new PresetRepository(CreateDb, merger);
        ItemRepo = new ItemRepository(CreateDb);
        SystemFieldRepo = new SystemFieldRepository(CreateDb, merger);

        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.CanWriteAsync(A<Guid>._)).Returns(true);
        A.CallTo(() => auth.CanReadAsync(A<Guid>._)).Returns(true);
        A.CallTo(() => auth.IsOwnerAsync(A<Guid>._)).Returns(true);
        PresetUseCase = new PresetUseCase(PresetRepo, ItemRepo, auth);
        ItemUseCase = new ItemUseCase(ItemRepo, PresetUseCase, auth);
        SystemFieldUseCase = new SystemFieldUseCase(SystemFieldRepo);

        Mapper = new TestFieldEditorMapper().Create();
        EditorRegistry = new TestFieldEditorRegistry();
        CellBuilder = A.Fake<IListCellBuilder>();
    }

    [TearDown]
    public void TearDownFlow()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
            if (File.Exists(path)) File.Delete(path);
    }

    private InventoryDbContext CreateDb() => new(_options);

    protected PresetEditorViewModel MakePresetEditorVm(Preset? existing = null, Action? onSaved = null, Action? onCancelled = null, Preset? seed = null)
    {
        A.CallTo(() => CellBuilder.HasListCellViewModel(A<Type>._)).Returns(true);
        return new PresetEditorViewModel(
            PresetUseCase,
            SystemFieldUseCase,
            A.Fake<Collectary.Presentation.Services.IDialogService>(),
            Mapper,
            onSaved: onSaved ?? (() => { }),
            onCancelled: onCancelled ?? (() => { }),
            existing: existing,
            seed: seed);
    }

    protected ItemEditingContext MakeItemContext(Func<Task>? saveAsync = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: EditorRegistry,
            listCellBuilder: CellBuilder,
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (saveAsync is not null) ctx.SaveAsync = saveAsync;
        return ctx;
    }

    protected ItemEditorViewModel MakeItemEditorVm(
        Preset preset,
        EffectiveFields effectiveFields,
        Item? existing = null,
        Action? onSaved = null,
        Action? onCancelled = null)
    {
        var ctx = MakeItemContext();
        var vm = new ItemEditorViewModel(
            ItemUseCase,
            PresetUseCase,
            preset,
            effectiveFields,
            onSaved: onSaved ?? (() => { }),
            onCancelled: onCancelled ?? (() => { }),
            context: ctx,
            existing: existing);
        ctx.SaveAsync = vm.PersistAsync;
        return vm;
    }

    protected static void SetDisplayName(ItemEditorViewModel vm, string name)
    {
        var dnEditor = vm.FieldEditors.OfType<DisplayNameFieldEditorViewModel>().FirstOrDefault();
        if (dnEditor is not null)
            dnEditor.Text = name;
        else
            vm.DisplayName = name;
    }

    protected static SystemFieldRowViewModel MakeSystemFieldRow(string label)
    {
        var def = new Collectary.Core.Domain.Fields.TextFieldDefinition { Label = label };
        var sf = new SystemField { Name = label, Definition = def };
        def.SystemFieldId = sf.Id;
        return new SystemFieldRowViewModel(sf);
    }
}
