using CloudWatcher.Data;
using CloudWatcher.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");

// Build DbContext options
var optionsBuilder = new DbContextOptionsBuilder<CloudWatcherContext>();
optionsBuilder.UseNpgsql(connectionString);

// Create context and seeder
using var context = new CloudWatcherContext(optionsBuilder.Options);
var seeder = new Wave4TestSeeder(context);

Console.WriteLine("🚀 Wave 4 Test Data Seeding Tool");
Console.WriteLine("=================================");
Console.WriteLine($"📍 Database: {connectionString?.Split(';')[2]}");
Console.WriteLine();

try
{
    // Ensure database exists and migrations are applied
    Console.WriteLine("🔍 Checking database connection...");
    if (await context.Database.CanConnectAsync())
    {
        Console.WriteLine("✅ Database connection successful");
    }
    else
    {
        Console.WriteLine("❌ Cannot connect to database");
        return 1;
    }

    // Run the seeder
    await seeder.SeedAsync();
    
    // Verify the data
    Console.WriteLine();
    Console.WriteLine("🔍 Verifying seeded data...");
    
    var partCount = await context.Parts.CountAsync(p => p.Code == "PART-001");
    var locationCount = await context.Locations.CountAsync(l => 
        l.Name.Contains("Warehouse") || 
        l.Name.Contains("Retail") || 
        l.Name.Contains("Truck"));
    var inventoryCount = await context.Inventory.CountAsync(i => i.Part!.Code == "PART-001");
    var orderCount = await context.Orders.CountAsync(o => 
        o.Status == "pending" || o.Status == "approved");
    
    Console.WriteLine($"   Parts: {partCount}");
    Console.WriteLine($"   Locations: {locationCount}");
    Console.WriteLine($"   Inventory Records: {inventoryCount}");
    Console.WriteLine($"   Orders: {orderCount}");
    
    Console.WriteLine();
    Console.WriteLine("✅ Wave 4 test data seeding completed successfully!");
    Console.WriteLine();
    Console.WriteLine("📋 Next Steps:");
    Console.WriteLine("   1. Run the API server");
    Console.WriteLine("   2. Test: GET /api/v2/inventory/550e8400-e29b-41d4-a716-446655440000");
    Console.WriteLine("   3. Test: GET /api/v2/inventory/550e8400-e29b-41d4-a716-446655440000/availability");
    Console.WriteLine("   4. Verify reserved, incoming, and backorder calculations");
    
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}
