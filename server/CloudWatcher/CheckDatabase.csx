using CloudWatcher.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Create a host to access the database
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<CloudWatcherContext>(options =>
            options.UseNpgsql(connectionString));
    })
    .Build();

using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();

Console.WriteLine("\n📍 Checking Locations in Database...\n");

var locations = await db.Locations.ToListAsync();

if (locations.Count == 0)
{
    Console.WriteLine("⚠️  No locations found in database!");
}
else
{
    Console.WriteLine($"✅ Found {locations.Count} locations:");
    foreach (var loc in locations)
    {
        Console.WriteLine($"  - {loc.Id} | {loc.Name ?? "(null)"} | Active: {loc.IsActive}");
    }
}

Console.WriteLine("\n📦 Checking Inventory Records with Locations...\n");

var inventory = await db.Inventory
    .Include(i => i.Location)
    .Where(i => i.PartId == Guid.Parse("550e8400-e29b-41d4-a716-446655440000"))
    .ToListAsync();

if (inventory.Count == 0)
{
    Console.WriteLine("⚠️  No inventory found for test part!");
}
else
{
    Console.WriteLine($"✅ Found {inventory.Count} inventory records:");
    foreach (var inv in inventory)
    {
        Console.WriteLine($"  - Location ID: {inv.LocationId}");
        Console.WriteLine($"    Location Obj: {(inv.Location != null ? "EXISTS" : "NULL")}");
        if (inv.Location != null)
        {
            Console.WriteLine($"    Location Name: {inv.Location.Name ?? "(null)"}");
        }
        Console.WriteLine($"    Quantity: {inv.QuantityOnHand}");
        Console.WriteLine();
    }
}
