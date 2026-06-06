using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DateRangeFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_ParsesRangeWithSeparator()
    {
        var ok = ((ITextImportable)new DateRangeFieldDefinition()).TryImportFromText("01/01/2024 - 12/31/2024", new CultureInfo("en-US"), out var v);
        Assert.That(ok, Is.True);
        var range = (DateRangeFieldValue)v;
        Assert.That(range.From, Is.EqualTo(new DateTime(2024, 1, 1)));
        Assert.That(range.To, Is.EqualTo(new DateTime(2024, 12, 31)));
    }

    [Test]
    public void TryImportFromText_RejectsSingleDate()
    {
        var ok = ((ITextImportable)new DateRangeFieldDefinition()).TryImportFromText("01/01/2024", new CultureInfo("en-US"), out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new DateRangeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<DateRangeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new DateRangeFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
