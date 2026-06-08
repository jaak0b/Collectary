using Avalonia.Threading;
using Collectary.Presentation.Services;

namespace Collectary.UI.Services;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
