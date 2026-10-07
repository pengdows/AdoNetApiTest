using AdoNet.Specification.Tests;

namespace FlatFile.Tests;

public sealed class FlatFileConnectionTests : ConnectionTestBase<FlatFileDbFactoryFixture>
{
    public FlatFileConnectionTests(FlatFileDbFactoryFixture fixture)
        : base(fixture)
    {
    }
}
