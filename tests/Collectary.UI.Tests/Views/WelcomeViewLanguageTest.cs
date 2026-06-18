using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Presentation.Localization;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class WelcomeViewLanguageTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    [Test]
    public void SwitchingLanguage_UpdatesAlreadyRenderedText_WithoutReconstructingTheView()
    {
        LocalizationService.Instance.Apply("en");

        var view = new WelcomeView();
        var window = new Window { Content = view, Width = 300, Height = 200 };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var label = (TextBlock)view.Content!;
            var english = label.Text;

            LocalizationService.Instance.Apply("de");
            Dispatcher.UIThread.RunJobs();

            Assert.That(label.Text, Is.Not.EqualTo(english),
                "an already-rendered localized binding must refresh when the language changes, without "
                + "rebuilding the view");
        }
        finally
        {
            window.Close();
        }
    }
}
