using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CloudWatcher.CloudStorage;
using CloudWatcher.RequestHandling;
using CloudWatcher.Data;
using CloudWatcher.Middleware;
using CloudWatcher.Auth;

var builder = WebApplication.CreateBuilder(args);

// Compute repo root
var repoRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.FullName;
var cloudRoot = Path.Combine(repoRoot, "Cloud");

// ============================================================================
// DATABASE CONFIGURATION
// ============================================================================

// Register DbContext with PostgreSQL (or SQL Server fallback)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    if (connectionString.Contains("Host=") || connectionString.Contains("Server=") && connectionString.Contains("Port="))
    {
        // PostgreSQL connection
        builder.Services.AddDbContext<CloudWatcherContext>(options =>
            options.UseNpgsql(connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("CloudWatcher")));
    }
    else
    {
        // SQL Server fallback
        builder.Services.AddDbContext<CloudWatcherContext>(options =>
            options.UseSqlServer(connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly("CloudWatcher")));
    }
}
else
{
    // In-memory database for development/testing
    builder.Services.AddDbContext<CloudWatcherContext>(options =>
        options.UseInMemoryDatabase("CloudWatcherDev"));
    
    builder.Logging.AddConsole().AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}

// ============================================================================
// DEPENDENCY INJECTION
// ============================================================================

// Register cloud storage services
builder.Services.AddScoped<ICloudStorageProvider, LocalFileStorageProvider>();
builder.Services.AddScoped(sp => new RequestHandler(sp.GetRequiredService<ICloudStorageProvider>()));

// ============================================================================
// CORS CONFIGURATION
// ============================================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDeviceAndMobileClients", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // React/PWA dev server
                "http://localhost:5173",  // Vite dev server
                "capacitor://localhost",  // Capacitor mobile app
                "ionic://localhost",      // Ionic mobile app
                "http://localhost",       // General local development
                "https://localhost"       // HTTPS local development
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ============================================================================
// AUTHENTICATION & AUTHORIZATION CONFIGURATION
// ============================================================================

// Add JWT Bearer authentication with Azure AD / OAuth2 support
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add custom authorization policies
builder.Services.AddCustomAuthorizationPolicies();

// ============================================================================
// CONTROLLER AND JSON CONFIGURATION
// ============================================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ============================================================================
// SWAGGER/OPENAPI CONFIGURATION
// ============================================================================

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

// ============================================================================
// HEALTH CHECKS CONFIGURATION
// ============================================================================

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CloudWatcherContext>("database", tags: new[] { "db", "ready" });

// ============================================================================
// LOGGING CONFIGURATION
// ============================================================================

// Add logging
builder.Services.AddLogging(options =>
{
    options.AddConsole();
    options.AddDebug();
});

// ============================================================================
// BUILD APPLICATION
// ============================================================================

var app = builder.Build();

// Create required directories
Directory.CreateDirectory(Path.Combine(cloudRoot, "Requests"));
Directory.CreateDirectory(Path.Combine(cloudRoot, "Responses"));

// ============================================================================
// MIDDLEWARE PIPELINE CONFIGURATION
// ============================================================================

// Add global error handling middleware (must be first)
app.UseMiddleware<GlobalErrorHandlerMiddleware>();

// Add request/response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

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

// Enable CORS
app.UseCors("AllowDeviceAndMobileClients");

// Add authentication middleware
app.UseAuthentication();

// Add authorization middleware
app.UseAuthorization();
app.MapControllers();

// ============================================================================
// HEALTH CHECK ENDPOINTS
// ============================================================================

// Map health check endpoints with detailed dependency checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow.ToString("o"),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        }, new JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(result);
    }
});

// Simpler health check for load balancers
app.MapGet("/health/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow.ToString("o") }))
    .WithName("HealthCheckLiveness")
    .WithOpenApi()
    .Produces(200);

// Detailed health check for readiness
app.MapGet("/health/ready", async (Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService) =>
{
    var result = await healthCheckService.CheckHealthAsync();
    return result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
        ? Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow.ToString("o") })
        : Results.StatusCode(503);
})
    .WithName("HealthCheckReadiness")
    .WithOpenApi()
    .Produces(200)
    .Produces(503);

// Legacy health endpoint (kept for backward compatibility)
app.MapGet("/health-legacy", () => new { status = "healthy", timestamp = DateTime.UtcNow.ToString("o") })
    .WithName("HealthCheckLegacy")
    .WithOpenApi()
    .ExcludeFromDescription()
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
