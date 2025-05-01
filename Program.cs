using TaskManagerApi.Data;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;
using System.Security.Authentication;

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

    c.OperationFilter<ErrorResponsesOperationFilter>();

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

// Configuración SSL/TLS crítica
mongoClientSettings.SslSettings = new SslSettings
{
    EnabledSslProtocols = SslProtocols.Tls12,
    ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true
};

mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.SocketTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.RetryWrites = true;
mongoClientSettings.ReadPreference = ReadPreference.Primary;
mongoClientSettings.ApplicationName = "TaskManagerAPI";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
builder.Services.AddSingleton<TaskContext>();

var app = builder.Build();

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Manager API v1");
        c.RoutePrefix = "swagger";
        c.ConfigObject.DisplayRequestDuration = true;
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Configuración CORS mejorada
app.UseCors(builder => builder
    .WithOrigins("https://gestorricardo.netlify.app")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthorization();
app.MapControllers();

app.Run();

public class ErrorResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.Add("400", new OpenApiResponse { Description = "Bad Request" });
        operation.Responses.Add("500", new OpenApiResponse
        {
            Description = "Server Error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository)
                }
            }
        });
    }
}
