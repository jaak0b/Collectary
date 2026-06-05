using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;

namespace Collectary.Presentation.ViewModels;

public partial class ItemEditingContext : ObservableObject
{
    public IFieldEditorRegistry EditorRegistry { get; }
    public IListCellBuilder ListCellBuilder { get; }

    [ObservableProperty]
    public partial bool IsNarrow { get; set; }

    /// <summary>App-wide default label layout, used to resolve a preset's null (inherit) choice.</summary>
    public FieldLabelLayout GlobalFieldLabelLayout { get; set; } = FieldLabelLayout.Adaptive;

    /// <summary>Resolved label placement for every editor built within this editing session.</summary>
    public bool LabelAbove { get; set; }
    public Func<Task> SaveAsync { get; set; } = () => Task.CompletedTask;

    /// <summary>Acquires a still image from the camera/file and decodes a barcode from it. Default: no scanner available.</summary>
    public Func<Task<BarcodeReadResult?>> ScanBarcodeAsync { get; set; } = () => Task.FromResult<BarcodeReadResult?>(null);

    /// <summary>Renders text as a QR-code bitmap for preview. Default: no generator available.</summary>
    public Func<string, Bitmap?> GenerateQrBitmap { get; set; } = _ => null;

    /// <summary>Picks a document and stores it, returning its blob key and original file name. Default: no-op.</summary>
    public Func<Task<(string Key, string FileName)?>> PickAndStoreFileAsync { get; set; }
        = () => Task.FromResult<(string, string)?>(null);

    /// <summary>Exports a stored document back out to a user-chosen location. Default: no-op.</summary>
    public Func<string, string, Task> ExportFileAsync { get; set; } = (_, _) => Task.CompletedTask;

    /// <summary>Deletes a stored document blob. Default: no-op.</summary>
    public Func<string, Task> DeleteFileAsync { get; set; } = _ => Task.CompletedTask;

    /// <summary>Loads the items that a link field may point at. Default: none available.</summary>
    public Func<Task<IReadOnlyList<LinkedItemOption>>> LoadLinkableItemsAsync { get; set; }
        = () => Task.FromResult<IReadOnlyList<LinkedItemOption>>(Array.Empty<LinkedItemOption>());

    /// <summary>Records an audio note, returning its blob key and length in seconds. Default: no microphone.</summary>
    public Func<Task<(string Key, int DurationSeconds)?>> RecordAudioAsync { get; set; }
        = () => Task.FromResult<(string, int)?>(null);

    /// <summary>Plays back a stored audio note. Default: no-op.</summary>
    public Func<string, Task> PlayAudioAsync { get; set; } = _ => Task.CompletedTask;
    public Action<ListFieldEditorViewModel> OpenList { get; set; } = _ => { };
    public Action<ListEntryEditorViewModel, string> OpenEntry { get; set; } = (_, _) => { };
    public Action GoBack { get; }
    public Func<Task<(string Key, string FileName, Bitmap Preview)?>> PickAndStoreImageAsync { get; }
    public Func<string, string, Task> ExportImageAsync { get; }
    public Func<string, Bitmap?> LoadImageBitmap { get; }
    public Func<string, Task> DeleteImageAsync { get; }

    public ItemEditingContext(
        IFieldEditorRegistry editorRegistry,
        IListCellBuilder listCellBuilder,
        Action goBack,
        Func<Task<(string Key, string FileName, Bitmap Preview)?>> pickAndStoreImageAsync,
        Func<string, string, Task> exportImageAsync,
        Func<string, Bitmap?> loadImageBitmap,
        Func<string, Task> deleteImageAsync)
    {
        EditorRegistry = editorRegistry;
        ListCellBuilder = listCellBuilder;
        GoBack = goBack;
        PickAndStoreImageAsync = pickAndStoreImageAsync;
        ExportImageAsync = exportImageAsync;
        LoadImageBitmap = loadImageBitmap;
        DeleteImageAsync = deleteImageAsync;
    }
}
