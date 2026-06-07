using Collectary.Core.Domain;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PresetRowViewModelTest
{
    private static PresetRowViewModel CreateRow() =>
        new(new Preset { Name = "P" }, itemCount: 0,
            onNavigate: () => { }, onEdit: () => { }, onDelete: () => Task.CompletedTask);

    [Test]
    public void IsDragging_TogglesAndRaisesPropertyChanged()
    {
        var row = CreateRow();
        Assert.That(((IDraggableRow)row).IsDragging, Is.False);
        var raised = false;
        row.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(PresetRowViewModel.IsDragging);

        ((IDraggableRow)row).IsDragging = true;

        Assert.That(row.IsDragging, Is.True);
        Assert.That(raised, Is.True);
    }
}
