using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;
using System;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));

// ✅ Configure Serilog Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app_log.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "logs-enron-{0:yyyy.MM.dd}"
    })
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog(); // Attach Serilog to .NET

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddControllers();
var app = builder.Build();

app.UseSerilogRequestLogging(); // Log all HTTP requests
app.UseCors("AllowAll");
app.MapControllers();

try
{
    Log.Information("🚀 Starting up the application...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "🚨 Application terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}