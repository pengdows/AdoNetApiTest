using System.Data.Common;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Supplies the provider-specific pieces needed to test output-parameter
/// execution. Output parameters are an ADO.NET capability, but their SQL shape
/// varies between stored procedures, callable statements, and provider-specific
/// command syntax.
/// </summary>
public interface IOutputParameterFixture : IDbFactoryFixture
{
	/// <summary>
	/// Creates a command text that assigns a result to the supplied provider SQL
	/// parameter marker.
	/// </summary>
	string CreateOutputParameterSql(string parameterMarker);

	/// <summary>
	/// Configures provider-specific type information required by the output
	/// parameter.
	/// </summary>
	void ConfigureOutputParameter(DbParameter parameter);

	/// <summary>
	/// Verifies the provider-specific value returned in the output parameter.
	/// </summary>
	void AssertOutputParameterValue(DbParameter parameter);
}
