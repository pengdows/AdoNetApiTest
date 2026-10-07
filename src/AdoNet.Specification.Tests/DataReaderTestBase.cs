using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace AdoNet.Specification.Tests;

[Collection("ISelectValueFixture Collection")]
public class DataReaderTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, ISelectValueFixture, IDeleteFixture
{
	public DataReaderTestBase(TFixture fixture)
		: base(fixture)
	{
		Fixture = fixture;
	}

	protected new TFixture Fixture { get; }

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void Dispose_command_before_reader()
	{
		using var connection = CreateOpenConnection();
		DbDataReader reader;
		using (var command = connection.CreateCommand())
		{
			command.CommandText = SelectTextSql;
			reader = command.ExecuteReader();
		}

		try
		{
			Assert.True(reader.Read());
			Assert.Equal("test", reader.GetString(0));
			Assert.False(reader.Read());
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider closes a reader when its command is disposed: {ex.GetType().Name}.");
		}
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void Depth_returns_zero()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.Equal(0, reader.Depth);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executescalar
	[Fact]
	public virtual void ExecuteScalar_returns_null_when_empty()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.SelectNoRows;
		Assert.Null(command.ExecuteScalar());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void FieldCount_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.Equal(1, reader.FieldCount);
	}

	/// <summary>
	/// For a result without hidden columns, VisibleFieldCount reports the visible
	/// column count and is never greater than FieldCount.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.visiblefieldcount.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.visiblefieldcount
	[Fact]
	public virtual void VisibleFieldCount_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();

