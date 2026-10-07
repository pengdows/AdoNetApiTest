using System;
using System.Data.Common;
using Xunit;

namespace AdoNet.Specification.Tests;

public abstract class DbProviderFactoryTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDbFactoryFixture
{
	protected DbProviderFactoryTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory
	[Fact]
	public virtual void DbProviderFactory_CreateCommand_is_not_null() => Assert.NotNull(Fixture.Factory.CreateCommand());

	/// <summary>
	/// The factory must not be required to implement command builders. When the
	/// capability flag is available, it must agree with the factory result.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatecommandbuilder.
	/// </summary>
	// Contract: OPTIONAL CAPABILITY TEST — providers that do not expose command builders are explicitly skipped; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatecommandbuilder
	[Fact]
	public virtual void DbProviderFactory_CreateCommandBuilder_matches_capability()
	{
		DbCommandBuilder commandBuilder;
		try
		{
			commandBuilder = Fixture.Factory.CreateCommandBuilder();
		}
		catch (NotSupportedException)
		{
			commandBuilder = null;
		}
#if NETSTANDARD2_0
		// CanCreateCommandBuilder is not part of this target framework. The
		// factory result is the only available capability signal.
		if (commandBuilder is null)
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not expose the optional command-builder capability.");
#else
		AssertOptionalFactoryObject(Fixture.Factory.CanCreateCommandBuilder, commandBuilder is not null, "command builder");
#endif
	}

	/// <summary>
	/// Retained under its historical name, but now checks the optional capability
	/// agreement rather than requiring a command builder.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createcommandbuilder.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST (required command builder) — NOW VALID OPTIONAL CAPABILITY-AGREEMENT TEST; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatecommandbuilder
	[Fact]
	#if NETSTANDARD2_0
	public virtual void DbProviderFactory_CreateCommandBuilder_is_not_null()
		=> throw Xunit.Sdk.SkipException.ForSkip("CanCreateCommandBuilder is not available on this target framework.");
	#else
	public virtual void DbProviderFactory_CreateCommandBuilder_is_not_null()
		=> AssertOptionalFactoryObject(Fixture.Factory.CanCreateCommandBuilder, Fixture.Factory.CreateCommandBuilder() is not null, "command builder");
	#endif

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory
	[Fact]
	public virtual void DbProviderFactory_CreateConnection_is_not_null() => Assert.NotNull(Fixture.Factory.CreateConnection());

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory
	[Fact]
	public virtual void DbProviderFactory_CreateConnectionStringBuilder_is_not_null() => Assert.NotNull(Fixture.Factory.CreateConnectionStringBuilder());

	/// <summary>
	/// The factory must not be required to implement data adapters. When the
	/// capability flag is available, it must agree with the factory result.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedataadapter.
	/// </summary>
	// Contract: OPTIONAL CAPABILITY TEST — providers that do not expose data adapters are explicitly skipped; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedataadapter
	[Fact]
	public virtual void DbProviderFactory_CreateDataAdapter_matches_capability()
	{
		System.Data.Common.DbDataAdapter dataAdapter;
		try
		{
			dataAdapter = Fixture.Factory.CreateDataAdapter();
		}
		catch (NotSupportedException)
		{
			dataAdapter = null;
		}
#if NETSTANDARD2_0
		// CanCreateDataAdapter is not part of this target framework. The
		// factory result is the only available capability signal.
		if (dataAdapter is null)
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not expose the optional data-adapter capability.");
#else
		AssertOptionalFactoryObject(Fixture.Factory.CanCreateDataAdapter, dataAdapter is not null, "data adapter");
#endif
	}

