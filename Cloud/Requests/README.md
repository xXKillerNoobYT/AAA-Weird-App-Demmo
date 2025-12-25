# Cloud/Requests Folder

## Purpose

This folder contains incoming request files from mobile devices and desktop clients to the server. The server's CloudWatcher monitors this folder for new `.json` files and processes them automatically.

## File Format

Each request file must be a valid JSON document following this schema:

```json
{
  "request_type": "string",
  "payload": {
    "[request-specific fields]": "varies by request_type"
  }
}
```

### Required Fields

- **request_type** (string): Type of request (see Supported Request Types below)
- **payload** (object): Request-specific data (structure varies)

### Optional Fields

- **timestamp** (ISO 8601 string): Client-side request timestamp
- **client_id** (string): Unique device identifier
- **device_type** (string): "mobile" or "desktop"
- **correlation_id** (string): For tracking related requests/responses

## Supported Request Types

| Type | Payload Structure | Description |
|------|-------------------|-------------|
| `echo_test` | `{ "message": "string" }` | Test request - echoes message back |
| `ping` | `{}` | Health check - returns success status |
| `get_parts` | `{ "category": "string" }` | Retrieve parts in category |
| `update_inventory` | `{ "truck_id": "string", "inventory": [...] }` | Update truck inventory |
| `get_inventory` | `{ "truck_id": "string" }` | Retrieve truck inventory |
| `create_parts_list` | `{ "name": "string", "parts": [...] }` | Create new parts list |

## Naming Convention

Request files should follow this naming pattern for clarity:

```
req-[truck_id]-[request_type]-[timestamp].json
```

**Examples:**
- `req-truck-001-ping-2025-12-24T10-30-00.json`
- `req-truck-002-get_parts-2025-12-24T14-15-30.json`
- `req-truck-001-update_inventory-2025-12-24T16-45-00.json`

## Processing Workflow

1. **Client uploads** request JSON to `/Cloud/Requests/`
2. **CloudWatcher detects** new file via FileSystemWatcher
3. **Validation phase** - checks for required fields (request_type, payload)
4. **Processing phase** - routes to appropriate handler by request_type
5. **Response generation** - creates response JSON with status, data, build_signature
6. **Response upload** - writes to `/Cloud/Responses/[truck_id]/`

## Response Location

After processing, responses appear in:
```
/Cloud/Responses/[truck_id]/[response_filename].json
```

The response filename matches the request filename but is placed in the device/truck's response folder.

## Error Handling

**Invalid requests** receive error responses:

```json
{
  "status": "error",
  "code": "invalid_schema",
  "message": "Request missing required fields: payload",
  "timestamp": "2025-12-24T22:30:00Z",
  "build_signature": "20251224.223000"
}
```

**Common error codes:**
- `invalid_schema` - Missing required fields
- `invalid_request_type` - Unknown request_type value
- `processing_error` - Internal server error during processing

## Testing

### Create a test request file:

```bash
# Echo test
cat > req-test-echo.json << 'EOF'
{
  "request_type": "echo_test",
  "payload": {
    "message": "Hello Server!"
  }
}
EOF

# Health check
cat > req-test-ping.json << 'EOF'
{
  "request_type": "ping",
  "payload": {}
}
EOF
```

### Monitor responses:

Watch `/Cloud/Responses/` for corresponding response files.

## Permissions

- **Read/Write:** Required for both desktop and server processes
- **Sync:** Cloud storage must sync files within 2 seconds
- **Conflict handling:** If same filename uploaded twice, latest wins (versioning via Archive folder)

## Cloud Storage Integration

This folder syncs to:
- **Primary:** SharePoint site: `https://yourorg.sharepoint.com/sites/weirdtoo/Cloud/Requests`
- **Secondary:** Google Drive folder: `WeirdToo/Cloud/Requests`

See `/Cloud/README.md` for cloud storage setup instructions.

## See Also

- `/Cloud/Responses/` - Where server responses are written
- `/Cloud/SpecSheets/` - Parts specification documents
- `/Cloud/Archive/` - Historical request/response backups
- `server/CloudWatcher/auth/OAUTH2_SETUP.md` - Cloud storage OAuth2 configuration
