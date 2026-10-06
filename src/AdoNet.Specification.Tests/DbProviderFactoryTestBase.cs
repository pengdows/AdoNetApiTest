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
	/// Retained as an implementation-comparison diagnostic. Providers that do not
	/// support this optional API can override the test; the capability agreement is
	/// tested separately above.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createcommandbuilder.
	/// </summary>
	[Fact]
	public virtual void DbProviderFactory_CreateCommandBuilder_is_not_null()
		=> Assert.NotNull(Fixture.Factory.CreateCommandBuilder());

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
	/// Retained as an implementation-comparison diagnostic. Providers that do not
	/// support this optional API can override the test; the capability agreement is
	/// tested separately above.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createdataadapter.
	/// </summary>
	[Fact]
	public virtual void DbProviderFactory_CreateDataAdapter_is_not_null()
		=> Assert.NotNull(Fixture.Factory.CreateDataAdapter());

	[Fact]
	public virtual void DbProviderFactory_CreateParameter_is_not_null() => Assert.NotNull(Fixture.Factory.CreateParameter());

	/// <summary>
	/// The capability flag must agree with whether the factory creates a data-source
	/// enumerator. Providers that expose this optional API remain visible in the
	/// comparison output.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator.
	/// </summary>
	[Fact]
	public virtual void DbProviderFactory_CanCreateDataSourceEnumerator_is_accurate() => Assert.Equal(Fixture.Factory.CanCreateDataSourceEnumerator, Fixture.Factory.CreateDataSourceEnumerator() is object);

	/// <summary>
	/// Retained as an implementation-comparison diagnostic for the historical
	/// provider set. Providers with a data-source enumerator can override it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.createdatasourceenumerator.
	/// </summary>
	[Fact]
	public virtual void DbProviderFactory_CreateDataSourceEnumerator_is_null()
		=> Assert.Null(Fixture.Factory.CreateDataSourceEnumerator());

	/// <summary>
	/// Retained as the original implementation-comparison diagnostic for providers
	/// expected not to expose a data-source enumerator; providers may override it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator.
	/// </summary>
	[Fact]
	public virtual void DbProviderFactory_CanCreateDataSourceEnumerator_is_false()
		=> Assert.False(Fixture.Factory.CanCreateDataSourceEnumerator);

	/// <summary>
	/// These original tests remain active so the suite continues to compare
	/// implementations. Providers with different optional-capability support can
	/// override them; the active capability agreement test above is also reported.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory.cancreatedatasourceenumerator.
	/// </summary>
	#if NETSTANDARD2_0
	[Fact]
	public virtual void DbProviderFactory_CanCreateCommandBuilder_is_true()
		=> throw Xunit.Sdk.SkipException.ForSkip("Not supported on this TargetFramework");

	[Fact]
	public virtual void DbProviderFactory_CanCreateDataAdapter_is_true()
		=> throw Xunit.Sdk.SkipException.ForSkip("Not supported on this TargetFramework");
	#else
	[Fact]
	public virtual void DbProviderFactory_CanCreateCommandBuilder_is_true()
		=> Assert.True(Fixture.Factory.CanCreateCommandBuilder);

	[Fact]
	public virtual void DbProviderFactory_CanCreateDataAdapter_is_true()
		=> Assert.True(Fixture.Factory.CanCreateDataAdapter);
	#endif
}
