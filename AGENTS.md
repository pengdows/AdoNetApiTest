# Repository Guidelines

## Project Structure

- `src/AdoNet.Specification.Tests/` contains reusable xUnit v3 base classes and fixtures for ADO.NET behavior.
- `src/AdoNet.Databases/` contains database setup and SQL helpers; `src/AdoNetApiTest/` contains the comparison-runner console application.
- `tests/<Provider>.Tests/` contains provider-specific fixtures and derived tests (for example, `tests/MySqlConnector.Tests/`).
- `docker/` defines local MySQL, PostgreSQL, and SQL Server services. Solution-wide settings are in `Directory.Build.props`, `global.json`, and `AdoNetApiTest.sln`.

## Build, Test, and Development Commands

Use the SDK version pinned by `global.json` (currently .NET 10.0.101):

```bash
dotnet restore AdoNetApiTest.sln
dotnet build AdoNetApiTest.sln
dotnet test tests/MySqlConnector.Tests/MySqlConnector.Tests.csproj
docker compose -f docker/docker-compose.yml up
dotnet run --project src/AdoNetApiTest/AdoNetApiTest.csproj
```

Run the individual provider test project relevant to a change. Database-backed suites need the matching service running and usually read `ConnectionString` from the environment. CI runs provider suites independently in Azure Pipelines and publishes test artifacts.

## Coding Style & Naming

Use four-space indentation, standard C# formatting, nullable-safe code where applicable, and the existing file-scoped/project conventions. Name types and public members in `PascalCase`, locals and parameters in `camelCase`, and provider test classes with the provider prefix plus the behavior under test (for example, `SqliteDataReaderTests`). Keep shared behavior in `src/AdoNet.Specification.Tests`; put provider-specific exceptions or overrides in that provider’s test project.

## Testing Guidelines

Tests use xUnit v3 with `Microsoft.NET.Test.Sdk`. Add or extend the appropriate `XTestBase<T>` coverage and use the existing fixture interfaces (`IDbFactoryFixture` and `ISelectValueFixture`). Test files conventionally end in `Tests.cs`; run the narrow project first, then the solution or affected provider matrix. There is no separate coverage threshold configured.

## Commits and Pull Requests

Use short, imperative commit subjects such as `Build with .NET 10.` or `Update local Docker images.` Keep unrelated changes separate. Pull requests should explain the behavioral change, identify affected providers, link the relevant issue when one exists, and include the test commands/results. Mention required database services or environment variables, and call out any intentional provider-specific skips or overrides.

## Configuration and Security

Do not commit credentials or connection strings containing secrets. Use local environment variables and the documented Docker services for database access; review `azure-pipelines.yml` before changing CI connection settings.
