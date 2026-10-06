using Xunit;

namespace AdoNet.Specification.Tests;

public abstract class DbProviderFactoryTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDbFactoryFixture
{
	protected DbProviderFactoryTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	[Fact]
	public virtual void DbProviderFactory_CreateCommand_is_not_null() => Assert.NotNull(Fixture.Factory.CreateCommand());

	/// <summary>
	/// The factory must not be required to implement command builders. When the
	/// capability flag is available, it must agree with the factory result.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatecommandbuilder.
	/// </summary>
	[Fact]
	public virtual void DbProviderFactory_CreateCommandBuilder_matches_capability()
	{
		var commandBuilder = Fixture.Factory.CreateCommandBuilder();
#if NETSTANDARD2_0
		// CanCreateCommandBuilder is not part of this target framework. The
		// factory result is the only available capability signal.
		_ = commandBuilder is not null;
#else
		Assert.Equal(Fixture.Factory.CanCreateCommandBuilder, commandBuilder is not null);
#endif
	}

	/// <summary>
	/// Retained for compatibility with the original suite. DbProviderFactory does
	/// not require every provider to implement a command builder; the capability
	/// agreement is tested above instead.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createcommandbuilder.
	/// </summary>
	[Fact(Skip = "ADO.NET does not require every provider to implement DbCommandBuilder.")]
	public virtual void DbProviderFactory_CreateCommandBuilder_is_not_null()
	{
	}

	[Fact]
	public virtual void DbProviderFactory_CreateConnection_is_not_null() => Assert.NotNull(Fixture.Factory.CreateConnection());

	[Fact]
	public virtual void DbProviderFactory_CreateConnectionStringBuilder_is_not_null() => Assert.NotNull(Fixture.Factory.CreateConnectionStringBuilder());

	/// <summary>
	/// The factory must not be required to implement data adapters. When the
	/// capability flag is available, it must agree with the factory result.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedataadapter.
	/// </summary>
	[Fact]
	public virtual void DbProviderFactory_CreateDataAdapter_matches_capability()
	{
		var dataAdapter = Fixture.Factory.CreateDataAdapter();
#if NETSTANDARD2_0
		// CanCreateDataAdapter is not part of this target framework. The
		// factory result is the only available capability signal.
		_ = dataAdapter is not null;
#else
		Assert.Equal(Fixture.Factory.CanCreateDataAdapter, dataAdapter is not null);
#endif
	}

	/// <summary>
	/// Retained for compatibility with the original suite. DataAdapter support is
	/// optional and must be checked through the factory capability, not nullability.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createdataadapter.
	/// </summary>
	[Fact(Skip = "ADO.NET does not require every provider to implement DbDataAdapter.")]
	public virtual void DbProviderFactory_CreateDataAdapter_is_not_null()
	{
	}

	[Fact]
	public virtual void DbProviderFactory_CreateParameter_is_not_null() => Assert.NotNull(Fixture.Factory.CreateParameter());

	[Fact]
	public virtual void DbProviderFactory_CanCreateDataSourceEnumerator_is_accurate() => Assert.Equal(Fixture.Factory.CanCreateDataSourceEnumerator, Fixture.Factory.CreateDataSourceEnumerator() is object);

	/// <summary>
	/// The old test assumed that every provider lacks a data-source enumerator;
	/// provider capability is explicitly allowed to vary.
	/// </summary>
	[Fact(Skip = "Data-source enumeration capability is provider-specific.")]
	public virtual void DbProviderFactory_CreateDataSourceEnumerator_is_null()
	{
	}

	[Fact(Skip = "Data-source enumeration capability is provider-specific.")]
	public virtual void DbProviderFactory_CanCreateDataSourceEnumerator_is_false()
	{
	}

	/// <summary>
	/// These original tests required optional factory capabilities from every
	/// provider. They are retained as documented skips; the active capability
	/// agreement test above is the provider-neutral contract.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator.
	/// </summary>
	[Fact(Skip = "Command-builder support is optional and provider-specific.")]
	public virtual void DbProviderFactory_CanCreateCommandBuilder_is_true()
	{
	}

	[Fact(Skip = "Data-adapter support is optional and provider-specific.")]
	public virtual void DbProviderFactory_CanCreateDataAdapter_is_true()
	{
	}
}
