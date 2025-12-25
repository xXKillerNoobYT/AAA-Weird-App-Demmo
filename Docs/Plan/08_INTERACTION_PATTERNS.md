# System Interaction Patterns

## 1. Request-Response Pattern (Synchronous)

### Pattern Structure
```
Client → API Gateway → Service → Database → Service → Client
```

### Use Cases
- REST API calls from mobile apps
- Direct HTTP requests for status checks
- Form submissions from web UI

### Example: Get Parts List
```
GET /api/parts?category=electronics&supplier=acme
    ↓
API Gateway: Validate token
    ↓
Request Processor: Query database
    ↓
Database: Fetch parts with filters
    ↓
Cache: Check if fresh (avoid db hit)
    ↓
Response: Return JSON array + metadata
    ↓
Client: Display in UI
```

### Characteristics
- Synchronous (blocking)
- Low latency (< 200ms)
- Strong consistency
- Connection state maintained

### Timeout Strategy
- Client: 30 seconds
- API Gateway: 25 seconds
- Service layer: 20 seconds
- Database: 15 seconds

---

## 2. Publish-Subscribe Pattern (Asynchronous)

### Pattern Structure
```
Event Source → Event Bus → Subscribers
                    ↓
            RequestUploaded
            ResponseReady
            StatusChanged
            ErrorOccurred
```

### Use Cases
- File upload notifications
- Status change broadcasts
- Multi-client updates
- Async processing triggers

### Implementation: In-Memory Bus
```
File Watcher Service (Publisher)
    ├─→ Event: "RequestUploaded"
    └─→ Payload: { request_id, device_id, timestamp }
                    ↓
        Event Bus (RequestUploaded event)
                    ↓
    Subscribers listening for RequestUploaded:
    ├─→ Request Processor Service
    ├─→ Notification Service
    └─→ Audit Logging Service
```

### Characteristics
- Asynchronous (non-blocking)
- Higher latency (50-500ms)
- Eventual consistency
- Loose coupling

---

## 3. Command Pattern (Request with Side Effects)

### Pattern Structure
```
Command Creator → Command Queue → Command Handler → Side Effects
```

### Example: Update Inventory Command
```
Mobile App (Warehouse)
    │
    └─→ Command: "UpdateInventory"
        {
          device_id: "warehouse-03",
          sku: "PUMP-001",
          quantity_change: +50,
          location: "Bin-A-12"
        }
        ↓
API Gateway: Accept command
        ↓
Request Processor: Validate command
        ↓
Command Queue (Redis): Enqueue
        ↓
Command Handler (Background): Execute
        ├─→ Database: Update inventory
        ├─→ Cache: Invalidate item
        └─→ Event: "InventoryUpdated"
        ↓
WebSocket: Broadcast to connected clients
```

### Characteristics
- Fire-and-forget semantics
- Command stores action intent
- Separates command from execution
- Enables audit trail

---

## 4. Agent-Based Pattern (AI Orchestration)

### Pattern Structure
```
Request → Orchestrator → Agent1 → Agent2 → ... → Response
                            ↓
                        [Context Shared]
```

### Example: Request Routing
```
Incoming Request (Update Inventory)
    ↓
AI Orchestrator
    ├─→ Create execution context
    ├─→ Initialize RequestRouter agent
    │
RequestRouter Agent
    ├─→ Analyze request type
    ├─→ Check user permissions
    ├─→ Determine approval chain
    ├─→ Decision: "Needs regional manager approval"
    │
    ├─→ Pass context to RegionalApprover component
    │
RegionalApprover (Component or Chain to ApprovalRouter Agent)
    ├─→ Find regional managers
    ├─→ Send notification
    ├─→ Wait for response (timeout: 4 hours)
    │
    ├─→ If approved:
    │   └─→ OrderGenerator Agent
    │       ├─→ Consolidate parts list
    │       └─→ Optimize supplier selection
    │
    └─→ If rejected:
        └─→ Create rejection response
        └─→ Send notification
    ↓
Response Handler
    ├─→ Format response JSON
    └─→ Return to requester
```

### Agent Communication
```
RequestRouter ←→ Orchestrator ←→ Shared Context
   ↓
PartsSpecialist (via Context)
   ↓
SupplierMatcher (receives context from PartsSpecialist)
```

### Characteristics
- Multi-step decision process
- Context preservation across steps
- LLM-based reasoning
- Audit trail of decisions

---

## 5. Cache-Aside Pattern

### Pattern Structure
```
Request
    ↓
Check Cache
    ├─→ HIT: Return cached value
    └─→ MISS:
        ├─→ Query Database
        ├─→ Populate Cache
        └─→ Return value
```

### Example: Parts Catalog
```
Mobile App
    │
    └─→ GET /api/parts/PUMP-001
            ↓
        Cache Layer (Redis)
            ├─→ Key: "parts:PUMP-001"
            ├─→ TTL: 24 hours
            │
            ├─→ Cache hit?
            │   YES: Return cached JSON
            │   NO: Continue
            │
        Database Query
            ├─→ SELECT * FROM parts WHERE sku='PUMP-001'
            │
        Cache Update
            ├─→ SET "parts:PUMP-001" = {part_data}
            │
        Return to Client
```

### Cache Invalidation Strategy
```
Event: Part Updated
    ↓
Cache Key: "parts:{sku}"
Cache Key: "parts:catalog" (list)
    ↓
Delete both cache entries
    ↓
Next request: Fetch fresh from DB
```

