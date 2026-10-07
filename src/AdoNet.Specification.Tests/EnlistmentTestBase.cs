using System;
using System.Data.Common;
using System.Transactions;
using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests explicit transaction enlistment for providers that opt in through
/// <see cref="IEnlistmentFixture"/>.
/// </summary>
public abstract class EnlistmentTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IEnlistmentFixture
{
	protected EnlistmentTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// A connection explicitly enlisted in a committable transaction must commit
	/// its work when that transaction commits. SQL and row identity are fixture
	/// responsibilities; enlistment and commit behavior are the shared contract.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.enlisttransaction.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising explicit enlistment — https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.enlisttransaction
	[Fact]
	public virtual void EnlistTransaction_commits_connection_work()
	{
		using var connection = CreateOpenConnection();
		Fixture.SetupEnlistment(connection);

		try
		{
			using var transaction = new CommittableTransaction();
			connection.EnlistTransaction(transaction);

			using (var command = connection.CreateCommand())
			{
				command.CommandText = Fixture.EnlistmentInsertSql;
				command.ExecuteNonQuery();
			}

			transaction.Commit();

			using var verification = connection.CreateCommand();
			verification.CommandText = Fixture.EnlistmentRowCountSql;
			Assert.Equal(1, Convert.ToInt32(verification.ExecuteScalar()));
		}
		finally
		{
			Fixture.CleanupEnlistment(connection);
		}
	}
}
