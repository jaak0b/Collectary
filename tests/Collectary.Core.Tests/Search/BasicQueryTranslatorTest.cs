using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class BasicQueryTranslatorTest
{
    private BasicQueryTranslator _translator = null!;

    [SetUp]
    public void SetUp() => _translator = new BasicQueryTranslator(
        new QueryParser(new QueryLexer()), new QueryTextWriter());

    [Test]
    public void TryFromText_SingleEqualsCondition_YieldsOneRow()
    {
        var model = _translator.TryFromText("Status = open");

        Assert.That(model, Is.Not.Null);
        var row = model!.Rows.Single();
        Assert.That(row.Field, Is.EqualTo("Status"));
        Assert.That(row.Operator, Is.EqualTo(QueryOperatorKind.Equals));
        Assert.That(row.Values, Is.EqualTo(new[] { "open" }));
        Assert.That(model.Sort, Is.Null);
    }

    [Test]
    public void TryFromText_FlatAndChain_YieldsRowsInOrder()
    {
        var model = _translator.TryFromText("name ~ loco AND Status in (open, done) AND Price = 3");

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Rows.Select(r => r.Field), Is.EqualTo(new[] { "name", "Status", "Price" }));
        Assert.That(model.Rows[0].Operator, Is.EqualTo(QueryOperatorKind.Contains));
        Assert.That(model.Rows[1].Operator, Is.EqualTo(QueryOperatorKind.In));
        Assert.That(model.Rows[1].Values, Is.EqualTo(new[] { "open", "done" }));
    }

    [Test]
    public void TryFromText_SingleOrderByField_YieldsSort()
    {
        var model = _translator.TryFromText("Status = open ORDER BY Name DESC");

        Assert.That(model!.Sort, Is.EqualTo(new BasicSort("Name", true)));
    }

    [Test]
    public void TryFromText_EmptyText_YieldsEmptyModel()
    {
        var model = _translator.TryFromText("   ");

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Rows, Is.Empty);
        Assert.That(model.Sort, Is.Null);
    }

    [Test]
    public void TryFromText_OrderByWithoutFilter_YieldsSortOnly()
    {
        var model = _translator.TryFromText("ORDER BY Name");

        Assert.That(model!.Rows, Is.Empty);
        Assert.That(model.Sort, Is.EqualTo(new BasicSort("Name", false)));
    }

    [TestCase("a = 1 OR b = 2")]
    [TestCase("NOT a = 1")]
    [TestCase("a not in (1, 2)")]
    [TestCase("(a = 1 OR b = 2) AND c = 3")]
    [TestCase("a > 1")]
    [TestCase("a >= 1")]
    [TestCase("a < 1")]
    [TestCase("a <= 1")]
    [TestCase("a != 1")]
    [TestCase("a !~ x")]
    [TestCase("a is empty")]
    [TestCase("a is not empty")]
    [TestCase("a = 1 AND a = 2")]
    [TestCase("a = 1 ORDER BY x, y")]
    [TestCase("a = ")]
    public void TryFromText_QueriesBeyondBasicMode_ReturnNull(string text)
    {
        Assert.That(_translator.TryFromText(text), Is.Null);
    }

    [Test]
    public void TryFromText_QuotedValueContainingComma_IsAccepted()
    {
        var model = _translator.TryFromText("preset = \"Smith, John\"");

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Rows.Single().Values, Is.EqualTo(new[] { "Smith, John" }));
    }

    [Test]
    public void TryFromText_InListWithQuotedCommaValue_KeepsOperandsSeparate()
    {
        var model = _translator.TryFromText("a in (x, \"y,z\")");

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Rows.Single().Values, Is.EqualTo(new[] { "x", "y,z" }));
    }

    [Test]
    public void ToText_ValuesContainingCommas_RoundTripUnchanged()
    {
        var text = _translator.ToText(new BasicQueryModel
        {
            Rows = [new BasicConditionRow("a", QueryOperatorKind.In, ["x", "y,z"])],
        });

        Assert.That(text, Is.EqualTo("a in (x, \"y,z\")"));
        var reparsed = _translator.TryFromText(text);
        Assert.That(reparsed!.Rows.Single().Values, Is.EqualTo(new[] { "x", "y,z" }));
    }

    [Test]
    public void TryFromText_DuplicateFieldLabels_AreRejectedCaseInsensitively()
    {
        Assert.That(_translator.TryFromText("Status = a AND STATUS = b"), Is.Null);
    }

    [Test]
    public void ToText_RowsAndSort_SerializeCanonically()
    {
        var model = new BasicQueryModel
        {
            Rows =
            [
                new BasicConditionRow("name", QueryOperatorKind.Contains, ["loco"]),
                new BasicConditionRow("Status", QueryOperatorKind.In, ["open", "in progress"]),
                new BasicConditionRow("Price", QueryOperatorKind.Equals, ["3"]),
            ],
            Sort = new BasicSort("Name", true),
        };

        Assert.That(_translator.ToText(model),
            Is.EqualTo("name ~ loco AND Status in (open, \"in progress\") AND Price = 3 ORDER BY Name DESC"));
    }

    [Test]
    public void ToText_EmptyModel_ReturnsEmptyString()
    {
        Assert.That(_translator.ToText(new BasicQueryModel()), Is.EqualTo(""));
    }

    [Test]
    public void ToText_SortOnly_WritesJustTheOrderClause()
    {
        var model = new BasicQueryModel { Sort = new BasicSort("Name", false) };

        Assert.That(_translator.ToText(model), Is.EqualTo("ORDER BY Name"));
    }

    [Test]
    public void RoundTrip_ModelToTextToModel_IsStructurallyIdentical()
    {
        var model = new BasicQueryModel
        {
            Rows =
            [
                new BasicConditionRow("Print run", QueryOperatorKind.Equals, ["two words"]),
                new BasicConditionRow("Tags", QueryOperatorKind.In, ["rare", "mint"]),
            ],
            Sort = new BasicSort("Print run", true),
        };

        var reloaded = _translator.TryFromText(_translator.ToText(model));

        Assert.That(reloaded, Is.Not.Null);
        Assert.That(
            reloaded!.Rows.Select(r => (r.Field, r.Operator, string.Join("|", r.Values))),
            Is.EqualTo(model.Rows.Select(r => (r.Field, r.Operator, string.Join("|", r.Values)))));
        Assert.That(reloaded.Sort, Is.EqualTo(model.Sort));
    }
}
