# Data Flow Architecture

## Primary Data Flows

### Flow 1: Device Request Submission

```
Device Client
    ↓
    │ 1. POST /api/request (JSON)
    │
API Gateway
    ↓
    │ 2. Validate token (OAuth2)
    │
Authentication Service
    ↓
    │ 3. Parse JSON request
    │
Request Processor Service
    ↓
    │ 4. Validate against schema
    │ 5. Generate request ID
    │ 6. Serialize to JSON file
    │
Cloud Storage Service
    ↓
    │ 7. Upload to /Cloud/Requests/{device-id}/{request-id}.json
    │
File Watcher Service
    ↓
    │ 8. Event: "RequestUploaded"
    │ 9. Trigger processing pipeline
    │
Business Logic Processor
    ↓
    │ 10. Route based on request_type
    │ 11. Execute business logic
    │
Database Service
    ↓
    │ 12. Store request metadata
    │ 13. Update status
    │
Cache Layer
    ↓
    │ 14. Cache request status
    │
Device via WebSocket
    ↓
    ACK: Request received
```

**Data Structures:**
- Input: `DeviceRequest { request_type, device_id, request_id, payload }`
- Output: `RequestResponse { success, request_id, message, timestamp }`

---

### Flow 2: AI-Driven Request Routing

```
Request Processor Service
    ↓
    │ 1. Read request metadata
    │ 2. Extract request_type
    │
Decision Router
    ↓
    │ 3. Switch on request_type
    │
    ├─→ "get_parts" ─────────────→ PartsSpecialist Agent
    │
    ├─→ "update_inventory" ────→ AI Orchestrator
    │                                    ↓
    │                            RequestRouter Agent
    │                                    ↓
    │                            (Analyze & Route)
    │                                    ↓
    │                            Approval Chain
    │
    └─→ "create_order" ────────→ OrderGenerator Agent
                                        ↓
                                SupplierMatcher Agent
                                        ↓
                                Cost Optimization
```

**AI Agent Flow:**
```
Request Payload
    ↓
AI Orchestrator (Python AutoGen)
    ↓
┌───────────────────────────────────┐
│ Agent Routing Decision            │
├───────────────────────────────────┤
│ 1. RequestRouter analyzes request │
│ 2. Applies business rules         │
│ 3. Checks permissions             │
│ 4. Determines next agent(s)       │
└───────────────────────────────────┘
    ↓
Agent Execution
    ├─→ Database: Query relevant data
    ├─→ External APIs: Fetch supplemental info
    └─→ Decision: Generate recommendation
    ↓
Response Generation
    ├─→ Format result JSON
    ├─→ Add metadata (timestamp, agent, confidence)
    └─→ Return to orchestrator
    ↓
Response Handler
```

---

### Flow 3: Response Delivery

```
Response Generator Service
    ↓
    │ 1. Receive response from processor
    │ 2. Format as JSON
    │ 3. Add response metadata
    │
Atomic Write Handler
    ↓
    │ 4. Write to temp file
    │ 5. Verify file integrity
    │
Cloud Storage Service
    ↓
    │ 6. Upload to /Cloud/Responses/{device-id}/{request-id}.json
    │ 7. Verify upload success
    │
File Watcher Service
    ↓
    │ 8. Event: "ResponseUploaded"
    │
WebSocket Hub
    ↓
    │ 9. Broadcast to device: { event: "ResponseReady", request_id }
    │
Device Client
    ↓
    │ 10. Download response from cloud
    │ 11. Process locally
    │
Cache Layer (Update)
    ↓
    │ 12. Cache response for 24 hours
    │
Database Service (Archive)
    ↓
    Response archived for audit trail
```

**Response Payload Example:**
```json
{
  "request_id": "req-12345",
  "status": "completed",
  "data": { ... },
  "timestamp": "2025-12-24T23:00:00Z",
  "execution_time_ms": 234
}
```

---

### Flow 4: Real-Time Connection Management

