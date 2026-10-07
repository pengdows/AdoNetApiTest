using Informix.Net.Core;
using Xunit;

namespace Informix.Tests;

public sealed class InformixParameterProbeTests
{
    private readonly ITestOutputHelper _output;

    public InformixParameterProbeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Probe_parameter_markers()
    {
        var connectionString = Environment.GetEnvironmentVariable("INFORMIX_CONNECTION_STRING")
            ?? "Host=localhost;Service=9088;Server=informixserver;Database=testdb;UID=informix;PWD=in4mix;Delimident=true;";

        using var connection = InformixClientFactory.Instance.CreateConnection()!;
        connection.ConnectionString = connectionString;
        connection.Open();

        foreach (var sql in new[] { "SELECT tabname FROM systables WHERE tabid = ?;", "SELECT tabname FROM systables WHERE tabid = @Parameter;" })
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "Parameter";
            parameter.Value = 42;
            command.Parameters.Add(parameter);

            try
            {
                _output.WriteLine($"{sql} => {command.ExecuteScalar()}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"{sql} => {ex.GetType().FullName}: {ex.Message}");
            }
        }
    }
}
