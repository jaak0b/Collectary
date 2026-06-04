using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels;

public partial class ListDetailViewModel : ViewModelBase
{
    public ListFieldEditorViewModel List { get; }

    public string Label => List.Label;
    public IReadOnlyList<FieldDefinition> ColumnFields => List.ColumnFields;
    public ObservableCollection<ListEntryRowViewModel> EntryRows => List.EntryRows;

    public IRelayCommand<ListEntryRowViewModel> EditEntryCommand => List.EditEntryCommand;
    public IRelayCommand<ListEntryRowViewModel> DeleteEntryCommand => List.DeleteEntryCommand;
    public IRelayCommand AddEntryCommand => List.AddEntryCommand;
    public IAsyncRelayCommand SaveCommand => List.SaveCommand;
    public IAsyncRelayCommand SaveAndGoBackCommand => List.SaveAndGoBackCommand;
    public IRelayCommand GoBackCommand => List.GoBackCommand;

    public ListDetailViewModel(ListFieldEditorViewModel list, ItemEditingContext context)
    {
        List = list;
    }
}
