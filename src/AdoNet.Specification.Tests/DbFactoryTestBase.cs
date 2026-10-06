using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace AdoNet.Specification.Tests;

public abstract class DbFactoryTestBase<TFixture> : IAsyncLifetime, IDisposable, IClassFixture<TFixture>
	where TFixture : class, IDbFactoryFixture
{
	private static readonly ConditionalWeakTable<TFixture, ParameterFormat> s_parameterFormats = new();

	protected DbFactoryTestBase(TFixture fixture)
	{
		Fixture = fixture;
		m_cancellationTokenSource = new CancellationTokenSource();
		m_cancellationTokenSource.Cancel();
		CanceledToken = m_cancellationTokenSource.Token;
	}

	public async ValueTask InitializeAsync()
	{
		await OnInitializeAsync().ConfigureAwait(false);
	}

	public async ValueTask DisposeAsync()
	{
		await OnDisposeAsync().ConfigureAwait(false);
	}

	public void Dispose()
	{
		m_cancellationTokenSource.Dispose();
	}

	protected TFixture Fixture { get; }

	protected CancellationToken CanceledToken { get; }

	protected virtual Task OnInitializeAsync() => Task.CompletedTask;

	protected virtual Task OnDisposeAsync() => Task.CompletedTask;

	protected virtual DbConnection CreateConnection() => Fixture.Factory.CreateConnection();

	protected virtual DbConnection CreateOpenConnection()
	{
		var connection = CreateConnection();
		connection.ConnectionString = ConnectionString;
		connection.Open();
		return connection;
	}

	protected virtual DbConnectionStringBuilder CreateConnectionStringBuilder()
		=> Fixture.Factory.CreateConnectionStringBuilder();

	/// <summary>
	/// Matches the provider-neutral behavior used by pengdows.crud: providers that
	/// explicitly advertise the positional format '?' use that placeholder; named
	/// providers receive their advertised marker plus the logical name. Missing or
	/// unusable DataSourceInformation is allowed to fail rather than being treated as
	/// evidence of positional support.
	/// The logical <see cref="DbParameter.ParameterName"/> remains independent of
	/// this SQL representation.
	/// See https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types.
	/// </summary>
	protected virtual string MakeParameterName(DbConnection connection, string name)
	{
		var format = s_parameterFormats.GetValue(Fixture, _ => ReadParameterFormat(connection));
		if (format.IsPositional)
			return "?";

		var parameterName = name.TrimStart('@', ':', '$', '?');
		return string.Concat(format.Marker, parameterName);
	}

	private static ParameterFormat ReadParameterFormat(DbConnection connection)
	{
		var schema = connection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
		if (schema.Rows.Count == 0)
			throw new InvalidOperationException("DataSourceInformation did not return a row.");

		var markerFormat = schema.Rows[0][DbMetaDataColumnNames.ParameterMarkerFormat] as string;
		if (string.IsNullOrEmpty(markerFormat))
			throw new InvalidOperationException("DataSourceInformation did not advertise ParameterMarkerFormat.");

		if (markerFormat == "?")
			return new ParameterFormat(true, null);

		const string placeholder = "{0}";
		var placeholderIndex = markerFormat.IndexOf(placeholder, StringComparison.Ordinal);
		if (placeholderIndex < 0)
			throw new InvalidOperationException($"Invalid ParameterMarkerFormat '{markerFormat}'.");

		var parameterMarker = markerFormat.Substring(0, placeholderIndex);
		if (markerFormat == placeholder)
		{
			var markerPattern = schema.Rows[0][DbMetaDataColumnNames.ParameterMarkerPattern] as string;
			if (markerPattern?.IndexOf('?') >= 0)
				return new ParameterFormat(true, null);

			if (markerPattern?.IndexOf('@') >= 0)
				parameterMarker = "@";

			else if (markerPattern?.IndexOf(':') >= 0)
				parameterMarker = ":";

			else if (markerPattern?.IndexOf('$') >= 0)
				parameterMarker = "$";
		}

		if (string.IsNullOrEmpty(parameterMarker))
			throw new InvalidOperationException($"Invalid ParameterMarkerFormat '{markerFormat}'.");

		return new ParameterFormat(false, parameterMarker);
	}

	private sealed class ParameterFormat
	{
		public ParameterFormat(bool isPositional, string marker)
		{
			IsPositional = isPositional;
			Marker = marker;
		}

		public bool IsPositional { get; }
		public string Marker { get; }
	}

	/// <summary>
	/// Formats a provider parameter from its logical name using the same named-versus-
	/// positional decision as <see cref="MakeParameterName(DbConnection, string)"/>.
	/// </summary>
	protected virtual string MakeParameterName(DbConnection connection, DbParameter parameter)
		=> MakeParameterName(connection, parameter.ParameterName);

	protected virtual string ConnectionString
	{
		get
		{
			if (m_connectionString == null)
				m_connectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? Fixture.ConnectionString;
			return m_connectionString;
		}
	}

	protected static Exception AssertThrowsAny<TException1, TException2>(Action action)
		where TException1 : Exception
		where TException2 : Exception
	{
		try
		{
			action();
			throw ThrowsException.ForNoException(typeof(TException1));
		}
		catch (TException1 ex)
		{
			return ex;
		}
		catch (TException2 ex)
		{
			return ex;
		}
		catch (Exception ex)
		{
			throw ThrowsException.ForIncorrectExceptionType(typeof(TException1), ex);
		}
	}

	protected static Exception AssertThrowsAny<TException1, TException2, TException3>(Action action)
		where TException1 : Exception
		where TException2 : Exception
		where TException3 : Exception
	{
		try
		{
			action();
			throw ThrowsException.ForNoException(typeof(TException1));
		}
		catch (TException1 ex)
		{
			return ex;
		}
		catch (TException2 ex)
		{
			return ex;
		}
		catch (TException3 ex)
		{
			return ex;
		}
		catch (Exception ex)
		{
			throw ThrowsException.ForIncorrectExceptionType(typeof(TException1), ex);
		}
	}

	readonly CancellationTokenSource m_cancellationTokenSource;
	string m_connectionString;
}
