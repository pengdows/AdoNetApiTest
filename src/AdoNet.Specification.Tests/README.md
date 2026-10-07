## ADO.NET Specification Tests

This package provides base classes that let you test an ADO.NET provider against
the documented `System.Data.Common` API contract. It is not a SQL dialect
compatibility suite and does not assume that all providers expose the same
optional capabilities.

ADO.NET does not have one separate provider specification document, so the
authoritative contract is the .NET API documentation and the referenced ADO.NET
guidance. The complete coverage goal, classification rules, and remaining work
are documented in [CONTRACT-COVERAGE.md](CONTRACT-COVERAGE.md).

Many libraries (such as
[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient),
[Npgsql](https://www.nuget.org/packages/Npgsql), and [MySqlConnector](https://www.nuget.org/packages/MySqlConnector))
exhibit similar behavior.

### Usage

Create a test project that uses [xunit](https://www.nuget.org/packages/xunit).

Write a class that implements `IDbFactoryFixture`:

```csharp
public class DbFactoryFixture : IDbFactoryFixture
{
	public DbFactoryFixture()
	{
		ConnectionString ="your test connection string";
	}

	public string ConnectionString { get; }
	public DbProviderFactory Factory => YourDbProviderFactory.Instance;
}
```

Parameterised SQL uses `MakeParameterName` to read
`DataSourceInformation.ParameterMarkerFormat` from the provider connection. This
keeps it aligned with the provider contract instead of assuming that the marker is
`@`; providers that do not advertise a named-parameter format use the positional
placeholder `?`.

Simple reader probes use the protected `SelectSql`, `SelectOneSql`, and related
query hooks on `DbFactoryTestBase`. A provider whose SQL dialect requires
`FROM dual`, a system table, or another dummy source should override those
hooks in its provider test project; the shared package does not bake that
dialect assumption into its contract tests.

Then write test classes that inherit from the classes in this package, e.g.,

```csharp
public sealed class ConnectionTests : ConnectionTestBase<DbFactoryFixture>
{
	public ConnectionTests(DbFactoryFixture fixture)
		: base(fixture)
	{
	}

	[Fact(Skip = "Override a method and provide a 'Skip' reason to opt out of a test.")]
	public override void Set_ConnectionString_throws_when_invalid()
	{
	}
}
```

Providers that support output parameters can additionally implement
`IOutputParameterFixture` and derive an `OutputParameterTestBase<TFixture>`.
That opt-in is intentional: the ADO.NET output-parameter contract is shared,
but the SQL command shape and native value configuration are provider-specific.

Providers that support changing the database on an open connection can likewise
implement `IChangeDatabaseFixture` and derive
`ChangeDatabaseTestBase<TFixture>`. The target database and the way the provider
reports the resulting database identity must come from the fixture.

Providers can test local transaction semantics by implementing
`ITransactionSemanticsFixture` and deriving `TransactionSemanticsTestBase<TFixture>`.
This keeps setup and verification SQL provider-specific while enforcing the
shared rollback-on-dispose contract.

Providers that support explicit `System.Transactions` enlistment can implement
`IEnlistmentFixture` and derive `EnlistmentTestBase<TFixture>` to verify that
committed enlisted work is actually committed.

Providers that support `InputOutput` or `ReturnValue` parameters can implement
`IParameterDirectionFixture` and derive `ParameterDirectionTestBase<TFixture>`.
Each direction is tested only when the fixture explicitly advertises support.

Providers can test an explicit isolation level by implementing
`IIsolationLevelFixture` and deriving `IsolationLevelTestBase<TFixture>`.
The ordinary transaction tests intentionally use the provider default.

On `net10.0`, providers that expose `DbProviderFactory.CreateDataSource` can
derive `DbDataSourceTestBase<TFixture>` to cover the modern data-source
connection, open, async-open, and command-creation contract.

### Conformance versus diagnostics

Provider-defined defaults, optional capabilities, and the generated
`GetValueConversionTestBase<TFixture>` matrix are tagged with the xUnit trait
`Category=Diagnostic`. They record useful interoperability differences, but
are not required contract assertions. The `System.Data.Common` contract does
not require one universal conversion result for every native type and typed
getter. Run the provider's conformance suite with diagnostics excluded:

```text
dotnet test --filter "Category!=Diagnostic"
```

When invoking the xUnit v3 in-process runner directly, use its equivalent
`-trait- "Category=Diagnostic"` option.

Run the diagnostic matrix separately when investigating interoperability or a
provider regression:

```text
dotnet test --filter "Category=Diagnostic"
```

Diagnostic cases are still executed and their assertions produce soft warnings;
they must not be converted into skips or hard failures. The only
conversion-matrix skips are for database types the fixture explicitly reports
as unsupported. Required reader
state, ordinal, null, schema, and valid-value behavior remains in
`DataReaderTestBase<TFixture>` and is intentionally not tagged as diagnostic.
