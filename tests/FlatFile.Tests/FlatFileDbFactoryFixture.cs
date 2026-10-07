using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using pengdows.flatfile;
using AdoNet.Specification.Tests;

namespace FlatFile.Tests;

public sealed class FlatFileDbFactoryFixture : IDbFactoryFixture, IDmlFixture, IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "adonet-flatfile-" + Guid.NewGuid().ToString("N"));

    public FlatFileDbFactoryFixture()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "people.jsonl"),
            "{\"id\":1,\"name\":\"Alice\"}\n{\"id\":2,\"name\":\"Bob\"}\n");
        File.WriteAllText(Path.Combine(_root, "people.table.json"), """
            {
              "name": "people",
              "path": "people.jsonl",
              "format": "Json",
              "json": {},
              "columns": [
                { "name": "id", "clrType": "int" },
                { "name": "name", "clrType": "string" }
              ]
            }
            """);
    }

    public DbProviderFactory Factory => FlatFileProviderFactory.Instance;

    public string ConnectionString => $"path={_root}";

	public IReadOnlyList<string> DmlSetupSql => new[]
	{
		"DELETE FROM people",
		"INSERT INTO people (id, name) VALUES (1, 'Alice'), (2, 'Bob')"
	};

	public IReadOnlyList<string> DmlCleanupSql => new[] { "DELETE FROM people" };

    public string DmlInsertSql => "INSERT INTO people (id, name) VALUES (3, 'Carol');";

    public string DmlMultiRowUpdateSql => "UPDATE people SET name = 'Updated' WHERE id > 0;";

    public string DmlUpdateSql => "UPDATE people SET name = 'Alicia' WHERE id = 1;";

    public string DmlUpdateNoRowsSql => "UPDATE people SET name = 'Nobody' WHERE id = 999;";

    public string DmlDeleteSql => "DELETE FROM people WHERE id = 2;";

    public string DmlDeleteNoRowsSql => "DELETE FROM people WHERE id = 999;";

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