		Assert.Equal(1, reader.VisibleFieldCount);
		Assert.InRange(reader.VisibleFieldCount, 0, reader.FieldCount);
	}

	// Contract: COMMON BEHAVIOR TEST — the base API does not prescribe closed-reader exception behavior for FieldCount; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.fieldcount
	[DiagnosticFact]
	public virtual void FieldCount_throws_when_closed()
		=> X_diagnostic_when_closed(
			r =>
			{
				var x = r.FieldCount;
			},
			"FieldCount on a closed reader");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetBytes_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(new byte[] { 0x7E, 0x57 });
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		var buffer = new byte[2];
		Assert.Equal(2, reader.GetBytes(0, 0, buffer, 0, buffer.Length));
		Assert.Equal(new byte[] { 0x7E, 0x57 }, buffer);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetChars_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTextSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		var buffer = new char[4];
		Assert.Equal(4, reader.GetChars(0, 0, buffer, 0, buffer.Length));
		Assert.Equal(new[] { 't', 'e', 's', 't' }, buffer);
	}

	// Contract: COMMON BEHAVIOR TEST — GetDataTypeName does not define a portable out-of-range exception; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getdatatypename
	[DiagnosticFact]
	public virtual void GetDataTypeName_throws_when_ordinal_out_of_range()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		X_diagnostic(() => reader.GetDataTypeName(1), "GetDataTypeName out-of-range handling");
	}

	// Contract: COMMON BEHAVIOR TEST — the base API does not prescribe closed-reader exception behavior for GetDataTypeName; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getdatatypename
	[DiagnosticFact]
	public virtual void GetDataTypeName_throws_when_closed()
		=> X_diagnostic_when_closed(r => r.GetDataTypeName(0), "GetDataTypeName on a closed reader");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetEnumerator_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		Assert.NotNull(reader.GetEnumerator());
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetFieldValue_of_DBNull_throws_when_not_null()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();

		Assert.True(hasData);
		Assert.Throws<InvalidCastException>(() => reader.GetFieldValue<DBNull>(0));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetFieldValue_throws_before_read()
		=> X_throws_before_read(r => r.GetFieldValue<DBNull>(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetFieldValue_throws_when_done()
		=> X_throws_when_done(r => r.GetFieldValue<DBNull>(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual async Task GetFieldValueAsync_is_canceled()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		reader.Read();

		var task = reader.GetFieldValueAsync<int>(0, CanceledToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
		Assert.True(task.IsCanceled);
	}

	/// <summary>
	/// GetFieldValueAsync must return the field value on a successful read. The
	/// default implementation may delegate to GetFieldValue, but it must preserve
	/// the reader contract through the asynchronous API.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldvalueasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldvalueasync
	[Fact]
	public virtual async Task GetFieldValueAsync_returns_value()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		var value = await reader.GetFieldValueAsync<object>(0).ConfigureAwait(false);

		Assert.Equal(1, Convert.ToInt32(value));
	}

	/// <summary>
	/// A reader returned by ExecuteReaderAsync must support successful ReadAsync
	/// and expose the same row data as the synchronous reader API.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.readasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.readasync
	[Fact]
	public virtual async Task ReadAsync_returns_rows()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.Equal(1, Convert.ToInt32(reader.GetValue(0)));
		Assert.False(await reader.ReadAsync().ConfigureAwait(false));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetFieldType_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTextSql;
		using var reader = command.ExecuteReader();
		Assert.Equal(typeof(string), reader.GetFieldType(0));
	}

	// Contract: COMMON BEHAVIOR TEST — the API does not prescribe the exact
	// out-of-range exception for GetFieldType.
	[DiagnosticFact]
	public virtual void GetFieldType_throws_when_ordinal_out_of_range()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		X_diagnostic(() => reader.GetFieldType(1), "GetFieldType out-of-range handling");
	}

	// Contract: COMMON BEHAVIOR TEST — the base API does not prescribe closed-reader exception behavior for GetFieldType; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldtype
	[DiagnosticFact]
	public virtual void GetFieldType_throws_when_closed()
		=> X_diagnostic_when_closed(r => r.GetFieldType(0), "GetFieldType on a closed reader");

	/// <summary>
	/// The provider-specific reader API must expose the provider-specific field
	/// type for a valid current column. The returned type may differ from the
	/// common CLR type, but it must be usable as metadata.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getproviderspecificfieldtype.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getproviderspecificfieldtype
	[Fact]
	public virtual void GetProviderSpecificFieldType_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();

		Assert.True(reader.Read());
		Assert.NotNull(reader.GetProviderSpecificFieldType(0));
	}

	/// <summary>
	/// GetProviderSpecificValue returns the current column value using the
	/// provider-specific representation selected by the reader.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getproviderspecificvalue.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getproviderspecificvalue
	[Fact]
	public virtual void GetProviderSpecificValue_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();

		Assert.True(reader.Read());
		Assert.NotNull(reader.GetProviderSpecificValue(0));
	}

	/// <summary>
	/// GetProviderSpecificValues copies one provider-specific value per result
	/// column and reports the number copied.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getproviderspecificvalues.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getproviderspecificvalues
	[Fact]
	public virtual void GetProviderSpecificValues_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();

		Assert.True(reader.Read());
		var values = new object[1];

		Assert.Equal(1, reader.GetProviderSpecificValues(values));
		Assert.NotNull(values[0]);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetName_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		Assert.Equal("id", reader.GetName(0));
	}

	// Contract: COMMON BEHAVIOR TEST — the API does not prescribe the exact
	// out-of-range exception for GetName.
	[DiagnosticFact]
	public virtual void GetName_throws_when_ordinal_out_of_range()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		X_diagnostic(() => reader.GetName(1), "GetName out-of-range handling");
	}

	// Contract: COMMON BEHAVIOR TEST — the base API does not prescribe closed-reader exception behavior for GetName; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getname
	[DiagnosticFact]
	public virtual void GetName_throws_when_closed()
		=> X_diagnostic_when_closed(r => r.GetName(0), "GetName on a closed reader");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetOrdinal_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		Assert.Equal(0, reader.GetOrdinal("Id"));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetOrdinal_throws_when_out_of_range()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("Name"));
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetString_works_utf8_two_bytes() => GetX_works(SelectSql("'Ä'"), r => r.GetString(0), "Ä");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetString_works_utf8_three_bytes() => GetX_works(SelectSql("'Ḁ'"), r => r.GetString(0), "Ḁ");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetString_works_utf8_four_bytes() => GetX_works(SelectSql("'😀'"), r => r.GetString(0), "😀");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFieldValue_works_utf8_two_bytes() => GetX_works(SelectSql("'Ä'"), r => r.GetFieldValue<string>(0), "Ä");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFieldValue_works_utf8_three_bytes() => GetX_works(SelectSql("'Ḁ'"), r => r.GetFieldValue<string>(0), "Ḁ");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFieldValue_works_utf8_four_bytes() => GetX_works(SelectSql("'😀'"), r => r.GetFieldValue<string>(0), "😀");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetValue_to_string_works_utf8_two_bytes() => GetX_works(SelectSql("'Ä'"), r => r.GetValue(0) as string, "Ä");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetValue_to_string_works_utf8_three_bytes() => GetX_works(SelectSql("'Ḁ'"), r => r.GetValue(0) as string, "Ḁ");

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetValue_to_string_works_utf8_four_bytes() => GetX_works(SelectSql("'😀'"), r => r.GetValue(0) as string, "😀");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetString_works()
		=> GetX_works(
			SelectTextSql,
			r => r.GetString(0),
			"test");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetValue_throws_before_read()
		=> X_throws_before_read(r => r.GetValue(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetValue_throws_when_done()
		=> X_throws_when_done(r => r.GetValue(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetValue_throws_when_closed()
		=> X_throws_when_closed(r => r.GetValue(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetValues_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("'a', NULL");
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		// Array may be wider than row
		var values = new object[3];
		var result = reader.GetValues(values);

		Assert.Equal(2, result);
		Assert.Equal("a", values[0]);
		Assert.Same(DBNull.Value, values[1]);
	}

	/// <summary>
	/// DbDataReader.GetValue represents a database NULL with DBNull.Value.
	/// A provider returning C# null breaks the common reader contract and makes
	/// IsDBNull/GetValue handling inconsistent.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getvalue.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getvalue
	[Fact]
	public virtual void GetValue_returns_DBNull_for_null()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("NULL");
		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Same(DBNull.Value, reader.GetValue(0));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetValues_when_too_narrow()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		var values = new object[0];
		Assert.Equal(0, reader.GetValues(values));
	}

	// Contract: COMMON BEHAVIOR TEST — null-array exception policy is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetValues_throws_for_null()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		reader.Read();
		try
		{
			reader.GetValues(default(object[]));
		}
		catch (ArgumentNullException)
		{
			return;
		}
		catch (Exception)
		{
			throw SkipException.ForSkip("The provider uses a provider-specific null-array policy for GetValues; the base ADO.NET contract does not specify the exception.");
		}

		throw SkipException.ForSkip("The provider accepts a null GetValues array; the base ADO.NET contract does not specify a required exception.");
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void HasRows_returns_true_when_rows()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.True(reader.HasRows);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void HasRows_returns_false_when_no_rows()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.SelectNoRows;
		using var reader = command.ExecuteReader();
		Assert.False(reader.HasRows);
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void HasRows_works_when_batching()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectNoRowsThenOneSql;
		using var reader = command.ExecuteReader();
		Assert.False(reader.HasRows);

		reader.NextResult();

		Assert.True(reader.HasRows);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void IsClosed_returns_false_when_active()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.False(reader.IsClosed);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void IsClosed_returns_true_when_closed()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		var reader = command.ExecuteReader();
		reader.Close();

		Assert.True(reader.IsClosed);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void IsClosed_returns_false_when_all_rows_read()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.False(reader.Read());
		Assert.False(reader.NextResult());
		Assert.False(reader.IsClosed);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void IsDBNull_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("NULL");
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();

		Assert.True(hasData);
		Assert.True(reader.IsDBNull(0));
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// IsDBNullAsync must report the value state through the asynchronous reader
	/// API just as IsDBNull does. The test deliberately uses a non-null literal so
	/// it does not depend on a provider-specific column type.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.isdbnullasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.isdbnullasync
	[Fact]
	public virtual async Task IsDBNullAsync_returns_value_state()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.False(await reader.IsDBNullAsync(0).ConfigureAwait(false));
	}

	/// <summary>
	/// NextResultAsync must complete successfully and report that a single-result
	/// command has no additional result set.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.nextresultasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.nextresultasync
	[Fact]
	public virtual async Task NextResultAsync_returns_false_after_last_result()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

		Assert.False(await reader.NextResultAsync().ConfigureAwait(false));
	}

	/// <summary>
	/// DisposeAsync must close the reader and release its active state.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.disposeasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.disposeasync
	[Fact]
	public virtual async Task DisposeAsync_closes_reader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

		await reader.DisposeAsync().ConfigureAwait(false);

		Assert.True(reader.IsClosed);
	}

#if NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// CloseAsync must complete the documented asynchronous reader-close
	/// operation and leave the reader closed.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.closeasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.closeasync
	[Fact]
	public virtual async Task CloseAsync_closes_reader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();

		await reader.CloseAsync().ConfigureAwait(false);

		Assert.True(reader.IsClosed);
	}
