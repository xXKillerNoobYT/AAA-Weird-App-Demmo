# Modularity Architecture

## 1. Module Definition & Boundaries

### Monolithic Module Structure
```
CloudWatcher.Server
├── CloudWatcher.Core/
│   ├── Interfaces/ (ICloudStorageProvider, IRequestHandler, etc.)
│   ├── Models/ (DeviceRequest, DeviceResponse, etc.)
│   └── Exceptions/ (Custom exceptions)
│
├── CloudWatcher.CloudStorage/
│   ├── Providers/ (SharePoint, Google Drive, Local implementations)
│   ├── Abstractions/ (ICloudStorageProvider interface)
│   └── Models/ (CloudFile, CloudFolder, etc.)
│
├── CloudWatcher.RequestHandling/
│   ├── RequestProcessor.cs (Core logic)
│   ├── RequestValidator.cs (Schema validation)
│   ├── RequestRouter.cs (Type-based routing)
│   └── Handlers/ (Type-specific handlers)
│
├── CloudWatcher.ResponseGeneration/
│   ├── ResponseBuilder.cs
│   ├── ResponseWriter.cs
│   └── ResponseCache.cs
│
├── CloudWatcher.API/
│   ├── Controllers/
│   │   ├── RequestController.cs
│   │   └── ResponseController.cs
│   ├── Middleware/
│   │   ├── AuthenticationMiddleware.cs
│   │   └── ExceptionHandlingMiddleware.cs
│   └── Models/ (DTOs)
│
├── CloudWatcher.WebSocket/
│   ├── DeviceHub.cs (SignalR hub)
│   ├── ConnectionManager.cs
│   └── MessageRouter.cs
│
├── CloudWatcher.FileWatcher/
│   ├── RequestFileWatcher.cs
│   └── ResponseFileWatcher.cs
│
├── CloudWatcher.Database/
│   ├── ApplicationDbContext.cs (EF Core)
│   ├── Entities/ (Data models)
│   └── Repositories/ (Data access)
│
├── CloudWatcher.Caching/
│   ├── CacheService.cs
│   ├── CacheInvalidationStrategy.cs
│   └── Decorators/ (Cache-aside implementation)
│
└── CloudWatcher.Notifications/
    ├── NotificationService.cs
    ├── Channels/ (Email, Slack, SMS)
    └── Templates/
```

---

## 2. Module Responsibilities

### CloudWatcher.Core
**Responsibility:** Define contracts and shared models

```csharp
// ICloudStorageProvider.cs - Contract
public interface ICloudStorageProvider
{
    Task<CloudOperationResult> UploadFileAsync(string folderPath, string fileName, byte[] content);
    Task<CloudOperationResult> DownloadFileAsync(string folderPath, string fileName);
    Task<CloudOperationResult> DeleteFileAsync(string folderPath, string fileName);
}

// DeviceRequest.cs - Shared model
public class DeviceRequest
{
    public string RequestType { get; set; }
    public string DeviceId { get; set; }
    public string RequestId { get; set; }
    public JObject Payload { get; set; }
}
```

**Dependencies:** None (core)
**Consumers:** All other modules

---

### CloudWatcher.CloudStorage
**Responsibility:** Cloud provider abstraction and implementations

```
Interface: ICloudStorageProvider
    ├─→ SharePointProvider (implements ICloudStorageProvider)
    ├─→ GoogleDriveProvider (implements ICloudStorageProvider)
    └─→ LocalFileStorageProvider (implements ICloudStorageProvider)
```

**Dependency Injection:**
```csharp
// Startup.cs
services.AddScoped<ICloudStorageProvider, SharePointProvider>();

// At runtime, can switch:
// services.AddScoped<ICloudStorageProvider, GoogleDriveProvider>();
```

**Dependencies:** CloudWatcher.Core
**Consumers:** RequestHandling, ResponseGeneration, FileWatcher

---

### CloudWatcher.RequestHandling
**Responsibility:** Receive, validate, and route requests

