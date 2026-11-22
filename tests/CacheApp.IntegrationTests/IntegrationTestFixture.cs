using System.Data.Common;
using CacheApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Redis;

namespace CacheApp.IntegrationTests;

[CollectionDefinition(CollectionName)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = "CacheApp integration tests";
}

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();
    private readonly string _redisInstanceName = $"cacheapp-integration-{Guid.NewGuid():N}:";
    private readonly string _invalidationChannel = $"cacheapp-integration-{Guid.NewGuid():N}";
    private string? _systemDatabaseConnectionString;
    private bool _databaseCreated;
    private bool _redisDisposed;

    public string DatabaseName { get; } = $"ProductCachingTest_{Guid.NewGuid():N}";

    public string ProductDatabaseConnectionString =>
        WithDatabase(SystemDatabaseConnectionString, DatabaseName);

    public ProductApiTestSettings CreateApiSettings()
    {
        if (!_databaseCreated)
        {
            throw new InvalidOperationException("The integration test database has not been created.");
        }

        return new ProductApiTestSettings(
            ProductDatabaseConnectionString,
            _redisContainer.GetConnectionString(),
            _redisInstanceName,
            _invalidationChannel);
    }

    public async ValueTask InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TestSqlServer");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            // RequiresExternalDependenciesAttribute skips the integration test in this case.
            return;
        }

        _systemDatabaseConnectionString = WithDatabase(configuredConnectionString, "master");

        try
        {
            await _redisContainer.StartAsync();
            await CreateDatabaseAsync();
            await ApplyMigrationsAsync();
        }
        catch (Exception initializationException)
        {
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Integration test dependency setup failed, and cleanup also failed.",
                    initializationException,
                    cleanupException);
            }

            throw new InvalidOperationException(
                "Could not start the Redis test container or initialize the temporary SQL Server database. " +
                "Verify that Docker is running and ConnectionStrings__TestSqlServer targets a SQL Server " +
                "system database with permission to create and drop databases.",
                initializationException);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_databaseCreated && _systemDatabaseConnectionString is not null)
            {
                var escapedDatabaseName = EscapeIdentifier(DatabaseName);
                await ExecuteSystemDatabaseCommandAsync(
                    $"IF DB_ID(N'{DatabaseName}') IS NOT NULL " +
                    $"BEGIN ALTER DATABASE [{escapedDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    $"DROP DATABASE [{escapedDatabaseName}]; END");
                _databaseCreated = false;
            }
        }
        finally
        {
            if (!_redisDisposed)
            {
                await _redisContainer.DisposeAsync();
                _redisDisposed = true;
            }
        }
    }

    private async Task CreateDatabaseAsync()
    {
        // The identifier is generated from a fixed prefix and GUID, then escaped for SQL Server.
        await ExecuteSystemDatabaseCommandAsync($"CREATE DATABASE [{EscapeIdentifier(DatabaseName)}]");
        _databaseCreated = true;
    }

    private async Task ExecuteSystemDatabaseCommandAsync(string commandText)
    {
        await using var context = CreateDbContext(SystemDatabaseConnectionString);
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        await using var context = CreateDbContext(ProductDatabaseConnectionString);
        await context.Database.MigrateAsync();
    }

    private static ProductDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ProductDbContext(options);
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        builder.Remove("Database");
        builder.Remove("Initial Catalog");
        builder["Initial Catalog"] = databaseName;
        return builder.ConnectionString;
    }

    private string SystemDatabaseConnectionString =>
        _systemDatabaseConnectionString ?? throw new InvalidOperationException(
            "The SQL Server test connection string has not been initialized.");

    private static string EscapeIdentifier(string identifier) => identifier.Replace("]", "]]", StringComparison.Ordinal);
}
