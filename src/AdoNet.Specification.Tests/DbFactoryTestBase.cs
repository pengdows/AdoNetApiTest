using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace AdoNet.Specification.Tests;

public abstract class DbFactoryTestBase<TFixture> : IAsyncLifetime, IDisposable, IClassFixture<TFixture>
	where TFixture : class, IDbFactoryFixture
{
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
	/// do not advertise a named-parameter format use the positional placeholder
	/// '?'; named providers receive their advertised marker plus the logical name.
	/// The logical <see cref="DbParameter.ParameterName"/> remains independent of
	/// this SQL representation.
	/// See https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types.
	/// </summary>
	protected virtual string MakeParameterName(DbConnection connection, string name)
	{
		DataTable schema;
		try
		{
			schema = connection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
		}
		catch (ArgumentException)
		{
			// A provider that cannot construct its metadata table cannot advertise
			// named parameters. Keep the SQL usable as a positional probe; the
			// provider's GetSchema test still reports the metadata defect.
			return "?";
		}
		if (schema.Rows.Count == 0)
			throw new InvalidOperationException("DataSourceInformation did not return a row.");

		var markerFormat = schema.Rows[0][DbMetaDataColumnNames.ParameterMarkerFormat] as string;
		if (string.IsNullOrEmpty(markerFormat))
			return "?";

		const string placeholder = "{0}";
		if (markerFormat.IndexOf(placeholder, StringComparison.Ordinal) < 0)
			return "?";

		var parameterName = name.TrimStart('@', ':', '$', '?');
		if (markerFormat == placeholder)
		{
			var markerPattern = schema.Rows[0][DbMetaDataColumnNames.ParameterMarkerPattern] as string;
			if (markerPattern?.IndexOf('?') >= 0)
				return "?";

			if (markerPattern?.IndexOf('@') >= 0)
				return "@" + parameterName;

			if (markerPattern?.IndexOf(':') >= 0)
				return ":" + parameterName;

			if (markerPattern?.IndexOf('$') >= 0)
				return "$" + parameterName;
		}

		return markerFormat.Replace(placeholder, parameterName);
	}

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