```csharp
public class RequestProcessor
{
    private readonly ICloudStorageProvider _storage;
    private readonly IValidator<DeviceRequest> _validator;
    private readonly IRequestRouter _router;

    public async Task ProcessAsync(DeviceRequest request)
    {
        // 1. Validate
        _validator.Validate(request);
        
        // 2. Route
        var handler = _router.Route(request.RequestType);
        
        // 3. Execute
        var response = await handler.ExecuteAsync(request);
        
        // 4. Persist
        await _storage.UploadFileAsync(...);
    }
}
```

**Module Composition:**
- RequestValidator (JSON schema validation)
- RequestRouter (Route by type)
- Specific handlers (GetPartsHandler, UpdateInventoryHandler, etc.)

**Dependencies:** CloudWatcher.Core, CloudWatcher.CloudStorage
**Consumers:** API, WebSocket

---

### CloudWatcher.API
**Responsibility:** HTTP endpoint contracts and request routing

```csharp
[ApiController]
[Route("api/[controller]")]
public class RequestController : ControllerBase
{
    private readonly IRequestProcessor _processor;
    
    [HttpPost("{deviceId}/{requestId}")]
    public async Task<IActionResult> UploadAsync(string deviceId, string requestId, [FromBody] DeviceRequest request)
    {
        var result = await _processor.ProcessAsync(request);
        return Ok(result);
    }
}
```

**Separation of Concerns:**
- Controllers → HTTP routing
- Services → Business logic (injected)
- Middleware → Cross-cutting concerns

**Dependencies:** All service modules
**Consumers:** External clients (mobile apps, web UI)

---

### CloudWatcher.WebSocket
**Responsibility:** Real-time device connections and messaging

```csharp
public class DeviceHub : Hub
{
    private readonly IConnectionManager _connManager;
    private readonly IMessageRouter _msgRouter;

    public override async Task OnConnectedAsync()
    {
        await _connManager.RegisterAsync(Context.ConnectionId, GetDeviceId());
    }

    public async Task NotifyResponse(string requestId)
    {
        await _msgRouter.RouteAsync(requestId);
    }
}
```

**Module Composition:**
- ConnectionManager (Track active connections)
- MessageRouter (Route messages to correct devices)
- StateCache (Redis-backed session state)

**Dependencies:** CloudWatcher.Core, CloudWatcher.Caching
**Consumers:** API (triggers broadcasts), Clients

---

## 3. Dependency Direction (Acyclic)

```
CloudWatcher.Core
    ↑
    │ depends on
    │
CloudWatcher.CloudStorage
CloudWatcher.Database
    ↑
    │ depends on
    │
CloudWatcher.RequestHandling
CloudWatcher.ResponseGeneration
    ↑
    │ depends on
    │
CloudWatcher.API
CloudWatcher.WebSocket
    ↑
    │ depends on
    │
External Clients (HTTP, WebSocket)
```

**Rule:** Never have circular dependencies
**Verification:** Dependency analysis during CI/CD

---

## 4. Module Independence

### Feature Modules (Can be deployed independently)
```
CloudWatcher.CloudStorage
    ├─→ Can be replaced entirely
    ├─→ Swaps SharePoint for Google Drive
    └─→ Only interface changes required (ICloudStorageProvider)

CloudWatcher.Notifications
    ├─→ Can be disabled (no-op implementation)
    ├─→ Can be swapped (Email → Slack)
    └─→ No impact on core functionality
```

### Optional Modules
```
CloudWatcher.WebSocket (Optional)
    ├─→ Remove if only polling needed
    └─→ Fallback: Client polling /api/status/{requestId}

CloudWatcher.Caching (Optional)
    ├─→ Remove for single-instance deployments
    └─→ Fallback: Direct database queries
```

---

## 5. Internal Module Organization

### Layered Architecture (Within Module)
```
CloudWatcher.RequestHandling
├── Controllers (API contracts)
├── Services (Business logic)
├── Handlers (Type-specific logic)
├── Validators (Input validation)
├── Models (DTOs)
└── Repositories (Data access)
```

