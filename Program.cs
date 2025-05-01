using TaskManagerApi.Data;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Configuración esencial
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuración MongoDB con reintentos
var connectionString = builder.Configuration.GetConnectionString("MongoDbConnection");
var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
mongoClientSettings.SslSettings = new SslSettings
{
    EnabledSslProtocols = SslProtocols.Tls12
};
mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.RetryWrites = true;

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
builder.Services.AddSingleton<TaskContext>();

var app = builder.Build();

// Middleware CRÍTICO en orden correcto
app.UseRouting();
app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthorization();

// Registro explícito de controladores
app.MapControllers();

// Endpoint health check mínimo
app.MapGet("/healthz", () => "Healthy");

// Evita que la aplicación se cierre en Render
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => Thread.Sleep(Timeout.Infinite));

app.Run();
