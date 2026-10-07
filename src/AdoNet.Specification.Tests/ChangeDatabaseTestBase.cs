using System.Threading.Tasks;
using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests the documented ChangeDatabase contract for providers that explicitly
/// advertise a valid alternate database through <see cref="IChangeDatabaseFixture"/>.
/// </summary>
public abstract class ChangeDatabaseTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IChangeDatabaseFixture
{
	protected ChangeDatabaseTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// ChangeDatabase changes the database associated with an open connection.
	/// The fixture supplies the valid target and provider-specific verification.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.changedatabase.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising ChangeDatabase support — https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.changedatabase
	[Fact]
	public virtual void ChangeDatabase_changes_open_connection_database()
	{
		using var connection = CreateOpenConnection();
		connection.ChangeDatabase(Fixture.ChangeDatabaseTarget);
		Fixture.AssertChangedDatabase(connection);
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// ChangeDatabaseAsync must perform the same database transition as the
	/// synchronous API for a provider that supports changing databases.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.changedatabaseasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising ChangeDatabase support — https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.changedatabaseasync
	[Fact]
	public virtual async Task ChangeDatabaseAsync_changes_open_connection_database()
	{
		using var connection = CreateOpenConnection();
		await connection.ChangeDatabaseAsync(Fixture.ChangeDatabaseTarget).ConfigureAwait(false);
		Fixture.AssertChangedDatabase(connection);
	}
#endif
}
