using System.ComponentModel;
using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.Messaging;

namespace Collectary.Presentation.Localization;

public sealed class LanguageChangedMessage;

public class LocalizationService : INotifyPropertyChanged
{
    public static readonly LocalizationService Instance = new();

    private ResourceManager[] _managers;

    private string _currentCode = "en";
    public string CurrentCode => _currentCode;

    private LocalizationService() => _managers = BuildManagers("en");

    private ResourceManager[] BuildManagers(string languageCode)
    {
        var baseNames = new[]
        {
            "Collectary.UI.Localization.Strings",
            "Collectary.UI.Localization.TemplateStrings",
        };
        var suffix = languageCode == "de" ? ".de" : ".en";
        return baseNames
            .Select(b => new ResourceManager(b + suffix, typeof(LocalizationService).Assembly))
            .ToArray();
    }

    public string this[string key]
    {
        get
        {
            foreach (var manager in _managers)
            {
                var value = manager.GetString(key, CultureInfo.CurrentUICulture);
                if (value is not null) return value;
            }
            return key;
        }
    }

    public void Apply(string languageCode)
    {
        _currentCode = languageCode;
        var culture = new CultureInfo(languageCode);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;

        _managers = BuildManagers(languageCode);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        WeakReferenceMessenger.Default.Send(new LanguageChangedMessage());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;
}
