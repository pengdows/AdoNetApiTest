using System.Data.Common;
using AdoNet.Specification.Tests;

namespace Informix.Tests;

public sealed class InformixConnectionStringBuilderTests : ConnectionStringTestBase<InformixDbFactoryFixture>
{
    public InformixConnectionStringBuilderTests(InformixDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class InformixConnectionTests : ConnectionTestBase<InformixDbFactoryFixture>
{
    public InformixConnectionTests(InformixDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class InformixCommandTests : CommandTestBase<InformixDbFactoryFixture>
{
    public InformixCommandTests(InformixDbFactoryFixture fixture) : base(fixture) { }

    // Informix accepts positional '?' markers, but its DataSourceInformation
    // metadata is malformed and cannot be consumed by DbConnection.GetSchema.
    // Keep that provider defect visible in the schema tests while allowing the
    // command behavior tests to exercise the verified positional-parameter path.
    protected override string MakeParameterName(DbConnection connection, string name) => "?";
}

public sealed class InformixParameterTests : ParameterTestBase<InformixDbFactoryFixture>
{
    public InformixParameterTests(InformixDbFactoryFixture fixture) : base(fixture) { }

    // Informix accepts positional '?' markers, but its DataSourceInformation
    // metadata is malformed and cannot be consumed by DbConnection.GetSchema.
    // This override is intentionally confined to the Informix guinea-pig tests.
    protected override string MakeParameterName(DbConnection connection, string name) => "?";
}

public sealed class InformixProviderFactoryTests : DbProviderFactoryTestBase<InformixDbFactoryFixture>
{
    public InformixProviderFactoryTests(InformixDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class InformixDmlTests : DmlTestBase<InformixDbFactoryFixture>
{
    public InformixDmlTests(InformixDbFactoryFixture fixture) : base(fixture) { }
}
