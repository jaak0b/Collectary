using Collectary.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Tests;

public abstract class DbIntegrationTestBase
{
    private string _dbPath = null!;
    protected DbContextOptions<InventoryDbContext> Options = null!;

    [SetUp]
    public void BaseSetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"collectary-test-{Guid.NewGuid():N}.db");
        Options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_dbPath};Foreign Keys=True")
            .Options;
        using var db = new InventoryDbContext(Options);
        db.Database.EnsureCreated();
    }

    [TearDown]
    public void BaseTearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    protected Func<InventoryDbContext> DbFactory => () => new InventoryDbContext(Options);
}

public abstract class FileSystemTestBase
{
    protected string TempDir = null!;

    [SetUp]
    public void BaseSetUp() =>
        TempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [TearDown]
    public void BaseTearDown()
    {
        if (Directory.Exists(TempDir))
            Directory.Delete(TempDir, recursive: true);
    }
}
