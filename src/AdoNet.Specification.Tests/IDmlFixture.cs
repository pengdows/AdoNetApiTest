using System.Collections.Generic;

namespace AdoNet.Specification.Tests;

/// <summary>
/// Supplies provider-specific SQL for the common affected-row contract tests.
///
/// The contract under test is <see cref="System.Data.Common.DbCommand.ExecuteNonQuery"/>:
/// INSERT, UPDATE, and DELETE return the number of rows affected by the command.
/// The test SQL is provider-specific because the specification project supports
/// multiple SQL dialects; the expected return values are provider-independent.
/// Implement this interface only when the provider can execute the DML contract
/// tests against a fixture-owned table.
/// </summary>
public interface IDmlFixture : IDbFactoryFixture
{
	/// <summary>
	/// Creates or resets the rows used by the DML tests. Each item is one complete
	/// provider-specific statement; statements must not be combined by splitting
	/// on semicolons because semicolons may occur inside literals or procedural
	/// bodies.
	/// </summary>
	IReadOnlyList<string> DmlSetupSql { get; }
	/// <summary>Returns complete cleanup statements for the DML test state.</summary>
	IReadOnlyList<string> DmlCleanupSql { get; }
	/// <summary>An INSERT expected to affect exactly one row.</summary>
	string DmlInsertSql { get; }
	/// <summary>An UPDATE expected to affect two rows.</summary>
	string DmlMultiRowUpdateSql { get; }
	/// <summary>
	/// An UPDATE expected to affect exactly one row. The statement must change the
	/// stored value; an update that writes the existing value is not a valid test
	/// of the affected-row contract because providers may expose changed-row or
	/// matched-row semantics through their native options.
	/// </summary>
	string DmlUpdateSql { get; }
	/// <summary>An UPDATE expected to affect no rows.</summary>
	string DmlUpdateNoRowsSql { get; }
	/// <summary>A DELETE expected to affect exactly one row.</summary>
	string DmlDeleteSql { get; }
	/// <summary>A DELETE expected to affect no rows.</summary>
	string DmlDeleteNoRowsSql { get; }
}
