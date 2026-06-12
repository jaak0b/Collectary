using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

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
    public void TryImportFromText_RejectsInvertedRange()
    {
        var ok = ((ITextImportable)new DateRangeFieldDefinition()).TryImportFromText("12/31/2024 - 01/01/2024", new CultureInfo("en-US"), out _);
        Assert.That(ok, Is.False);
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

    [Test]
    public void TryCreateMatcher_Equals_MatchesWhenDayFallsInsideRange()
    {
        var def = new DateRangeFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["2025-06-15"], out var matcher, out _), Is.True);

        var item = new Item
        {
            Values = [new DateRangeFieldValue
            {
                FieldDefinitionId = def.Id,
                From = new DateTime(2025, 6, 1),
                To = new DateTime(2025, 6, 30),
            }],
        };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);

        ((DateRangeFieldValue)item.Values[0]).To = new DateTime(2025, 6, 10);
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_Greater_MatchesRangesStartingAfterTheDay()
    {
        var def = new DateRangeFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["2025-01-01"], out var matcher, out _), Is.True);

        var item = new Item
        {
            Values = [new DateRangeFieldValue
            {
                FieldDefinitionId = def.Id,
                From = new DateTime(2025, 2, 1),
                To = new DateTime(2025, 3, 1),
            }],
        };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);

        ((DateRangeFieldValue)item.Values[0]).From = new DateTime(2024, 12, 1);
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_EveryRangeOperator_ComparesAgainstTheDay()
    {
        var def = new DateRangeFieldDefinition();
        ISearchableFieldDefinition search = def;
        var item = new Item
        {
            Values = [new DateRangeFieldValue
            {
                FieldDefinitionId = def.Id,
                From = new DateTime(2025, 6, 1),
                To = new DateTime(2025, 6, 30),
            }],
        };
        var expectations = new (QueryOperatorKind Op, string Day, bool Matches)[]
        {
            (QueryOperatorKind.NotEquals, "2025-07-15", true),
            (QueryOperatorKind.NotEquals, "2025-06-15", false),
            (QueryOperatorKind.Less, "2025-07-01", true),
            (QueryOperatorKind.Less, "2025-06-30", false),
            (QueryOperatorKind.LessOrEqual, "2025-06-30", true),
            (QueryOperatorKind.GreaterOrEqual, "2025-06-01", true),
            (QueryOperatorKind.GreaterOrEqual, "2025-06-02", false),
        };
        foreach (var (op, day, expected) in expectations)
        {
            Assert.That(search.TryCreateMatcher(op, [day], out var matcher, out _), Is.True);
            Assert.That(matcher!.Matches(item, [def.Id]), Is.EqualTo(expected), $"{op} {day}");
            Assert.That(matcher.ServerFilter([def.Id])!.Compile()(item), Is.EqualTo(expected),
                $"{op} {day} must agree between SQL and memory");
        }
    }

    [Test]
    public void TryCreateMatcher_RangeBoundaries_AreInclusive()
    {
        var def = new DateRangeFieldDefinition();
        ISearchableFieldDefinition search = def;
        var item = new Item
        {
            Values = [new DateRangeFieldValue
            {
                FieldDefinitionId = def.Id,
                From = new DateTime(2025, 6, 10),
                To = new DateTime(2025, 6, 20),
            }],
        };
        var expectations = new (QueryOperatorKind Op, string Day, bool Matches)[]
        {
            (QueryOperatorKind.Equals, "2025-06-10", true),
            (QueryOperatorKind.Equals, "2025-06-20", true),
            (QueryOperatorKind.Equals, "2025-06-09", false),
            (QueryOperatorKind.Equals, "2025-06-21", false),
            (QueryOperatorKind.NotEquals, "2025-06-10", false),
            (QueryOperatorKind.NotEquals, "2025-06-21", true),
            (QueryOperatorKind.NotEquals, "2025-06-09", true),
            (QueryOperatorKind.Less, "2025-06-21", true),
            (QueryOperatorKind.LessOrEqual, "2025-06-20", true),
            (QueryOperatorKind.LessOrEqual, "2025-06-19", false),
            (QueryOperatorKind.Greater, "2025-06-09", true),
            (QueryOperatorKind.Greater, "2025-06-10", false),
            (QueryOperatorKind.GreaterOrEqual, "2025-06-10", true),
            (QueryOperatorKind.GreaterOrEqual, "2025-06-11", false),
        };
        foreach (var (op, day, expected) in expectations)
        {
            Assert.That(search.TryCreateMatcher(op, [day], out var matcher, out _), Is.True);
            Assert.That(matcher!.Matches(item, [def.Id]), Is.EqualTo(expected), $"{op} {day}");
            Assert.That(matcher.ServerFilter([def.Id])!.Compile()(item), Is.EqualTo(expected),
                $"{op} {day} (server)");
        }
    }

    [Test]
    public void TryCreateMatcher_OpenEndedRanges_MatchOnTheKnownEnd()
    {
        var def = new DateRangeFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["2025-06-15"], out var matcher, out _), Is.True);

        var fromOnly = new Item
        {
            Values = [new DateRangeFieldValue { FieldDefinitionId = def.Id, From = new DateTime(2025, 6, 1) }],
        };
        var toOnly = new Item
        {
            Values = [new DateRangeFieldValue { FieldDefinitionId = def.Id, To = new DateTime(2025, 6, 30) }],
        };
        foreach (var item in new[] { fromOnly, toOnly })
        {
            Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
            Assert.That(matcher.ServerFilter([def.Id])!.Compile()(item), Is.True);
        }

        var fromAfterDay = new Item
        {
            Values = [new DateRangeFieldValue { FieldDefinitionId = def.Id, From = new DateTime(2025, 7, 1) }],
        };
        Assert.That(matcher!.Matches(fromAfterDay, [def.Id]), Is.False);
        Assert.That(matcher.ServerFilter([def.Id])!.Compile()(fromAfterDay), Is.False);
    }

    [Test]
    public void TryCreateMatcher_EmptinessAndFailures_BehaveLikeOtherFields()
    {
        var def = new DateRangeFieldDefinition();
        ISearchableFieldDefinition search = def;

        Assert.That(search.TryCreateMatcher(QueryOperatorKind.IsEmpty, [], out var empty, out _), Is.True);
        Assert.That(empty!.Matches(new Item(), [def.Id]), Is.True);
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.IsNotEmpty, [], out var notEmpty, out _), Is.True);
        Assert.That(notEmpty!.Matches(new Item(), [def.Id]), Is.False);

        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["x"], out _, out var opError), Is.False);
        Assert.That(opError, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["junk"], out _, out var valueError), Is.False);
        Assert.That(valueError, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new DateRangeFieldDefinition();
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new DateRangeFieldValue { From = new DateTime(2025, 1, 1) }),
            Is.EqualTo(new DateTime(2025, 1, 1)));
        Assert.That(search.SortKey(new Item(), new DateRangeFieldValue { To = new DateTime(2025, 2, 1) }),
            Is.EqualTo(new DateTime(2025, 2, 1)));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
