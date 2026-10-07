using System.Data;
using System.Data.Common;
using Xunit;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests the documented output-parameter execution contract for providers that
/// explicitly opt in through <see cref="IOutputParameterFixture"/>.
/// </summary>
public abstract class OutputParameterTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IOutputParameterFixture
{
	protected OutputParameterTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// An output parameter must be populated after successful command execution.
	/// The fixture supplies the SQL and native value details; this shared test
	/// enforces the common DbParameter direction and execution lifecycle.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.direction
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising output-parameter support — https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.direction
	[Fact]
	public virtual void ExecuteNonQuery_populates_output_parameter()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		var parameter = command.CreateParameter();
		parameter.ParameterName = "output";
		parameter.Direction = ParameterDirection.Output;
		Fixture.ConfigureOutputParameter(parameter);
		command.Parameters.Add(parameter);
		command.CommandText = Fixture.CreateOutputParameterSql(MakeParameterName(connection, parameter));

		command.ExecuteNonQuery();

		Fixture.AssertOutputParameterValue(parameter);
	}
}
