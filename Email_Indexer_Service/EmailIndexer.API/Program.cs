using EasyNetQ;
using MongoDB.Driver;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EmailIndexer.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Register RabbitMQ
builder.Services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus("host=localhost"));

// Register MongoDB
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));

// Register Services
builder.Services.AddSingleton<EmailConsumer>();

var app = builder.Build();

// Start Consumer
var emailConsumer = app.Services.GetRequiredService<EmailConsumer>();
Task.Run(() => emailConsumer.StartListening());

app.Run();