using System;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Records a provider interoperability difference without turning it into an
/// xUnit skip or a conformance failure. The test still executes and remains in
/// the result set; the warning is captured with the test's console output.
/// </summary>
internal static class SoftWarning
{
	public static void Report(string message) => Console.WriteLine($"[SOFT WARNING] {message}");
}
