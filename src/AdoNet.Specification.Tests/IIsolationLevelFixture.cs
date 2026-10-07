using System.Data;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Identifies an isolation level that the configured provider supports for an
/// explicit transaction request.
/// </summary>
public interface IIsolationLevelFixture : IDbFactoryFixture
{
	IsolationLevel SupportedIsolationLevel { get; }
}
