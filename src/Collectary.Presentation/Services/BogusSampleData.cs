using Bogus;

namespace Collectary.Presentation.Services;

public sealed class BogusSampleData : ISampleData
{
    private readonly Faker _faker;

    public BogusSampleData() : this(null)
    {
    }

    public BogusSampleData(int? seed)
    {
        _faker = new Faker();
        if (seed.HasValue)
            _faker.Random = new Randomizer(seed.Value);
    }

    public string Words(int count) => string.Join(" ", _faker.Lorem.Words(count));

    public string Sentence() => _faker.Lorem.Sentence();

    public int Int(int minInclusive, int maxInclusive) => _faker.Random.Int(minInclusive, maxInclusive);

    public decimal Decimal(decimal min, decimal max, int places) =>
        decimal.Round(_faker.Random.Decimal(min, max), places);

    public bool Bool() => _faker.Random.Bool();

    public DateTime PastDateUtc() => DateTime.SpecifyKind(_faker.Date.Past(), DateTimeKind.Utc);

    public string Email() => _faker.Internet.Email();

    public string Url() => _faker.Internet.Url();

    public string Phone() => _faker.Phone.PhoneNumber("+## ### #######");

    public string Digits(int length) => _faker.Random.ReplaceNumbers(new string('#', length));

    public IReadOnlyList<string> WordList(int count) => _faker.Lorem.Words(count).Distinct().ToList();

    public T PickOne<T>(IReadOnlyList<T> items) => _faker.Random.ListItem(items as IList<T> ?? items.ToList());
}
