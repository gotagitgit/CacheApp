using System.Runtime.CompilerServices;

namespace CacheApp.IntegrationTests;

public sealed class RequiresExternalDependenciesAttribute : FactAttribute
{
    public RequiresExternalDependenciesAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__ProductDatabase")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__Redis")))
        {
            Skip = "Set ConnectionStrings__ProductDatabase and ConnectionStrings__Redis to run SQL Server/Redis integration tests.";
        }
    }
}
