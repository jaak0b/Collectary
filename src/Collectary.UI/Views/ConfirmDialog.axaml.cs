using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Collectary.UI.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog() : this(string.Empty) { }

    public ConfirmDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        YesButton.Click += (_, _) => Close(true);
        NoButton.Click += (_, _) => Close(false);
    }
}
