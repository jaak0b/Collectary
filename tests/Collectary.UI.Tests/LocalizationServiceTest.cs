using CommunityToolkit.Mvvm.Messaging;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests;

[TestFixture]
public class LocalizationServiceTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Indexer_ReturnsEnglishString()
    {
        LocalizationService.Instance.Apply("en");
        Assert.That(LocalizationService.Instance["Save"], Is.EqualTo("Save"));
    }

    [Test]
    public void Apply_German_SwitchesStringsAndCode()
    {
        LocalizationService.Instance.Apply("de");
        Assert.That(LocalizationService.Instance.CurrentCode, Is.EqualTo("de"));
        Assert.That(LocalizationService.Instance["Save"], Is.EqualTo("Speichern"));
    }

    [Test]
    public void SettingsSectionHeaders_AreLocalizedInBothLanguages()
    {
        LocalizationService.Instance.Apply("en");
        Assert.That(LocalizationService.Instance["Settings_Appearance"], Is.EqualTo("Appearance"));
        Assert.That(LocalizationService.Instance["Settings_Language"], Is.EqualTo("Language"));

        LocalizationService.Instance.Apply("de");
        Assert.That(LocalizationService.Instance["Settings_Appearance"], Is.EqualTo("Darstellung"));
        Assert.That(LocalizationService.Instance["Settings_Language"], Is.EqualTo("Sprache"));
    }

    [Test]
    public void Indexer_FallsBackToKey_WhenMissing() =>
        Assert.That(LocalizationService.Instance["___no_such_key___"], Is.EqualTo("___no_such_key___"));

    [Test]
    public void Indexer_ResolvesKeyFromTemplateStringsResource()
    {
        LocalizationService.Instance.Apply("en");
        Assert.That(LocalizationService.Instance["Tmpl_books_Name"], Is.EqualTo("Books"));
    }

    [Test]
    public void Indexer_ResolvesTemplateKeyInGerman()
    {
        LocalizationService.Instance.Apply("de");
        Assert.That(LocalizationService.Instance["Tmpl_books_Name"], Is.EqualTo("Bücher"));
    }

    [Test]
    public void Indexer_ResolvesAcrossBothResourceFiles()
    {
        LocalizationService.Instance.Apply("en");
        Assert.That(LocalizationService.Instance["Save"], Is.EqualTo("Save"), "main Strings resource");
        Assert.That(LocalizationService.Instance["TemplateCategory_Collectibles"], Is.EqualTo("Collectibles"),
            "TemplateStrings resource");
    }

    [Test]
    public void ShareStrings_UseProfileTerminology_InEnglish()
    {
        LocalizationService.Instance.Apply("en");
        Assert.That(LocalizationService.Instance["Share_AddUser"], Is.EqualTo("Share with a profile"));
        Assert.That(LocalizationService.Instance["Share_Username"], Is.EqualTo("Profile name"));
        Assert.That(LocalizationService.Instance["Share_TransferTo"], Is.EqualTo("New owner's profile name"));
        Assert.That(LocalizationService.Instance["Share_UserNotFound"], Is.EqualTo("No profile with that name exists."));
    }

    [Test]
    public void ShareStrings_UseProfileTerminology_InGerman()
    {
        LocalizationService.Instance.Apply("de");
        Assert.That(LocalizationService.Instance["Share_AddUser"], Is.EqualTo("Mit einem Profil teilen"));
        Assert.That(LocalizationService.Instance["Share_Username"], Is.EqualTo("Profilname"));
        Assert.That(LocalizationService.Instance["Share_TransferTo"], Is.EqualTo("Profilname des neuen Eigentümers"));
        Assert.That(LocalizationService.Instance["Share_UserNotFound"], Is.EqualTo("Es existiert kein Profil mit diesem Namen."));
    }

    [Test]
    public void BarcodeCameraStrings_AreLocalized_InEnglish()
    {
        LocalizationService.Instance.Apply("en");
        Assert.Multiple(() =>
        {
            Assert.That(LocalizationService.Instance["Barcode_ScanFromFile"], Is.EqualTo("From file…"));
            Assert.That(LocalizationService.Instance["Barcode_ScanFromCamera"], Is.EqualTo("From camera…"));
            Assert.That(LocalizationService.Instance["Barcode_NoCameraAvailable"], Is.EqualTo("No camera available"));
            Assert.That(LocalizationService.Instance["Barcode_CameraScanner"], Is.EqualTo("Camera scanner"));
            Assert.That(LocalizationService.Instance["Barcode_CameraPermissionDenied"], Is.EqualTo("Camera access is needed to scan."));
            Assert.That(LocalizationService.Instance["Barcode_CameraStartFailed"], Is.EqualTo("The camera couldn't be started."));
            Assert.That(LocalizationService.Instance["Barcode_SwitchCamera"], Is.EqualTo("Switch camera"));
        });
    }

    [Test]
    public void BarcodeCameraStrings_AreLocalized_InGerman()
    {
        LocalizationService.Instance.Apply("de");
        Assert.Multiple(() =>
        {
            Assert.That(LocalizationService.Instance["Barcode_ScanFromFile"], Is.EqualTo("Aus Datei…"));
            Assert.That(LocalizationService.Instance["Barcode_ScanFromCamera"], Is.EqualTo("Mit Kamera…"));
            Assert.That(LocalizationService.Instance["Barcode_NoCameraAvailable"], Is.EqualTo("Keine Kamera verfügbar"));
            Assert.That(LocalizationService.Instance["Barcode_CameraScanner"], Is.EqualTo("Kamera-Scanner"));
            Assert.That(LocalizationService.Instance["Barcode_CameraPermissionDenied"], Is.EqualTo("Kamerazugriff wird zum Scannen benötigt."));
            Assert.That(LocalizationService.Instance["Barcode_CameraStartFailed"], Is.EqualTo("Die Kamera konnte nicht gestartet werden."));
            Assert.That(LocalizationService.Instance["Barcode_SwitchCamera"], Is.EqualTo("Kamera wechseln"));
        });
    }

    [Test]
    public void Apply_RaisesLanguageChangedAndPropertyChanged()
    {
        var languageChanged = false;
        string? changedProperty = "untouched";
        LocalizationService.Instance.LanguageChanged += Handler;
        LocalizationService.Instance.PropertyChanged += PropHandler;
        try
        {
            LocalizationService.Instance.Apply("de");
            Assert.That(languageChanged, Is.True);
            Assert.That(changedProperty, Is.Null.Or.Empty,
                "the notification must signal that all properties changed (null/empty name) so already-bound "
                + "indexer bindings like {Binding [Key]} refresh live — the WPF-style \"Item[]\" name is not "
                + "honoured by Avalonia's compiled bindings");
        }
        finally
        {
            LocalizationService.Instance.LanguageChanged -= Handler;
            LocalizationService.Instance.PropertyChanged -= PropHandler;
        }

        void Handler(object? s, EventArgs e) => languageChanged = true;
        void PropHandler(object? s, System.ComponentModel.PropertyChangedEventArgs e) => changedProperty = e.PropertyName;
    }

    [Test]
    public void Apply_BroadcastsLanguageChangedMessageToWeakRecipients()
    {
        var recipient = new object();
        var received = 0;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(recipient, (_, _) => received++);
        try
        {
            LocalizationService.Instance.Apply("de");
            Assert.That(received, Is.EqualTo(1));
        }
        finally
        {
            WeakReferenceMessenger.Default.Unregister<LanguageChangedMessage>(recipient);
            LocalizationService.Instance.Apply("en");
        }

        GC.KeepAlive(recipient);
    }
}
