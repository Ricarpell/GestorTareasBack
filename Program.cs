
     using TaskManagerApi.Data;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;
using System.Security.Authentication;
using Microsoft.Extensions.Hosting;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Configuración de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configuración de controladores
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configuración de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Manager API",
        Version = "v1",
        Description = "API para gestión de tareas con MongoDB",
        Contact = new OpenApiContact
        {
            Name = "Soporte",
            Email = "soporte@taskmanager.com"
        }
    });

    c.MapType<ObjectId>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "24-digit hex string",
        Example = OpenApiAnyFactory.CreateFromJson("\"507f191e810c19729de860ea\"")
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configuración mejorada de MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDbConnection");
var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

mongoClientSettings.SslSettings = new SslSettings
{
    EnabledSslProtocols = SslProtocols.Tls12,
    ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true
};

mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(45);
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(45);
mongoClientSettings.SocketTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.RetryWrites = true;
mongoClientSettings.ReadPreference = ReadPreference.Primary;

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
builder.Services.AddSingleton<TaskContext>();

var app = builder.Build();

// Middleware para manejar errores globalmente
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsync($"Error interno: {ex.Message}");
        app.Logger.LogError(ex, "Error no controlado");
    }
});

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Manager API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Configuración CORS actualizada
app.UseCors(builder => builder
    .WithOrigins(
        "https://gestorricardo.netlify.app",
        "http://localhost:3000",
        "http://localhost:5500",
        "http://127.0.0.1:5500",
        "https://gestortareasback.onrender.com")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseRouting();
app.UseAuthorization();

// Configuración explícita de endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("API de Tareas funcionando");
    });
    endpoints.MapGet("/healthz", () => "Healthy");
});

// Solución para mantener la aplicación corriendo en Render
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => Thread.Sleep(Timeout.Infinite));

app.Run();
     