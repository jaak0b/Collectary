using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Persistence;
using Collectary.Presentation.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class ProfilePickerFlowTest
{
    private string _dbPath = null!;
    private DbContextOptions<InventoryDbContext> _options = null!;
    private ProfileService _profiles = null!;
    private UserSession _session = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"collectary-test-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_dbPath};Pooling=False")
            .Options;
        using (var db = CreateDb())
            db.Database.EnsureCreated();

        _session = new UserSession();
        _profiles = new ProfileService(new UserRepository(CreateDb), _session);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
            if (File.Exists(path)) File.Delete(path);
    }

    private InventoryDbContext CreateDb() => new(_options);

    [Test]
    public async Task AddProfile_ThenSelect_PersistsAndEntersSession()
    {
        User? entered = null;
        var picker = new ProfilePickerViewModel(_profiles, user =>
        {
            _profiles.SelectProfile(user);
            entered = user;
            return Task.CompletedTask;
        });
        await picker.LoadAsync();

        picker.BeginAddCommand.Execute(null);
        picker.NewProfileName = "Alice";
        await picker.CreateProfileCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(entered, Is.Not.Null);
            Assert.That(_session.CurrentUser, Is.SameAs(entered));
            Assert.That(_session.CurrentUser!.DisplayName, Is.EqualTo("Alice"));
        });

        var reloaded = new ProfilePickerViewModel(_profiles, _ => Task.CompletedTask);
        await reloaded.LoadAsync();
        Assert.That(reloaded.Profiles.Select(t => t.Name), Does.Contain("Alice"));
    }

    [Test]
    public async Task AddTwoProfilesWithSameName_GetDistinctUsernames()
    {
        var first = await _profiles.CreateProfileAsync("Alice");
        var second = await _profiles.CreateProfileAsync("Alice");

        Assert.Multiple(() =>
        {
            Assert.That(first.Username, Is.EqualTo("alice"));
            Assert.That(second.Username, Is.EqualTo("alice-2"));
            Assert.That(second.DisplayName, Is.EqualTo("Alice"));
        });
    }
}