### Characteristics
- Reduces database load
- Faster response times (10ms vs 100ms)
- Stale data possible
- Manual invalidation required

---

## 6. Outbox Pattern (Guaranteed Delivery)

### Pattern Structure
```
Event Creator
    ├─→ Write main data to DB (transaction)
    ├─→ Write event to outbox table
    │
Transaction commits
    ↓
Outbox Processor (separate service)
    ├─→ Poll outbox table
    ├─→ Process events
    ├─→ Publish to event bus
    ├─→ Mark as published
    └─→ Delete from outbox
```

### Example: Response Creation
```
Response Generated
    ├─→ START TRANSACTION
    ├─→ INSERT into responses table
    ├─→ INSERT into outbox table
    │   {event: "ResponseCreated", data: {...}}
    ├─→ COMMIT TRANSACTION
    │
Outbox Processor (every 5 seconds)
    ├─→ Query outbox WHERE processed=false
    ├─→ FOR EACH event:
    │   ├─→ Publish to message bus
    │   ├─→ Update outbox SET processed=true
    │   └─→ Subscribers receive event
    │
Subscribers
    ├─→ WebSocket Hub (broadcast to clients)
    ├─→ Notification Service (send alerts)
    └─→ Audit Logger (log event)
```

### Characteristics
- Guarantees delivery (at-least-once)
- Handles failures gracefully
- Slight delay (process latency)
- Database-backed queue

---

## 7. Retry with Exponential Backoff

### Pattern Structure
```
Operation fails
    ↓
Retry 1 (delay: 1s)
    └─→ Still fails
        ↓
    Retry 2 (delay: 2s)
        └─→ Still fails
            ↓
        Retry 3 (delay: 4s)
            └─→ Still fails
                ↓
            Retry 4 (delay: 8s)
                └─→ If fails: Give up
```

### Example: Cloud Upload
```
Cloud Storage Service (Upload to SharePoint)
    │
    ├─→ Attempt 1: POST file.json
    ├─→ Response: 503 Service Unavailable
    │
    ├─→ Wait 1 second
    │
    ├─→ Attempt 2: POST file.json
    ├─→ Response: 500 Internal Server Error
    │
    ├─→ Wait 2 seconds
    │
    ├─→ Attempt 3: POST file.json
    ├─→ Response: 200 OK
    │
    └─→ Success!
```

### Idempotency
```
Each attempt must be idempotent:
- Upload: Use same file name
- Database insert: Use unique constraint
- API call: Include idempotency key
```

### Circuit Breaker
```
After 5 failures in 1 minute:
    ├─→ Status: OPEN
    ├─→ Fail fast (don't retry)
    └─→ Check health every 30 seconds
        └─→ If healthy: Try again
```

---

## 8. Hub-Spoke Pattern (WebSocket Broadcasting)

### Pattern Structure
```
Multiple Clients ↔ Hub ↔ Message Broadcast
```

### Example: Device Connection
```
Device 1 (Truck-001)
    │
Device 2 (Truck-002)  ──→ WebSocket Hub ──→ Broadcast
    │                         │                 ├─→ Device 1
Device 3 (Truck-003)          │                 ├─→ Device 2
    │                         │                 └─→ Device 3
    │               [Connection Manager]
    │               [Message Router]
    │               [State Cache]
    │
    └─→ All clients receive event: "ResponseReady"
        { request_id: "req-123", timestamp: "..." }
```

### State Management
```
Connected Clients State:
{
  "truck-001": { socket: WebSocket, device_type: "truck", connected_at: "..." },
  "truck-002": { socket: WebSocket, device_type: "truck", connected_at: "..." },
  "warehouse-01": { socket: WebSocket, device_type: "warehouse", connected_at: "..." }
}
```

### Characteristics
- Real-time delivery
- Broadcast efficiency
- Connection-aware
- Session affinity for scaling

---

## 9. Saga Pattern (Distributed Transactions)

### Example: Order Creation (Orchestration Style)
```
Create Order Request
    ↓
Order Service
    ├─→ 1. Create order record
    │   Status: Pending
    ├─→ 2. Call PartsService
    │   └─→ Reserve parts
    │   └─→ If fails: Compensate (delete order)
    ├─→ 3. Call SupplierService
    │   └─→ Create purchase order
    │   └─→ If fails: Compensate (unreserve parts, delete order)
    ├─→ 4. Call InventoryService
    │   └─→ Update inventory
    │   └─→ If fails: Compensate
    ├─→ 5. Update order status
    │   Status: Completed
    │
Final State: All services consistent
```

### Compensation Steps
```
If step 2 fails:
    └─→ Compensate step 1: Delete order
    └─→ Return error to client

If step 3 fails:
    ├─→ Compensate step 2: Unreserve parts
    ├─→ Compensate step 1: Delete order
    └─→ Return error to client
```

---

## Summary: Pattern Selection Guide

| Scenario | Pattern | Trade-offs |
|----------|---------|-----------|
| Mobile app status query | Request-Response | Latency, strong consistency |
| File upload notification | Publish-Subscribe | Eventual consistency |
| Inventory update | Command + Outbox | Complexity, guaranteed delivery |
| AI request routing | Agent-Based | Execution time, flexibility |
| Parts list | Cache-Aside | Stale data possible |
| Response delivery | Outbox + Pub-Sub | Latency (5-30s), reliability |
| Cloud upload failure | Retry + Circuit Breaker | Eventual success, resource costs |
| Real-time updates | WebSocket Hub | Memory usage, session affinity |
| Order creation | Saga + Compensation | Complexity, distributed state |

