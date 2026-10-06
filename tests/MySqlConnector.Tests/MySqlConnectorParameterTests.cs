using AdoNet.Specification.Tests;

namespace MySqlConnector.Tests;

public sealed class MySqlConnectorParameterTests : ParameterTestBase<MySqlConnectorDbFactoryFixture>
{
	public MySqlConnectorParameterTests(MySqlConnectorDbFactoryFixture fixture)
		: base(fixture)
	{
	}

	[Fact(Skip = "Allows `null` as well as `DBNull.Value` for backwards compatibility.")]
	public override void Bind_requires_set_value()
	{
	}

}
