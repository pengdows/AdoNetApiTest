using System.Collections.Generic;
using System.Data.Common;
using AdoNet.Specification.Tests;
using Informix.Net.Core;

namespace Informix.Tests;

public sealed class InformixDbFactoryFixture : IDbFactoryFixture, IDmlFixture
{
    public InformixDbFactoryFixture()
    {
        EnsureDatabase();
    }

    public DbProviderFactory Factory => InformixClientFactory.Instance;

    public string ConnectionString =>
        Environment.GetEnvironmentVariable("INFORMIX_CONNECTION_STRING")
        ?? "Host=localhost;Service=9088;Server=informixserver;Database=testdb;UID=informix;PWD=in4mix;Delimident=true;";

	public IReadOnlyList<string> DmlSetupSql => new[]
	{
		"DROP TABLE IF EXISTS people",
		"CREATE TABLE people (id INTEGER, name VARCHAR(100))",
		"INSERT INTO people (id, name) VALUES (1, 'Alice')",
		"INSERT INTO people (id, name) VALUES (2, 'Bob')"
	};

	public IReadOnlyList<string> DmlCleanupSql => new[] { "DROP TABLE IF EXISTS people" };
    public string DmlInsertSql => "INSERT INTO people (id, name) VALUES (3, 'Carol');";
    public string DmlMultiRowUpdateSql => "UPDATE people SET name = 'Updated' WHERE id > 0;";
    public string DmlUpdateSql => "UPDATE people SET name = 'Alicia' WHERE id = 1;";
    public string DmlUpdateNoRowsSql => "UPDATE people SET name = 'Nobody' WHERE id = 999;";
    public string DmlDeleteSql => "DELETE FROM people WHERE id = 2;";
    public string DmlDeleteNoRowsSql => "DELETE FROM people WHERE id = 999;";

    private void EnsureDatabase()
    {
        using (var existing = Factory.CreateConnection()!)
        {
            existing.ConnectionString = ConnectionString;
            try
            {
                existing.Open();
                return;
            }
            catch (IfxException)
            {
            }
        }

        var builder = new IfxConnectionStringBuilder(ConnectionString)
        {
            Database = string.Empty
        };

        using var connection = Factory.CreateConnection()!;
        connection.ConnectionString = builder.ConnectionString;
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE DATABASE testdb WITH LOG;";
        try
        {
            command.ExecuteNonQuery();
        }
        catch (IfxException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
        }
    }
}
