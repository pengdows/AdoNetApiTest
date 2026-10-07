using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace AdoNet.Specification.Tests;

public class TransactionTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDbFactoryFixture
{
	public TransactionTestBase(TFixture fixture)
		: base(fixture)
	{
		Fixture = fixture;
	}

	protected new TFixture Fixture { get; }

	// Contract: WAS INVALID CONTRACT TEST (required Serializable from every provider) — NOW VALID DEFAULT-ISOLATION TEST; https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.begintransaction
	[Fact]
	public virtual void BeginTransaction_throws_when_closed()
	{
		using var connection = CreateConnection();
		Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction());
	}

	// Contract: COMMON BEHAVIOR TEST — DbConnection only requires starting a transaction; whether parallel transactions are rejected is provider-specific; https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.begintransaction
	[DiagnosticFact]
	public virtual void BeginTransaction_throws_when_parallel_transaction()
	{
		using var connection = CreateOpenConnection();
		using (connection.BeginTransaction())
		{
			try
			{
				using var parallel = connection.BeginTransaction();
			}
			catch (InvalidOperationException)
			{
				return;
			}

			throw Xunit.Sdk.SkipException.ForSkip("The provider permits parallel transactions; the base ADO.NET contract does not require rejection.");
		}
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction
	[Fact]
	public virtual void BeginTransaction_works()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		Assert.NotNull(transaction);
		Assert.Same(connection, transaction.Connection);
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// BeginTransactionAsync is required to provide the same transaction contract
	/// as BeginTransaction. The base implementation may delegate to synchronous
	/// transaction creation, so this test validates the result rather than requiring
	/// a particular I/O strategy.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.begintransactionasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.begintransactionasync
	[Fact]
	public virtual async Task BeginTransactionAsync_works()
	{
		using var connection = CreateOpenConnection();
		using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

		Assert.NotNull(transaction);
		Assert.Same(connection, transaction.Connection);
	}

	/// <summary>
	/// CommitAsync must complete the same transaction state transition as Commit.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.commitasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.commitasync
	[Fact]
	public virtual async Task CommitAsync_clears_Connection()
	{
		using var connection = CreateOpenConnection();
		using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

		await transaction.CommitAsync().ConfigureAwait(false);

		Assert.Null(((IDbTransaction)transaction).Connection);
	}

	/// <summary>
	/// RollbackAsync must complete the same transaction state transition as
	/// Rollback.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.rollbackasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.rollbackasync
	[Fact]
	public virtual async Task RollbackAsync_clears_Connection()
	{
		using var connection = CreateOpenConnection();
		using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

		await transaction.RollbackAsync().ConfigureAwait(false);

		Assert.Null(transaction.Connection);
	}
#endif

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction
	[Fact]
	public virtual void Commit_transaction_clears_Connection()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		Assert.Same(connection, transaction.Connection);
		transaction.Commit();
		Assert.Null(transaction.Connection);
	}

	/// <summary>
	/// Disposing a transaction makes it no longer valid, so its associated
	/// connection must no longer be exposed through Connection.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.system-data-idbtransaction-connection.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.system-data-idbtransaction-connection
	[Fact]
	public virtual void Dispose_transaction_clears_Connection()
	{
		using var connection = CreateOpenConnection();
		var transaction = connection.BeginTransaction();

		transaction.Dispose();

		Assert.Null(((IDbTransaction)transaction).Connection);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction
	[Fact]
	public virtual void Commit_transaction_throws_after_Dispose()
	{
		using var connection = CreateOpenConnection();
		var transaction = connection.BeginTransaction();
		transaction.Dispose();
		Assert.Throws<ObjectDisposedException>(() => transaction.Commit());
	}

	// Contract: COMMON BEHAVIOR TEST — the API requires a pending transaction but does not prescribe the exception type after completion; https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.commit
	[DiagnosticFact]
	public virtual void Commit_transaction_twice_throws()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		Assert.Same(connection, transaction.Connection);
		transaction.Commit();
		Assert.ThrowsAny<Exception>(() => transaction.Commit());
	}

	// Contract: COMMON BEHAVIOR TEST — the API requires a pending transaction but does not prescribe the exception type after completion; https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.rollback
	[DiagnosticFact]
	public virtual void Commit_transaction_then_Rollback_throws()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		Assert.Same(connection, transaction.Connection);
		transaction.Commit();
		Assert.ThrowsAny<Exception>(() => transaction.Rollback());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction
	[Fact]
	public virtual void Rollback_transaction_clears_Connection()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		Assert.Same(connection, transaction.Connection);
		transaction.Rollback();
		Assert.Null(transaction.Connection);
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// DisposeAsync must release a transaction and invalidate its associated
	/// connection reference just like synchronous disposal.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.disposeasync
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.system-data-idbtransaction-connection.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.disposeasync
	[Fact]
	public virtual async Task DisposeAsync_clears_Connection()
	{
		using var connection = CreateOpenConnection();
		var transaction = connection.BeginTransaction();

		await transaction.DisposeAsync().ConfigureAwait(false);

		Assert.Null(((IDbTransaction)transaction).Connection);
	}
