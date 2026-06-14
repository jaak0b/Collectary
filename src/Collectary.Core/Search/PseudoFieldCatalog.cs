using System.Globalization;
using System.Linq.Expressions;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.Search;

public sealed class ItemPropertyMatcher : IFieldConditionMatcher
{
    private readonly Func<Item, bool> _predicate;
    private readonly Expression<Func<Item, bool>>? _serverFilter;

    public ItemPropertyMatcher(Func<Item, bool> predicate, Expression<Func<Item, bool>>? serverFilter)
    {
        _predicate = predicate;
        _serverFilter = serverFilter;
    }

    public Expression<Func<Item, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => _serverFilter;

    public bool Matches(Item item, IReadOnlyCollection<Guid> definitionIds) => _predicate(item);
}

public sealed record PseudoBindOutcome(
    IFieldConditionMatcher? Matcher,
    QueryErrorCode? Error,
    QueryErrorCode? Notice);

public class PseudoFieldCatalog
{
    private readonly AsciiCaseFolding _folding = new();
    private readonly TimeZoneInfo _timeZone;
    private readonly CultureInfo? _culture;

    public PseudoFieldCatalog(TimeZoneInfo? timeZone = null, CultureInfo? culture = null)
    {
        _timeZone = timeZone ?? TimeZoneInfo.Local;
        _culture = culture;
    }

    public IReadOnlyList<string> Labels => ["name", "preset", "collection", "created", "updated"];

    public IReadOnlyList<string> AliasesFor(string label) =>
        Matches(label, "collection") ? ["preset"] : [];

    public bool SuggestsPresetNames(string label) =>
        Matches(label, "preset") || Matches(label, "collection");

    public IReadOnlyList<QueryOperatorKind> OperatorsFor(string label)
    {
        if (Matches(label, "name"))
            return
            [
                QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
                QueryOperatorKind.Contains, QueryOperatorKind.NotContains,
                QueryOperatorKind.In, QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
            ];
        if (Matches(label, "preset") || Matches(label, "collection"))
            return
            [
                QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
                QueryOperatorKind.Contains, QueryOperatorKind.NotContains, QueryOperatorKind.In,
            ];
        if (Matches(label, "created") || Matches(label, "updated"))
            return
            [
                QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
                QueryOperatorKind.Less, QueryOperatorKind.LessOrEqual,
                QueryOperatorKind.Greater, QueryOperatorKind.GreaterOrEqual,
            ];
        return [];
    }

    public PseudoBindOutcome? TryCreateMatcher(
        string label, QueryOperatorKind op, IReadOnlyList<string> operands, SearchCatalogSnapshot snapshot)
    {
        if (Matches(label, "name")) return BindName(op, operands);
        if (Matches(label, "preset") || Matches(label, "collection")) return BindPreset(op, operands, snapshot);
        if (Matches(label, "created")) return BindTimestamp(op, operands, created: true);
        if (Matches(label, "updated")) return BindTimestamp(op, operands, created: false);
        return null;
    }

