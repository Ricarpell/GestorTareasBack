using TaskManagerApi.Data;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
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
        Example = new OpenApiString("507f191e810c19729de860ea")
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configuración de MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDbConnection");
var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

mongoClientSettings.SslSettings = new SslSettings
{
    EnabledSslProtocols = SslProtocols.Tls12
};
mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.SocketTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.RetryWrites = true;
mongoClientSettings.ReadPreference = ReadPreference.Primary;

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
builder.Services.AddSingleton<TaskContext>();

var app = builder.Build();

// Middleware para registrar solicitudes
app.Use(async (context, next) =>
{
    app.Logger.LogInformation($"Solicitud recibida: {context.Request.Method} {context.Request.Path}");
    await next();
});

// Middleware para manejar errores globalmente
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception != null)
        {
            app.Logger.LogError(exception, "Error no controlado");
            var errorResponse = new
            {
                error = app.Environment.IsDevelopment() ? exception.Message : "Error interno del servidor"
            };
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    });
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

// Configuración CORS
var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")?.Split(";") 
    ?? new[] { "https://gestorricardo.netlify.app", "http://localhost:3000", "http://localhost:5500", "http://127.0.0.1:5500" };
app.UseCors(builder => builder
    .WithOrigins(allowedOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthorization();
app.MapControllers();

// Endpoints adicionales
app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("API de Tareas funcionando");
});
app.MapGet("/healthz", () => new { status = "Healthy" });

// Configuración del puerto dinámico para Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
