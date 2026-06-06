namespace Collectary.Presentation.Services;

public interface ISampleData
{
    string Words(int count);
    string Sentence();
    int Int(int minInclusive, int maxInclusive);
    decimal Decimal(decimal min, decimal max, int places);
    bool Bool();
    DateTime PastDateUtc();
    string Email();
    string Url();
    string Phone();
    string Digits(int length);
    IReadOnlyList<string> WordList(int count);
    T PickOne<T>(IReadOnlyList<T> items);
}
