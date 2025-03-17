using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Nest;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));

// Register Elasticsearch
builder.Services.AddSingleton<IElasticClient>(_ =>
{
    var settings = new ConnectionSettings(new Uri("http://localhost:9200")).DefaultIndex("emails");
    return new ElasticClient(settings);
});

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

app.UseCors("AllowAll");
app.MapControllers();
app.Run();