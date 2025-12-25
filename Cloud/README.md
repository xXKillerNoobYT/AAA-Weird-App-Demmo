# Cloud Folder - File-Based Request/Response Protocol

## Overview

The Cloud folder implements a file-based request/response communication protocol between mobile/desktop devices and the server. Instead of HTTP APIs, files are synchronized to cloud storage (SharePoint or Google Drive), enabling devices to work offline and sync when connectivity returns.

## Architecture

```
Device (Truck/Mobile)
    ↓
    Write request JSON to /Cloud/Requests/
    ↓
Sync to cloud storage (SharePoint or Google Drive)
    ↓
CloudWatcher monitors /Cloud/Requests/
    ↓
Process request, validate schema
    ↓
Write response JSON to /Cloud/Responses/[device-id]/
    ↓
Sync response back to cloud
    ↓
Device polls /Cloud/Responses/[device-id]/ for response
    ↓
Device reads response, processes result
```

## Folder Structure

```
Cloud/
├── Requests/              → Device request upload location
├── Responses/             → Server response output location
│   ├── truck-001/        → Device-specific response folder
│   ├── truck-002/        → Another device
│   └── device-mobile-*/  → Mobile devices
├── SpecSheets/           → Parts catalogs and specs
├── Archive/              → Historical requests/responses
└── README.md             → This file
```

## Quick Start: Sending a Request

### 1. Mobile Device (or Desktop Client)

Create a request file in JSON format:

```json
{
  "request_type": "ping",
  "payload": {}
}
```

### 2. Upload to Cloud

Copy the file to `Cloud/Requests/` via cloud storage sync:

```bash
# Via cloud storage provider (automatic sync)
# File: Cloud/Requests/req-truck-001-ping-2025-12-24T10-30-00.json
```

### 3. Server Processes Request

CloudWatcher detects the new file, validates schema, routes to handler:

```
Validation: ✓ request_type="ping" found
            ✓ payload={} (empty, expected)
Routing:    → PingHandler
Processing: → Server health check
Response:   ✓ Success
```

### 4. Server Writes Response

Response is written to device-specific folder:

```
File created: Cloud/Responses/truck-001/resp-ping-2025-12-24T10-30-00.json
```

### 5. Device Retrieves Response

Mobile app polls `Cloud/Responses/truck-001/` for matching response:

```json
{
  "status": "success",
  "code": "ping_ok",
  "message": "Server is healthy",
  "data": {
    "server_version": "1.0.0",
    "database_connected": true
  },
  "timestamp": "2025-12-24T10:30:05Z",
  "build_signature": "20251224.103000"
}
```

## Supported Request Types

| Type | Purpose | Payload | Response |
|------|---------|---------|----------|
| `ping` | Health check | `{}` | Server status, version |
| `echo_test` | Test request | `{ "message": "..." }` | Echoed message |
| `get_parts` | Retrieve parts | `{ "category": "..." }` | Parts list with specs |
| `get_inventory` | Truck inventory | `{ "truck_id": "..." }` | Current inventory state |
| `update_inventory` | Update inventory | `{ "truck_id": "...", "inventory": [...] }` | Confirmation |
| `create_parts_list` | New parts list | `{ "name": "...", "parts": [...] }` | List ID, created_at |

See individual README files for detailed schemas:
- [Requests/README.md](Requests/README.md) - Request format & examples
- [Responses/README.md](Responses/README.md) - Response format & examples
- [SpecSheets/README.md](SpecSheets/README.md) - Part specifications

## Offline-First Design

### Device Behavior

1. **Online:** Sync requests/responses with cloud in real-time
2. **Offline:** Queue requests locally, cache responses
3. **Reconnect:** Sync pending requests, download new responses

### CloudWatcher Behavior

1. **Monitor:** FileSystemWatcher triggers on new request files
2. **Process:** Validates schema, routes by type, calls handler
3. **Respond:** Writes atomic response (temp + move pattern)
4. **Notify:** Response syncs back to cloud immediately

## Schema Validation

All requests are validated against a strict schema:

**Required:**
- `request_type` (string): One of [ping, echo_test, get_parts, ...]
- `payload` (object): Request-specific data

**Optional:**
- `timestamp` (ISO 8601): Client timestamp
- `client_id` (string): Device identifier
- `correlation_id` (string): For tracing

**Invalid requests receive error responses:**

```json
{
  "status": "error",
  "code": "invalid_schema",
  "message": "Missing required field: payload",
  "timestamp": "2025-12-24T10:30:00Z",
  "build_signature": "20251224.103000"
}
```

## Cloud Storage Configuration

### Azure AD OAuth2

Both SharePoint and Google Drive require OAuth2 authentication:

1. **Azure AD Registration** - See [auth/OAUTH2_SETUP.md](../server/CloudWatcher/auth/OAUTH2_SETUP.md)
2. **Credentials** - Stored in [auth/appsettings.json](../server/CloudWatcher/auth/appsettings.json)
3. **Token Management** - OAuth2Helper.cs handles refresh/validation

Configuration locations:
```
server/CloudWatcher/auth/
├── appsettings.json     ← Provider URLs, client IDs
├── OAUTH2_SETUP.md      ← Registration guide
└── OAuth2Helper.cs      ← Token management code
```

