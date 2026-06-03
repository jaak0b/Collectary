namespace Collectary.UI.ViewModels;

public class BreadcrumbNode
{
    public string Title { get; }
    public ViewModelBase Content { get; }

    public BreadcrumbNode(string title, ViewModelBase content)
    {
        Title = title;
        Content = content;
    }
}
