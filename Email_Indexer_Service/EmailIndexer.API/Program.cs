using EasyNetQ;
using EmailIndexer.Infrastructure;
using EmailIndexer.Infrastructure.Data;
using EmailIndexer.Infrastructure.Messaging;
using EmailIndexer.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL
builder.Services.AddDbContext<IndexerDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=emailindexer;Username=postgres;Password=yourpassword",
        b => b.MigrationsAssembly("EmailIndexer.Infrastructure")));

// Register RabbitMQ
builder.Services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus("host=localhost"));

// Register Services
builder.Services.AddScoped<EmailIndexerService>();
builder.Services.AddSingleton<EmailConsumer>();

var app = builder.Build();

// Start Consumer
var emailConsumer = app.Services.GetRequiredService<EmailConsumer>();
Task.Run(() => emailConsumer.StartListening());

app.Run();