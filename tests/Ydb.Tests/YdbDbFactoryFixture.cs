using System;
using System.Collections.Generic;
using System.Data.Common;
using AdoNet.Specification.Tests;
using Ydb.Sdk.Ado;

namespace Ydb.Tests;

public sealed class YdbDbFactoryFixture : IDbFactoryFixture, IDmlFixture, IDisposable
{
    public DbProviderFactory Factory => YdbProviderFactory.Instance;

    public string ConnectionString =>
        Environment.GetEnvironmentVariable("YDB_CONNECTION_STRING")
        ?? "Host=localhost;Port=2136;Database=/local";

	public IReadOnlyList<string> DmlSetupSql => new[]
	{
		"CREATE TABLE IF NOT EXISTS people (id Int32, name Utf8, PRIMARY KEY (id))",
		"UPSERT INTO people (id, name) VALUES (1, 'Alice'), (2, 'Bob')"
	};

	public IReadOnlyList<string> DmlCleanupSql => new[] { "DROP TABLE IF EXISTS people" };
    public string DmlInsertSql => "UPSERT INTO people (id, name) VALUES (3, 'Carol');";
    public string DmlMultiRowUpdateSql => "UPSERT INTO people (id, name) VALUES (1, 'Updated'), (2, 'Updated');";
    public string DmlUpdateSql => "UPSERT INTO people (id, name) VALUES (1, 'Alicia');";
    public string DmlUpdateNoRowsSql => "UPSERT INTO people SELECT 999, 'Nobody' WHERE false;";
    public string DmlDeleteSql => "DELETE FROM people WHERE id = 2;";
    public string DmlDeleteNoRowsSql => "DELETE FROM people WHERE id = 999;";

    public void Dispose()
    {
    }
}
