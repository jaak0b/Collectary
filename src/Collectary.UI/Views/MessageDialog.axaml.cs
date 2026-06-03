using Avalonia.Controls;

namespace Collectary.UI.Views;

public partial class MessageDialog : Window
{
    public MessageDialog() : this(string.Empty, "Message") { }

    public MessageDialog(string message, string title)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        OkButton.Click += (_, _) => Close();
    }
}
