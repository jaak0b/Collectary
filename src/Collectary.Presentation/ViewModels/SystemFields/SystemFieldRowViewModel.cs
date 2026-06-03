using System.Windows.Input;
using Collectary.Core.Domain;

namespace Collectary.UI.ViewModels.SystemFields;

public class SystemFieldRowViewModel
{
    public SystemField SystemField { get; }
    public string Name => SystemField.Name;
    public ICommand? AddToCollectionCommand { get; init; }

    public SystemFieldRowViewModel(SystemField sf) => SystemField = sf;
}
