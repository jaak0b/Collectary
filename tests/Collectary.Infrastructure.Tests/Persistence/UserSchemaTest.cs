using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Tests.Persistence;

[TestFixture]
public class UserSchemaTest : DbIntegrationTestBase
{
    [Test]
    public void UsersTable_HasNoEmailColumn()
    {
        using var db = DbFactory();
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Users')";
        var columns = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            columns.Add(reader.GetString(1));

        Assert.That(columns, Does.Not.Contain("Email"));
    }

    [Test]
    public void Schema_HasNoUserCredentialsTable()
    {
        using var db = DbFactory();
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type='table' AND name='UserCredentials'";

        Assert.That(command.ExecuteScalar(), Is.Null);
    }
}
