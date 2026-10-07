using System.Data;
using Xunit;
using Xunit.Sdk;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Tests non-input parameter directions for providers that explicitly advertise
/// support through <see cref="IParameterDirectionFixture"/>.
/// </summary>
public abstract class ParameterDirectionTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IParameterDirectionFixture
{
	protected ParameterDirectionTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// An InputOutput parameter must be populated after successful execution when
	/// the provider advertises support for that direction.
	/// See https://learn.microsoft.com/dotnet/api/system.data.parameterdirection
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.direction.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising InputOutput support — https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.direction
	[Fact]
	public virtual void ExecuteNonQuery_populates_input_output_parameter()
		=> ExecuteAndAssert(ParameterDirection.InputOutput);

	/// <summary>
	/// A ReturnValue parameter must be populated after successful execution when
	/// the provider advertises support for that direction.
	/// See https://learn.microsoft.com/dotnet/api/system.data.parameterdirection
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.direction.
	/// </summary>
	// Contract: VALID CONTRACT TEST for providers advertising ReturnValue support — https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.direction
	[Fact]
	public virtual void ExecuteNonQuery_populates_return_value_parameter()
		=> ExecuteAndAssert(ParameterDirection.ReturnValue);

	private void ExecuteAndAssert(ParameterDirection direction)
	{
		if (!Fixture.SupportsParameterDirection(direction))
			throw SkipException.ForSkip($"The provider does not support {direction} parameters.");

		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		var parameter = command.CreateParameter();
		parameter.ParameterName = "value";
		parameter.Direction = direction;
		Fixture.ConfigureParameter(parameter, direction);
		command.Parameters.Add(parameter);
		command.CommandText = Fixture.CreateParameterDirectionSql(MakeParameterName(connection, parameter), direction);

		command.ExecuteNonQuery();

		Fixture.AssertParameterValue(parameter, direction);
	}
}
