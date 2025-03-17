using EasyNetQ;
using EmailCleaner.Infrastructure.Messaging;
using EmailCleaner.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Register EasyNetQ
var bus = RabbitHutch.CreateBus("host=localhost");
builder.Services.AddSingleton<IBus>(bus);

// Register services
builder.Services.AddScoped<EmailProcessor>();
builder.Services.AddScoped<EasyNetQPublisher>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();