	/// <summary>
	/// Retained under its historical name, but now checks the optional capability
	/// agreement rather than requiring a data adapter.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createdataadapter.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST (required data adapter) — NOW VALID OPTIONAL CAPABILITY-AGREEMENT TEST; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedataadapter
	[Fact]
	#if NETSTANDARD2_0
	public virtual void DbProviderFactory_CreateDataAdapter_is_not_null()
		=> throw Xunit.Sdk.SkipException.ForSkip("CanCreateDataAdapter is not available on this target framework.");
	#else
	public virtual void DbProviderFactory_CreateDataAdapter_is_not_null()
		=> AssertOptionalFactoryObject(Fixture.Factory.CanCreateDataAdapter, Fixture.Factory.CreateDataAdapter() is not null, "data adapter");
	#endif

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory
	[Fact]
	public virtual void DbProviderFactory_CreateParameter_is_not_null() => Assert.NotNull(Fixture.Factory.CreateParameter());

#if NET10_0_OR_GREATER
	/// <summary>
	/// CanCreateBatch must agree with whether CreateBatch provides a batch object.
	/// A provider that does not support batches may report that through
	/// NotSupportedException; it must not advertise support and then fail.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatebatch
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createbatch.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatebatch
	[Fact]
	public virtual void DbProviderFactory_CreateBatch_matches_capability()
	{
		DbBatch batch;
		try
		{
			batch = Fixture.Factory.CreateBatch();
		}
		catch (NotSupportedException)
		{
			Assert.False(Fixture.Factory.CanCreateBatch);
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not implement factory-level batches.");
		}

		if (batch is null)
		{
			Assert.False(Fixture.Factory.CanCreateBatch, "The provider advertises batch support but returned null.");
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not implement factory-level batches.");
		}

		Assert.True(Fixture.Factory.CanCreateBatch, "The provider returned a batch while advertising batch support as unavailable.");
		batch.Dispose();
	}

	/// <summary>
	/// A factory advertising batch support must create a batch command.
	/// Unsupported factories may report NotSupportedException.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createbatchcommand.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createbatchcommand
	[Fact]
	public virtual void DbProviderFactory_CreateBatchCommand_matches_capability()
	{
		DbBatchCommand batchCommand;
		try
		{
			batchCommand = Fixture.Factory.CreateBatchCommand();
		}
		catch (NotSupportedException)
		{
			Assert.False(Fixture.Factory.CanCreateBatch);
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not implement factory-level batch commands.");
		}

		if (batchCommand is null)
		{
			Assert.False(Fixture.Factory.CanCreateBatch, "The provider advertises batch-command support but returned null.");
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not implement factory-level batch commands.");
		}

		Assert.True(Fixture.Factory.CanCreateBatch,
			"The provider returned a batch command while advertising batch support as unavailable.");
	}

	/// <summary>
	/// CreateDataSource is a modern optional factory API. Providers that implement
	/// it must return a usable data-source; providers that do not may explicitly
	/// report NotSupportedException.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createdatasource.
	/// </summary>
	// Contract: OPTIONAL CAPABILITY TEST — unimplemented data sources are skipped; an implemented data source must be usable; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createdatasource
	[Fact]
	public virtual void DbProviderFactory_CreateDataSource_is_usable_when_supported()
	{
		try
		{
			using var dataSource = Fixture.Factory.CreateDataSource(Fixture.ConnectionString);
			Assert.NotNull(dataSource);
		}
		catch (NotSupportedException)
		{
			throw Xunit.Sdk.SkipException.ForSkip("The provider does not expose the optional data-source capability.");
		}
	}
#endif

	/// <summary>
	/// The capability flag must agree with whether the factory creates a data-source
	/// enumerator. Providers that expose this optional API remain visible in the
	/// comparison output.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory
	[Fact]
	public virtual void DbProviderFactory_CanCreateDataSourceEnumerator_is_accurate()
	{
		var enumerator = Fixture.Factory.CreateDataSourceEnumerator();
		AssertOptionalFactoryObject(Fixture.Factory.CanCreateDataSourceEnumerator, enumerator is not null, "data-source enumerator");
	}

