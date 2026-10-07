using System;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests the ADO.NET affected-row contract for data-modification commands.
///
/// This covers the behavior relied upon by optimistic concurrency and write
/// success detection. It is deliberately separate from the existing SELECT-only
/// ExecuteNonQuery test.
/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery.
/// </summary>
public abstract class DmlTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDmlFixture
{
	protected DmlTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// ADO.NET requires INSERT, UPDATE, and DELETE to return their affected-row
	/// count, including zero when the predicate matches no rows.
	/// This is the contract consumed by optimistic-concurrency code: a zero count
	/// means that no row matched the write predicate.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery
	[Fact]
	public virtual void ExecuteNonQuery_returns_multirow_affected_count_for_DML()
		=> WithDmlState(connection => Assert.Equal(2, Execute(connection, Fixture.DmlMultiRowUpdateSql)));

	[Fact]
	public virtual void ExecuteNonQuery_returns_inserted_row_count()
		=> WithDmlState(connection => Assert.Equal(1, Execute(connection, Fixture.DmlInsertSql)));

	[Fact]
	public virtual void ExecuteNonQuery_returns_updated_row_count()
		=> WithDmlState(connection => Assert.Equal(1, Execute(connection, Fixture.DmlUpdateSql)));

	[Fact]
	public virtual void ExecuteNonQuery_returns_zero_when_update_matches_no_rows()
		=> WithDmlState(connection => Assert.Equal(0, Execute(connection, Fixture.DmlUpdateNoRowsSql)));

	[Fact]
	public virtual void ExecuteNonQuery_returns_deleted_row_count()
		=> WithDmlState(connection => Assert.Equal(1, Execute(connection, Fixture.DmlDeleteSql)));

	[Fact]
	public virtual void ExecuteNonQuery_returns_zero_when_delete_matches_no_rows()
		=> WithDmlState(connection => Assert.Equal(0, Execute(connection, Fixture.DmlDeleteNoRowsSql)));

	[Fact]
	public virtual async Task ExecuteNonQueryAsync_returns_affected_rows_for_DML()
		=> await WithDmlStateAsync(async connection =>
			Assert.Equal(1, await ExecuteAsync(connection, Fixture.DmlUpdateSql).ConfigureAwait(false))).ConfigureAwait(false);

	[Fact]
	public virtual void ExecuteReader_returns_affected_rows_after_close_for_DML()
		=> WithDmlState(connection =>
		{
			using var command = connection.CreateCommand();
			command.CommandText = Fixture.DmlDeleteSql;
			using var reader = command.ExecuteReader();
			reader.Close();
			Assert.Equal(1, reader.RecordsAffected);
		});

	private static int Execute(DbConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		return command.ExecuteNonQuery();
	}

	/// <summary>
	/// Fixture setup and cleanup may contain multiple provider-specific statements.
	/// Execute them separately because ADO.NET does not require providers to support
	/// multi-statement prepares; Informix, for example, rejects them. Cleanup is
	/// best-effort so it cannot replace the setup or assertion failure.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepare.
	/// </summary>
	private void WithDmlState(Action<DbConnection> action)
	{
		using var connection = CreateOpenConnection();
		try
		{
			ExecuteScript(connection, Fixture.DmlSetupSql);
			action(connection);
		}
		finally
		{
			TryExecuteScript(connection, Fixture.DmlCleanupSql);
		}
	}

	private async Task WithDmlStateAsync(Func<DbConnection, Task> action)
	{
		using var connection = CreateOpenConnection();
		try
		{
			ExecuteScript(connection, Fixture.DmlSetupSql);
			await action(connection).ConfigureAwait(false);
		}
		finally
		{
			TryExecuteScript(connection, Fixture.DmlCleanupSql);
		}
	}

	private static void ExecuteScript(DbConnection connection, System.Collections.Generic.IEnumerable<string> statements)
	{
		foreach (var statement in statements)
		{
			if (!string.IsNullOrWhiteSpace(statement))
				Execute(connection, statement);
		}
	}

	private static void TryExecuteScript(DbConnection connection, System.Collections.Generic.IEnumerable<string> statements)
	{
		try
		{
			ExecuteScript(connection, statements);
		}
		catch (Exception)
		{
			// Cleanup must not replace the assertion or setup exception.
		}
	}

	private static async Task<int> ExecuteAsync(DbConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
	}
}