#endif

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction
	[Fact]
	public virtual void Rollback_transaction_throws_after_Dispose()
	{
		using var connection = CreateOpenConnection();
		var transaction = connection.BeginTransaction();
		transaction.Dispose();
		Assert.Throws<ObjectDisposedException>(() => transaction.Rollback());
	}

	// Contract: COMMON BEHAVIOR TEST — the API requires a pending transaction but does not prescribe the exception type after completion; https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.rollback
	[DiagnosticFact]
	public virtual void Rollback_transaction_twice_throws()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		Assert.Same(connection, transaction.Connection);
		transaction.Rollback();
		Assert.ThrowsAny<Exception>(() => transaction.Rollback());
	}

	// Contract: COMMON BEHAVIOR TEST — the API requires a pending transaction but does not prescribe the exception type after completion; https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.commit
	[DiagnosticFact]
	public virtual void Rollback_transaction_then_Commit_throws()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		Assert.Same(connection, transaction.Connection);
		transaction.Rollback();
		Assert.ThrowsAny<Exception>(() => transaction.Commit());
	}

#if NET10_0_OR_GREATER
	/// <summary>
	/// SupportsSavepoints is the capability agreement for the synchronous
	/// savepoint methods. A provider that reports false must reject each method;
	/// a provider that reports true must implement the complete savepoint surface.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.supportssavepoints
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.save.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.supportssavepoints
	[Fact]
	public virtual void Savepoint_methods_match_support_flag()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction();
		const string savepoint = "adonet_specification_savepoint";

		if (!transaction.SupportsSavepoints)
		{
			Assert.Throws<NotSupportedException>(() => transaction.Save(savepoint));
			Assert.Throws<NotSupportedException>(() => transaction.Rollback(savepoint));
			Assert.Throws<NotSupportedException>(() => transaction.Release(savepoint));
			return;
		}

		transaction.Save(savepoint);
		transaction.Rollback(savepoint);
		transaction.Save(savepoint);
		transaction.Release(savepoint);
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// The asynchronous savepoint methods must obey the same capability contract
	/// as their synchronous counterparts.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.saveasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.saveasync
	[Fact]
	public virtual async Task Savepoint_async_methods_match_support_flag()
	{
		using var connection = CreateOpenConnection();
		await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
		const string savepoint = "adonet_specification_savepoint_async";

		if (!transaction.SupportsSavepoints)
		{
			await Assert.ThrowsAsync<NotSupportedException>(() => transaction.SaveAsync(savepoint)).ConfigureAwait(false);
			await Assert.ThrowsAsync<NotSupportedException>(() => transaction.RollbackAsync(savepoint)).ConfigureAwait(false);
			await Assert.ThrowsAsync<NotSupportedException>(() => transaction.ReleaseAsync(savepoint)).ConfigureAwait(false);
			return;
		}

		await transaction.SaveAsync(savepoint).ConfigureAwait(false);
		await transaction.RollbackAsync(savepoint).ConfigureAwait(false);
		await transaction.SaveAsync(savepoint).ConfigureAwait(false);
		await transaction.ReleaseAsync(savepoint).ConfigureAwait(false);
	}
#endif
#endif
}
