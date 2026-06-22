using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ListCellViewModelsExtraTest
{
    [Test]
    public void Text_ReflectsValueToString()
    {
        var value = new TextFieldValue { Value = "hello" };
        var cell = new TextListCellViewModel(value, new TextFieldDefinition());
        Assert.That(cell.Text, Is.EqualTo("hello"));
    }

    [Test]
    public void Text_EmptyValue_IsEmptyString()
    {
        var cell = new TextListCellViewModel(new TextFieldValue { Value = null }, new TextFieldDefinition());
        Assert.That(cell.Text, Is.EqualTo(""));
    }

    [Test]
    public void Currency_WithValue_FormatsWithSymbolAndTwoDecimals()
    {
        var cell = new CurrencyListCellViewModel(
            new CurrencyFieldValue { Value = 12.5m },
            new CurrencyFieldDefinition { CurrencySymbol = "$" });
        Assert.That(cell.Display, Is.EqualTo($"$ {12.5m:F2}"));
    }

    [Test]
    public void Currency_NullValue_IsEmpty()
    {
        var cell = new CurrencyListCellViewModel(
            new CurrencyFieldValue { Value = null },
            new CurrencyFieldDefinition { CurrencySymbol = "$" });
        Assert.That(cell.Display, Is.EqualTo(""));
    }

    [Test]
    public void Currency_NonCurrencyDefinition_UsesEmptySymbol()
    {
        var cell = new CurrencyListCellViewModel(
            new CurrencyFieldValue { Value = 5m },
            new TextFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo($" {5m:F2}"));
    }

    [Test]
    public void Percentage_WithValue_FormatsOneDecimalWithSign()
    {
        var cell = new PercentageListCellViewModel(
            new PercentageFieldValue { Value = 42.5m }, new PercentageFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo($"{42.5m:F1} %"));
    }

    [Test]
    public void Percentage_NullValue_IsEmpty()
    {
        var cell = new PercentageListCellViewModel(
            new PercentageFieldValue { Value = null }, new PercentageFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo(""));
    }

    [Test]
    public void Duration_WithValue_ReflectsToString()
    {
        var value = new DurationFieldValue { TotalMinutes = 125 };
        var cell = new DurationListCellViewModel(value, new DurationFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo("2 h 05 min"));
    }

    [Test]
    public void Duration_NullValue_IsEmpty()
    {
        var cell = new DurationListCellViewModel(new DurationFieldValue { TotalMinutes = null }, new DurationFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo(""));
    }


    [Test]
    public void Tags_WithTags_JoinsThem()
    {
        var cell = new TagsListCellViewModel(new TagsFieldValue { Tags = { "a", "b" } }, new TagsFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo("a, b"));
    }

    [Test]
    public void Tags_Empty_IsEmpty()
    {
        var cell = new TagsListCellViewModel(new TagsFieldValue(), new TagsFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo(""));
    }

    [Test]
    public void Color_ValidHex_ProducesArgbSwatch()
    {
        var cell = new ColorListCellViewModel(
            new ColorFieldValue { Value = "#FF8000" },
            new ColorFieldDefinition { Format = ColorFormat.Hex });
        Assert.That(cell.SwatchHex, Is.EqualTo("#FFFF8000"));
    }

    [Test]
    public void Color_NullValue_IsTransparent()
    {
        var cell = new ColorListCellViewModel(
            new ColorFieldValue { Value = null },
            new ColorFieldDefinition { Format = ColorFormat.Hex });
        Assert.That(cell.SwatchHex, Is.EqualTo("#00FFFFFF"));
    }
}
