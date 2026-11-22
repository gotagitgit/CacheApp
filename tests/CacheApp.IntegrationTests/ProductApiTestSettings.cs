namespace CacheApp.IntegrationTests;

public sealed record ProductApiTestSettings(
    string ProductDatabaseConnectionString,
    string RedisConnectionString,
    string RedisInstanceName,
    string InvalidationChannel);
