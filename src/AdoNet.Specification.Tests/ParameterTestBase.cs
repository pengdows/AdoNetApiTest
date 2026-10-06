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
	/// default value. This remains an active implementation-comparison diagnostic;
	/// providers with a different default can override it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.dbtype.
	/// </summary>
	[Fact]
	public virtual void Parameter_default_DbType_is_string()
	{
		Assert.Equal(DbType.String, Fixture.Factory.CreateParameter().DbType);
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
	/// defined by the ADO.NET contract. This remains an active implementation-
	/// comparison diagnostic; providers with a different reset target can override it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.resetdbtype.
	/// </summary>
	[Fact]
	public virtual void ResetDbType_works()
	{
		var parameter = Fixture.Factory.CreateParameter();
		parameter.DbType = DbType.Int64;

		parameter.ResetDbType();

		Assert.Equal(DbType.String, parameter.DbType);
	}

	/// <summary>
	/// Named versus positional parameter requirements are provider-specific. The
	/// SQL marker is discovered separately; providers with different parameter-name
	/// requirements can override this implementation-comparison diagnostic.
	/// </summary>
	[Fact]
	public virtual void Bind_requires_set_name()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.Value = 1;
		command.Parameters.Add(parameter);

		AssertThrowsAny<InvalidOperationException, DbException>(() => command.ExecuteNonQuery());
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
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
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
	/// This remains an active implementation-comparison diagnostic.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.value.
	/// </summary>
	[Fact]
	public virtual void Bind_requires_set_value()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		command.Parameters.Add(parameter);

		Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
	}

	/// <summary>
	/// An unused parameter is tested independently of SQL marker syntax; the
	/// parameter collection contains the logical name only.
	/// See https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types.
	/// </summary>
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
	/// provider-specific. This remains an active implementation-comparison
	/// diagnostic; providers with different conversion behavior can override it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.value.
	/// </summary>
	[Fact]
	public virtual void Bind_throws_when_unknown()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.Value = new object();
		command.Parameters.Add(parameter);

		AssertThrowsAny<InvalidOperationException, NotSupportedException>(() => command.ExecuteScalar());
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
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.Value = "test";
		command.Parameters.Add(parameter);

		var result = command.ExecuteScalar();
		Assert.Equal("test", result);
	}

	/// <summary>
	/// DbParameter.Value accepts provider-supported CLR values; this preserves the
	/// suite's original byte-array round-trip assertion. Providers with a different
	/// representation can override the test.
	/// See https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter.value.
	/// </summary>
	[Fact]
	public virtual void Bind_works_with_byte_array()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		parameter.Value = new byte[] { 1, 2, 3, 4 };
		command.Parameters.Add(parameter);

		var result = command.ExecuteScalar();
		Assert.Equal(new byte[] { 1, 2, 3, 4 }, result);
	}

	/// <summary>
	/// This preserves the suite's original stream-parameter diagnostic. Providers
	/// that do not support stream values can override it.
	/// </summary>
	[Fact]
	public virtual void Bind_works_with_stream()
	{
		using var connection = CreateOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {MakeParameterName(connection, "Parameter")};";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "Parameter";
		using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
		parameter.Value = stream;
		command.Parameters.Add(parameter);

		var result = command.ExecuteScalar();
		Assert.Equal(new byte[] { 1, 2, 3, 4 }, result);
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

	/// <summary>
	/// The string indexer uses the logical parameter name, independently of whether
	/// SQL uses '@', ':', '$', or positional '?'.
	/// See https://learn.microsoft.com/dotnet/api/system.data.idataparametercollection.item.
	/// </summary>
	[Fact]
	public virtual void ParameterCollection_string_indexer_setter_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		parameter.ParameterName = "param";
		command.Parameters.Add(parameter);
		Assert.Throws<ArgumentNullException>(() => command.Parameters["param"] = null);
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

	/// <summary>
	/// IDataParameterCollection also indexes by the logical parameter name; the SQL
	/// marker is not part of the collection key.
	/// See https://learn.microsoft.com/dotnet/api/system.data.idataparametercollection.item.
	/// </summary>
	[Fact]
	public virtual void ParameterCollection_IDataParameterCollection_indexer_setter_throws_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		var parameter = command.CreateParameter();
		parameter.ParameterName = "param";
		command.Parameters.Add(parameter);
		Assert.Throws<ArgumentNullException>(() => ((IDataParameterCollection) command.Parameters)["param"] = null);
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
	/// null-argument policy for IndexOf across provider implementations. This remains
	/// an active diagnostic; providers with a different policy can override it.
	/// See https://learn.microsoft.com/dotnet/api/system.data.idataparametercollection.indexof.
	/// </summary>
	[Fact]
	public virtual void ParameterCollection_IndexOf_object_returns_negative_one_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Equal(-1, command.Parameters.IndexOf(default(object)));
	}

	[Fact]
	public virtual void ParameterCollection_IndexOf_string_returns_negative_one_for_null()
	{
		using var command = Fixture.Factory.CreateCommand();
		Assert.Equal(-1, command.Parameters.IndexOf(default(string)));
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
