using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

namespace Collectary.UI.Tests;

public class HeadlessTestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

[SetUpFixture]
public class AvaloniaSetup
{
    [OneTimeSetUp]
    public void InitializeAvalonia() =>
        HeadlessTestApp.BuildAvaloniaApp().SetupWithoutStarting();
}
