namespace Collectary.Presentation.Services;

public interface IUiDispatcher
{
    void Post(Action action);
}
