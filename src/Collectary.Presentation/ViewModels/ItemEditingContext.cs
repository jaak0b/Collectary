using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Presentation.DI;

namespace Collectary.Presentation.ViewModels;

public partial class ItemEditingContext : ObservableObject
{
    public IFieldEditorRegistry EditorRegistry { get; }
    public IListCellBuilder ListCellBuilder { get; }

    [ObservableProperty]
    public partial bool IsNarrow { get; set; }
    public Func<Task> SaveAsync { get; set; } = () => Task.CompletedTask;
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