### Dependency Flow
```
Controller
    ↓ depends on
Service
    ↓ depends on
Repository + Validator
    ↓ depends on
Models
```

---

## 6. Extension Points

### Provider Pattern
```
Current: ICloudStorageProvider implementations
Future: Easy to add:
  - DropboxProvider
  - OneDriveProvider
  - S3Provider
  
No changes to RequestHandling needed
```

### Handler Pattern
```
Current: RequestTypeRouter routes to specific handler
Future: Easy to add:
  - GetPartsHandler
  - UpdateInventoryHandler
  - CreateOrderHandler
  - GenerateReportHandler
  
Just register new handler in DI container
```

### Notification Channels
```
Current: INotificationChannel interface
Implementations:
  - EmailNotificationChannel
  - SlackNotificationChannel
  - SmsNotificationChannel
  
Future: Add new channels without changing notification service
```

---

## 7. Testing Module Independence

### Unit Test Structure
```
CloudWatcher.Tests/
├── CloudStorage.Tests/
│   ├── SharePointProviderTests.cs (Mock HTTP)
│   ├── GoogleDriveProviderTests.cs (Mock API)
│   └── LocalStorageProviderTests.cs (Real files)
│
├── RequestHandling.Tests/
│   ├── RequestProcessorTests.cs
│   ├── RequestValidatorTests.cs
│   └── RequestRouterTests.cs
│
├── API.Tests/
│   ├── RequestControllerTests.cs
│   └── ResponseControllerTests.cs
│
└── WebSocket.Tests/
    ├── DeviceHubTests.cs
    └── ConnectionManagerTests.cs
```

### Mocking Strategy
```csharp
// Test RequestProcessor without CloudStorage
var mockStorage = new Mock<ICloudStorageProvider>();
var processor = new RequestProcessor(mockStorage.Object, ...);

// Test API without RequestProcessor
var mockProcessor = new Mock<IRequestProcessor>();
var controller = new RequestController(mockProcessor.Object);
```

---

## 8. Configuration & Feature Toggles

### Feature Flags
```json
{
  "Features": {
    "WebSocketEnabled": true,
    "CachingEnabled": true,
    "NotificationsEnabled": false,
    "CloudProviders": {
      "SharePoint": { "Enabled": true, "Weight": 70 },
      "GoogleDrive": { "Enabled": true, "Weight": 20 },
      "LocalStorage": { "Enabled": true, "Weight": 10 }
    }
  }
}
```

### Runtime Configuration
```csharp
// Startup.cs
var features = configuration.GetSection("Features");

if (features["WebSocketEnabled"] == "true")
    services.AddSignalR();

if (features["CachingEnabled"] == "true")
    services.AddStackExchangeRedis(...);
```

---

## 9. Package Structure

### NuGet Packages
```
CloudWatcher.Core (Published)
    ├─→ Models and interfaces
    ├─→ Version: 1.0.0
    └─→ Used by external integrations

CloudWatcher.CloudStorage (Published)
    ├─→ Provider implementations
    ├─→ Version: 1.0.0
    └─→ Can be used standalone

CloudWatcher.Api (Published)
    ├─→ Complete solution
    ├─→ Version: 1.0.0
    └─→ Depends on Core + CloudStorage
```

---

## 10. Evolution & Refactoring

### From Monolith to Microservices (Future)
```
Current State (Monolithic):
    CloudWatcher.Api
    ├─→ Request handling
    ├─→ Response generation
    ├─→ Cloud storage
    └─→ WebSocket

Future State (Microservices):
    Service 1: Request Handler (HTTP)
    Service 2: Response Generator (HTTP)
    Service 3: Cloud Storage (gRPC)
    Service 4: WebSocket Hub (WebSocket)
    
    Each service gets own database
    Services communicate via message bus (RabbitMQ)
```

### Migration Path
```
Step 1: Ensure modules are independent (DONE)
Step 2: Extract modules into separate .NET class libraries (DONE)
Step 3: Add abstraction layer (event bus) (FUTURE)
Step 4: Move to separate processes (FUTURE)
Step 5: Containerize and orchestrate (FUTURE)
```

