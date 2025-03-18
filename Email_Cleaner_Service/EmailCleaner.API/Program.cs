using EasyNetQ;
using EmailCleaner.Infrastructure.Messaging;
using EmailCleaner.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Register EasyNetQ
var bus = RabbitHutch.CreateBus("host=localhost");
builder.Services.AddSingleton<IBus>(bus);

// Register services
builder.Services.AddScoped<EmailProcessor>();
builder.Services.AddScoped<EasyNetQPublisher>();

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