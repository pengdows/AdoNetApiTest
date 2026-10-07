using System;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests the ADO.NET affected-row contract for data-modification commands.
///
/// This covers the behavior relied upon by optimistic concurrency and write
/// success detection in consumers such as pengdows.crud. It is deliberately
/// separate from the existing SELECT-only ExecuteNonQuery test.
/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery.
/// </summary>
public abstract class DmlTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDbFactoryFixture, IDmlFixture
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
	public virtual void ExecuteNonQuery_returns_affected_rows_for_DML()
	{
		using var connection = CreateOpenConnection();
		try
		{
			ExecuteScript(connection, Fixture.DmlSetupSql);
			// ADO.NET returns the total number of rows affected by a set-based DML statement.
			Assert.Equal(2, Execute(connection, Fixture.DmlMultiRowUpdateSql));
			Assert.Equal(1, Execute(connection, Fixture.DmlInsertSql));
			Assert.Equal(1, Execute(connection, Fixture.DmlUpdateSql));
			Assert.Equal(0, Execute(connection, Fixture.DmlUpdateNoRowsSql));
			Assert.Equal(1, Execute(connection, Fixture.DmlDeleteSql));
			Assert.Equal(0, Execute(connection, Fixture.DmlDeleteNoRowsSql));
		}
		finally
		{
			ExecuteScript(connection, Fixture.DmlCleanupSql);
		}
	}

	/// <summary>
	/// ADO.NET requires ExecuteNonQuery to return -1 for a result-producing
	/// statement such as SELECT; the number of returned rows is not an affected
	/// row count.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery
	[Fact]
	public virtual void ExecuteNonQuery_returns_negative_one_for_result_producing_statement()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		Assert.Equal(-1, command.ExecuteNonQuery());
	}

	private static int Execute(DbConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		return command.ExecuteNonQuery();
	}

	/// <summary>
	/// Fixture setup and cleanup may contain multiple provider-specific statements.
	/// Execute them separately because ADO.NET does not require providers to support
	/// multi-statement prepares; Informix, for example, rejects them.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepare.
	/// </summary>
	private static void ExecuteScript(DbConnection connection, string sql)
	{
		foreach (var statement in sql.Split(';').Select(statement => statement.Trim()).Where(statement => statement.Length > 0))
		{
			Execute(connection, statement);
		}
	}
}