#endif
#endif

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual async Task IsDBNullAsync_is_canceled()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		reader.Read();

		var task = reader.IsDBNullAsync(0, CanceledToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
		Assert.True(task.IsCanceled);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void IsDBNull_throws_before_read()
		=> X_throws_before_read(r => r.IsDBNull(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void IsDBNull_throws_when_done()
		=> X_throws_when_done(r => r.IsDBNull(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void IsDBNull_throws_when_closed()
		=> X_throws_when_closed(r => r.IsDBNull(0));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void Item_by_ordinal_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTextSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		Assert.Equal("test", reader[0]);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void Item_by_name_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTextAsIdSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		Assert.Equal("test", reader["Id"]);
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void NextResult_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTwoResultsSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);
		Assert.Equal(1L, reader.GetInt64(0));

		var hasResults = reader.NextResult();
		Assert.True(hasResults);

		hasData = reader.Read();
		Assert.True(hasData);
		Assert.Equal(2L, reader.GetInt64(0));

		hasResults = reader.NextResult();
		Assert.False(hasResults);
	}

	/// <summary>
	/// `SingleRow` is a provider hint; the API documentation does not require a
	/// provider to truncate a result set after one row. This preserves the
	/// historical interoperability diagnostic but must not be treated as a
	/// conformance failure.
	/// The old assertion was an INVALID CONTRACT TEST. See
	/// https://learn.microsoft.com/dotnet/api/system.data.commandbehavior.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST — NOW COMMON BEHAVIOR DIAGNOSTIC; https://learn.microsoft.com/dotnet/api/system.data.commandbehavior
	[DiagnosticFact]
	public virtual void SingleRow_returns_one_row()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTwoRowsSql;

		using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
		var hasData = reader.Read();
		Assert.True(hasData);
		Assert.Equal(1L, Convert.ToInt64(reader.GetValue(0)));
	}

	/// <summary>
	/// `SingleRow` does not require providers to suppress additional result sets;
	/// it is a hint described by CommandBehavior. This is retained as a diagnostic,
	/// not as a universal contract test.
	/// See https://learn.microsoft.com/dotnet/api/system.data.commandbehavior.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST — NOW COMMON BEHAVIOR DIAGNOSTIC; https://learn.microsoft.com/dotnet/api/system.data.commandbehavior
	[DiagnosticFact]
	public virtual void SingleRow_returns_one_result_set()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectThreeResultsSql;

		using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
		var hasData = reader.Read();
		Assert.True(hasData);
		Assert.Equal(1L, Convert.ToInt64(reader.GetValue(0)));
	}

	/// <summary>
	/// `SingleResult` is also a provider hint rather than a requirement to discard
	/// all later result sets. Retain the observation for interoperability testing,
	/// but do not use it as an ADO.NET conformance gate.
	/// The former assertion was an INVALID CONTRACT TEST. See
	/// https://learn.microsoft.com/dotnet/api/system.data.commandbehavior.
	/// </summary>
	// Contract: WAS INVALID CONTRACT TEST — NOW COMMON BEHAVIOR DIAGNOSTIC; https://learn.microsoft.com/dotnet/api/system.data.commandbehavior
	[DiagnosticFact]
	public virtual void SingleResult_returns_one_result_set()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectThreeResultsSql;

		using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
		var hasData = reader.Read();
		Assert.True(hasData);
		Assert.Equal(1L, Convert.ToInt64(reader.GetValue(0)));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void NextResult_can_be_called_more_than_once()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		var hasResults = reader.NextResult();
		Assert.False(hasResults);

		hasResults = reader.NextResult();
		Assert.False(hasResults);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void Read_works()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectTwoRowsSql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);
		Assert.Equal(1L, reader.GetInt64(0));

		hasData = reader.Read();
		Assert.True(hasData);
		Assert.Equal(2L, reader.GetInt64(0));

		hasData = reader.Read();
		Assert.False(hasData);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void Read_keeps_returning_false()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.String, ValueKind.Empty);
		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.False(reader.Read());
		Assert.False(reader.Read());
		Assert.False(reader.Read());
	}

	// Contract: COMMON BEHAVIOR TEST — the base API does not prescribe closed-reader exception behavior for Read; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.read
	[DiagnosticFact]
	public virtual void Read_throws_when_closed() => X_diagnostic_when_closed(r => r.Read(), "Read on a closed reader");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void RecordsAffected_returns_negative_1_when_no_rows()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.SelectNoRows;
		using var reader = command.ExecuteReader();
		Assert.Equal(-1, reader.RecordsAffected);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void RecordsAffected_returns_negative_1_after_close_when_no_rows()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.SelectNoRows;
		using var reader = command.ExecuteReader();
		reader.Close();
		Assert.Equal(-1, reader.RecordsAffected);
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void RecordsAffected_returns_negative_1_after_dispose_when_no_rows()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.SelectNoRows;
		using var reader = command.ExecuteReader();
		reader.Dispose();
		Assert.Equal(-1, reader.RecordsAffected);
	}

	// Contract: COMMON BEHAVIOR TEST — the base API does not prescribe closed-reader exception behavior for NextResult; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.nextresult
	[DiagnosticFact]
	public virtual void NextResult_throws_when_closed() => X_diagnostic_when_closed(r => r.NextResult(), "NextResult on a closed reader");

	// Contract: COMMON BEHAVIOR TEST — negative dataOffset handling is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getbytes
	[DiagnosticFact]
	public virtual void GetBytes_throws_when_dataOffset_is_negative() => TestGetBytes(reader =>
	{
		AssertBufferBoundaryPolicy(() => reader.GetBytes(0, -1, new byte[4], 0, 4), "GetBytes negative dataOffset");
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetBytes_reads_nothing_when_dataOffset_is_too_large() => TestGetBytes(reader =>
	{
		Assert.Equal(0, reader.GetBytes(0, 6, new byte[4], 0, 4));
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetBytes_returns_length_when_buffer_is_null() => TestGetBytes(reader =>
	{
		Assert.Equal(4, reader.GetBytes(0, 0, null, 0, 0));
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetBytes_returns_length_when_buffer_is_null_and_dataIndex_specified() => TestGetBytes(reader =>
	{
		Assert.Equal(4, reader.GetBytes(0, 1, null, 0, 0));
	});

	// Contract: COMMON BEHAVIOR TEST — bufferOffset validation is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getbytes
	[DiagnosticFact]
	public virtual void GetBytes_throws_when_bufferOffset_is_negative() => TestGetBytes(reader =>
	{
		AssertBufferBoundaryPolicy(() => reader.GetBytes(0, 0, new byte[4], -1, 4), "GetBytes negative bufferOffset");
	});

	// Contract: COMMON BEHAVIOR TEST — bufferOffset validation is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getbytes
	[DiagnosticFact]
	public virtual void GetBytes_throws_when_bufferOffset_is_too_large() => TestGetBytes(reader =>
	{
		AssertBufferBoundaryPolicy(() => reader.GetBytes(0, 0, new byte[4], 5, 0), "GetBytes oversized bufferOffset");
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getbytes
	[Fact]
	public virtual void GetBytes_reads_nothing_at_end_of_buffer() => TestGetBytes(reader =>
	{
		Assert.Equal(0, reader.GetBytes(0, 0, new byte[4], 4, 0));
	});

	// Contract: COMMON BEHAVIOR TEST — buffer-boundary handling is provider-specific; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getbytes
	[DiagnosticFact]
	public virtual void GetBytes_throws_when_bufferOffset_plus_length_is_too_long() => TestGetBytes(reader =>
	{
		try
		{
			reader.GetBytes(0, 0, new byte[4], 2, 3);
		}
		catch (ArgumentException)
		{
			return;
		}
		catch (IndexOutOfRangeException)
		{
			return;
		}

		throw SkipException.ForSkip("The provider permits a buffer range beyond the available buffer; boundary behavior is not a portable contract.");
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetBytes_works_when_buffer_is_large() => TestGetBytes(reader =>
	{
		var buffer = new byte[6];
		Assert.Equal(4, reader.GetBytes(0, 0, buffer, 0, 6));
		Assert.Equal(new byte[] { 1, 2, 3, 4, 0, 0 }, buffer);
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetBytes_reads_part_of_blob() => TestGetBytes(reader =>
	{
		var buffer = new byte[5];
		Assert.Equal(2, reader.GetBytes(0, 1, buffer, 2, 2));
		Assert.Equal(new byte[] { 0, 0, 2, 3, 0 }, buffer);
	});

	private void TestGetBytes(Action<DbDataReader> action)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(new byte[] { 1, 2, 3, 4 });
		using var reader = command.ExecuteReader();
		reader.Read();
		action(reader);
	}

	// Contract: COMMON BEHAVIOR TEST — negative dataOffset validation is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[DiagnosticFact]
	public virtual void GetChars_throws_when_dataOffset_is_negative() => TestGetChars(reader =>
	{
		AssertBufferBoundaryPolicy(() => reader.GetChars(0, -1, new char[4], 0, 4), "GetChars negative dataOffset");
	});

	// Contract: COMMON BEHAVIOR TEST — GetChars does not prescribe behavior when
	// dataOffset exceeds the value length.
	[DiagnosticFact]
	public virtual void GetChars_reads_nothing_when_dataOffset_is_too_large() => TestGetChars(reader =>
	{
		try
		{
			if (reader.GetChars(0, 6, new char[4], 0, 4) != 0)
			{
				SoftWarning.Report("The provider returns characters for a dataOffset beyond the value length.");
			}
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider throws {ex.GetType().Name} for a dataOffset beyond the value length.");
		}
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[Fact]
	public virtual void GetChars_returns_length_when_buffer_is_null() => TestGetChars(reader =>
	{
		Assert.Equal(4, reader.GetChars(0, 0, null, 0, 0));
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[Fact]
	public virtual void GetChars_returns_length_when_buffer_is_null_and_dataOffset_is_specified() => TestGetChars(reader =>
	{
		Assert.Equal(4, reader.GetChars(0, 1, null, 0, 0));
	});

	// Contract: COMMON BEHAVIOR TEST — bufferOffset validation is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[DiagnosticFact]
	public virtual void GetChars_throws_when_bufferOffset_is_negative() => TestGetChars(reader =>
	{
		AssertBufferBoundaryPolicy(() => reader.GetChars(0, 0, new char[4], -1, 4), "GetChars negative bufferOffset");
	});

	// Contract: COMMON BEHAVIOR TEST — bufferOffset validation is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[DiagnosticFact]
	public virtual void GetChars_throws_when_bufferOffset_is_too_large() => TestGetChars(reader =>
	{
		AssertBufferBoundaryPolicy(() => reader.GetChars(0, 0, new char[4], 5, 0), "GetChars oversized bufferOffset");
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[Fact]
	public virtual void GetChars_reads_nothing_at_end_of_buffer() => TestGetChars(reader =>
	{
		Assert.Equal(0, reader.GetChars(0, 0, new char[4], 4, 0));
	});

	// Contract: COMMON BEHAVIOR TEST — bufferOffset validation is not specified by DbDataReader; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[DiagnosticFact]
	public virtual void GetChars_throws_when_bufferOffset_plus_length_is_too_long() => TestGetChars(reader =>
	{
		AssertBufferBoundaryPolicy(() => reader.GetChars(0, 0, new char[4], 2, 3), "GetChars buffer range");
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getchars
	[Fact]
	public virtual void GetChars_works_when_buffer_is_large() => TestGetChars(reader =>
	{
		var buffer = new char[6];
		Assert.Equal(4, reader.GetChars(0, 0, buffer, 0, 6));
		Assert.Equal(new[] { 'a', 'b', '¢', 'd', '\0', '\0' }, buffer);
	});

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetChars_reads_part_of_string() => TestGetChars(reader =>
	{
		var buffer = new char[5];
		Assert.Equal(2, reader.GetChars(0, 1, buffer, 2, 2));
		Assert.Equal(new[] { '\0', '\0', 'b', '¢', '\0' }, buffer);
	});

	private void TestGetChars(Action<DbDataReader> action)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		// NB: Intentionally using a multi-byte UTF-8 character
		command.CommandText = SelectSql("'ab¢d'");
		using var reader = command.ExecuteReader();
		reader.Read();
		action(reader);
	}

	private static void AssertBufferBoundaryPolicy(Action action, string operation)
	{
		try
		{
			action();
		}
		catch (ArgumentException)
		{
			return;
		}
		catch (IndexOutOfRangeException)
		{
			return;
		}
		catch (InvalidOperationException)
		{
			throw SkipException.ForSkip($"The provider rejects {operation} with a provider-specific state exception.");
		}

		throw SkipException.ForSkip($"The provider accepts {operation}; buffer-boundary behavior is not a portable ADO.NET contract.");
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetTextReader_for_one_String() => TestGetTextReader(ValueKind.One, "1");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetTextReader_for_empty_String() => TestGetTextReader(ValueKind.Empty, "");

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetTextReader_returns_empty_for_null_String() => TestGetTextReader(ValueKind.Null, "");

	// Contract: COMMON BEHAVIOR TEST — null text materialization/exception policy is provider-specific; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetTextReader_throws_for_null_String()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.String, ValueKind.Null);
		using var reader = command.ExecuteReader();
		reader.Read();
		try
		{
			using var textReader = reader.GetTextReader(0);
		}
		catch (Exception exception) when (exception.GetType() == Fixture.NullValueExceptionType)
		{
			return;
		}

		throw SkipException.ForSkip("The provider does not use the fixture's null-value exception policy; null text materialization is not a portable ADO.NET contract.");
	}

	// Contract: COMMON BEHAVIOR TEST — TextReader materialization is provider-dependent; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldvalue
	[DiagnosticFact]
	public virtual void GetFieldValue_for_TextReader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.String, ValueKind.One);
		using var reader = command.ExecuteReader();
		reader.Read();
		TextReader textReader;
		try
		{
			textReader = reader.GetFieldValue<TextReader>(0);
		}
		catch (InvalidCastException)
		{
			throw SkipException.ForSkip("The provider does not expose text values as TextReader instances.");
		}

		using (textReader)
		{
		Assert.Equal("1", textReader.ReadToEnd());
		}
	}

	// Contract: COMMON BEHAVIOR TEST — async TextReader materialization is provider-dependent; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldvalueasync
	[DiagnosticFact]
	public virtual async Task GetFieldValueAsync_for_TextReader()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.String, ValueKind.One);
		using var reader = await command.ExecuteReaderAsync();
		await reader.ReadAsync();
		TextReader textReader;
		try
		{
			textReader = await reader.GetFieldValueAsync<TextReader>(0);
		}
		catch (InvalidCastException)
		{
			throw SkipException.ForSkip("The provider does not expose text values as TextReader instances.");
		}

		using (textReader)
		{
		Assert.Equal("1", textReader.ReadToEnd());
		}
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFieldValue_for_TextReader_throws_for_null_String()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.String, ValueKind.Null);
		using var reader = command.ExecuteReader();
		reader.Read();
		try
		{
			reader.GetFieldValue<TextReader>(0);
			SoftWarning.Report("The provider materializes a null text value as TextReader; null conversion behavior is provider-specific.");
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider throws {ex.GetType().Name} for null TextReader conversion; null conversion behavior is provider-specific.");
		}
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual async Task GetFieldValueAsync_for_TextReader_throws_for_null_String()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.String, ValueKind.Null);
		using var reader = await command.ExecuteReaderAsync();
		await reader.ReadAsync();
		try
		{
			await reader.GetFieldValueAsync<TextReader>(0);
			SoftWarning.Report("The provider materializes a null text value as TextReader asynchronously; null conversion behavior is provider-specific.");
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider throws {ex.GetType().Name} for async null TextReader conversion; null conversion behavior is provider-specific.");
		}
	}

	// Contract: COMMON BEHAVIOR TEST — provider-specific stream materialization; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldvalue
	[DiagnosticFact]
	public virtual void GetFieldValue_for_Stream()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.Binary, ValueKind.One);
		using var reader = command.ExecuteReader();
		reader.Read();
		Stream stream;
		try
		{
			stream = reader.GetFieldValue<Stream>(0);
		}
		catch (InvalidCastException)
		{
			throw SkipException.ForSkip("The provider does not expose binary values as Stream instances.");
		}

		using (stream)
		{
		Assert.Equal(0x11, stream.ReadByte());
		}
	}

	// Contract: COMMON BEHAVIOR TEST — async provider-specific stream materialization; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldvalueasync
	[DiagnosticFact]
	public virtual async Task GetFieldValueAsync_for_Stream()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.Binary, ValueKind.One);
		using var reader = await command.ExecuteReaderAsync();
		await reader.ReadAsync();
		Stream stream;
		try
		{
			stream = await reader.GetFieldValueAsync<Stream>(0);
		}
		catch (InvalidCastException)
		{
			throw SkipException.ForSkip("The provider does not expose binary values as Stream instances.");
		}

		using (stream)
		{
		Assert.Equal(0x11, stream.ReadByte());
		}
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFieldValue_for_Stream_throws_for_null_Binary()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.Binary, ValueKind.Null);
		using var reader = command.ExecuteReader();
		reader.Read();
		try
		{
			reader.GetFieldValue<Stream>(0);
			SoftWarning.Report("The provider materializes a null binary value as Stream; null conversion behavior is provider-specific.");
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider throws {ex.GetType().Name} for null Stream conversion; null conversion behavior is provider-specific.");
		}
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual async Task GetFieldValueAsync_for_Stream_throws_for_null_Binary()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.Binary, ValueKind.Null);
		using var reader = await command.ExecuteReaderAsync();
		await reader.ReadAsync();
		try
		{
			await reader.GetFieldValueAsync<Stream>(0);
			SoftWarning.Report("The provider materializes a null binary value as Stream asynchronously; null conversion behavior is provider-specific.");
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider throws {ex.GetType().Name} for async null Stream conversion; null conversion behavior is provider-specific.");
		}
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void FieldCount_is_zero_after_Delete() => Test_X_after_Delete(x => Assert.Equal(0, x.FieldCount));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void IsDBNull_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.IsDBNull(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual async Task IsDBNullAsync_throws_after_Delete() => await Test_X_after_Delete(async x => await Assert.ThrowsAsync<InvalidOperationException>(async () => await x.IsDBNullAsync(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void Read_returns_false_after_Delete() => Test_X_after_Delete(x => Assert.False(x.Read()));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void NextResult_returns_false_after_Delete() => Test_X_after_Delete(x => Assert.False(x.NextResult()));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetDataTypeName_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetDataTypeName(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFieldType_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetFieldType(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFieldValue_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetFieldValue<object>(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual async Task GetFieldValueAsync_throws_after_Delete() => await Test_X_after_Delete(async x => await Assert.ThrowsAsync<InvalidOperationException>(async () => await x.GetFieldValueAsync<object>(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetBoolean_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetBoolean(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetByte_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetByte(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetBytes_throws_after_Delete()
	{
		Test_X_after_Delete(x =>
		{
			var bytes = new byte[1];
			Assert.Throws<InvalidOperationException>(() => x.GetBytes(0, 0, bytes, 0, 1));
		});
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetChar_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetChar(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetChars_throws_after_Delete()
	{
		Test_X_after_Delete(x =>
		{
			var chars = new char[1];
			Assert.Throws<InvalidOperationException>(() => x.GetChars(0, 0, chars, 0, 1));
		});
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetColumnSchema_is_empty_after_Delete() => Test_X_after_Delete(x =>
	{
		try
		{
			if (x.GetColumnSchema().Count != 0)
			{
				SoftWarning.Report("The provider retains column schema after the reader has no current row.");
			}
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider throws {ex.GetType().Name} when column schema is requested without a current row.");
		}
	});

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetDataTime_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetDateTime(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetDecimal_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetDecimal(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetDouble_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetDouble(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetFloat_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetFloat(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetGuid_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetGuid(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetInt16_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetInt16(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetInt32_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetInt32(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetInt64_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetInt64(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetSchemaTable_is_null_after_Delete() => Test_X_after_Delete(x =>
	{
		try
		{
			if (x.GetSchemaTable() is not null)
			{
				SoftWarning.Report("The provider returns a schema table after the reader has no current row.");
			}
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"The provider throws {ex.GetType().Name} when schema table is requested without a current row.");
		}
	});

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetStream_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetStream(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetString_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetString(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetTextReader_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetTextReader(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetName_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() => x.GetName(0)));

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetValue_throws_after_Delete() => Test_X_after_Delete(x => Assert.Throws<InvalidOperationException>(() =>x.GetValue(0)));

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetStream_throws_for_non_binary_type()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.Throws<InvalidOperationException>(() => reader.GetStream(0));
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetTextReader_throws_for_non_text_type()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		using var reader = command.ExecuteReader();
		Assert.Throws<InvalidOperationException>(() => reader.GetTextReader(0));
	}

	// Contract: COMMON BEHAVIOR TEST — useful provider interoperability diagnostic, but not an ADO.NET contract; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[DiagnosticFact]
	public virtual void GetValues_throws_after_Delete()
	{
		Test_X_after_Delete(x =>
		{
			var values = new object[1];
			Assert.Throws<InvalidOperationException>(() => x.GetValues(values));
		});
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetColumnSchema_ColumnName()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		var columns = reader.GetColumnSchema();
		var column = Assert.Single(columns);
		Assert.Equal("id", column.ColumnName);
	}

	// Contract: COMMON BEHAVIOR TEST — native DataTypeName metadata may be unavailable; https://learn.microsoft.com/dotnet/api/system.data.common.dbcolumn.datatypename
	[DiagnosticFact]
	public virtual void GetColumnSchema_DataTypeName()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		var columns = reader.GetColumnSchema();
		var column = Assert.Single(columns);
		if (column.DataTypeName is not null)
			return;

		throw SkipException.ForSkip("The provider does not expose a native DataTypeName for this result column.");
	}

	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader
	[Fact]
	public virtual void GetColumnSchema_DataType()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		var columns = reader.GetColumnSchema();
		var column = Assert.Single(columns);
		switch (Type.GetTypeCode(column.DataType))
		{
		case TypeCode.Byte:
		case TypeCode.Int16:
		case TypeCode.Int32:
		case TypeCode.Int64:
		case TypeCode.SByte:
		case TypeCode.UInt16:
		case TypeCode.UInt32:
		case TypeCode.UInt64:
			return;
		}
		Assert.Fail("DataType isn't numeric");
	}

	/// <summary>
	/// Providers that implement GetSchemaTable should return metadata for an
	/// active result. The API permits providers without schema-table support to
	/// return null, so that capability is reported as a skip rather than treated
	/// as a conformance failure.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getschematable.
	/// </summary>
	// Contract: COMMON BEHAVIOR TEST — optional reader-schema capability; https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getschematable
	[DiagnosticFact]
	public virtual void GetSchemaTable_returns_metadata_when_supported()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		var schema = reader.GetSchemaTable();

		if (schema is null)
			throw SkipException.ForSkip("The provider does not expose reader schema tables.");

		Assert.NotEmpty(schema.Columns);
		Assert.NotEmpty(schema.Rows);
	}

#if NET10_0_OR_GREATER
	/// <summary>
	/// The asynchronous schema API must expose the same column metadata as the
	/// synchronous API. Providers without schema-table support may return null.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getschematableasync.
	/// </summary>
	// Contract: OPTIONAL CAPABILITY TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getschematableasync
	[DiagnosticFact]
	public virtual async Task GetSchemaTableAsync_returns_metadata_when_supported()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		var schema = await reader.GetSchemaTableAsync().ConfigureAwait(false);

		if (schema is null)
			throw SkipException.ForSkip("The provider does not expose reader schema tables asynchronously.");

		Assert.NotEmpty(schema.Columns);
		Assert.NotEmpty(schema.Rows);
	}

	/// <summary>
	/// The asynchronous column-schema API must preserve the column metadata
	/// exposed synchronously.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getcolumnschemaasync.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getcolumnschemaasync
	[Fact]
	public virtual async Task GetColumnSchemaAsync_returns_column_metadata()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneAsIdSql;
		using var reader = command.ExecuteReader();
		var columns = await reader.GetColumnSchemaAsync().ConfigureAwait(false);

		var column = Assert.Single(columns);
		Assert.Equal("id", column.ColumnName);
	}
#endif

	private void TestGetTextReader(ValueKind valueKind, string expected)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(DbType.String, valueKind);
		using var reader = command.ExecuteReader();
		reader.Read();
		using var textReader = reader.GetTextReader(0);
		Assert.Equal(expected, textReader.ReadToEnd());
	}

	private void GetX_works<T>(string sql, Func<DbDataReader, T> action, T expected)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();

		Assert.True(hasData);
		Assert.Equal(expected, action(reader));
	}

	private void X_throws_before_read(Action<DbDataReader> action)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("NULL");
		using var reader = command.ExecuteReader();
		Assert.Throws<InvalidOperationException>(() => action(reader));
	}

	private void X_throws_when_done(Action<DbDataReader> action)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectSql("NULL");
		using var reader = command.ExecuteReader();
		var hasData = reader.Read();
		Assert.True(hasData);

		hasData = reader.Read();
		Assert.False(hasData);

		Assert.Throws<InvalidOperationException>(() => action(reader));
	}

	private void X_throws_when_closed(Action<DbDataReader> action)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		var reader = command.ExecuteReader();
		((IDisposable) reader).Dispose();

		Assert.Throws<InvalidOperationException>(() => action(reader));
	}

	private void X_diagnostic_when_closed(Action<DbDataReader> action, string behavior)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = SelectOneSql;
		var reader = command.ExecuteReader();
		((IDisposable)reader).Dispose();

		try
		{
			action(reader);
		}
		catch (SkipException)
		{
			throw;
		}
		catch (Exception)
		{
			return;
		}

		throw SkipException.ForSkip($"The provider does not throw for {behavior}; closed-reader exception behavior is not a portable ADO.NET contract.");
	}

	private void X_diagnostic(Action action, string behavior)
	{
		try
		{
			action();
		}
		catch (SkipException)
		{
			throw;
		}
		catch (Exception)
		{
			return;
		}

		throw SkipException.ForSkip($"The provider does not throw for {behavior}; out-of-range exception behavior is not a portable ADO.NET contract.");
	}

	private void Test_X_after_Delete(Action<DbDataReader> action)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.DeleteNoRows;
		using var reader = command.ExecuteReader();
		try
		{
			action(reader);
		}
		catch (XunitException)
		{
			throw SkipException.ForSkip("Post-delete reader state is provider-specific and is not an ADO.NET conformance requirement.");
		}
	}

	private async Task Test_X_after_Delete(Func<DbDataReader, Task> action)
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.DeleteNoRows;
		using var reader = command.ExecuteReader();
		try
		{
			await action(reader);
		}
		catch (XunitException)
		{
			throw SkipException.ForSkip("Post-delete reader state is provider-specific and is not an ADO.NET conformance requirement.");
		}
	}
}
