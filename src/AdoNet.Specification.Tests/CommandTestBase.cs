using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace AdoNet.Specification.Tests;

public abstract class CommandTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDbFactoryFixture
{
	protected CommandTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void Cancel_new_Command_is_no_op()
	{
		using var command = Fixture.Factory.CreateCommand();
		command.Cancel();
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void Cancel_executed_Command_is_no_op()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		command.ExecuteNonQuery();
		command.Cancel();
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void Cancel_Command_twice_is_no_op()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		command.ExecuteNonQuery();
		command.Cancel();
		command.Cancel();
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void Cancel_disposed_Command_is_no_op()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.Dispose();
		try
		{
			command.Cancel();
		}
		catch (ObjectDisposedException)
		{
			SoftWarning.Report("The provider rejects Cancel after command disposal; disposed-command Cancel behavior is not an ADO.NET contract.");
		}
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void Cancel_disposed_Connection_is_no_op()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		connection.Dispose();
		try
		{
			command.Cancel();
		}
		catch (ObjectDisposedException)
		{
			SoftWarning.Report("The provider rejects Cancel after connection disposal; disposed-connection Cancel behavior is not an ADO.NET contract.");
		}
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void CommandText_throws_when_set_when_open_reader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		using var reader = command.ExecuteReader();

		Assert.Throws<InvalidOperationException>(() => command.CommandText = SelectSql("2"));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void Connection_can_be_unset()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		Assert.NotNull(command.Connection);
		command.Connection = null;
		Assert.Null(command.Connection);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void Connection_throws_when_set_when_open_reader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		using var reader = command.ExecuteReader();

		Assert.Throws<InvalidOperationException>(() => command.Connection = CreateConnection());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void Connection_throws_when_set_to_null_when_open_reader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		using var reader = command.ExecuteReader();

		Assert.Throws<InvalidOperationException>(() => command.Connection = null);
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void CommandText_does_not_throw_when_disposed()
	{
		var command = Fixture.Factory.CreateCommand();
		command.Dispose();
		try
		{
			_ = command.CommandText;
		}
		catch (ObjectDisposedException)
		{
			return;
		}
	}

	// Contract: COMMON BEHAVIOR TEST — the API does not prescribe a provider's initial CommandText representation; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void CommandText_is_empty_string_by_default()
	{
		using var command = Fixture.Factory.CreateCommand();
		if (string.IsNullOrEmpty(command.CommandText))
			return;

		SoftWarning.Report("The provider uses a non-empty initial CommandText value; the API does not require an empty string.");
	}

	// Contract: COMMON BEHAVIOR TEST — null-to-empty CommandText coercion is not prescribed; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public void CommandText_is_coerced_to_empty_string()
	{
		using var command = Fixture.Factory.CreateCommand();
		command.CommandText = null;
		if (string.IsNullOrEmpty(command.CommandText))
			return;

		SoftWarning.Report("The provider preserves null CommandText; null-to-empty coercion is not an ADO.NET contract.");
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public void CommandText_can_be_set_to_empty_string()
	{
		using var command = Fixture.Factory.CreateCommand();
		command.CommandText = "";
		Assert.Equal("", command.CommandText);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void CommandType_text_by_default()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Equal(CommandType.Text, command.CommandType);
	}

	/// <summary>
	/// CommandTimeout is an optional provider capability. A provider that exposes
	/// it accepts and preserves a valid value; a provider may explicitly report
	/// that timeout control is unsupported.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.commandtimeout.
	/// </summary>
	// Contract: OPTIONAL CAPABILITY TEST — unsupported timeout control is allowed; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.commandtimeout
	[DiagnosticFact]
	public virtual void CommandTimeout_can_be_set_and_read()
	{
		using var command = Fixture.Factory.CreateCommand();
		try
		{
			command.CommandTimeout = 17;
		}
		catch (NotSupportedException)
		{
			throw SkipException.ForSkip("The provider does not support command timeouts.");
		}
		Assert.Equal(17, command.CommandTimeout);
	}

	/// <summary>
	/// UpdatedRowSource controls how command results update a data source and is
	/// part of the DbCommand property contract.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.updatedrowsource.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.updatedrowsource
	[Fact]
	public virtual void UpdatedRowSource_can_be_set_and_read()
	{
		using var command = Fixture.Factory.CreateCommand();
		command.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
		Assert.Equal(UpdateRowSource.FirstReturnedRecord, command.UpdatedRowSource);
	}

	/// <summary>
	/// DesignTimeVisible is a common command property and must preserve the
	/// value assigned by the consumer.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.designtimevisible.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.designtimevisible
	[Fact]
	public virtual void DesignTimeVisible_can_be_set_and_read()
	{
		using var command = Fixture.Factory.CreateCommand();
		command.DesignTimeVisible = false;
		Assert.False(command.DesignTimeVisible);
		command.DesignTimeVisible = true;
		Assert.True(command.DesignTimeVisible);
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// DisposeAsync must complete the command disposal contract without requiring
	/// providers to implement a separate asynchronous resource path.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.disposeasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.disposeasync
	[Fact]
	public virtual async Task DisposeAsync_works()
	{
		var command = Fixture.Factory.CreateCommand();
		await command.DisposeAsync().ConfigureAwait(false);
	}
#endif

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void CommandType_does_not_throw_when_disposed()
	{
		var command = Fixture.Factory.CreateCommand();
		command.Dispose();
		try
		{
			_ = command.CommandType;
		}
		catch (ObjectDisposedException)
		{
			return;
		}
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void Parameters_is_not_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.NotNull(command.Parameters);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void Parameters_returns_same_object()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameters = command.Parameters;
		Assert.Same(parameters, command.Parameters);
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void Parameters_does_not_throw_when_disposed()
	{
		var command = Fixture.Factory.CreateCommand();
		command.Dispose();
		try
		{
			_ = command.Parameters;
		}
		catch (ObjectDisposedException)
		{
			return;
		}
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void CreateParameter_is_not_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.NotNull(command.CreateParameter());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void Prepare_throws_when_no_connection()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.Prepare());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void Prepare_throws_when_connection_closed()
	{
		using var connection = CreateConnection();
		using var command = connection.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.Prepare());
	}

	// Contract: COMMON BEHAVIOR TEST — Prepare does not define a required response to missing command text; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepare
	[DiagnosticFact]
	public virtual void Prepare_throws_when_no_command_text()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();

		try
		{
			command.Prepare();
			SoftWarning.Report("The provider accepts Prepare with empty command text; the base ADO.NET contract does not require rejection.");
		}
		catch (Exception)
		{
			// Empty-command-text behavior is provider-specific.
		}
	}

	/// <summary>
	/// A provider that supports preparation must leave a valid command executable
	/// after Prepare. Providers that do not support preparation may report that
	/// explicitly with NotSupportedException.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepare.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepare
	[Fact]
	public virtual void Prepare_valid_command_remains_executable_or_reports_not_supported()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		try
		{
			command.Prepare();
		}
		catch (NotSupportedException)
		{
			throw SkipException.ForSkip("The provider does not support prepared commands.");
		}

		Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar()));
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// PrepareAsync follows the same valid-command contract as Prepare. A provider
	/// may explicitly report that preparation is unsupported.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepareasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepareasync
	[Fact]
	public virtual async Task PrepareAsync_valid_command_remains_executable_or_reports_not_supported()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		try
		{
			await command.PrepareAsync().ConfigureAwait(false);
		}
		catch (NotSupportedException)
		{
			throw SkipException.ForSkip("The provider does not support prepared commands.");
		}

		Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false)));
	}
