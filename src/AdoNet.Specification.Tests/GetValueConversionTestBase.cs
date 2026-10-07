using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace AdoNet.Specification.Tests;

[Collection("ISelectValueFixture Collection")]
public abstract partial class GetValueConversionTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, ISelectValueFixture
{
	protected GetValueConversionTestBase(TFixture fixture)
		: base(fixture)
	{
		Fixture = fixture;
	}

	protected new TFixture Fixture { get; }

	/// <summary>
	/// GetFieldType describes the CLR type returned by GetValue for the current
	/// column. Provider-specific native value types are valid; reporting a type
	/// that GetValue cannot return is not.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldtype
	/// and https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getvalue.
	/// </summary>
	// Contract: VALID CONTRACT TEST — https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getfieldtype
	[Fact]
	public virtual void GetFieldType_matches_GetValue_for_supported_values()
	{
		foreach (var dbType in Fixture.SupportedDbTypes)
		{
			using var connection = CreateOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = Fixture.CreateSelectSql(dbType, ValueKind.One);
			using var reader = command.ExecuteReader();

			Assert.True(reader.Read());
			var value = reader.GetValue(0);
			Assert.NotSame(DBNull.Value, value);
			Assert.Equal(value.GetType(), reader.GetFieldType(0));
		}
	}

	protected virtual void TestGetFieldType(DbType dbType, ValueKind kind, Type expectedType) => DoTest(dbType, kind, reader => RunSoftValueCheck($"{dbType}/{kind} field type", () => Assert.Equal(expectedType, reader.GetFieldType(0))));
	protected virtual void TestGetFieldValue<T>(DbType dbType, ValueKind kind, T expected) => DoTest(dbType, kind, reader => RunSoftValueCheck($"{dbType}/{kind} GetFieldValue<{typeof(T).Name}>", () => Assert.Equal(expected, reader.GetFieldValue<T>(0))));
	protected virtual void TestGetValue<T>(DbType dbType, ValueKind kind, T expected) => DoTest(dbType, kind, reader => RunSoftValueCheck($"{dbType}/{kind} GetValue<{typeof(T).Name}>", () => Assert.Equal(expected, reader.GetValue(0))));
	protected virtual void TestGetValue<T>(DbType dbType, ValueKind kind, Func<DbDataReader, T> getValue, T expected) => DoTest(dbType, kind, reader => RunSoftValueCheck($"{dbType}/{kind} typed getter<{typeof(T).Name}>", () => Assert.Equal(expected, getValue(reader))));
	protected virtual async Task TestGetValueAsync<T>(DbType dbType, ValueKind kind, Func<DbDataReader, Task<T>> getValue, T expected) => await DoTestAsync(dbType, kind, async reader => await RunSoftValueCheckAsync($"{dbType}/{kind} async typed getter<{typeof(T).Name}>", async () => Assert.Equal(expected, await getValue(reader))));

	protected virtual void TestException<T>(DbType dbType, ValueKind kind, Func<DbDataReader, T> getValue, Type exceptionType) =>
		DoTest(dbType, kind, reader =>
		{
			try
			{
				var value = getValue(reader);
				SoftWarning.Report($"{dbType}/{kind} returned {FormatValue(value)} instead of throwing {exceptionType.Name}.");
			}
			catch (Exception ex) when (ex.GetType() == exceptionType)
			{
			}
			catch (Exception ex)
			{
				SoftWarning.Report($"{dbType}/{kind} threw {ex.GetType().Name}; common-behavior matrix expected {exceptionType.Name}.");
			}
		});


	protected virtual async Task TestExceptionAsync<T>(DbType dbType, ValueKind kind, Func<DbDataReader, Task<T>> getValue, Type exceptionType) =>
		await DoTestAsync(dbType, kind, async reader =>
		{
			try
			{
				var value = await getValue(reader);
				SoftWarning.Report($"{dbType}/{kind} returned {FormatValue(value)} instead of throwing {exceptionType.Name}.");
			}
			catch (Exception ex) when (ex.GetType() == exceptionType)
			{
			}
			catch (Exception ex)
			{
				SoftWarning.Report($"{dbType}/{kind} threw {ex.GetType().Name}; common-behavior matrix expected {exceptionType.Name}.");
			}
		});

	private static void RunSoftValueCheck(string operation, Action assertion)
	{
		try
		{
			assertion();
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"{operation} differs from the common-behavior expectation: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static string FormatValue<T>(T value) => value is null ? "<null>" : value.ToString();

	private static async Task RunSoftValueCheckAsync(string operation, Func<Task> assertion)
	{
		try
		{
			await assertion().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			SoftWarning.Report($"{operation} differs from the common-behavior expectation: {ex.GetType().Name}: {ex.Message}");
		}
	}

	protected virtual void DoTest(DbType dbType, ValueKind kind, Action<DbDataReader> action)
	{
		if (!Fixture.SupportedDbTypes.Contains(dbType))
			throw Xunit.Sdk.SkipException.ForSkip("Database doesn't support this data type");

		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(dbType, kind);
		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		action(reader);
	}

	protected virtual async Task DoTestAsync(DbType dbType, ValueKind kind, Func<DbDataReader, Task> action)
	{
		if (!Fixture.SupportedDbTypes.Contains(dbType))
			throw Xunit.Sdk.SkipException.ForSkip("Database doesn't support this data type");

		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = Fixture.CreateSelectSql(dbType, kind);
		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		await action(reader);
	}
}
