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
using CloudWatcher.Services;
using CloudWatcher.WebSockets;

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

// Register WebSocket services
builder.Services.AddSingleton<WebSocketConnectionPool>();
builder.Services.AddScoped<WebSocketMessageRouter>();

// Register background services
builder.Services.AddHostedService<InventoryAuditRetentionService>();

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
    options.SwaggerDoc("v1", new()
    {
        Title = "CloudWatcher API",
        Version = "v1",
        Description = "RESTful API for managing device requests and responses"
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
// APPLY PENDING DATABASE MIGRATIONS ON STARTUP
// ============================================================================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CloudWatcher.Data.CloudWatcherContext>();
        db.Database.Migrate();
        
        // Check for --seed-wave4 command-line argument
        if (args.Contains("--seed-wave4"))
        {
            app.Logger.LogInformation("🌱 Seeding Wave 4 test data (triggered by --seed-wave4 argument)");
            var seeder = new CloudWatcher.Seeds.Wave4TestSeeder(db);
            await seeder.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migration failed at startup");
        // Allow app to continue; health checks will surface DB readiness
    }
}

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

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Development-only API key bypass for local testing (injects dev identity BEFORE authentication)
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<LocalDevApiKeyMiddleware>();
}

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

// Note: /health/live and /health/ready are handled by HealthController
// Legacy health endpoint (kept for backward compatibility)
app.MapGet("/health-legacy", () => new { status = "healthy", timestamp = DateTime.UtcNow.ToString("o") })
    .WithName("HealthCheckLegacy")
    .WithOpenApi()
    .ExcludeFromDescription()
    .Produces(200);

// Note: /info endpoint is handled by HealthController

await app.RunAsync();
