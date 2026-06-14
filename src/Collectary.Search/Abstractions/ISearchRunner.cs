namespace Collectary.Search;

public sealed record SearchOutcome(
    IReadOnlyList<object> Items,
    IReadOnlyList<QueryError> Errors,
    IReadOnlyList<QueryNotice> Notices);

public interface ISearchRunner
{
    Task<SearchOutcome> SearchAsync(string queryText);
}
