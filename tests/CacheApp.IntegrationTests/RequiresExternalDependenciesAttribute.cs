using System.Runtime.CompilerServices;

namespace CacheApp.IntegrationTests;

public sealed class RequiresExternalDependenciesAttribute : FactAttribute
{
    public RequiresExternalDependenciesAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__TestSqlServer")))
        {
            Skip = "Set ConnectionStrings__TestSqlServer to a SQL Server system-database connection string with permission to create and drop test databases.";
        }
    }
}