```
Device Client
    ↓
    │ 1. WebSocket connect
    │ ws://server/hubs/devicehub?token={jwt}
    │
Authentication Service
    ↓
    │ 2. Validate token
    │
WebSocket Hub (SignalR)
    ↓
    │ 3. Register connection
    │ 4. Store in ConnectionManager
    │
Cache Layer
    ↓
    │ 5. Cache connection state
    │ 6. TTL = connection timeout
    │
    ┌─ Maintain Connection ─────────────┐
    │                                    │
    │ ← Heartbeat (every 30 seconds)    │
    │ → Pong response                   │
    │                                    │
    └────────────────────────────────────┘
    ↓
Device Disconnection
    ↓
    │ 7. Clean up connection
    │
Connection Manager
    ↓
    │ 8. Remove from active connections
    │
Cache Layer
    ↓
    │ 9. Evict connection state
```

---

### Flow 5: Offline Sync (Mobile App)

```
Mobile Device (Truck App)
    ↓
    │ 1. Generate request (offline)
    │ 2. Queue in local SQLite
    │
Background Service
    ↓
    │ 3. Monitor network status
    │ 4. On connectivity: start sync
    │
Sync Manager
    ├─→ Check queue for pending requests
    ├─→ Validate network (online check)
    └─→ Rate limit: 5 requests/minute
    ↓
Request Upload Loop
    ├─→ Dequeue first request
    ├─→ Add authentication headers
    ├─→ POST to /api/request/{device-id}
    ├─→ On 200 OK: mark as synced
    ├─→ On error: retry exponentially
    │   (1s, 2s, 4s, 8s, then give up)
    └─→ Repeat until queue empty
    ↓
Response Polling
    ├─→ Query /Cloud/Responses/{device-id}
    ├─→ Check for new files
    ├─→ Download and process
    ├─→ Mark in local cache
    └─→ Notify user
    ↓
Local Database Update
    ↓
UI Refresh
```

---

### Flow 6: Database Consistency

```
Request Processor
    ↓
    │ 1. Start transaction
    │
Database Service (PostgreSQL)
    ├─→ 2. Write request metadata
    ├─→ 3. Update device status
    ├─→ 4. Record timestamp
    └─→ 5. Commit or rollback
    ↓
Cache Invalidation
    ↓
    │ 6. Remove stale entries
    │ 7. Update latest status
    │
Cache Layer (Redis)
    ↓
Consistency Verified
```

---

## Data Transformation Pipeline

### Request JSON Schema
```json
{
  "request_type": "get_parts|update_inventory|create_order|...",
  "request_id": "req-{uuid}",
  "device_id": "truck-001",
  "timestamp": "ISO-8601",
  "payload": {
    // Payload structure varies by request_type
  }
}
```

### Response JSON Schema
```json
{
  "request_id": "req-{uuid}",
  "status": "completed|pending|error",
  "data": { /* response data */ },
  "errors": [ /* error details */ ],
  "metadata": {
    "timestamp": "ISO-8601",
    "execution_time_ms": 234,
    "agent_decisions": [ /* audit trail */ ]
  }
}
```

---

## Cross-Component Data Flow

### Request → Processor → Agent → Response
1. Request arrives with payload
2. Processor validates and routes
3. AI agents process in sequence
4. Each agent adds context
5. Final agent generates response
6. Response serialized and delivered

### Caching Strategy
- **Request metadata**: 1 hour
- **Response data**: 24 hours
- **Connection state**: Connection duration
- **User permissions**: 30 minutes
- **Parts catalog**: 24 hours

### Data Consistency Model
- **Strong consistency**: Database writes (transactions)
- **Eventual consistency**: Cache layer (async updates)
- **Weak consistency**: WebSocket broadcasts (best-effort)

---

## Error Flow

```
Error Detected
    ↓
┌──────────────────┐
│ Error Level?     │
├──────────────────┤
│                  │
├─→ Critical ─→ Alert team + Fallback behavior
│
├─→ High    ─→ Retry + Log + Monitor
│
└─→ Low     ─→ Log + Continue
    ↓
Error Handler Service
    ├─→ Log to Datadog/ELK
    ├─→ Notify via Slack
    ├─→ Update device status
    └─→ Persist error record
    ↓
Database Service
```

---

## Throughput & Latency Per Flow

| Flow | Throughput | P95 Latency | Notes |
|------|-----------|----------|-------|
| Device Request | 1000 req/sec | 200ms | Per device scaling |
| AI Routing | 100 req/sec | 500ms | LLM inference time |
| Response Delivery | 500 resp/sec | 150ms | Cloud upload |
| WebSocket Message | 10000 msg/sec | 50ms | In-memory broadcast |
| Offline Sync | 100 req/sec | 1000ms | Network dependent |

