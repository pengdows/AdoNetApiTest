using AdoNet.Specification.Tests;
using Xunit;

namespace DuckDB.Tests;

public sealed class DuckDBConnectionStringBuilderTests : ConnectionStringTestBase<DuckDBDbFactoryFixture>
{
    public DuckDBConnectionStringBuilderTests(DuckDBDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class DuckDBConnectionTests : ConnectionTestBase<DuckDBDbFactoryFixture>
{
    public DuckDBConnectionTests(DuckDBDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class DuckDBCommandTests : CommandTestBase<DuckDBDbFactoryFixture>
{
    public DuckDBCommandTests(DuckDBDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class DuckDBParameterTests : ParameterTestBase<DuckDBDbFactoryFixture>
{
    public DuckDBParameterTests(DuckDBDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class DuckDBDmlTests : DmlTestBase<DuckDBDbFactoryFixture>
{
    public DuckDBDmlTests(DuckDBDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class DuckDBProviderFactoryTests : DbProviderFactoryTestBase<DuckDBDbFactoryFixture>
{
    public DuckDBProviderFactoryTests(DuckDBDbFactoryFixture fixture) : base(fixture) { }
}
