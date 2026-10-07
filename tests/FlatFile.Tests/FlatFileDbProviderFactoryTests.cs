using AdoNet.Specification.Tests;

namespace FlatFile.Tests;

public sealed class FlatFileDbProviderFactoryTests : DbProviderFactoryTestBase<FlatFileDbFactoryFixture>
{
    public FlatFileDbProviderFactoryTests(FlatFileDbFactoryFixture fixture)
        : base(fixture)
    {
    }
}