	/// <summary>
	/// Retained under its historical name, but now checks the optional enumerator
	/// capability agreement rather than requiring a null result.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createdatasourceenumerator.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST (required data-source enumerator) — NOW VALID OPTIONAL CAPABILITY-AGREEMENT TEST; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator
	[Fact]
	public virtual void DbProviderFactory_CreateDataSourceEnumerator_is_null()
		=> AssertOptionalFactoryObject(Fixture.Factory.CanCreateDataSourceEnumerator, Fixture.Factory.CreateDataSourceEnumerator() is not null, "data-source enumerator");

	/// <summary>
	/// Retained under its historical name, but now checks the optional enumerator
	/// capability agreement rather than requiring the flag to be false.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST (required no enumerator) — NOW VALID OPTIONAL CAPABILITY-AGREEMENT TEST; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator
	[Fact]
	public virtual void DbProviderFactory_CanCreateDataSourceEnumerator_is_false()
		=> AssertOptionalFactoryObject(Fixture.Factory.CanCreateDataSourceEnumerator, Fixture.Factory.CreateDataSourceEnumerator() is not null, "data-source enumerator");

	/// <summary>
	/// A factory advertising data-source enumeration must return an enumerator
	/// whose documented metadata operation completes with a data table. An
	/// unadvertised capability is explicitly skipped; an advertised capability
	/// that cannot be used is a hard failure.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasourceenumerator.getdatasources.
	/// </summary>
	// Contract: OPTIONAL CAPABILITY TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasourceenumerator.getdatasources
	[Fact]
	public virtual void DbDataSourceEnumerator_GetDataSources_matches_capability()
	{
		var enumerator = Fixture.Factory.CreateDataSourceEnumerator();
		AssertOptionalFactoryObject(Fixture.Factory.CanCreateDataSourceEnumerator, enumerator is not null, "data-source enumerator");
		Assert.NotNull(enumerator.GetDataSources());
	}

	/// <summary>
	/// These original test names remain active so existing provider test classes do
	/// not lose their inherited surface. Their assertions now validate capability
	/// agreement rather than imposing a provider-specific feature set.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator.
	/// </summary>
	#if NETSTANDARD2_0
	// Contract: TARGET-FRAMEWORK-LIMITED OPTIONAL CAPABILITY CHECK — capability property is unavailable on netstandard2.0; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory
	[Fact]
	public virtual void DbProviderFactory_CanCreateCommandBuilder_is_true()
		=> throw Xunit.Sdk.SkipException.ForSkip("Not supported on this TargetFramework");

	// Contract: TARGET-FRAMEWORK-LIMITED OPTIONAL CAPABILITY CHECK — capability property is unavailable on netstandard2.0; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory
	[Fact]
	public virtual void DbProviderFactory_CanCreateDataAdapter_is_true()
		=> throw Xunit.Sdk.SkipException.ForSkip("Not supported on this TargetFramework");
	#else
	// Contract: WAS INVALID CONTRACT TEST (required command-builder support) — NOW VALID OPTIONAL CAPABILITY-AGREEMENT TEST; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatecommandbuilder
	[Fact]
	public virtual void DbProviderFactory_CanCreateCommandBuilder_is_true()
		=> AssertOptionalFactoryObject(Fixture.Factory.CanCreateCommandBuilder, Fixture.Factory.CreateCommandBuilder() is not null, "command builder");

	// Contract: WAS INVALID CONTRACT TEST (required data-adapter support) — NOW VALID OPTIONAL CAPABILITY-AGREEMENT TEST; https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedataadapter
	[Fact]
	public virtual void DbProviderFactory_CanCreateDataAdapter_is_true()
		=> AssertOptionalFactoryObject(Fixture.Factory.CanCreateDataAdapter, Fixture.Factory.CreateDataAdapter() is not null, "data adapter");
	#endif

	private static void AssertOptionalFactoryObject(bool capability, bool created, string feature)
	{
		if (capability)
		{
			Assert.True(created, $"The provider advertises {feature} support but did not create it.");
			return;
		}

		Assert.False(created, $"The provider created {feature} support while advertising it as unavailable.");

		throw Xunit.Sdk.SkipException.ForSkip($"The provider does not expose the optional {feature} capability.");
	}
}
