namespace AdoNet.Specification.Tests;

/// <summary>
/// Supplies provider-specific SQL for the common affected-row contract tests.
///
/// The contract under test is <see cref="System.Data.Common.DbCommand.ExecuteNonQuery"/>:
/// INSERT, UPDATE, and DELETE return the number of rows affected by the command.
/// The test SQL is provider-specific because the specification project supports
/// multiple SQL dialects; the expected return values are provider-independent.
/// </summary>
public interface IDmlFixture
{
	/// <summary>Creates or resets the rows used by the DML tests.</summary>
	string DmlSetupSql { get; }
	/// <summary>Cleans up the rows used by the DML tests.</summary>
	string DmlCleanupSql { get; }
	/// <summary>An INSERT expected to affect exactly one row.</summary>
	string DmlInsertSql { get; }
	/// <summary>An UPDATE expected to affect two rows.</summary>
	string DmlMultiRowUpdateSql { get; }
	/// <summary>An UPDATE expected to affect exactly one row.</summary>
	string DmlUpdateSql { get; }
	/// <summary>An UPDATE expected to affect no rows.</summary>
	string DmlUpdateNoRowsSql { get; }
	/// <summary>A DELETE expected to affect exactly one row.</summary>
	string DmlDeleteSql { get; }
	/// <summary>A DELETE expected to affect no rows.</summary>
	string DmlDeleteNoRowsSql { get; }
}
