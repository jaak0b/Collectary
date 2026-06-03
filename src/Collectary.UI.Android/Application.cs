using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Collectary.UI.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<global::Collectary.UI.App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}
