using EasyNetQ;
using MongoDB.Driver;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EmailIndexer.Infrastructure.Messaging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("EmailIndexerService"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("RabbitMQ") // Track RabbitMQ events
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317"); // OpenTelemetry Collector
            });
    });

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

// Register RabbitMQ
builder.Services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus("host=localhost"));

// Register MongoDB
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));

// Register Services
builder.Services.AddSingleton<EmailConsumer>();

var app = builder.Build();
app.UseRouting();
app.UseSerilogRequestLogging(); // Log all HTTP requests

// Start Consumer
var emailConsumer = app.Services.GetRequiredService<EmailConsumer>();
Task.Run(() => emailConsumer.StartListening());

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