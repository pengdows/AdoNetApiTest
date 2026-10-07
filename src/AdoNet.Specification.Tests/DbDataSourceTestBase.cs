#if NET10_0_OR_GREATER
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests the modern <see cref="DbDataSource"/> contract for providers that
/// expose <see cref="DbProviderFactory.CreateDataSource(string)"/>.
/// </summary>
public abstract class DbDataSourceTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDbFactoryFixture
{
	protected DbDataSourceTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// A supported data source exposes its provider-defined connection string.
	/// The exact text is intentionally not compared because the API permits the
	/// data source to normalize or augment it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.connectionstring.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising DbDataSource support — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.connectionstring
	[Fact]
	public virtual void ConnectionString_returns_value()
	{
		using var dataSource = CreateDataSourceOrSkip();
		Assert.NotNull(dataSource.ConnectionString);
	}

	/// <summary>
	/// CreateConnection returns a new closed connection for the represented
	/// database.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.createconnection.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising DbDataSource support — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.createconnection
	[Fact]
	public virtual void CreateConnection_returns_closed_connection()
	{
		using var dataSource = CreateDataSourceOrSkip();
		using var connection = dataSource.CreateConnection();
		Assert.NotNull(connection);
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	/// <summary>
	/// OpenConnection returns a new connection that is already open.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.openconnection.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising DbDataSource support — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.openconnection
	[Fact]
	public virtual void OpenConnection_returns_open_connection()
	{
		using var dataSource = CreateDataSourceOrSkip();
		using var connection = dataSource.OpenConnection();
		Assert.NotNull(connection);
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	/// <summary>
	/// OpenConnectionAsync must provide the same open-connection postcondition as
	/// OpenConnection.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.openconnectionasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising DbDataSource support — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.openconnectionasync
	[Fact]
	public virtual async Task OpenConnectionAsync_returns_open_connection()
	{
		using var dataSource = CreateDataSourceOrSkip();
		await using var connection = await dataSource.OpenConnectionAsync().ConfigureAwait(false);
		Assert.NotNull(connection);
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	/// <summary>
	/// CreateCommand returns a command associated with the data source and ready
	/// to execute the supplied command text.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.createcommand.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising DbDataSource support — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.createcommand
	[Fact]
	public virtual void CreateCommand_returns_command_with_command_text()
	{
		using var dataSource = CreateDataSourceOrSkip();
		using var command = dataSource.CreateCommand(SelectOneSql);
		Assert.NotNull(command);
		Assert.Equal(SelectOneSql, command.CommandText);
	}

	/// <summary>
	/// A data source may expose batch creation independently of the provider
	/// factory. Unsupported data sources report that optional capability through
	/// NotSupportedException.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.createbatch.
	/// </summary>
	// Contract: OPTIONAL CAPABILITY TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.createbatch
	[DiagnosticFact]
	public virtual void CreateBatch_returns_batch_when_supported()
	{
		using var dataSource = CreateDataSourceOrSkip();
		try
		{
			using var batch = dataSource.CreateBatch();
			Assert.NotNull(batch);
		}
		catch (NotSupportedException)
		{
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not expose data-source batch creation.");
		}
	}

	/// <summary>
	/// DisposeAsync must release a supported data source without requiring a
	/// provider-specific asynchronous disposal pattern.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.disposeasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising DbDataSource support — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource.disposeasync
	[Fact]
	public virtual async Task DisposeAsync_works()
	{
		var dataSource = CreateDataSourceOrSkip();
		await dataSource.DisposeAsync().ConfigureAwait(false);
	}

	protected virtual DbDataSource CreateDataSourceOrSkip()
	{
		try
		{
			return Fixture.Factory.CreateDataSource(Fixture.ConnectionString)
				?? throw Xunit.Sdk.SkipException.ForSkip("The provider does not expose DbDataSource.");
		}
		catch (NotSupportedException)
		{
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not expose DbDataSource.");
		}
	}
}
#endif
