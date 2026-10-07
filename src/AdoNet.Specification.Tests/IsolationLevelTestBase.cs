using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests explicit isolation-level transactions for providers that opt in with
/// <see cref="IIsolationLevelFixture"/>.
/// </summary>
public abstract class IsolationLevelTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IIsolationLevelFixture
{
	protected IsolationLevelTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// BeginTransaction(level) must create a transaction using the supported level
	/// supplied by the provider fixture.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.begintransaction.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising the isolation level — https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.begintransaction
	[Fact]
	public virtual void BeginTransaction_uses_supported_isolation_level()
	{
		using var connection = CreateOpenConnection();
		using var transaction = connection.BeginTransaction(Fixture.SupportedIsolationLevel);

		Assert.Equal(Fixture.SupportedIsolationLevel, transaction.IsolationLevel);
	}
}
