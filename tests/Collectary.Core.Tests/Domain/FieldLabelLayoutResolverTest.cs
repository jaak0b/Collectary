using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class FieldLabelLayoutResolverTest
{
    private readonly FieldLabelLayoutResolver _resolver = new();

    [Test]
    public void Beside_IsNeverAbove([Values(1, 2, 3)] int columnCount) =>
        Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Beside, FieldLabelLayout.Above, columnCount), Is.False);

    [Test]
    public void Above_IsAlwaysAbove([Values(1, 2, 3)] int columnCount) =>
        Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Above, FieldLabelLayout.Beside, columnCount), Is.True);

    [Test]
    public void Adaptive_IsAbove_OnlyWhenMultiColumn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Adaptive, FieldLabelLayout.Beside, 1), Is.False);
            Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Adaptive, FieldLabelLayout.Beside, 2), Is.True);
        });
    }

    [Test]
    public void NullPreset_InheritsGlobalDefault()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_resolver.ResolveLabelAbove(null, FieldLabelLayout.Above, 1), Is.True);
            Assert.That(_resolver.ResolveLabelAbove(null, FieldLabelLayout.Beside, 2), Is.False);
            Assert.That(_resolver.ResolveLabelAbove(null, FieldLabelLayout.Adaptive, 2), Is.True);
        });
    }

    [Test]
    public void PresetValue_OverridesGlobalDefault() =>
        Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Beside, FieldLabelLayout.Above, 3), Is.False);

    [Test]
    public void Narrow_ForcesAbove_EvenWhenBeside() =>
        Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Beside, FieldLabelLayout.Beside, 1, isNarrow: true), Is.True);

    [Test]
    public void NotNarrow_KeepsBaseDecision() =>
        Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Beside, FieldLabelLayout.Beside, 1, isNarrow: false), Is.False);

    [Test]
    public void Narrow_DoesNotUnstack_WhenBaseIsAbove() =>
        Assert.That(_resolver.ResolveLabelAbove(FieldLabelLayout.Above, FieldLabelLayout.Above, 1, isNarrow: false), Is.True);
}

[TestFixture]
public class PresetFieldLabelLayoutDefaultTest
{
    [Test]
    public void Preset_FieldLabelLayout_DefaultsToNull() =>
        Assert.That(new Preset().FieldLabelLayout, Is.Null);
}
