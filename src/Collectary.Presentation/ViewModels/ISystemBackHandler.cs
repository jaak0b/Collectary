namespace Collectary.Presentation.ViewModels;

public interface ISystemBackHandler
{
    Task<bool> HandleSystemBackAsync();
}
