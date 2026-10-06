using System.Data.Common;
using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests the ADO.NET affected-row contract for data-modification commands.
///
/// This covers the behavior relied upon by optimistic concurrency and write
/// success detection in consumers such as pengdows.crud. It is deliberately
/// separate from the existing SELECT-only ExecuteNonQuery test.
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
	/// </summary>
	[Fact]
	public virtual void ExecuteNonQuery_returns_affected_rows_for_DML()
	{
		using var connection = CreateOpenConnection();
		try
		{
			Execute(connection, Fixture.DmlSetupSql);
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
			Execute(connection, Fixture.DmlCleanupSql);
		}
	}

	/// <summary>
	/// ADO.NET requires ExecuteNonQuery to return -1 for a result-producing
	/// statement such as SELECT; the number of returned rows is not an affected
	/// row count.
	/// </summary>
	[Fact]
	public virtual void ExecuteNonQuery_returns_negative_one_for_result_producing_statement()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT 1;";
		Assert.Equal(-1, command.ExecuteNonQuery());
	}

	private static int Execute(DbConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		return command.ExecuteNonQuery();
	}
}
