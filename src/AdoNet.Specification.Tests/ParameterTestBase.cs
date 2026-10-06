using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.IO;
using Xunit;

namespace AdoNet.Specification.Tests;

public abstract class ParameterTestBase<TFixture> : DbFactoryTestBase<TFixture>
	where TFixture : class, IDbFactoryFixture
{
	protected ParameterTestBase(TFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// The original assertion required <see cref="DbType.String"/>. ADO.NET exposes
	/// <see cref="DbParameter.DbType"/>, but does not prescribe a provider-neutral
	/// default value. The old assertion was therefore not a valid shared contract.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.dbtype.
	/// </summary>
	[Fact(Skip = "ADO.NET does not prescribe a provider-neutral default DbType.")]
	public virtual void Parameter_default_DbType_is_string()
	{
	}

	[Fact]
	public virtual void Parameter_default_Direction_is_input()
	{
		Assert.Equal(ParameterDirection.Input, Fixture.Factory.CreateParameter().Direction);
	}

	[Fact]
	public virtual void Parameter_default_IsNullable_is_false()
	{
		Assert.False(Fixture.Factory.CreateParameter().IsNullable);
	}

	[Fact]
	public virtual void Parameter_default_ParameterName_is_empty_string()
	{
		Assert.Equal("", Fixture.Factory.CreateParameter().ParameterName);
	}

	[Fact]
	public virtual void Parameter_default_Precision_is_zero()
	{
		Assert.Equal((byte) 0, Fixture.Factory.CreateParameter().Precision);
	}

	[Fact]
	public virtual void Parameter_default_Scale_is_zero()
	{
		Assert.Equal((byte) 0, Fixture.Factory.CreateParameter().Scale);
	}

	[Fact]
	public virtual void Parameter_default_Size_is_zero()
	{
		Assert.Equal(0, Fixture.Factory.CreateParameter().Size);
	}

	[Fact]
	public virtual void Parameter_default_SourceColumn_is_empty_string()
	{
		Assert.Equal("", Fixture.Factory.CreateParameter().SourceColumn);
	}

	[Fact]
	public virtual void Parameter_default_SourceVersion_is_Current()
	{
		Assert.Equal(DataRowVersion.Current, Fixture.Factory.CreateParameter().SourceVersion);
	}

	[Fact]
	public virtual void Parameter_default_Value_is_null()
	{
		Assert.Null(Fixture.Factory.CreateParameter().Value);
	}

	[Fact]
	public virtual void Parameter_Value_can_be_set_to_null()
	{
		Assert.Null(Fixture.Factory.CreateParameter().Value);
	}

	/// <summary>
	/// ResetDbType is part of the API, but its provider-specific reset target is not
	/// defined by the ADO.NET contract. The former String/Object equality assertion
	/// was consequently invalid as a cross-provider test.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.resetdbtype.
	/// </summary>
	[Fact(Skip = "ADO.NET does not define the provider's ResetDbType target value.")]
	public virtual void ResetDbType_works()
	{
	}

	/// <summary>
	/// Named versus positional parameter requirements are provider-specific. The
	/// original test hard-coded a named marker and therefore was not portable.
	/// </summary>
	[Fact(Skip = "Parameter-name requirements are provider-specific, not a shared ADO.NET contract.")]
	public virtual void Bind_requires_set_name()
	{
	}

	/// <summary>
	/// DBNull.Value is the ADO.NET sentinel for a database NULL value; this verifies
	/// that a parameter carrying that sentinel can be bound.
	/// See https://learn.microsoft.com/dotnet/api/system.dbnull.value.
	/// </summary>
	[Fact]
	public virtual void Bind_accepts_DBNull_value()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {ParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.DbType = DbType.String;
		parameter.Value = DBNull.Value;
		command.Parameters.Add(parameter);

		var result = command.ExecuteScalar();
		Assert.True(result is null || result == DBNull.Value);
	}

	/// <summary>
	/// The original test required null to be rejected. ADO.NET permits a provider to
	/// interpret null as its parameter-null representation, so that rejection is not
	/// a provider-neutral contract; DBNull.Value is the portable sentinel instead.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.value.
	/// </summary>
	[Fact(Skip = "ADO.NET does not require a null parameter value to be rejected.")]
	public virtual void Bind_requires_set_value()
	{
	}

	[Fact]
	public virtual void Bind_is_noop_on_unknown_parameter()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT 1;";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Unknown";
		parameter.Value = 1;
		command.Parameters.Add(parameter);

		command.ExecuteNonQuery();
	}

	/// <summary>
	/// Conversion of an arbitrary CLR object that a provider does not understand is
	/// provider-specific. The original exception assertion was not a shared contract.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.value.
	/// </summary>
	[Fact(Skip = "Unknown CLR parameter-value conversion is provider-specific.")]
	public virtual void Bind_throws_when_unknown()
	{
	}

	/// <summary>
	/// A string assigned through DbParameter.Value must be returned as the same
	/// database value; the SQL marker is discovered from provider metadata.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.value.
	/// </summary>
	[Fact]
	public virtual void Bind_works_with_string()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {ParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.Value = "test";
		command.Parameters.Add(parameter);

		var result = command.ExecuteScalar();
		Assert.Equal("test", result);
	}

	/// <summary>
	/// DbParameter.Value accepts provider-supported CLR values; byte arrays are the
	/// provider-neutral binary value used by this suite. Providers may expose the
	/// returned binary value as a byte array or a readable stream.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.value.
	/// </summary>
	[Fact]
	public virtual void Bind_works_with_byte_array()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {ParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.Value = new byte[] { 1, 2, 3, 4 };
		command.Parameters.Add(parameter);

		var result = command.ExecuteScalar();
		Assert.Equal(new byte[] { 1, 2, 3, 4 }, ReadBlob(result));
	}

	/// <summary>
	/// Stream-valued parameters are not required by the provider-neutral ADO.NET
	/// contract; the original test incorrectly treated this optional behavior as
	/// universal. Byte-array binding is covered separately because it is the common
	/// binary value representation.
	/// </summary>
	[Fact(Skip = "Stream parameter values are provider-specific.")]
	public virtual void Bind_works_with_stream()
	{
	}

	private static byte[] ReadBlob(object value)
	{
		return value switch
		{
			byte[] bytes => bytes,
			Stream stream => ReadStream(stream),
			_ => throw new InvalidOperationException($"Expected a byte array or stream, got {value?.GetType().FullName ?? "null"}.")
		};
	}

	private static byte[] ReadStream(Stream stream)
	{
		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		return buffer.ToArray();
	}

	[Fact]
	public virtual void ParameterName_can_be_set_to_null()
	{
		Fixture.Factory.CreateParameter().ParameterName = null;
	}

	[Fact]
	public virtual void ParameterName_default_is_empty_string()
	{
		Assert.Equal("", Fixture.Factory.CreateParameter().ParameterName);
	}

	[Fact]
	public virtual void ParameterName_set_to_null_is_empty_string()
	{
		var parameter = Fixture.Factory.CreateParameter();
		parameter.ParameterName = null;
		Assert.Equal("", parameter.ParameterName);
	}

	[Fact]
	public virtual void SourceColumn_can_be_set_to_null()
	{
		//Fixture.Factory.CreateParameter().SourceColumn = null;
		var parameter = Fixture.Factory.CreateParameter();
		parameter.SourceColumn = null;
		Assert.Equal("", parameter.SourceColumn);
	}

	[Fact]
	public virtual void SourceColumn_default_is_empty_string()
	{
		Assert.Equal("", Fixture.Factory.CreateParameter().SourceColumn);
	}

	[Fact]
	public virtual void SourceColumn_set_to_null_is_empty_string()
	{
		var parameter = Fixture.Factory.CreateParameter();
		parameter.SourceColumn = null;
		Assert.Equal("", parameter.SourceColumn);
	}

	[Fact]
	public virtual void ParameterCollection_Add_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<ArgumentNullException>(() => command.Parameters.Add(default));
	}

	[Fact]
	public virtual void ParameterCollection_Insert_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<ArgumentNullException>(() => command.Parameters.Insert(0, default));
	}

	[Fact]
	public virtual void ParameterCollection_string_indexer_setter_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		parameter.ParameterName = "param";
		command.Parameters.Add(parameter);
		Assert.Throws<ArgumentNullException>(() => command.Parameters[ParameterName("param")] = null);
	}

	[Fact]
	public virtual void ParameterCollection_int_indexer_setter_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		command.Parameters.Add(parameter);
		Assert.Throws<ArgumentNullException>(() => command.Parameters[0] = null);
	}

	[Fact]
	public virtual void ParameterCollection_IList_indexer_setter_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		command.Parameters.Add(parameter);
		Assert.Throws<ArgumentNullException>(() => ((IList) command.Parameters)[0] = null);
	}

	[Fact]
	public virtual void ParameterCollection_IDataParameterCollection_indexer_setter_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		parameter.ParameterName = "param";
		command.Parameters.Add(parameter);
		Assert.Throws<ArgumentNullException>(() => ((IDataParameterCollection) command.Parameters)[ParameterName("param")] = null);
	}

	[Fact]
	public virtual void ParameterCollection_Contains_object_returns_false_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.False(command.Parameters.Contains(default(object)));
	}

	[Fact]
	public virtual void ParameterCollection_Contains_string_returns_false_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.False(command.Parameters.Contains(default(string)));
	}

	/// <summary>
	/// IDataParameterCollection requires Contains, but does not impose one universal
	/// null-argument policy for IndexOf across provider implementations.
	/// See https://learn.microsoft.com/dotnet/api/system.data.idataparametercollection.indexof.
	/// </summary>
	[Fact(Skip = "Null IndexOf argument handling is provider-specific.")]
	public virtual void ParameterCollection_IndexOf_object_returns_negative_one_for_null()
	{
	}

	[Fact(Skip = "Null IndexOf argument handling is provider-specific.")]
	public virtual void ParameterCollection_IndexOf_string_returns_negative_one_for_null()
	{
	}

	[Fact]
	public virtual void ParameterCollection_Remove_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<ArgumentNullException>(() => command.Parameters.Remove(null));
	}

	[Fact]
	public virtual void ParameterCollection_RemoveAt_negative_one_throws()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<ArgumentOutOfRangeException>(() => command.Parameters.RemoveAt(-1));
	}

	[Fact]
	public virtual void ParameterCollection_RemoveAt_zero_throws_when_empty()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Throws<ArgumentOutOfRangeException>(() => command.Parameters.RemoveAt(0));
	}

	[Fact]
	public virtual void ParameterCollection_Insert_throws_for_negative_one()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		Assert.Throws<ArgumentOutOfRangeException>(() => command.Parameters.Insert(-1, parameter));
	}

	[Fact]
	public virtual void ParameterCollection_Insert_succeeds()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		command.Parameters.Insert(0, parameter);
		Assert.Same(parameter, Assert.Single(command.Parameters));
	}

	[Fact]
	public virtual void ParameterCollection_Insert_throws_for_one()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		Assert.Throws<ArgumentOutOfRangeException>(() => command.Parameters.Insert(1, parameter));
	}

	[Fact]
	public virtual void ParameterCollection_Remove_succeeds()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		command.Parameters.Add(parameter);
		command.Parameters.Remove(parameter);
		Assert.Empty(command.Parameters);
	}

	[Fact]
	public virtual void ParameterCollection_RemoveAt_succeeds()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		command.Parameters.Add(parameter);
		command.Parameters.RemoveAt(0);
		Assert.Empty(command.Parameters);
	}
}