### Supported Providers

**Primary: SharePoint**
- Site: `https://yourorg.sharepoint.com/sites/weirdtoo`
- Folder: `/Cloud/Requests`, `/Cloud/Responses`, etc.
- Sync: Built-in SharePoint sync client (OneDrive)
- Latency: <2 seconds

**Secondary: Google Drive**
- Folder: `WeirdToo/Cloud/`
- Sync: Google Drive desktop sync
- Latency: <5 seconds
- Used as fallback if SharePoint unavailable

## Performance Characteristics

| Metric | Target | Notes |
|--------|--------|-------|
| Request validation | <100ms | JSON schema check |
| Response generation | <500ms | Handler execution time varies |
| Cloud sync latency | <2s | SharePoint; <5s for Google Drive |
| Round-trip latency | <10s | Request upload + processing + response download |
| Concurrent requests | 100+ | CloudWatcher can handle multiple file triggers |
| File size limit | 5 MB | Request/response payload limit |

## Security Considerations

### OAuth2 Authentication
- All cloud access requires OAuth2 tokens
- Tokens automatically refresh before expiry
- Redirect URIs must match registered URLs in Azure AD

### Data Encryption
- HTTPS for all cloud sync (automatic via provider)
- At-rest encryption on cloud provider (SharePoint/Drive)
- Optional: Encrypt sensitive payloads before upload

### Request Signing (Future)
- Plan: Add HMAC signature to requests for integrity verification
- Current: Timestamp in response serves as validation

### Audit Logging
- All requests logged to database audit table
- All responses tracked with status (success/error)
- Errors logged with request details for troubleshooting

See [Archive/README.md](Archive/README.md) for long-term archival and compliance.

## Troubleshooting

### Request Not Processed

1. **Check CloudWatcher is running:**
   ```bash
   ps aux | grep CloudWatcher
   ```

2. **Verify request format:**
   ```json
   {
     "request_type": "ping",
     "payload": {}
   }
   ```

3. **Check response folder:**
   ```bash
   ls Cloud/Responses/[truck-id]/
   ```

### Response Not Appearing

1. **Check cloud sync status** - Verify SharePoint/Drive sync is active
2. **Check response folder permissions** - Ensure write access for CloudWatcher
3. **Check CloudWatcher logs** - Look for processing errors
4. **Check request file format** - Ensure valid JSON

### Connection Issues

1. **Test cloud connectivity:**
   ```bash
   ping yourorg.sharepoint.com
   ```

2. **Test OAuth token:**
   - Run OAuth2Helper.ValidateTokenAsync()
   - Check token expiry (should be 1 hour from generation)

3. **Check network proxy/firewall** - May block cloud access

## Files & Folders

```
Cloud/
├── Requests/           → Upload device requests here
│   ├── README.md
│   ├── .gitkeep
│   └── truck-001/      → (Optional) Device-specific folder
├── Responses/          → Server writes responses here
│   ├── README.md
│   ├── .gitkeep
│   ├── truck-001/      → Device 1 response folder
│   ├── truck-002/      → Device 2 response folder
│   └── device-mobile-001/  → Mobile response folder
├── SpecSheets/         → Parts catalogs and specs
│   ├── README.md
│   ├── .gitkeep
│   ├── parts-catalogs/
│   ├── tech-specs/
│   ├── suppliers/
│   ├── assembly-guides/
│   └── safety-data-sheets/
├── Archive/            → Historical requests/responses (>30 days)
│   ├── README.md
│   ├── .gitkeep
│   ├── 2025/
│   │   ├── 2025-12/
│   │   └── 2025-11/
│   └── index-all.json  → Master archive index
└── README.md           → This file
```

## Key Files

| File | Purpose |
|------|---------|
| [Requests/README.md](Requests/README.md) | Request format, schema, examples |
| [Responses/README.md](Responses/README.md) | Response format, status codes, examples |
| [SpecSheets/README.md](SpecSheets/README.md) | Parts specs, catalogs, documents |
| [Archive/README.md](Archive/README.md) | Archival procedures, retention policy |
| [../server/CloudWatcher/Program.cs](../server/CloudWatcher/Program.cs) | Server request processor |
| [../server/CloudWatcher/auth/OAUTH2_SETUP.md](../server/CloudWatcher/auth/OAUTH2_SETUP.md) | Cloud provider OAuth setup |

## Next Steps

1. ✅ **Folder structure created** (this step)
2. ⏳ **OAuth2 setup** - See [auth/OAUTH2_SETUP.md](../server/CloudWatcher/auth/OAUTH2_SETUP.md)
3. ⏳ **Cloud provider configuration** - Register in Azure AD/Google Cloud
4. ⏳ **Mobile app integration** - Upload requests, download responses
5. ⏳ **API layer** - Future: HTTP API wrapping cloud protocol
6. ⏳ **AI integration** - Future: AutoGen agents for smart processing

## Support

For issues or questions:
- Check individual README.md files in each folder
- Review CloudWatcher logs for processing errors
- Check OAuth2 token validity in Azure AD
- Verify cloud sync is active (SharePoint/Drive)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-12-24 | Initial folder structure and documentation |

---

**Last Updated:** 2025-12-24  
**Status:** ✅ Ready for Device Integration
