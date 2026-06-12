using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class PseudoFieldCatalogTest
{
    private PseudoFieldCatalog _catalog = null!;
    private SearchCatalogSnapshot _snapshot = null!;
    private SearchPresetEntry _books = null!;
    private SearchPresetEntry _games = null!;

    [SetUp]
    public void SetUp()
    {
        _catalog = new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.InvariantCulture);
        _books = new SearchPresetEntry(Guid.NewGuid(), "Books");
        _games = new SearchPresetEntry(Guid.NewGuid(), "Games");
        _snapshot = new SearchCatalogSnapshot { Presets = [_books, _games] };
    }

    private IFieldConditionMatcher Matcher(string label, QueryOperatorKind op, params string[] operands)
    {
        var outcome = _catalog.TryCreateMatcher(label, op, operands, _snapshot);
        Assert.That(outcome?.Matcher, Is.Not.Null, $"expected {label} to bind {op}");
        return outcome!.Matcher!;
    }

    [Test]
    public void TryCreateMatcher_UnknownLabel_ReturnsNull()
    {
        Assert.That(_catalog.TryCreateMatcher("Ghost", QueryOperatorKind.Equals, ["x"], _snapshot), Is.Null);
    }

    [Test]
    public void OperatorsFor_UnknownLabel_IsEmpty()
    {
        Assert.That(_catalog.OperatorsFor("Ghost"), Is.Empty);
    }

    [Test]
    public void Name_NotEqualsAndNotContains_NegateTheComparison()
    {
        Assert.That(Matcher("name", QueryOperatorKind.NotEquals, "loco")
            .Matches(new Item { DisplayName = "Wagon" }, []), Is.True);
        Assert.That(Matcher("name", QueryOperatorKind.NotEquals, "loco")
            .Matches(new Item { DisplayName = "LOCO" }, []), Is.False);
        Assert.That(Matcher("name", QueryOperatorKind.NotContains, "oc")
            .Matches(new Item { DisplayName = "Wagon" }, []), Is.True);
        Assert.That(Matcher("name", QueryOperatorKind.NotContains, "oc")
            .Matches(new Item { DisplayName = "Loco" }, []), Is.False);
    }

    [Test]
    public void Name_InAndEmptiness_MatchOnDisplayName()
    {
        Assert.That(Matcher("name", QueryOperatorKind.In, "a", "B")
            .Matches(new Item { DisplayName = "b" }, []), Is.True);
        Assert.That(Matcher("name", QueryOperatorKind.In, "a", "B")
            .Matches(new Item { DisplayName = "c" }, []), Is.False);
        Assert.That(Matcher("name", QueryOperatorKind.IsEmpty)
            .Matches(new Item { DisplayName = "" }, []), Is.True);
        Assert.That(Matcher("name", QueryOperatorKind.IsNotEmpty)
            .Matches(new Item { DisplayName = "x" }, []), Is.True);
    }

    [Test]
    public void Name_RelationalOperator_ReportsOperatorNotSupported()
    {
        var outcome = _catalog.TryCreateMatcher("name", QueryOperatorKind.Greater, ["1"], _snapshot);

        Assert.That(outcome!.Matcher, Is.Null);
        Assert.That(outcome.Error, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }

    [Test]
    public void Preset_NotEquals_MatchesItemsOfOtherPresets()
    {
        var matcher = Matcher("preset", QueryOperatorKind.NotEquals, "Books");

        Assert.That(matcher.Matches(new Item { PresetId = _games.Id }, []), Is.True);
        Assert.That(matcher.Matches(new Item { PresetId = _books.Id }, []), Is.False);
    }

    [Test]
    public void Preset_ContainsAndIn_ResolveNamesToIds()
    {
        Assert.That(Matcher("preset", QueryOperatorKind.Contains, "ook")
            .Matches(new Item { PresetId = _books.Id }, []), Is.True);
        Assert.That(Matcher("preset", QueryOperatorKind.In, "books", "games")
            .Matches(new Item { PresetId = _games.Id }, []), Is.True);
    }

    [Test]
    public void Preset_EmptinessOperator_ReportsOperatorNotSupported()
    {
        var outcome = _catalog.TryCreateMatcher("preset", QueryOperatorKind.IsEmpty, [], _snapshot);

        Assert.That(outcome!.Error, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }

    [Test]
    public void Timestamps_DayWindow_UsesTheProvidedTimeZone()
    {
        var plusTen = TimeZoneInfo.CreateCustomTimeZone("Test+10", TimeSpan.FromHours(10), "Test+10", "Test+10");
        var catalog = new PseudoFieldCatalog(plusTen, CultureInfo.InvariantCulture);
        var item = new Item { CreatedAt = new DateTime(2026, 6, 12, 22, 0, 0, DateTimeKind.Utc) };

        var june13 = catalog.TryCreateMatcher("created", QueryOperatorKind.Equals, ["2026-06-13"], _snapshot);
        var june12 = catalog.TryCreateMatcher("created", QueryOperatorKind.Equals, ["2026-06-12"], _snapshot);

        Assert.That(june13!.Matcher!.Matches(item, []), Is.True);
        Assert.That(june12!.Matcher!.Matches(item, []), Is.False);
    }

    [Test]
    public void Timestamps_ServerFilter_UsesTheSameTimeZoneAdjustedWindow()
    {
        var plusTen = TimeZoneInfo.CreateCustomTimeZone("Test+10", TimeSpan.FromHours(10), "Test+10", "Test+10");
        var catalog = new PseudoFieldCatalog(plusTen, CultureInfo.InvariantCulture);
        var item = new Item { CreatedAt = new DateTime(2026, 6, 12, 22, 0, 0, DateTimeKind.Utc) };

        var outcome = catalog.TryCreateMatcher("created", QueryOperatorKind.Equals, ["2026-06-13"], _snapshot);
        var filter = outcome!.Matcher!.ServerFilter([])!.Compile();

        Assert.That(filter(item), Is.True);
    }

    [Test]
    public void Timestamps_ParseDatesInTheProvidedCulture()
    {
        var german = new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.GetCultureInfo("de-DE"));
        var item = new Item { CreatedAt = new DateTime(2026, 6, 13, 10, 0, 0, DateTimeKind.Utc) };

        var outcome = german.TryCreateMatcher("created", QueryOperatorKind.Equals, ["13.06.2026"], _snapshot);

        Assert.That(outcome!.Error, Is.Null);
        Assert.That(outcome.Matcher!.Matches(item, []), Is.True);
    }

    [Test]
    public void Timestamps_AmbiguousDottedDate_FollowsTheCultureNotInvariant()
    {
        var german = new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.GetCultureInfo("de-DE"));
        var june1 = new Item { CreatedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) };
        var january6 = new Item { CreatedAt = new DateTime(2026, 1, 6, 10, 0, 0, DateTimeKind.Utc) };

        var matcher = german.TryCreateMatcher("created", QueryOperatorKind.Equals, ["01.06.2026"], _snapshot)!.Matcher!;

        Assert.That(matcher.Matches(june1, []), Is.True);
        Assert.That(matcher.Matches(january6, []), Is.False);
    }

    [Test]
    public void Timestamps_IsoDate_ParsesUnderAnyCulture()
    {
        var german = new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.GetCultureInfo("de-DE"));
        var item = new Item { CreatedAt = new DateTime(2026, 6, 13, 10, 0, 0, DateTimeKind.Utc) };

        var outcome = german.TryCreateMatcher("created", QueryOperatorKind.Equals, ["2026-06-13"], _snapshot);

        Assert.That(outcome!.Error, Is.Null);
        Assert.That(outcome.Matcher!.Matches(item, []), Is.True);
    }

    [Test]
    public void Timestamps_DefaultConstructedCatalog_StillBindsDates()
    {
        var outcome = new PseudoFieldCatalog()
            .TryCreateMatcher("created", QueryOperatorKind.Equals, ["2026-06-13"], _snapshot);

        Assert.That(outcome!.Error, Is.Null);
        Assert.That(outcome.Matcher, Is.Not.Null);
        Assert.That(() => outcome.Matcher!.Matches(new Item(), []), Throws.Nothing);
    }

    [Test]
    public void Timestamps_EachCatalogParsesWithItsOwnCulture_NotTheMachineCulture()
    {
        var german = new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.GetCultureInfo("de-DE"));
        var american = new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.GetCultureInfo("en-US"));
        var june1 = new Item { CreatedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) };
        var january6 = new Item { CreatedAt = new DateTime(2026, 1, 6, 10, 0, 0, DateTimeKind.Utc) };

        var dotted = german.TryCreateMatcher("created", QueryOperatorKind.Equals, ["01.06.2026"], _snapshot)!.Matcher!;
        var slashed = american.TryCreateMatcher("created", QueryOperatorKind.Equals, ["01/06/2026"], _snapshot)!.Matcher!;

        Assert.That(dotted.Matches(june1, []), Is.True);
        Assert.That(slashed.Matches(january6, []), Is.True);
    }

    [Test]
    public void Timestamps_AllComparisons_FilterByDayWindow()
    {
        var noon = new DateTime(2025, 6, 15, 12, 0, 0);
        var itemCreated = new Item { CreatedAt = noon };
        var itemUpdated = new Item { UpdatedAt = noon };

        Assert.That(Matcher("created", QueryOperatorKind.NotEquals, "2025-06-15")
            .Matches(itemCreated, []), Is.False);
        Assert.That(Matcher("created", QueryOperatorKind.LessOrEqual, "2025-06-15")
            .Matches(itemCreated, []), Is.True);
        Assert.That(Matcher("created", QueryOperatorKind.GreaterOrEqual, "2025-06-16")
            .Matches(itemCreated, []), Is.False);
        Assert.That(Matcher("updated", QueryOperatorKind.Equals, "2025-06-15")
            .Matches(itemUpdated, []), Is.True);
        Assert.That(Matcher("updated", QueryOperatorKind.NotEquals, "2025-06-14")
            .Matches(itemUpdated, []), Is.True);
        Assert.That(Matcher("updated", QueryOperatorKind.LessOrEqual, "2025-06-15")
            .Matches(itemUpdated, []), Is.True);
        Assert.That(Matcher("updated", QueryOperatorKind.GreaterOrEqual, "2025-06-15")
            .Matches(itemUpdated, []), Is.True);
        Assert.That(Matcher("updated", QueryOperatorKind.Less, "2025-06-15")
            .Matches(itemUpdated, []), Is.False);
        Assert.That(Matcher("updated", QueryOperatorKind.Greater, "2025-06-15")
            .Matches(itemUpdated, []), Is.False);
    }

    [Test]
    public void Timestamps_ServerFilters_CompileToTheSamePredicates()
    {
        var noon = new DateTime(2025, 6, 15, 12, 0, 0);

        foreach (var label in new[] { "created", "updated" })
        {
            foreach (var op in new[]
            {
                QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
                QueryOperatorKind.Less, QueryOperatorKind.LessOrEqual,
                QueryOperatorKind.Greater, QueryOperatorKind.GreaterOrEqual,
            })
            {
                var matcher = Matcher(label, op, "2025-06-15");
                var item = label == "created"
                    ? new Item { CreatedAt = noon }
                    : new Item { UpdatedAt = noon };
                var compiled = matcher.ServerFilter([])!.Compile();
                Assert.That(compiled(item), Is.EqualTo(matcher.Matches(item, [])),
                    $"{label} {op} must behave identically in SQL and memory");
            }
        }
    }

    [Test]
    public void Timestamps_DayBoundaries_AreInclusiveAtMidnightAndExclusiveAtNextMidnight()
    {
        var dayStart = new DateTime(2025, 6, 15, 0, 0, 0);
        var nextMidnight = new DateTime(2025, 6, 16, 0, 0, 0);

        foreach (var label in new[] { "created", "updated" })
        {
            Item At(DateTime stamp) => label == "created"
                ? new Item { CreatedAt = stamp }
                : new Item { UpdatedAt = stamp };

            var expectations = new (QueryOperatorKind Op, DateTime Stamp, bool Matches)[]
            {
                (QueryOperatorKind.Equals, dayStart, true),
                (QueryOperatorKind.Equals, nextMidnight, false),
                (QueryOperatorKind.NotEquals, dayStart, false),
                (QueryOperatorKind.NotEquals, nextMidnight, true),
                (QueryOperatorKind.Less, dayStart, false),
                (QueryOperatorKind.LessOrEqual, nextMidnight, false),
                (QueryOperatorKind.LessOrEqual, nextMidnight.AddTicks(-1), true),
                (QueryOperatorKind.Greater, nextMidnight, true),
                (QueryOperatorKind.Greater, nextMidnight.AddTicks(-1), false),
                (QueryOperatorKind.GreaterOrEqual, dayStart, true),
                (QueryOperatorKind.GreaterOrEqual, dayStart.AddTicks(-1), false),
            };
            foreach (var (op, stamp, expected) in expectations)
            {
                var matcher = Matcher(label, op, "2025-06-15");
                Assert.That(matcher.Matches(At(stamp), []), Is.EqualTo(expected),
                    $"{label} {op} at {stamp:O}");
                Assert.That(matcher.ServerFilter([])!.Compile()(At(stamp)), Is.EqualTo(expected),
                    $"{label} {op} at {stamp:O} (server)");
            }
        }
    }

    [Test]
    public void Timestamps_InOperator_ReportsOperatorNotSupported()
    {
        var outcome = _catalog.TryCreateMatcher("created", QueryOperatorKind.In, ["2025-01-01"], _snapshot);

        Assert.That(outcome!.Error, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }

    [Test]
    public void SortKey_UnknownLabel_IsNull()
    {
        Assert.That(_catalog.SortKey("Ghost", _snapshot), Is.Null);
    }

    [Test]
    public void SortKey_Updated_ReturnsTheTimestamp()
    {
        var stamp = new DateTime(2025, 3, 1);

        Assert.That(_catalog.SortKey("updated", _snapshot)!(new Item { UpdatedAt = stamp }), Is.EqualTo(stamp));
    }

    [Test]
    public void SortKey_Preset_UnknownPresetId_IsNull()
    {
        Assert.That(_catalog.SortKey("collection", _snapshot)!(new Item { PresetId = Guid.NewGuid() }), Is.Null);
    }
}
