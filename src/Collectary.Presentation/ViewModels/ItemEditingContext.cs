using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class ItemEditingContext : ObservableObject
{
    public IFieldEditorRegistry EditorRegistry { get; }
    public IListCellBuilder ListCellBuilder { get; }

    public ISampleData SampleData { get; set; } = new BogusSampleData();

    public IDialogService Dialogs { get; set; } = new NoopDialogService();

    [ObservableProperty]
    public partial bool IsNarrow { get; set; }

    /// <summary>App-wide default label layout, used to resolve a preset's null (inherit) choice.</summary>
    public FieldLabelLayout GlobalFieldLabelLayout { get; set; } = FieldLabelLayout.Adaptive;

    /// <summary>Resolved label placement for every editor built within this editing session.</summary>
    public bool LabelAbove { get; set; }

    /// <summary>
    /// Minimum width a field column needs before the grid keeps more than one. Beside-labelled fields
    /// place the label and input side by side, so they need more room than stacked ones; demanding it
    /// makes a beside layout fall back to a single full-width column on narrow/medium widths instead of
    /// squeezing inputs until they overflow their column.
    /// </summary>
    public double FieldMinColumnWidth => LabelAbove ? 200 : 360;
    public Func<Task> SaveAsync { get; set; } = () => Task.CompletedTask;

    /// <summary>Acquires a still image from the camera/file and decodes a barcode from it. Default: no scanner available.</summary>
    public Func<Task<BarcodeReadResult?>> ScanBarcodeAsync { get; set; } = () => Task.FromResult<BarcodeReadResult?>(null);

    /// <summary>Opens the live-camera scanner and resolves with the first decoded barcode, or null when cancelled. Default: no camera.</summary>
    public Func<Task<BarcodeReadResult?>> ScanBarcodeFromCameraAsync { get; set; } = () => Task.FromResult<BarcodeReadResult?>(null);

    /// <summary>Resolves whether a live camera with at least one device exists. Probed off the UI thread; default: none.</summary>
    public Func<Task<bool>> IsCameraScanAvailableAsync { get; set; } = () => Task.FromResult(false);

    /// <summary>Requests an OS permission at the moment a feature needs it, prompting the user the first time. Default: granted (desktop/browser gate access themselves).</summary>
    public Func<RuntimePermission, Task<bool>> RequestPermissionAsync { get; set; } = _ => Task.FromResult(true);

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

    public IAudioRecorder? AudioRecorder { get; set; }

    public IAudioPlayer? AudioPlayer { get; set; }

    /// <summary>The microphone the user picked in Settings, or null for the system default.</summary>
    public Func<string?> ResolveAudioInputDeviceId { get; set; } = () => null;

    /// <summary>The playback device the user picked in Settings, or null for the system default.</summary>
    public Func<string?> ResolveAudioOutputDeviceId { get; set; } = () => null;

    /// <summary>Opens the app Settings screen. Default: no-op (no host navigation available).</summary>
    public Action OpenSettings { get; set; } = () => { };

    public Func<Stream, Task<string>> StoreAudioAsync { get; set; } = _ => Task.FromResult(string.Empty);

    public Func<string, Stream?> OpenAudioStream { get; set; } = _ => null;
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
