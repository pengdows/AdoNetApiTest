using AdoNet.Specification.Tests;
using Xunit;

namespace FlatFile.Tests;

public sealed class FlatFileCommandTests : CommandTestBase<FlatFileDbFactoryFixture>
{
    public FlatFileCommandTests(FlatFileDbFactoryFixture fixture)
        : base(fixture)
    {
    }

    [Fact(Skip = "FlatFile permits only one writer connection per database root; this test requires a parallel writer.")]
    public override void ExecuteReader_throws_when_transaction_mismatched()
    {
    }
}

public sealed class FlatFileDmlTests : DmlTestBase<FlatFileDbFactoryFixture>
{
    public FlatFileDmlTests(FlatFileDbFactoryFixture fixture)
        : base(fixture)
    {
    }
}