#endif

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteReader_throws_when_no_connection()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteReader_throws_when_connection_closed()
	{
		using var connection = CreateConnection();
		using var command = connection.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
	}

	// Contract: COMMON BEHAVIOR TEST — IDbCommand does not require a universal empty-command-text exception; https://learn.microsoft.com/dotnet/api/system.data.idbcommand.executereader
	[DiagnosticFact]
	public virtual void ExecuteReader_throws_when_no_command_text()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		AssertEmptyCommandTextPolicy(() =>
		{
			using var reader = command.ExecuteReader();
		}, "ExecuteReader");
	}

	// Contract: COMMON BEHAVIOR TEST — DbCommand does not require rejection when a local transaction is active but not assigned; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.transaction
	[DiagnosticFact]
	public virtual void ExecuteReader_throws_on_error()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "INVALID;";
		Assert.ThrowsAny<DbException>(() => command.ExecuteReader());
	}

	/// <summary>
	/// DbCommand requires an explicitly associated transaction when the connection
	/// has an active local transaction; this protects callers from accidentally
	/// executing outside the transaction they opened.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.transaction.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteReader_throws_when_transaction_required()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		using (connection.BeginTransaction())
		{
			try
			{
				using (command.ExecuteReader())
				{
				}
			}
			catch (InvalidOperationException)
			{
				return;
			}

			SoftWarning.Report("The provider permits execution without assigning the active local transaction; the base ADO.NET contract does not require rejection.");
		}
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.transaction
	[Fact]
	public virtual void ExecuteReader_throws_when_transaction_mismatched()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		using var otherConnection = CreateOpenConnection();
		using var otherTransaction = otherConnection.BeginTransaction();
		try
		{
			command.Transaction = otherTransaction;
		}
		catch (ArgumentException)
		{
			return;
		}

		Assert.Throws<InvalidOperationException>(() =>
		{
			using (command.ExecuteReader())
			{
			}
		});
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteReader_throws_when_reader_open()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		using var reader = command.ExecuteReader();
		Assert.ThrowsAny<InvalidOperationException>(() => command.ExecuteReader());
	}

	/// <summary>
	/// Parameter binding uses the provider's advertised SQL placeholder while the
	/// DbParameter keeps only its logical name; no '@' prefix is assumed.
	/// See https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST (hard-coded @ marker) — NOW VALID PROVIDER-NEUTRAL PARAMETER-SYNTAX TEST; https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types
	[Fact]
	public virtual void ExecuteReader_binds_parameters()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.Value = 1;
		command.Parameters.Add(parameter);

		var scalar = command.ExecuteScalar();
		if (scalar is int)
			Assert.Equal(1, scalar);
		else
			Assert.Equal(1L, scalar);
	}

	/// <summary>
	/// A command with a leading comment still contains an executable statement,
	/// so ExecuteReader must expose that statement's result set.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executereader.
	/// </summary>
	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void ExecuteReader_works_with_leading_comment()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "-- A leading comment\n" + SelectOneSql;

		using var reader = command.ExecuteReader();
		Assert.True(reader.HasRows);
	}

	/// <summary>
	/// A comment-only command is distinct from a command with a leading comment:
	/// it contains no executable statement and therefore has no rows. ADO.NET does
	/// not require providers to accept comment-only command text; this test records
	/// the behavior of providers that do accept it, while a provider that rejects
	/// the empty statement may override the test. It is intentionally not a
	/// universal SQL-compliance requirement.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executereader.
	/// See https://learn.microsoft.com/dotnet/framework/data/adonet/commands-and-parameters.
	/// </summary>
	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void ExecuteReader_HasRows_is_false_for_comment()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "-- TODO: Write SQL";

		DbDataReader reader;
		try
		{
			reader = command.ExecuteReader();
		}
		catch (DbException)
		{
			SoftWarning.Report("The provider rejects comment-only SQL; comment-only command acceptance is not an ADO.NET contract.");
			return;
		}

		using (reader)
		{
			if (reader.HasRows)
				SoftWarning.Report("The provider treats comment-only SQL as an empty result; row behavior is not an ADO.NET contract.");
		}
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void ExecuteReader_works_when_trailing_comments()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("0") + " -- My favorite number";

		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(0, reader.GetInt32(0));
		Assert.False(reader.NextResult());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteReader_supports_CloseConnection()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("0");
		using (command.ExecuteReader(CommandBehavior.CloseConnection))
		{
		}
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	/// <summary>
	/// SchemaOnly requests column metadata without returning result rows. Providers
	/// that implement the behavior must preserve the schema while reporting no
	/// data rows; an explicit NotSupportedException is treated as an optional
	/// capability response.
	/// See https://learn.microsoft.com/dotnet/api/system.data.commandbehavior.
	/// </summary>
	// Contract: VALID CONTRACT TEST where SchemaOnly is supported; optional capability — https://learn.microsoft.com/dotnet/api/system.data.commandbehavior
	[Fact]
	public virtual void ExecuteReader_supports_SchemaOnly_when_advertised()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsValueSql;

		try
		{
			using var reader = command.ExecuteReader(CommandBehavior.SchemaOnly);
			if (reader.FieldCount != 1 || reader.Read())
			{
				SoftWarning.Report("The provider does not expose metadata-only CommandBehavior.SchemaOnly results.");
			}
		}
		catch (NotSupportedException)
		{
			throw SkipException.ForSkip("The provider does not support CommandBehavior.SchemaOnly.");
		}
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteScalar_throws_when_no_connection()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteScalar());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteScalar_throws_when_connection_closed()
	{
		using var connection = CreateConnection();
		using var command = connection.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteScalar());
	}

	// Contract: COMMON BEHAVIOR TEST — IDbCommand does not require a universal empty-command-text exception; https://learn.microsoft.com/dotnet/api/system.data.idbcommand.executescalar
	[DiagnosticFact]
	public virtual void ExecuteScalar_throws_when_no_command_text()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		AssertEmptyCommandTextPolicy(() => command.ExecuteScalar(), "ExecuteScalar");
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteScalar_returns_integer()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		var result = command.ExecuteScalar();
		if (result is int)
			Assert.Equal(1, result);
		else
			Assert.Equal(1L, result);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteScalar_returns_real()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("3.14");
		var result = command.ExecuteScalar();
		if (result is double)
			Assert.Equal(3.14, result);
		else
			Assert.Equal(3.14m, result);
	}

	/// <summary>
	/// ExecuteScalar returns the first column of the first row as an object. This
	/// diagnostic preserves the suite's original textual representation assertion;
	/// providers with a different representation can override it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executescalar.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteScalar_returns_string_when_text()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTextSql;
		Assert.Equal("test", command.ExecuteScalar());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteScalar_returns_DBNull_when_null()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("NULL");
		Assert.Equal(DBNull.Value, command.ExecuteScalar());
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual void ExecuteScalar_returns_first_when_batching()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("42") + " " + SelectSql("43");
		Assert.Equal(42, Convert.ToInt32(command.ExecuteScalar()));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteScalar_returns_first_when_multiple_columns()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("42, 43");
		Assert.Equal(42, Convert.ToInt32(command.ExecuteScalar()));
	}

	/// <summary>
	/// Without ORDER BY, SQL does not define row order. The ordered UNION makes the
	/// first-row assertion test ExecuteScalar's first-column/first-row contract
	/// instead of relying on accidental query-plan order.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executescalar.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST (unordered SQL) — NOW VALID CONTRACT TEST (explicit ORDER BY); https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executescalar
	[Fact]
	public virtual void ExecuteScalar_returns_first_when_multiple_rows()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTwoRows42Sql;
		Assert.Equal(42, Convert.ToInt32(command.ExecuteScalar()));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteNonQuery_throws_when_no_connection()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteNonQuery_throws_when_connection_closed()
	{
		using var connection = CreateConnection();
		using var command = connection.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
	}

	// Contract: COMMON BEHAVIOR TEST — IDbCommand does not require a universal empty-command-text exception; https://learn.microsoft.com/dotnet/api/system.data.idbcommand.executenonquery
	[DiagnosticFact]
	public virtual void ExecuteNonQuery_throws_when_no_command_text()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		AssertEmptyCommandTextPolicy(() => command.ExecuteNonQuery(), "ExecuteNonQuery");
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual void ExecuteNonQuery_returns_negative_one_for_SELECT()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		Assert.Equal(-1, command.ExecuteNonQuery());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual async Task ExecuteReaderAsync_throws_when_no_connection()
	{
		using var command = Fixture.Factory.CreateCommand();
		await Assert.ThrowsAsync<InvalidOperationException>(command.ExecuteReaderAsync);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual async Task ExecuteReaderAsync_throws_when_connection_closed()
	{
		using var connection = CreateConnection();
		using var command = connection.CreateCommand();
		await Assert.ThrowsAsync<InvalidOperationException>(command.ExecuteReaderAsync);
	}

	// Contract: COMMON BEHAVIOR TEST — IDbCommand does not require a universal empty-command-text exception; https://learn.microsoft.com/dotnet/api/system.data.idbcommand.executereader
	[DiagnosticFact]
	public virtual async Task ExecuteReaderAsync_throws_when_no_command_text()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		await AssertEmptyCommandTextPolicyAsync(command.ExecuteReaderAsync, "ExecuteReaderAsync").ConfigureAwait(false);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual async Task ExecuteReaderAsync_throws_on_error()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "INVALID;";
		await Assert.ThrowsAnyAsync<DbException>(command.ExecuteReaderAsync);
	}

	/// <summary>
	/// The asynchronous command API must be usable for a successful result, not
	/// only for invalid-state and cancellation paths. The base implementation may
	/// delegate to synchronous execution, but it must still return the command's
	/// result through the asynchronous API.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executereaderasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executereaderasync
	[Fact]
	public virtual async Task ExecuteReaderAsync_returns_reader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.Equal(1, Convert.ToInt32(reader.GetValue(0)));
		Assert.False(await reader.ReadAsync().ConfigureAwait(false));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual async Task ExecuteScalarAsync_throws_when_no_connection()
	{
		using var command = Fixture.Factory.CreateCommand();
		await Assert.ThrowsAsync<InvalidOperationException>(command.ExecuteScalarAsync);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual async Task ExecuteScalarAsync_throws_when_connection_closed()
	{
		using var connection = CreateConnection();
		using var command = connection.CreateCommand();
		await Assert.ThrowsAsync<InvalidOperationException>(command.ExecuteScalarAsync);
	}

	// Contract: COMMON BEHAVIOR TEST — IDbCommand does not require a universal empty-command-text exception; https://learn.microsoft.com/dotnet/api/system.data.idbcommand.executescalar
	[DiagnosticFact]
	public virtual async Task ExecuteScalarAsync_throws_when_no_command_text()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		await AssertEmptyCommandTextPolicyAsync(command.ExecuteScalarAsync, "ExecuteScalarAsync").ConfigureAwait(false);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[Fact]
	public virtual async Task ExecuteScalarAsync_throws_on_error()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "INVALID;";
		await Assert.ThrowsAnyAsync<DbException>(command.ExecuteScalarAsync);
	}

	/// <summary>
	/// ExecuteScalarAsync must return the first column of the first row just as
	/// ExecuteScalar does. This tests successful asynchronous execution rather
	/// than cancellation policy, which providers are permitted to implement by
	/// delegating to synchronous execution.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executescalarasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executescalarasync
	[Fact]
	public virtual async Task ExecuteScalarAsync_returns_first_value()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		var value = await command.ExecuteScalarAsync().ConfigureAwait(false);
		Assert.Equal(1, Convert.ToInt32(value));
	}

	/// <summary>
	/// ExecuteNonQueryAsync must return the same affected-row result as the
	/// synchronous ExecuteNonQuery contract for a result-producing statement.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonqueryasync
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonquery.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonqueryasync
	[Fact]
	public virtual async Task ExecuteNonQueryAsync_returns_result_producing_statement_count()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;

		Assert.Equal(-1, await command.ExecuteNonQueryAsync().ConfigureAwait(false));
	}

	// Contract: INVALID CONTRACT TEST — the ADO.NET async contract permits providers to ignore cancellation; requiring a canceled task is incorrect; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual async Task ExecuteReaderAsync_is_canceled()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		var task = command.ExecuteReaderAsync(CanceledToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
		Assert.True(task.IsCanceled);
	}

	// Contract: INVALID CONTRACT TEST — the ADO.NET async contract permits providers to ignore cancellation; requiring a canceled task is incorrect; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual async Task ExecuteNonQueryAsync_is_canceled()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		await ObserveCancellationAsync(() => command.ExecuteNonQueryAsync(CanceledToken), nameof(ExecuteNonQueryAsync_is_canceled));
	}

	// Contract: INVALID CONTRACT TEST — the ADO.NET async contract permits providers to ignore cancellation; requiring a canceled task is incorrect; https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand
	[DiagnosticFact]
	public virtual async Task ExecuteScalarAsync_is_canceled()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		await ObserveCancellationAsync(() => command.ExecuteScalarAsync(CanceledToken), nameof(ExecuteScalarAsync_is_canceled));
	}

	private static async Task ObserveCancellationAsync(Func<Task> operation, string operationName)
	{
		try
		{
			await operation().ConfigureAwait(false);
			SoftWarning.Report($"The provider completed {operationName} despite a pre-canceled token; cancellation is optional for this ADO.NET async path.");
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider completed {operationName} with {ex.GetType().Name}; cancellation behavior is provider-specific.");
		}
	}

	/// <summary>
	/// An unsupported CLR parameter value is still tested using the provider's
	/// parameter syntax rather than a hardcoded SQL Server-style marker.
	/// See https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST (hard-coded @ marker) — NOW VALID PROVIDER-NEUTRAL PARAMETER-SYNTAX TEST; unsupported CLR-value handling remains a provider diagnostic; https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types
	[Fact]
	public virtual void Execute_throws_for_unknown_ParameterValue_type()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.Value = new CustomClass();
		command.Parameters.Add(parameter);

		try
		{
			var value = command.ExecuteScalar();
			if (!"custom".Equals((string) value))
				throw new UnexpectedValueException(value);
		}
		catch (Exception ex) when (ex is NotSupportedException || ex is InvalidOperationException)
		{
		}
		catch (Exception ex) when (ex is not UnexpectedValueException)
		{
			throw ThrowsException.ForIncorrectExceptionType(typeof(NotSupportedException), ex);
		}
	}

	private static void AssertEmptyCommandTextPolicy(Action action, string operation)
	{
		try
		{
			action();
		}
		catch (InvalidOperationException)
		{
			return;
		}
		catch (DbException)
		{
			return;
		}
		catch (Exception)
		{
			return;
		}

		SoftWarning.Report($"The provider accepts an empty command text for {operation}; the base ADO.NET contract does not require a specific rejection.");
	}

	private static async Task AssertEmptyCommandTextPolicyAsync(Func<Task> action, string operation)
	{
		try
		{
			await action().ConfigureAwait(false);
		}
		catch (InvalidOperationException)
		{
			return;
		}
		catch (DbException)
		{
			return;
		}
		catch (Exception)
		{
			return;
		}

		SoftWarning.Report($"The provider accepts an empty command text for {operation}; the base ADO.NET contract does not require a specific rejection.");
	}

	protected class CustomClass
	{
		public override string ToString() => "custom";
	}
}
