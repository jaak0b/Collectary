using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels.ListCells;

public class RatingListCellViewModel : ListCellViewModelBase
{
    public IReadOnlyList<RatingStarViewModel> Stars { get; }

    public RatingListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        var value = (source as RatingFieldValue)?.Stars ?? 0;
        var maxStars = (definition as RatingFieldDefinition)?.MaxStars ?? 5;
        Stars = Enumerable.Range(1, maxStars)
            .Select(position => new RatingStarViewModel(position, position <= value))
            .ToList();
    }
}
