using CacheApp.Api.Endpoints;
using CacheApp.Application.Products;
using CacheApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.AddInfrastructureAsync(builder.Configuration);
builder.Services.AddScoped<ProductService>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapProductEndpoints();

await app.RunAsync();

public partial class Program
{
}
