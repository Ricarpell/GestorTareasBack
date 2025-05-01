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

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACIÓN BÁSICA ========== //
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ========== SWAGGER (SOLO EN DESARROLLO) ========== //
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Manager API",
        Version = "v1",
        Description = "API para gestión de tareas con MongoDB",
        Contact = new OpenApiContact { Name = "Soporte", Email = "soporte@taskmanager.com" }
    });

    c.MapType<ObjectId>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "24-digit hex string",
        Example = OpenApiAnyFactory.CreateFromJson("\"507f191e810c19729de860ea\"")
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ========== MONGO DB (CON SSL/TLS) ========== //
var connectionString = builder.Configuration.GetConnectionString("MongoDbConnection");
var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

// Configuración SSL/TLS crítica para MongoDB Atlas
mongoClientSettings.SslSettings = new SslSettings
{
    EnabledSslProtocols = SslProtocols.Tls12,
    ServerCertificateValidationCallback = (sender, cert, chain, errors) => true // Solo para desarrollo
};

mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.SocketTimeout = TimeSpan.FromSeconds(30);

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
builder.Services.AddSingleton<TaskContext>();

// ========== CONSTRUIR APP ========== //
var app = builder.Build();

// ========== MIDDLEWARE PIPELINE ========== //
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Manager API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();

// CORS (Ajusta los orígenes en producción)
app.UseCors(builder => builder
    .WithOrigins("https://gestorricardo.netlify.app", "http://localhost:3000")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthorization();
app.MapControllers();

// ========== EVITAR CIERRE INESPERADO (RENDER) ========== //
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => Thread.Sleep(Timeout.Infinite));

app.Run();