    public Func<Item, IComparable?>? SortKey(string label, SearchCatalogSnapshot snapshot)
    {
        if (Matches(label, "name")) return item => item.DisplayName;
        if (Matches(label, "created")) return item => item.CreatedAt;
        if (Matches(label, "updated")) return item => item.UpdatedAt;
        if (Matches(label, "preset") || Matches(label, "collection"))
        {
            var namesById = snapshot.Presets
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);
            return item => namesById.GetValueOrDefault(item.PresetId);
        }
        return null;
    }

    private bool Matches(string label, string pseudo) =>
        string.Equals(label, pseudo, StringComparison.OrdinalIgnoreCase);

    private PseudoBindOutcome BindName(QueryOperatorKind op, IReadOnlyList<string> operands)
    {
        switch (op)
        {
            case QueryOperatorKind.Equals:
            case QueryOperatorKind.NotEquals:
            {
                var folded = _folding.Fold(operands[0]);
                var lowered = operands[0].ToLowerInvariant();
                var equals = op == QueryOperatorKind.Equals;
                return Bound(
                    item => _folding.AreEqual(item.DisplayName, folded) == equals,
                    equals
                        ? (Expression<Func<Item, bool>>?)(item =>
                            item.DisplayName.ToLower() == folded || item.DisplayName.ToLower() == lowered)
                        : null);
            }
            case QueryOperatorKind.Contains:
            case QueryOperatorKind.NotContains:
            {
                var folded = _folding.Fold(operands[0]);
                var lowered = operands[0].ToLowerInvariant();
                var contains = op == QueryOperatorKind.Contains;
                return Bound(
                    item => _folding.Contains(item.DisplayName, folded) == contains,
                    contains
                        ? (Expression<Func<Item, bool>>?)(item =>
                            item.DisplayName.ToLower().Contains(folded)
                            || item.DisplayName.ToLower().Contains(lowered))
                        : null);
            }
            case QueryOperatorKind.In:
            {
                var folded = operands.Select(_folding.Fold).ToList();
                var variants = operands
                    .SelectMany(o => new[] { _folding.Fold(o), o.ToLowerInvariant() })
                    .Distinct()
                    .ToList();
                return Bound(
                    item => folded.Contains(_folding.Fold(item.DisplayName)),
                    item => variants.Contains(item.DisplayName.ToLower()));
            }
            case QueryOperatorKind.IsEmpty:
                return Bound(item => item.DisplayName == "", item => item.DisplayName == "");
            case QueryOperatorKind.IsNotEmpty:
                return Bound(item => item.DisplayName != "", item => item.DisplayName != "");
            default:
                return new PseudoBindOutcome(null, QueryErrorCode.OperatorNotSupported, null);
        }
    }

    private PseudoBindOutcome BindPreset(
        QueryOperatorKind op, IReadOnlyList<string> operands, SearchCatalogSnapshot snapshot)
    {
        var positive = op is QueryOperatorKind.Equals or QueryOperatorKind.Contains or QueryOperatorKind.In;
        Func<string, bool> nameMatches = op switch
        {
            QueryOperatorKind.Equals or QueryOperatorKind.NotEquals or QueryOperatorKind.In =>
                name => operands.Any(o => _folding.AreEqual(name, _folding.Fold(o))),
            QueryOperatorKind.Contains or QueryOperatorKind.NotContains =>
                name => _folding.Contains(name, operands[0]),
            _ => _ => false,
        };
        if (op is not (QueryOperatorKind.Equals or QueryOperatorKind.NotEquals
            or QueryOperatorKind.Contains or QueryOperatorKind.NotContains or QueryOperatorKind.In))
            return new PseudoBindOutcome(null, QueryErrorCode.OperatorNotSupported, null);

        var ids = snapshot.Presets.Where(p => nameMatches(p.Name)).Select(p => p.Id).ToHashSet();
        var notice = ids.Count == 0 ? QueryErrorCode.InvalidValue : (QueryErrorCode?)null;
        var matcher = new ItemPropertyMatcher(
            positive
                ? item => ids.Contains(item.PresetId)
                : item => !ids.Contains(item.PresetId),
            positive
                ? item => ids.Contains(item.PresetId)
                : item => !ids.Contains(item.PresetId));
        return new PseudoBindOutcome(matcher, null, notice);
    }

    private PseudoBindOutcome BindTimestamp(QueryOperatorKind op, IReadOnlyList<string> operands, bool created)
    {
        if (op is not (QueryOperatorKind.Equals or QueryOperatorKind.NotEquals
            or QueryOperatorKind.Less or QueryOperatorKind.LessOrEqual
            or QueryOperatorKind.Greater or QueryOperatorKind.GreaterOrEqual))
            return new PseudoBindOutcome(null, QueryErrorCode.OperatorNotSupported, null);
        if (operands.Count != 1 || !TryParseDate(operands[0], out var parsed))
            return new PseudoBindOutcome(null, QueryErrorCode.InvalidValue, null);

        var dayStart = ToUtcInstant(parsed.Date);
        var dayEnd = ToUtcInstant(parsed.Date.AddDays(1));
        Func<Item, DateTime> stamp = created ? item => item.CreatedAt : item => item.UpdatedAt;
        Func<Item, bool> predicate = op switch
        {
            QueryOperatorKind.Equals => item => stamp(item) >= dayStart && stamp(item) < dayEnd,
            QueryOperatorKind.NotEquals => item => stamp(item) < dayStart || stamp(item) >= dayEnd,
            QueryOperatorKind.Less => item => stamp(item) < dayStart,
            QueryOperatorKind.LessOrEqual => item => stamp(item) < dayEnd,
            QueryOperatorKind.Greater => item => stamp(item) >= dayEnd,
            _ => item => stamp(item) >= dayStart,
        };
        var serverFilter = created
            ? op switch
            {
                QueryOperatorKind.Equals => (Expression<Func<Item, bool>>)(item =>
                    item.CreatedAt >= dayStart && item.CreatedAt < dayEnd),
                QueryOperatorKind.NotEquals => item => item.CreatedAt < dayStart || item.CreatedAt >= dayEnd,
                QueryOperatorKind.Less => item => item.CreatedAt < dayStart,
                QueryOperatorKind.LessOrEqual => item => item.CreatedAt < dayEnd,
                QueryOperatorKind.Greater => item => item.CreatedAt >= dayEnd,
                _ => item => item.CreatedAt >= dayStart,
            }
            : op switch
            {
                QueryOperatorKind.Equals => (Expression<Func<Item, bool>>)(item =>
                    item.UpdatedAt >= dayStart && item.UpdatedAt < dayEnd),
                QueryOperatorKind.NotEquals => item => item.UpdatedAt < dayStart || item.UpdatedAt >= dayEnd,
                QueryOperatorKind.Less => item => item.UpdatedAt < dayStart,
                QueryOperatorKind.LessOrEqual => item => item.UpdatedAt < dayEnd,
                QueryOperatorKind.Greater => item => item.UpdatedAt >= dayEnd,
                _ => item => item.UpdatedAt >= dayStart,
            };
        return new PseudoBindOutcome(new ItemPropertyMatcher(predicate, serverFilter), null, null);
    }

    private bool TryParseDate(string raw, out DateTime parsed) =>
        DateTime.TryParse(raw, _culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed)
        || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);

    private DateTime ToUtcInstant(DateTime wallClock)
    {
        var unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
        if (_timeZone.IsInvalidTime(unspecified))
            unspecified = unspecified.AddHours(1);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
    }

    private PseudoBindOutcome Bound(Func<Item, bool> predicate, Expression<Func<Item, bool>>? serverFilter) =>
        new(new ItemPropertyMatcher(predicate, serverFilter), null, null);
}
