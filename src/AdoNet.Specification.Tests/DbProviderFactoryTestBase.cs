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

	[Fact]
	public virtual void DbProviderFactory_CreateConnection_is_not_null() => Assert.NotNull(Fixture.Factory.CreateConnection());

	[Fact]
	public virtual void DbProviderFactory_CreateConnectionStringBuilder_is_not_null() => Assert.NotNull(Fixture.Factory.CreateConnectionStringBuilder());

	/// <summary>
	/// The factory must not be required to implement data adapters. When the
	/// capability flag is available, it must agree with the factory result.
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

	[Fact]
	public virtual void DbProviderFactory_CreateParameter_is_not_null() => Assert.NotNull(Fixture.Factory.CreateParameter());

	[Fact]
	public virtual void DbProviderFactory_CanCreateDataSourceEnumerator_is_accurate() => Assert.Equal(Fixture.Factory.CanCreateDataSourceEnumerator, Fixture.Factory.CreateDataSourceEnumerator() is object);
}
