using System.Windows.Input;
using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.SharedFields;

public class SharedFieldRowViewModel
{
    public SharedField SharedField { get; }
    public string Name => SharedField.Name;
    public ICommand? AddToCollectionCommand { get; init; }

    public SharedFieldRowViewModel(SharedField sf) => SharedField = sf;
}
