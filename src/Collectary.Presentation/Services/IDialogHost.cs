using System.ComponentModel;
using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.Services;

public interface IDialogHost : INotifyPropertyChanged
{
    ViewModelBase? ActiveDialog { get; }

    bool HasActiveDialog { get; }
}
