using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using CloudWatcher.CloudStorage;
using CloudWatcher.RequestHandling;

var builder = WebApplication.CreateBuilder(args);

// Compute repo root
var repoRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.FullName;
var cloudRoot = Path.Combine(repoRoot, "Cloud");

// Register services
builder.Services.AddScoped<ICloudStorageProvider, LocalFileStorageProvider>();
builder.Services.AddScoped(sp => new RequestHandler(sp.GetRequiredService<ICloudStorageProvider>()));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CloudWatcher API",
        Version = "v1",
        Description = "RESTful API for managing device requests and responses",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "CloudWatcher Team"
        }
    });

    // Add XML documentation if file exists
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "CloudWatcher.xml");
    if (File.Exists(xmlFile))
    {
        options.IncludeXmlComments(xmlFile);
    }
});

// Add logging
builder.Services.AddLogging(options =>
{
    options.AddConsole();
    options.AddDebug();
});

var app = builder.Build();

// Create required directories
Directory.CreateDirectory(Path.Combine(cloudRoot, "Requests"));
Directory.CreateDirectory(Path.Combine(cloudRoot, "Responses"));

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CloudWatcher API v1");
        options.RoutePrefix = string.Empty; // Serve at root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow.ToString("o") })
    .WithName("HealthCheck")
    .WithOpenApi()
    .Produces(200);

// Add info endpoint
app.MapGet("/info", () => new
{
    service = "CloudWatcher",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    cloudRoot = cloudRoot,
    timestamp = DateTime.UtcNow.ToString("o")
})
    .WithName("ServiceInfo")
    .WithOpenApi()
    .Produces(200);

await app.RunAsync();
