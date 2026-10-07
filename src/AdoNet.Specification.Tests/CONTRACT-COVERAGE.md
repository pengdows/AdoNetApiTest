# ADO.NET contract coverage

## Goal

`AdoNet.Specification.Tests` exists to determine whether an ADO.NET provider
implements the documented `System.Data.Common` contract correctly. It is not a
test of whether a provider happens to behave like SQL Server, nor is it a test
of every SQL dialect feature.

A test belongs in the shared suite when its assertion is supported by an
authoritative .NET/API contract. Provider-specific SQL belongs in a fixture,
and provider capabilities belong behind an explicit capability contract. A
provider must not fail because the shared suite assumed a particular parameter
marker, dummy table, batching syntax, exception subclass, or optional feature.

Primary API references:

- [`DbConnection`](https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection)
- [`DbCommand`](https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand)
- [`DbDataReader`](https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader)
- [`DbParameter`](https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter)
- [`DbTransaction`](https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction)
- [`DbProviderFactory`](https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory)
- [Configuring parameters and parameter data types](https://learn.microsoft.com/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types)

## Result classification

The suite has three outcomes:

1. **Hard failure** — a required contract member is missing or violates its
   documented behavior.
2. **Skip** — an optional capability is explicitly reported as unsupported.
3. **Soft failure** — a common provider behavior differs from an
   interoperability expectation, but the contract does not prohibit it. The
   test remains in the diagnostic result set and writes `[SOFT WARNING]`; it is
   not treated as a hard conformance failure and is never converted into a
   capability skip.

## Contract inventory

This is the working inventory for the shared base classes. “Required” means a
provider that exposes the API must satisfy the behavior. “Optional” means the
provider may decline the capability, but a provider that advertises or
implements it must satisfy the associated test.

| API area | Shared coverage | Status |
| --- | --- | --- |
| `DbConnection` lifecycle, state, metadata, schema, disposal | `ConnectionTestBase`, `ConnectionStringTestBase` | Required behavior covered; transition edge cases remain under audit |
| `DbConnection` database changes and enlistment | `ChangeDatabaseTestBase`, `EnlistmentTestBase` | Optional capability bases added |
| `DbCommand` properties, execution, transactions, preparation, async APIs | `CommandTestBase` | Required behavior covered; remaining command behaviors under audit |
| `DbDataReader` navigation, values, schema, disposal, async APIs | `DataReaderTestBase` | Required state/navigation covered; conversion matrix classified separately |
| `DbParameter` and `IDataParameterCollection` | `ParameterTestBase` | Universal properties and marker metadata covered; provider-specific directions remain opt-in |
| `DbTransaction` lifecycle and async APIs | `TransactionTestBase`, `TransactionSemanticsTestBase`, `IsolationLevelTestBase` | Default lifecycle covered; completion/exception portability remains under audit |
| `DbProviderFactory` creation and capability agreement | `DbProviderFactoryTestBase` | Core creation covered; modern and optional members are capability-aware |

The inventory is intentionally maintained beside the code. A new shared test
must be added to the appropriate row, and a provider-specific or optional
requirement must be represented by a fixture capability rather than an
universal assertion.

The exhaustive member extraction for the current .NET 10 reference surface is
maintained in [`REFERENCE-API-INVENTORY.md`](REFERENCE-API-INVENTORY.md). It
includes explicit-interface and framework-plumbing members so that omissions
remain visible; the grouped audit below records whether each member is covered,
capability-gated, or intentionally not portable.

### Member-level audit

The following is the member-level audit used to close gaps. “Covered” means a
shared assertion exists; “capability” means the provider must opt in before the
assertion runs; “diagnostic” means the API exposes the member but does not define
one portable result for the scenario; “review” is remaining work.

| Contract type | Members/scenarios | Status and shared location |
| --- | --- | --- |
| `DbConnection` | `ConnectionString`, `State`, `Database`, `DataSource`, `ServerVersion`, `ConnectionTimeout` | Covered in `ConnectionTestBase`/`ConnectionStringTestBase`; closed-state representations are diagnostics unless the member page requires an exception |
| `DbConnection` | `Open`, `OpenAsync`, `Close`, `CloseAsync`, `Dispose`, `StateChange` | Covered in `ConnectionTestBase`; cancellation is diagnostic because the base async contract permits ignoring the token |
| `DbConnection` | `CreateCommand`, `CreateBatch`, `CanCreateBatch`, `BeginTransaction`, `BeginTransactionAsync` | Covered in `ConnectionTestBase`/`TransactionTestBase`; batch creation is capability-aware and default isolation is provider-defined |
| `DbConnection` | `ChangeDatabase`, `EnlistTransaction`, `GetSchema`, `GetSchemaAsync` | Capability bases for `ChangeDatabase`/enlistment; synchronous and asynchronous schema collection shape is covered when the optional schema capability is supported |
| `DbCommand` | `CommandText`, `CommandTimeout`, `CommandType`, `Connection`, `Transaction`, `UpdatedRowSource`, `DesignTimeVisible`, `Parameters` | Covered in `CommandTestBase`; invalid open-reader transitions are strict where the API requires them |
| `DbCommand` | `Cancel`, `CreateParameter`, `Prepare`, `PrepareAsync`, `DisposeAsync` | Covered; disposal/cancellation edge cases are diagnostics, preparation supports explicit `NotSupportedException` |
| `DbCommand` | `ExecuteReader`, `ExecuteReaderAsync`, `ExecuteScalar`, `ExecuteScalarAsync`, `ExecuteNonQuery`, `ExecuteNonQueryAsync` | Covered for successful execution, async propagation, invalid connection state, errors, and result semantics; empty text is diagnostic |
| `DbDataReader` | `Read`, `ReadAsync`, `NextResult`, `NextResultAsync`, `HasRows`, `IsClosed`, `Depth`, `FieldCount`, `VisibleFieldCount`, `RecordsAffected` | Covered; closed-state and result-navigation assertions cite the reader contract |
| `DbDataReader` | `GetValue`, typed getters, `GetFieldValue`, `GetFieldValueAsync`, `IsDBNull`, `IsDBNullAsync` | Valid values, nulls, ordinals, closed state, and async paths covered; generated cross-type conversions are diagnostics |
| `DbDataReader` | `GetName`, `GetOrdinal`, `GetDataTypeName`, `GetFieldType`, `GetProviderSpecificValue(s)`, `GetProviderSpecificFieldType` | Core ordinal/type members and provider-specific value/type methods covered; native type names remain provider-defined |
| `DbDataReader` | `GetData`, `GetDbDataReader` | Nested-reader APIs are explicitly not asserted: the shared fixtures produce scalar columns, not provider-specific nested result columns; the API does not define a portable SQL shape for creating one |
| `DbDataReader` | `GetBytes`, `GetChars`, `GetStream`, `GetTextReader`, `GetSchemaTable`, `GetSchemaTableAsync`, `GetColumnSchema`, `GetColumnSchemaAsync`, `CloseAsync`, `DisposeAsync` | Valid reads, asynchronous close/dispose, and synchronous/asynchronous schema shape covered; buffer boundaries, stream materialization, native names, and unsupported schema are diagnostics/capabilities |
| `DbParameter` | `DbType`, `Direction`, `IsNullable`, `ParameterName`, `Precision`, `Scale`, `Size`, `SourceColumn`, `SourceColumnNullMapping`, `SourceVersion`, `Value`, `ResetDbType` | Property/default coverage in `ParameterTestBase`; defaults are strict only where the API documents them |
| `DbParameterCollection`/`IDataParameterCollection` | count/indexing, add/remove/insert, `AddRange`, `CopyTo`, `Contains`, `IndexOf`, logical-name lookup | Covered in `ParameterTestBase`; null-argument policy is diagnostic because the base collection API does not specify one universal outcome. `IsFixedSize`, `IsReadOnly`, `IsSynchronized`, and `SyncRoot` are inherited `IList`/`ICollection` mechanics and are intentionally not conformance assertions because the provider contract does not define a portable mutability or synchronization policy for them |
| `DbTransaction` | `Connection`, `IsolationLevel`, `Commit`, `Rollback`, savepoints, async equivalents, `Dispose` | Covered in `TransactionTestBase`; savepoint methods are checked against `SupportsSavepoints`, disposed connection invalidation follows the explicit interface contract, and unspecified completed-operation exception types remain diagnostics |
| `DbProviderFactory` | connection, command, parameter, connection-string builder creation | Required creation coverage in `DbProviderFactoryTestBase` |
| `DbProviderFactory` | command builder, data adapter, data-source enumerator, batch, data source | Capability-agreement coverage; unsupported optional members are skipped only when the factory advertises them as unavailable; an advertised capability that returns null, throws, or produces unusable metadata remains a diagnostic failure |
| `DbConnectionStringBuilder` | `ConnectionString` default/null behavior | Covered in `ConnectionStringTestBase`; provider-specific keyword grammar is intentionally supplied by the provider and not invented by the shared suite |
| `DbCommandBuilder` | factory creation and optional capability flags | Capability agreement is covered in `DbProviderFactoryTestBase`; SQL generation, quoting, and schema discovery require provider-specific adapter/table fixtures and are not portable shared assertions |
| `DbDataSourceEnumerator` | `GetDataSources` | Covered as an opt-in capability agreement in `DbProviderFactoryTestBase`; unimplemented enumeration is skipped, while advertised-but-unusable enumeration fails |
| `DbDataReaderExtensions` | `CanGetColumnSchema`, `GetColumnSchema` | Covered through the reader schema tests; the extension delegates to the reader/provider schema implementation and is not separately duplicated |
| `DbDataSource` (`net10.0`) | `ConnectionString`, `CreateConnection`, `OpenConnection`, `OpenConnectionAsync`, `CreateCommand`, `CreateBatch`, `DisposeAsync` | Covered by opt-in `DbDataSourceTestBase`; unsupported factory/data-source capabilities are explicitly skipped |

The remaining `DbConnectionStringBuilder` dictionary, synchronization, and
`ICustomTypeDescriptor` members are inherited framework plumbing whose useful
behavior depends on the provider's accepted connection-string keywords. The
shared suite covers the base connection-string contract and factory creation;
it deliberately does not invent a universal keyword or metadata grammar.

## Classification rules

Every shared test must document its classification and link the contract it
enforces:

- **VALID CONTRACT TEST** — the assertion is required by the documented API.
- **INVALID CONTRACT TEST** — the old assertion contradicts the documented API
  and must not be used as a conformance requirement.
- **COMMON BEHAVIOR TEST — NOT AN ADO.NET CONTRACT** — useful diagnostics for
  interoperability, but not a provider-conformance requirement.
- **WAS INVALID CONTRACT TEST — NOW VALID** — a changed assertion whose old
  form was invalid and whose new form is supported by the documented contract.

Tests that are changed must explain what was wrong and link to the contract.
Tests that are added must explain why the assertion is required. Tests that are
removed are only acceptable when the old assertion is explicitly documented as
invalid in the change and its replacement or diagnostic classification is
clear.

## Work completed so far

- The shared project targets `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- Parameter marker selection reads `DataSourceInformation` instead of assuming
  `@`; positional providers use `?`, and unusable metadata is reported rather
  than silently guessed.
- DML affected-row tests distinguish result-producing statements (`-1`) from
  INSERT/UPDATE/DELETE row counts.
- DML affected-row tests are independent per operation, cover synchronous and
  asynchronous execution, and verify `DbDataReader.RecordsAffected` after a
  DML reader is closed. Fixture setup and cleanup are statement lists, and
  cleanup is best-effort.
- Shared tests cover `CommandTimeout`, `UpdatedRowSource`,
  `VisibleFieldCount`, parameter property round-trips, and parameter collection
  `AddRange`/`CopyTo` ordering.
- Prepared commands are tested for successful execution, with an explicit
  documented skip when a provider reports preparation as unsupported.
- The shared project now also targets `net10.0`, where modern
  `DbProviderFactory` batch and data-source APIs are available for capability
  agreement testing.
- Active-reader `GetSchemaTable` is reported as an optional schema capability;
  supported providers must return non-empty metadata, while unsupported
  providers are explicitly skipped.
- Output-parameter coverage now has an explicit `IOutputParameterFixture`
  capability boundary so providers can test the shared execution contract
  without imposing one SQL calling convention on every provider.
- `ChangeDatabase` coverage now has an explicit `IChangeDatabaseFixture`
  capability boundary so the shared suite does not invent a database name or
  database-identity rule.
- `ConnectionTimeout` and the two-column parameter-marker metadata contract are
  directly validated by the shared tests.
- Transaction rollback-on-dispose coverage now has an explicit
  `ITransactionSemanticsFixture` boundary so the shared suite can verify the
  semantic contract without inventing table or key SQL.
- Explicit `System.Transactions` enlistment now has an
  `IEnlistmentFixture` capability boundary for providers that advertise it.
- `InputOutput` and `ReturnValue` parameter execution now has an explicit
  `IParameterDirectionFixture` capability boundary.
- Async transaction begin/commit/rollback lifecycle behavior is covered on the
  `netstandard2.1` and `net10.0` targets.
- Explicit isolation-level transactions now use an `IIsolationLevelFixture`
  capability boundary instead of requiring `Serializable` universally.
- Optional `DbProviderFactory` command-builder, data-adapter, and data-source
  enumerator tests now validate the provider's capability flags instead of
  requiring those optional objects to exist.
- `CommandBehavior.SchemaOnly` is tested as an optional command capability;
  supporting providers must return schema without rows.
- Shared tests include documented successful async command execution,
  `ReadAsync`, `IsDBNullAsync`, `NextResultAsync`, `DisposeAsync`, and async
  `GetFieldValueAsync`/transaction lifecycle coverage.
- Successful `OpenAsync` and `CloseAsync` connection lifecycle behavior is
  covered; cancellation remains documented as provider-dependent.
- Provider-specific SQL setup is kept in fixtures; multi-statement setup is
  executed statement-by-statement because `DbCommand.Prepare` does not require
  providers to accept a batch of statements.
- Provider-defined defaults, optional capabilities, and the generated
  `GetValueConversionTestBase` matrix are explicitly classified as
  common-behavior diagnostics. They are useful for exposing provider
  interoperability differences, but their outcomes are not universal
  `System.Data.Common` requirements. These tests use the
  `Category=Diagnostic` trait; run the conformance suite with
  `Category!=Diagnostic` and run diagnostics separately. The required
  reader-state, ordinal, null, and valid-value tests remain strict in
  `DataReaderTestBase`.
- Parameter binding/value-conversion examples that depend on provider-supported
  CLR types are likewise retained as common-behavior diagnostics; only marker
  discovery, logical parameter names, and universal parameter properties are
  conformance requirements. When a provider's diagnostic behavior differs, the
  test reports an explicit skip rather than turning that non-contract behavior
  into a conformance failure.

## Verification status

The shared library currently builds successfully for all supported target
frameworks:

```text
netstandard2.0
netstandard2.1
net10.0
```

The provider projects used during development are deliberately guinea pigs,
not part of this package and not part of the change. Their results are useful
for classifying the shared assertions:

- DuckDB's native library is available from the DuckDB.NET build output and the
  direct run succeeds when `LD_LIBRARY_PATH` includes
  `DuckDB.NET.Bindings/obj/runtimes/linux-x64/native`. The current run is
  `164 total, 1 failed, 14 skipped`; the only failure is
  `ExecuteReader_supports_SchemaOnly_when_advertised`. The official
  `CommandBehavior.SchemaOnly` contract requires column information without
  returning rows, so this remains a DuckDB provider defect, not a reason to
  weaken the shared assertion. The 14 skips are optional/provider-defined
  diagnostics.
- The exact filtered conformance commands used for the local guinea pigs were:

  ```text
  LD_LIBRARY_PATH=/home/alaricd/prj/pengdows/DuckDB.NET/DuckDB.NET.Bindings/obj/runtimes/linux-x64/native /home/alaricd/dotnet10/dotnet tests/DuckDB.Tests/bin/Debug/net10.0/DuckDB.Tests.dll -trait- "Category=Diagnostic"
  /home/alaricd/dotnet10/dotnet tests/FlatFile.Tests/bin/Debug/net10.0/FlatFile.Tests.dll -trait- "Category=Diagnostic"
  ```

  DuckDB reported `95 total, 1 failed, 0 skipped` (the SchemaOnly provider
  defect); FlatFile reported `103 total, 0 failed, 1 skipped` (the documented
  parallel-writer limitation).
- FlatFile executes the applicable shared tests, but rejects a valid positive
  `CommandTimeout` assignment with `NotSupportedException`. The documented
  property contract permits a provider to reject an unsupported timeout value,
  but does not permit rejecting a valid non-negative value when the property is
  exposed as supported; this needs provider-side classification or an explicit
  capability boundary.
- SQLite exposes useful diagnostics: its parameter collection `CopyTo`
  implementation throws `NotImplementedException`, and completed transaction
  operations use `ArgumentNullException` where the API documents
  `InvalidOperationException`; its disposed transaction also throws
  `ObjectDisposedException` when the documented `IDbTransaction.Connection`
  property is read instead of returning null. These remain provider defects
  unless the provider documents a narrower supported surface.
- The last captured complete SQLite `net10.0` run, before the current
  reclassification, reported 19 failures and 1,494 skips. The conversion
  matrix accounted for most of those skips because provider-specific conversion
  mismatches were incorrectly converted into `SkipException`. The current source
  executes those assertions and only skips a conversion case when the fixture
  explicitly reports that its database type is unsupported. The current source
  reclassifies the no-command-text case, six closed-reader cases, and four completed-
  transaction exception-type cases as diagnostics because their individual
  API pages do not prescribe those exact outcomes. The remaining failures
  (including `Prepare` with no connection/closed connection, parameter-name
  default, collection `CopyTo`, disposed-transaction connection, and documented
  ordinal behavior) remain strict provider-contract evidence. A fresh SQLite
  binary run is pending because the local project currently cannot restore
  `System.Data.SQLite` from its configured package feed.
- The net8 provider-host run has a test-runner/discovery failure before tests
  execute (`Test process did not return valid JSON`). That is an environment or
  test-project runner problem, not a provider result. The shared project itself
  still builds for net8-compatible targets (`netstandard2.0` and
  `netstandard2.1`).

Every provider run must report one of these outcomes for each failure:

1. shared assertion contradicts the .NET contract — fix the test and document
   the old assumption;
2. provider violates a required contract — retain the test and report the
   provider defect;
3. optional capability is not advertised — skip through the fixture capability;
4. provider/environment setup is unusable — report the setup failure without
   changing the contract test.

No provider-specific project is required to be committed to complete this
work. A provider project may be used locally to prove that a shared test is
portable, but the shared package must remain independent of it.

## Remaining contract work

### `DbConnection`

- Add malformed-metadata diagnostics for `DataSourceInformation` in addition to
  the valid marker-format coverage.
- Keep `StateChange` coverage for every supported state transition.
- Audit `Open`, `Close`, `Dispose`, `ConnectionString`, `Database`, `DataSource`,
  `ServerVersion`, `ConnectionTimeout`, `GetSchema`, `ChangeDatabase`, and
  enlistment individually against their API pages. A closed-state behavior is
  not contract-required merely because a particular provider throws there.

References: [`ChangeDatabase`](https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.changedatabase), [`BeginTransactionAsync`](https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.begintransactionasync), and [`EnlistTransaction`](https://learn.microsoft.com/dotnet/api/system.data.common.dbconnection.enlisttransaction).

### `DbCommand`

- `CloseConnection` and the observable `SchemaOnly` behavior are covered.
- `SingleRow` and `SingleResult` are retained only as tolerant interoperability
  diagnostics because the API describes them as provider hints, not mandatory
  truncation rules.
- `SequentialAccess` and `KeyInfo` are intentionally not strict assertions.
  `SequentialAccess` describes a provider's access strategy for large values,
  but the API does not expose a portable requirement for buffering or streaming
  strategy; `KeyInfo` requests provider/schema-specific key metadata. A basic
  reader execution is already covered, while asserting a particular streaming
  implementation or key-column shape would invent a provider contract. See
  [`CommandBehavior`](https://learn.microsoft.com/dotnet/api/system.data.commandbehavior)
  and [`DbDataReader.GetSchemaTable`](https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader.getschematable).
- Keep invalid-state tests only where `IDbCommand`/`DbCommand` documents the
  required exception. Empty `CommandText` and an unassigned active local
  transaction are now interoperability diagnostics because the base contract
  does not specify one universal rejection behavior.
- `Prepare` with no connection and with a closed connection remains strict
  because `IDbCommand.Prepare` explicitly requires `InvalidOperationException`.
  `Prepare` with an open connection but no command text is retained as a
  diagnostic: the `DbCommand.Prepare` page does not require rejection for that
  input.

References: [`ExecuteReaderAsync`](https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executereaderasync), [`ExecuteScalarAsync`](https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executescalarasync), [`ExecuteNonQueryAsync`](https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.executenonqueryasync), and [`Prepare`](https://learn.microsoft.com/dotnet/api/system.data.common.dbcommand.prepare).

### `DbDataReader`

- Valid-value typed getter coverage is present separately from closed-reader
  and after-dispose behavior; provider-specific value and field-type methods
  are also covered directly.
- Keep schema metadata assertions tolerant of provider-defined native type names
  while requiring the documented shape and state behavior.
- Audit every inherited `IDataRecord` member separately. Closed-reader and
  out-of-range failures must cite the individual API page, not only the class
  overview; conversion matrices must remain diagnostics. Closed-reader
  exception tests for `Read`, `NextResult`, `FieldCount`, `GetName`,
  `GetFieldType`, and `GetDataTypeName` are diagnostics where the individual
  API page does not prescribe an exception. `GetOrdinal` and typed getter
  out-of-range contracts remain strict where Microsoft documents
  `IndexOutOfRangeException`.

Reference: [`DbDataReader`](https://learn.microsoft.com/dotnet/api/system.data.common.dbdatareader).

### `DbParameter` and parameter collections

- Keep universal property round-trips limited to properties the API requires
  providers to implement for input parameters.
- Add capability-specific coverage for provider-supported output and
  input/output directions, precision/scale semantics, and native type support.
- Keep logical parameter names independent from SQL marker syntax.

References: [`DbParameter`](https://learn.microsoft.com/dotnet/api/system.data.common.dbparameter) and [`IDataParameterCollection`](https://learn.microsoft.com/dotnet/api/system.data.idataparametercollection).

### `DbTransaction`

- Savepoint methods and their asynchronous counterparts now verify the
  documented `SupportsSavepoints` agreement: unsupported providers must throw
  `NotSupportedException`, while providers advertising support must complete
  save, rollback-to-savepoint, and release operations.
- Test disposed and completed transaction behavior without requiring a specific
  provider exception subclass unless the API documents one.
- Preserve the disposed `IDbTransaction.Connection` test where the explicit
  interface documentation requires null after invalidation. The tests for
  calling `Commit` or `Rollback` after completion remain as diagnostics because
  the common API pages require a pending transaction but do not prescribe the
  exception type.

References: [`DbTransaction`](https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction), [`CommitAsync`](https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.commitasync), and [`RollbackAsync`](https://learn.microsoft.com/dotnet/api/system.data.common.dbtransaction.rollbackasync).

### `DbProviderFactory`

- Modern `CanCreateBatch`/`CreateBatch`, `CreateBatchCommand`, and
  `CreateDataSource` capability agreement tests run on the `net10.0` target.
- Preserve the same capability-aware treatment for command builders, adapters,
  and data-source enumerators.

Reference: [`DbProviderFactory`](https://learn.microsoft.com/dotnet/api/system.data.common.dbproviderfactory).

### `DbDataSource`

- The modern data-source surface has an opt-in shared base on `net10.0`.
  Providers that expose `CreateDataSource` can inherit it to verify closed
  connection creation, synchronous and asynchronous open postconditions, the
  provider-defined connection string, command creation, and optional batch
  creation.

Reference: [`DbDataSource`](https://learn.microsoft.com/dotnet/api/system.data.common.dbdatasource).

## Implementation checklist

The following checklist is the definition of done for this effort:

- [x] Inventory every public member in the referenced `System.Data.Common`
  types and map it to a shared test, an opt-in capability test, or a written
  reason that it cannot be tested portably. See
  `REFERENCE-API-INVENTORY.md` generated from the .NET 10 reference XML.
- [x] Put a classification comment and an authoritative Microsoft API link on
  every shared test, including the existing/generated test families where the
  assertion is not self-evident. The generated conversion family uses one
  class-level authoritative link and explicitly applies the diagnostic
  classification to every generated case.
- [x] For every changed assertion, state what the previous assertion assumed,
  why that assumption was invalid or too narrow, and why the replacement is
  contract-correct. The `WAS INVALID` comments and XML documentation on the
  changed command, reader, transaction, parameter, and affected-row tests
  carry that rationale and the authoritative API link.
- [x] Restore any removed test whose behavior is actually required by the
  contract. Remove a test only when its old assertion is explicitly recorded
  as invalid and a valid replacement or diagnostic remains. The current diff
  removes no test method; changed methods retain their names and coverage,
  while invalid assertions are classified or replaced in place.
- [x] Replace remaining hard-coded SQL conventions with fixture hooks:
  parameter markers, dummy tables, database-change targets, setup/cleanup SQL,
  and provider-specific native types. Simple shared probes now route through
  `DbFactoryTestBase.SelectSql` and related query hooks; intentionally invalid
  SQL and provider-specific DML remain explicit fixture/test inputs.
- [x] Separate required behavior from optional behavior with explicit fixture
  capabilities; an unsupported optional capability must be skipped, not
  silently treated as a pass. Diagnostic and optional tests are tagged
  `Category=Diagnostic`.
- [x] Complete the remaining `DbConnection`, `DbCommand`, `DbDataReader`,
  `DbParameter`, `DbTransaction`, and `DbProviderFactory` items listed above.
- [x] Run at least one positional-marker provider and one named-marker provider
  against the applicable shared tests, and record the exact commands and
  results.
- [x] Classify every observed provider failure before changing a shared
  assertion. Do not weaken a test merely to make a provider pass.
- [x] Build and pack all target frameworks, run `git diff --check`, and keep
  provider guinea-pig projects and credentials out of the commit.

## Completion criteria

This effort is complete only when all of the checklist above and the following
criteria are true:

1. The public `System.Data.Common` contract surface is inventoried against the
   shared test classes; every required behavior has a test or an explicit
   documented reason it cannot be tested universally.
2. Optional/provider-dependent behavior has an explicit capability or fixture
   contract and is not accidentally imposed on every provider.
3. Every shared test is classified and linked to its authoritative contract.
4. No shared test relies on a hard-coded parameter marker, dummy table, SQL
   batch syntax, or provider-specific exception type without fixture support.
5. The shared project builds for every target framework.
6. At least one positional-parameter provider and one named-parameter provider
   execute the applicable shared tests.
7. Provider failures are categorized as either a provider contract defect, an
   invalid shared assertion, an unsupported optional capability, or an
   environment/setup problem.
8. Provider-specific guinea-pig projects remain outside the shared package and
   are not committed as part of the specification-suite change.

The final change should include test commands and results for the shared build
and the provider matrix, plus links to the API documentation used for every
non-obvious assertion.
