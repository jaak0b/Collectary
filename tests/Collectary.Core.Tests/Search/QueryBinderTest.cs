using System.Globalization;
using System.Linq.Expressions;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class QueryBinderTest
{
    private QueryParser _parser = null!;
    private QueryBinder _binder = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new QueryParser(new QueryLexer());
        _binder = new QueryBinder(new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.InvariantCulture));
    }

    private sealed class StubMatcher : IFieldConditionMatcher
    {
        public Expression<Func<Item, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => null;
        public bool Matches(Item item, IReadOnlyCollection<Guid> definitionIds) => true;
    }

    private sealed class StubSearchableDefinition : FieldDefinition<TextFieldValue>, ISearchableFieldDefinition
    {
        public IReadOnlyList<QueryOperatorKind> SupportedOperators { get; init; } =
            new[] { QueryOperatorKind.Equals };
        public bool RejectOperand { get; init; }

        public IEnumerable<string> ValueSuggestions() => [];

        public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
            out IFieldConditionMatcher? matcher, out QueryErrorCode? error)
        {
            matcher = null;
            error = null;
            if (!SupportedOperators.Contains(op))
            {
                error = QueryErrorCode.OperatorNotSupported;
                return false;
            }
            if (RejectOperand)
            {
                error = QueryErrorCode.InvalidValue;
                return false;
            }
            matcher = new StubMatcher();
            return true;
        }

        public IComparable? SortKey(Item item, FieldValue? value) => (value as TextFieldValue)?.Value;
    }

    private sealed class NonSearchableDefinition : FieldDefinition<TextFieldValue>;

    private static SearchCatalogSnapshot Snapshot(
        IEnumerable<FieldDefinition>? fields = null,
        IEnumerable<SearchPresetEntry>? presets = null)
    {
        var groups = (fields ?? [])
            .GroupBy(f => f.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SearchFieldGroup(g.Key, g.ToList()))
            .ToList();
        return new SearchCatalogSnapshot
        {
            Fields = groups,
            Presets = (presets ?? []).ToList(),
        };
    }

    private ParsedQuery Parse(string text)
    {
        var result = _parser.Parse(text);
        Assert.That(result.Errors, Is.Empty);
        return result.Query!;
    }

    private static BoundConditionNode RootCondition(QueryBindResult result)
    {
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Query!.Root, Is.InstanceOf<BoundConditionNode>());
        return (BoundConditionNode)result.Query.Root!;
    }

    [Test]
    public void Bind_KnownField_BindsOneMatcherPerSearchableDefinition()
    {
        var first = new StubSearchableDefinition { Label = "Status" };
        var second = new StubSearchableDefinition { Label = "status" };
        var result = _binder.Bind(Parse("Status = open"), Snapshot([first, second]));

        var condition = RootCondition(result);
        Assert.That(condition.Operator, Is.EqualTo(QueryOperatorKind.Equals));
        Assert.That(condition.Bindings, Has.Count.EqualTo(2));
        Assert.That(condition.Bindings.SelectMany(b => b.DefinitionIds),
            Is.EquivalentTo(new[] { first.Id, second.Id }));
    }

    [Test]
    public void Bind_UnknownField_ReportsUnknownFieldWithSpan()
    {
        var result = _binder.Bind(Parse("Ghost = 1"), Snapshot());

        var error = result.Errors.Single();
        Assert.That(error.Code, Is.EqualTo(QueryErrorCode.UnknownField));
        Assert.That(error.Start, Is.EqualTo(0));
        Assert.That(error.Length, Is.EqualTo(5));
        Assert.That(error.Detail, Is.EqualTo("Ghost"));
    }

    [Test]
    public void Bind_FieldWithoutSearchSupport_ReportsFieldNotSearchable()
    {
        var result = _binder.Bind(
            Parse("Photo = x"),
            Snapshot([new NonSearchableDefinition { Label = "Photo" }]));

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.FieldNotSearchable));
    }

    [Test]
    public void Bind_OperatorUnsupportedByEveryDefinition_ReportsOperatorNotSupported()
    {
        var result = _binder.Bind(
            Parse("Status > 1"),
            Snapshot([new StubSearchableDefinition { Label = "Status" }]));

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }

    [Test]
    public void Bind_OperatorUnsupportedBySomeDefinitions_BindsRestAndNotices()
    {
        var supporting = new StubSearchableDefinition
        {
            Label = "Status",
            SupportedOperators = new[] { QueryOperatorKind.Equals, QueryOperatorKind.Greater },
        };
        var refusing = new StubSearchableDefinition { Label = "Status" };
        var result = _binder.Bind(Parse("Status > 1"), Snapshot([supporting, refusing]));

        var condition = RootCondition(result);
        Assert.That(condition.Bindings.Single().DefinitionIds, Is.EqualTo(new[] { supporting.Id }));
        Assert.That(result.Notices.Single().Code, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
        Assert.That(result.Notices.Single().Field, Is.EqualTo("Status"));
    }

    [Test]
    public void Bind_OperandRejectedByEveryDefinition_ReportsInvalidValue()
    {
        var result = _binder.Bind(
            Parse("Status = open"),
            Snapshot([new StubSearchableDefinition { Label = "Status", RejectOperand = true }]));

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void Bind_BooleanStructure_IsPreservedInBoundTree()
    {
        var snapshot = Snapshot([
            new StubSearchableDefinition { Label = "a" },
            new StubSearchableDefinition { Label = "b" },
            new StubSearchableDefinition { Label = "c" },
        ]);
        var result = _binder.Bind(Parse("a = 1 OR NOT (b = 2 AND c = 3)"), snapshot);

        Assert.That(result.Errors, Is.Empty);
        var or = (BoundOrNode)result.Query!.Root!;
        var not = (BoundNotNode)or.Right;
        Assert.That(not.Operand, Is.InstanceOf<BoundAndNode>());
    }

    [Test]
    public void Bind_EmptyQuery_YieldsMatchAllWithNoRoot()
    {
        var result = _binder.Bind(Parse(""), Snapshot());

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Query!.Root, Is.Null);
    }

    [Test]
    public void Bind_PseudoNameEquals_MatchesDisplayNameCaseInsensitively()
    {
        var result = _binder.Bind(Parse("name = loco"), Snapshot());

        var matcher = RootCondition(result).Bindings.Single().Matcher;
        Assert.That(matcher.Matches(new Item { DisplayName = "LOCO" }, []), Is.True);
        Assert.That(matcher.Matches(new Item { DisplayName = "other" }, []), Is.False);
    }

    [Test]
    public void Bind_PseudoNameContains_MatchesSubstring()
    {
        var result = _binder.Bind(Parse("name ~ oc"), Snapshot());

        var matcher = RootCondition(result).Bindings.Single().Matcher;
        Assert.That(matcher.Matches(new Item { DisplayName = "Loco 42" }, []), Is.True);
        Assert.That(matcher.Matches(new Item { DisplayName = "Wagon" }, []), Is.False);
    }

    [Test]
    public void Bind_RealFieldLabeledName_BindsPseudoAndRealTogether()
    {
        var real = new StubSearchableDefinition { Label = "Name" };
        var result = _binder.Bind(Parse("name = x"), Snapshot([real]));

        Assert.That(RootCondition(result).Bindings, Has.Count.EqualTo(2));
    }

    [Test]
    public void Bind_PseudoPresetEquals_MatchesItemsOfPresetsWithThatName()
    {
        var books = new SearchPresetEntry(Guid.NewGuid(), "Books");
        var games = new SearchPresetEntry(Guid.NewGuid(), "Games");
        var result = _binder.Bind(Parse("preset = books"), Snapshot(presets: [books, games]));

        var matcher = RootCondition(result).Bindings.Single().Matcher;
        Assert.That(matcher.Matches(new Item { PresetId = books.Id }, []), Is.True);
        Assert.That(matcher.Matches(new Item { PresetId = games.Id }, []), Is.False);
    }

    [Test]
    public void Bind_PseudoCollectionAlias_BindsLikePreset()
    {
        var books = new SearchPresetEntry(Guid.NewGuid(), "Books");
        var result = _binder.Bind(Parse("collection = Books"), Snapshot(presets: [books]));

        var matcher = RootCondition(result).Bindings.Single().Matcher;
        Assert.That(matcher.Matches(new Item { PresetId = books.Id }, []), Is.True);
    }

    [Test]
    public void Bind_PseudoPresetUnknownName_BindsMatchNothingAndNotices()
    {
        var result = _binder.Bind(Parse("preset = Ghost"), Snapshot());

        var matcher = RootCondition(result).Bindings.Single().Matcher;
        Assert.That(matcher.Matches(new Item(), []), Is.False);
        Assert.That(result.Notices.Single().Code, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void Bind_PseudoCreatedComparisons_FilterByDay()
    {
        var jan1Morning = new Item { CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc) };
        var feb2 = new Item { CreatedAt = new DateTime(2025, 2, 2, 0, 0, 0, DateTimeKind.Utc) };

        var equals = RootCondition(_binder.Bind(Parse("created = 2025-01-01"), Snapshot()))
            .Bindings.Single().Matcher;
        Assert.That(equals.Matches(jan1Morning, []), Is.True);
        Assert.That(equals.Matches(feb2, []), Is.False);

        var before = RootCondition(_binder.Bind(Parse("created < 2025-02-01"), Snapshot()))
            .Bindings.Single().Matcher;
        Assert.That(before.Matches(jan1Morning, []), Is.True);
        Assert.That(before.Matches(feb2, []), Is.False);

        var after = RootCondition(_binder.Bind(Parse("updated > 2025-01-01"), Snapshot()))
            .Bindings.Single().Matcher;
        Assert.That(after.Matches(new Item { UpdatedAt = new DateTime(2025, 1, 2) }, []), Is.True);
        Assert.That(after.Matches(new Item { UpdatedAt = new DateTime(2025, 1, 1, 23, 0, 0) }, []), Is.False);
    }

    [Test]
    public void Bind_PseudoCreatedWithUnparsableDate_ReportsInvalidValueAtOperand()
    {
        var result = _binder.Bind(Parse("created = nonsense"), Snapshot());

        var error = result.Errors.Single();
        Assert.That(error.Code, Is.EqualTo(QueryErrorCode.InvalidValue));
        Assert.That(error.Start, Is.EqualTo("created = ".Length));
    }

    [Test]
    public void Bind_OrderByField_ProducesSortKeyFromDefinition()
    {
        var definition = new StubSearchableDefinition { Label = "Author" };
        var result = _binder.Bind(Parse("ORDER BY Author DESC"), Snapshot([definition]));

        Assert.That(result.Errors, Is.Empty);
        var orderBy = result.Query!.OrderBy.Single();
        Assert.That(orderBy.Descending, Is.True);
        var item = new Item
        {
            Values = [new TextFieldValue { FieldDefinitionId = definition.Id, Value = "Twain" }],
        };
        Assert.That(orderBy.SortKey(item), Is.EqualTo("Twain"));
        Assert.That(orderBy.SortKey(new Item()), Is.Null);
    }

    [Test]
    public void Bind_OrderByPseudoFields_SortByItemProperties()
    {
        var books = new SearchPresetEntry(Guid.NewGuid(), "Books");
        var result = _binder.Bind(
            Parse("ORDER BY name, created, preset"),
            Snapshot(presets: [books]));

        Assert.That(result.Errors, Is.Empty);
        var item = new Item
        {
            DisplayName = "Loco",
            PresetId = books.Id,
            CreatedAt = new DateTime(2025, 3, 1),
        };
        Assert.That(result.Query!.OrderBy[0].SortKey(item), Is.EqualTo("Loco"));
        Assert.That(result.Query.OrderBy[1].SortKey(item), Is.EqualTo(new DateTime(2025, 3, 1)));
        Assert.That(result.Query.OrderBy[2].SortKey(item), Is.EqualTo("Books"));
    }

    [Test]
    public void Bind_OrderByUnknownField_ReportsUnknownField()
    {
        var result = _binder.Bind(Parse("ORDER BY Ghost"), Snapshot());

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.UnknownField));
    }
}
