using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class RatingFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly RatingFieldDefinition _definition;
    private readonly RatingFieldValue _fieldValue;

    [ObservableProperty]
    public partial int Stars { get; set; }

    [ObservableProperty]
    public partial int? HoverStars { get; set; }

    public int MaxStars => _definition.MaxStars;

    public ObservableCollection<RatingStarViewModel> StarItems { get; } = new();

    public RatingFieldEditorViewModel(RatingFieldDefinition definition, RatingFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Stars = value.Stars ?? 0;
        for (var position = 1; position <= _definition.MaxStars; position++)
            StarItems.Add(new RatingStarViewModel(position, position <= Stars));
    }

    public override FieldDefinition Definition => _definition;

    public void SetRating(int position) => Stars = position == Stars ? 0 : position;

    public void PreviewRating(int position) => HoverStars = position;

    public void ClearPreview() => HoverStars = null;

    partial void OnStarsChanged(int value) => RefreshLitStars();

    partial void OnHoverStarsChanged(int? value) => RefreshLitStars();

    private void RefreshLitStars()
    {
        var litThreshold = HoverStars ?? Stars;
        foreach (var star in StarItems)
            star.IsLit = star.Position <= litThreshold;
    }

    public override void Randomize(Services.ISampleData data) => Stars = data.Int(1, _definition.MaxStars);

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Stars = Stars > 0 ? Stars : null;
        return _fieldValue;
    }
}
