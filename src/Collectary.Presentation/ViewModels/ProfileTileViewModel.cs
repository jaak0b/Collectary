using Avalonia.Media;
using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels;

public class ProfileTileViewModel : ViewModelBase
{
    private readonly IReadOnlyList<string> _palette =
    [
        "#E53935", "#8E24AA", "#3949AB", "#1E88E5", "#00897B",
        "#43A047", "#FB8C00", "#F4511E", "#6D4C41", "#546E7A",
    ];

    public User Profile { get; }

    public ProfileTileViewModel(User profile)
    {
        Profile = profile;
    }

    public string Name => Profile.DisplayName;

    public string Initial =>
        string.IsNullOrWhiteSpace(Profile.DisplayName)
            ? "?"
            : Profile.DisplayName.Trim()[..1].ToUpperInvariant();

    public IBrush AvatarBrush => new SolidColorBrush(Color.Parse(_palette[PaletteIndex()]));

    private int PaletteIndex()
    {
        var sum = 0;
        foreach (var ch in Profile.DisplayName)
            sum += ch;
        return sum % _palette.Count;
    }
}
