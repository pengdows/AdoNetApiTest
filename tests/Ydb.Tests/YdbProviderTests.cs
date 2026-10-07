using AdoNet.Specification.Tests;

namespace Ydb.Tests;

public sealed class YdbConnectionStringBuilderTests : ConnectionStringTestBase<YdbDbFactoryFixture>
{
    public YdbConnectionStringBuilderTests(YdbDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class YdbConnectionTests : ConnectionTestBase<YdbDbFactoryFixture>
{
    public YdbConnectionTests(YdbDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class YdbCommandTests : CommandTestBase<YdbDbFactoryFixture>
{
    public YdbCommandTests(YdbDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class YdbParameterTests : ParameterTestBase<YdbDbFactoryFixture>
{
    public YdbParameterTests(YdbDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class YdbProviderFactoryTests : DbProviderFactoryTestBase<YdbDbFactoryFixture>
{
    public YdbProviderFactoryTests(YdbDbFactoryFixture fixture) : base(fixture) { }
}

public sealed class YdbDmlTests : DmlTestBase<YdbDbFactoryFixture>
{
    public YdbDmlTests(YdbDbFactoryFixture fixture) : base(fixture) { }
}
