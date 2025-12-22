using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

class Program
{
    // Compute repo root from current working directory (project folder): CloudWatcher -> server -> repo root
    static string RepoRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.FullName;
    static string CloudRoot = Path.Combine(RepoRoot, "Cloud");
    static string RequestsRoot = Path.Combine(CloudRoot, "Requests");
    static string ResponsesRoot = Path.Combine(CloudRoot, "Responses");

    static void Main()
    {
        Directory.CreateDirectory(RequestsRoot);
        Directory.CreateDirectory(ResponsesRoot);
        Console.WriteLine($"RepoRoot: {RepoRoot}");
        Console.WriteLine($"CloudRoot: {CloudRoot}");
        Console.WriteLine($"CloudWatcher started. Watching: {RequestsRoot}");

        using var watcher = new FileSystemWatcher(RequestsRoot, "*.json")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        watcher.Created += async (s, e) => await OnNewRequest(e.FullPath);
        watcher.Changed += async (s, e) => await OnFileChanged(e.FullPath);

        // Process any existing JSON files at startup
        foreach (var file in Directory.EnumerateFiles(RequestsRoot, "*.json", SearchOption.AllDirectories))
        {
            await OnNewRequest(file);
        }

        Console.WriteLine("Press Ctrl+C to exit.");
        // Prevent exit
        new ManualResetEvent(false).WaitOne();
    }

    static async Task OnFileChanged(string path)
    {
        // Optional: handle file writes completing
    }

    static async Task OnNewRequest(string path)
    {
        try
        {
            // Wait briefly to ensure writer finished
            await Task.Delay(200);

            var deviceFolder = Directory.GetParent(path)!.Name; // e.g., truck-001
            var fileContent = await File.ReadAllTextAsync(path);
            var doc = JsonDocument.Parse(fileContent);

            var requestId = doc.RootElement.TryGetProperty("request_id", out var rid) ? rid.GetString() : null;
            var deviceId = doc.RootElement.TryGetProperty("device_id", out var did) ? did.GetString() : deviceFolder;
            var requestType = doc.RootElement.TryGetProperty("request_type", out var rt) ? rt.GetString() : "unknown";

            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = Path.GetFileNameWithoutExtension(path);
            }

            Console.WriteLine($"Processing request: {requestId} from {deviceId} ({requestType})");

            // Minimal demo response
            var response = new
            {
                request_id = requestId,
                status = "success",
                data = new { echo = requestType, received_at = DateTime.UtcNow.ToString("o") },
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var deviceResponseDir = Path.Combine(ResponsesRoot, deviceId!);
            Directory.CreateDirectory(deviceResponseDir);

            var responsePath = Path.Combine(deviceResponseDir, $"{requestId}.json");
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(responsePath, json);

            // Archive or delete request file after processing
            File.Delete(path);
            Console.WriteLine($"Wrote response: {responsePath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing request {path}: {ex}");
        }
    }
}
