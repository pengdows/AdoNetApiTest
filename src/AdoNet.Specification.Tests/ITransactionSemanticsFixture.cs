using System.Data.Common;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Supplies provider-specific SQL and setup for transaction commit/rollback
/// semantic tests.
/// </summary>
public interface ITransactionSemanticsFixture : IDbFactoryFixture
{
	/// <summary>Creates or resets the rows used by the transaction test.</summary>
	void SetupTransactionSemantics(DbConnection connection);

	/// <summary>Removes the rows used by the transaction test.</summary>
	void CleanupTransactionSemantics(DbConnection connection);

	/// <summary>A command whose effects should be rolled back on disposal.</summary>
	string TransactionalInsertSql { get; }

	/// <summary>
	/// A scalar query returning the number of rows affected by
	/// <see cref="TransactionalInsertSql"/>.
	/// </summary>
	string TransactionalRowCountSql { get; }
}
