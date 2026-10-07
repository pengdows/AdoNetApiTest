using System.Data.Common;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Supplies provider-specific verification for explicit
/// <see cref="DbConnection.EnlistTransaction(System.Transactions.Transaction)"/>
/// support.
/// </summary>
public interface IEnlistmentFixture : IDbFactoryFixture
{
	void SetupEnlistment(DbConnection connection);

	void CleanupEnlistment(DbConnection connection);

	string EnlistmentInsertSql { get; }

	string EnlistmentRowCountSql { get; }
}
