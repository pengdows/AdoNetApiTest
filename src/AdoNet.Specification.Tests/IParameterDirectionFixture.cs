using System.Data;
using System.Data.Common;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Supplies provider-specific SQL and value verification for non-input
/// parameter directions.
/// </summary>
public interface IParameterDirectionFixture : IDbFactoryFixture
{
	/// <summary>Whether the provider supports the requested direction.</summary>
	bool SupportsParameterDirection(ParameterDirection direction);

	/// <summary>Creates command text using the supplied SQL marker.</summary>
	string CreateParameterDirectionSql(string parameterMarker, ParameterDirection direction);

	/// <summary>Configures native type information for the parameter.</summary>
	void ConfigureParameter(DbParameter parameter, ParameterDirection direction);

	/// <summary>Verifies the provider-specific value returned by the command.</summary>
	void AssertParameterValue(DbParameter parameter, ParameterDirection direction);
}
