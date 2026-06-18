using CommunityToolkit.Mvvm.ComponentModel;

namespace Collectary.Presentation.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif
}
