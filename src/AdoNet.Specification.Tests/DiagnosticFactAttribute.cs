using System.Collections.Generic;
using Xunit;
using Xunit.v3;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Marks a test as provider-interoperability diagnostics rather than a required
/// ADO.NET contract assertion. These tests still execute. Common-behavior
/// differences are emitted as soft warnings; optional capabilities may be
/// skipped when the provider explicitly reports them as unsupported.
/// </summary>
public sealed class DiagnosticFactAttribute : FactAttribute, ITraitAttribute
{
	/// <inheritdoc />
	public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
		new[] { new KeyValuePair<string, string>("Category", "Diagnostic") };
}
