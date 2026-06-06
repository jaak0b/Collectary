using System.Globalization;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.UseCases.Import;

namespace Collectary.Core.Tests.UseCases.Import;

[TestFixture]
public class FieldTypeInferenceTest
{
    private readonly CultureInfo _invariant = CultureInfo.InvariantCulture;
    private readonly CultureInfo _de = new("de-DE");

    private WorkbookCell Text(string s) => new(s, WorkbookCellKind.Text);
    private WorkbookCell Number(string s) => new(s, WorkbookCellKind.Number);
    private WorkbookCell Blank() => new(null, WorkbookCellKind.Blank);

    [Test]
    public void Infer_AllIntegers_ReturnsInteger()
    {
        var column = new[] { Text("1"), Text("2"), Text("3") };
        Assert.That(new FieldTypeInference().Infer(column, _invariant), Is.TypeOf<IntegerFieldDefinition>());
    }

    [Test]
    public void Infer_AllDates_ReturnsDate()
    {
        var column = new[] { Text("2024-01-01"), Text("2024-02-15") };
        Assert.That(new FieldTypeInference().Infer(column, _invariant), Is.TypeOf<DateFieldDefinition>());
    }

    [Test]
    public void Infer_AllEmails_ReturnsEmail()
    {
        var column = new[] { Text("a@b.com"), Text("c@d.org") };
        Assert.That(new FieldTypeInference().Infer(column, _invariant), Is.TypeOf<EmailFieldDefinition>());
    }

    [Test]
    public void Infer_MixedValues_FallsBackToText()
    {
        var column = new[] { Text("abc"), Text("123") };
        Assert.That(new FieldTypeInference().Infer(column, _invariant), Is.TypeOf<TextFieldDefinition>());
    }

    [Test]
    public void Infer_AllBlank_ReturnsText()
    {
        var column = new[] { Blank(), Blank() };
        Assert.That(new FieldTypeInference().Infer(column, _invariant), Is.TypeOf<TextFieldDefinition>());
    }

    [Test]
    public void Infer_TypedNumberCells_ParseInvariantNotSourceCulture()
    {
        var column = new[] { Number("1234.56"), Number("99.10") };
        Assert.That(new FieldTypeInference().Infer(column, _de), Is.TypeOf<DecimalFieldDefinition>());
    }
}
