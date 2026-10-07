using AdoNet.Specification.Tests;

namespace FlatFile.Tests;

public sealed class FlatFileParameterTests : ParameterTestBase<FlatFileDbFactoryFixture>
{
    public FlatFileParameterTests(FlatFileDbFactoryFixture fixture)
        : base(fixture)
    {
    }
}
