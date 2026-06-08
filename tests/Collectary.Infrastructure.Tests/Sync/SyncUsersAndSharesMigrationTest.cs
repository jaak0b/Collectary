using Collectary.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class SyncUsersAndSharesMigrationTest
{
    private const string PreviousMigration = "20260606140821_ConfigurableFieldTypeSettings";

    private string _dbPath = null!;
    private DbContextOptions<InventoryDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"collectary-mig-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Test]
    public async Task Migration_BackfillsExistingProfilesAndSharesAsDirty()
    {
        using (var db = new InventoryDbContext(_options))
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Users (Id, Username, DisplayName) VALUES ({0}, 'alice', 'Alice')",
                Guid.NewGuid().ToString());
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO CollectionShares (Id, PresetId, SharedWithUserId, GrantedByUserId, Permission, GrantedAt) VALUES ({0}, {1}, {2}, {3}, 0, {4})",
                Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                DateTime.UtcNow.ToString("o"));

            await migrator.MigrateAsync();
        }

        using (var db = new InventoryDbContext(_options))
        {
            var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Username == "alice");
            var share = await db.CollectionShares.IgnoreQueryFilters().SingleAsync();
            Assert.Multiple(() =>
            {
                Assert.That(user.IsDirty, Is.True, "an existing profile must be dirtied so it pushes to the sync folder");
                Assert.That(user.Revision, Is.EqualTo(1));
                Assert.That(share.IsDirty, Is.True, "an existing share must be dirtied so it pushes to the sync folder");
                Assert.That(share.Revision, Is.EqualTo(1));
            });
        }
    }
}
