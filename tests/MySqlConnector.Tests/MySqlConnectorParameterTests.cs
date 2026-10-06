using AdoNet.Specification.Tests;

namespace MySqlConnector.Tests;

public sealed class MySqlConnectorParameterTests : ParameterTestBase<MySqlConnectorDbFactoryFixture>
{
	public MySqlConnectorParameterTests(MySqlConnectorDbFactoryFixture fixture)
		: base(fixture)
	{
	}

}
