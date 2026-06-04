using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Collectary.Presentation.Localization;

public class LocalizationService : INotifyPropertyChanged
{
    public static readonly LocalizationService Instance = new();

    private ResourceManager _rm = new("Collectary.UI.Localization.Strings.en",
        typeof(LocalizationService).Assembly);

    private string _currentCode = "en";
    public string CurrentCode => _currentCode;

    private LocalizationService() { }

    public string this[string key] =>
        _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public void Apply(string languageCode)
    {
        _currentCode = languageCode;
        var culture = new CultureInfo(languageCode);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;

        _rm = languageCode == "de"
            ? new ResourceManager("Collectary.UI.Localization.Strings.de",
                typeof(LocalizationService).Assembly)
            : new ResourceManager("Collectary.UI.Localization.Strings.en",
                typeof(LocalizationService).Assembly);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;
}
