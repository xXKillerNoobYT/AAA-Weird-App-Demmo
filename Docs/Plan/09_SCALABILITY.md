# Scalability Architecture

## 1. Horizontal Scaling Strategy

### API Gateway Layer (Stateless)
```
Load Balancer (NGINX/Azure LB)
    ├─→ Instance 1: API Gateway (Port 5000)
    ├─→ Instance 2: API Gateway (Port 5000)
    ├─→ Instance 3: API Gateway (Port 5000)
    └─→ Health check: /health (every 5s)
```

**Scaling Characteristics:**
- Stateless (easy horizontal scaling)
- Connection independent
- Request routing per instance
- Auto-scale trigger: CPU > 70% or requests/sec > 1000

### Request Processor Services
```
Scale: 3-10 instances based on load

Load Balancer
    ├─→ Instance 1: Processor
    ├─→ Instance 2: Processor
    └─→ Instance 3: Processor
         ↓
    Shared message queue (RabbitMQ)
```

**Queue-Based Processing:**
- Messages enqueued by API Gateway
- Processors dequeue and process
- Scaling: Auto-scale when queue depth > 100

---

## 2. Database Scaling

### PostgreSQL Primary-Replica Setup
```
Write Requests                  Read Requests
    ↓                               ↓
Primary (PostgreSQL)          Replica 1 (Read-Only)
    ├─→ Write                       │
    ├─→ Sync replication            │
    │   (WAL shipping)              │
    │                               │
    └─→ Replica 2 (Read-Only) ←─────┘
         Backup replica
         (for HA)
```

**Configuration:**
- Replication lag: < 100ms
- Async replication for writes (fast acknowledgment)
- Read queries routed to replicas
- Failover: Automatic if primary down > 30s

### Connection Pooling
```
Client
    ├─→ Connection pool
    │   (Min: 5, Max: 100)
    ├─→ PgBouncer (proxy)
    │   - Multiplexes connections
    │   - Reduces DB connection count
    └─→ PostgreSQL
        (Max connections: 500)
```

### Sharding (Future if needed)
```
Data partitioned by device_id:
Device 1-999     → Shard 1 (PostgreSQL instance 1)
Device 1000-1999 → Shard 2 (PostgreSQL instance 2)
Device 2000-2999 → Shard 3 (PostgreSQL instance 3)
```

---

## 3. Cache Layer Scaling (Redis)

### Redis Cluster (High Availability)
```
Application
    ↓
Redis Cluster (3+ master nodes)
    ├─→ Master 1 (Shard 1)
    │   ├─→ Replica 1.1 (backup)
    │   └─→ Replica 1.2 (backup)
    ├─→ Master 2 (Shard 2)
    │   ├─→ Replica 2.1 (backup)
    │   └─→ Replica 2.2 (backup)
    └─→ Master 3 (Shard 3)
        ├─→ Replica 3.1 (backup)
        └─→ Replica 3.2 (backup)
```

**Characteristics:**
- Automatic failover (replication)
- Data sharding (better memory utilization)
- Latency: < 5ms
- Memory: 10GB+ in production

---

## 4. Cloud Storage Scaling

### Multi-Provider Redundancy
```
API Request
    ↓
Load Balancer (Provider selection)
    ├─→ 70% to SharePoint (primary)
    │   └─→ Multi-region replication
    │
    ├─→ 20% to Google Drive (secondary)
    │   └─→ Geo-redundancy
    │
    └─→ 10% to Local Storage (cache/fallback)
```

### Parallel Upload Strategy
```
Large file (> 100MB)
    ├─→ Split into chunks (10MB each)
    ├─→ Upload 3 chunks in parallel
    ├─→ Retry failed chunks
    └─→ Verify checksum
```

---

## 5. WebSocket Hub Scaling

### Session Affinity with Multiple Hubs
```
Device Client
    ↓
Load Balancer (Sticky sessions)
    ├─→ Hub Instance 1 (300 connections)
    ├─→ Hub Instance 2 (300 connections)
    ├─→ Hub Instance 3 (300 connections)
    └─→ Hub Instance 4 (300 connections)
```

**Pub-Sub for Cross-Hub Broadcasting:**
```
Hub 1 receives message: "ResponseReady"
    ├─→ Send to local clients (session affinity)
    ├─→ Publish to Redis pub-sub
    │
Hub 2, 3, 4
    ├─→ Receive from Redis
    ├─→ Broadcast to their local clients
```

**Configuration:**
- Connections per hub: 300-500
- Auto-scale: When connections > 400
- Session affinity: Hash(device_id) % num_hubs

---

## 6. File Watcher Service Scaling

### Distributed File Monitoring
```
Cloud Storage Folder
/Cloud/Requests/{device-id}/
    ├─→ Partitioned by device_id
    │
    ├─→ Watcher 1: Watches devices 1-10
    ├─→ Watcher 2: Watches devices 11-20
    ├─→ Watcher 3: Watches devices 21-30
    │
    ├─→ Each watcher publishes events
    │   to shared message bus
```

**Coordination:**
- Redis locks for exclusive watching
- Heartbeat: Every 10 seconds
- Failover: Auto-reassign if heartbeat missing > 30s

---

## 7. AI Orchestrator Scaling

### Python Service with Load Balancing
```
Request Router
    ├─→ Queue request in RabbitMQ
    │
Worker Pool (Python processes)
    ├─→ Worker 1: Processing request
    ├─→ Worker 2: Processing request
    ├─→ Worker 3: Idle (waiting)
    ├─→ Worker 4: Idle (waiting)
    │
    └─→ Auto-scale: Workers 3-10
        (based on queue depth)
```

**Scaling Considerations:**
- Python processes (not async)
- Each worker: ~500MB RAM
- LLM inference: GPU acceleration (optional)
- Queue-based for reliable processing

---

## 8. Load Shedding & Rate Limiting

### Token Bucket Algorithm
```
Client sends request
    ├─→ Rate limiter checks token bucket
    │   (Refill rate: 1000 tokens/sec)
    │
    ├─→ Tokens available?
    │   YES: Grant request, consume token
    │   NO: Return 429 Too Many Requests
    │
    └─→ Exponential backoff on client
        (Retry after 1s, 2s, 4s, ...)
```

### Per-Device Rate Limits
```
Device 1 (Truck): 50 req/min
Device 2 (Truck): 50 req/min
Device 3 (Warehouse): 200 req/min
Device 4 (Office): 500 req/min
```

---

## 9. Caching Strategies

### 3-Tier Cache Architecture
```
Request
    ↓
Tier 1: In-Memory Cache (App process)
    ├─→ Latency: < 1ms
    ├─→ Size: 100MB per instance
    └─→ TTL: 5 minutes
    ├─→ Miss
        ↓
    Tier 2: Redis (Distributed)
        ├─→ Latency: 5-10ms
        ├─→ Size: 10GB cluster
        └─→ TTL: 1 hour
        ├─→ Miss
            ↓
        Tier 3: Database
            ├─→ Latency: 50-100ms
            ├─→ Full source of truth
            └─→ Query result cached in Tier 2
```

### Cache Invalidation Pattern
```
Data update event
    ├─→ Invalidate Tier 2 (Redis)
    ├─→ Signal all instances
    └─→ Tier 1 expires naturally (5 min TTL)
```

---

## 10. Monitoring & Auto-Scaling

### Metrics Collection
```
Prometheus (Metrics)
    ├─→ CPU: > 70% → Scale up
    ├─→ Memory: > 80% → Alert
    ├─→ Disk I/O: > 500 IOPS → Queue growing?
    ├─→ Network: Bandwidth utilization
    └─→ Application metrics:
        ├─→ Request latency (p50, p95, p99)
        ├─→ Error rate
        ├─→ Cache hit ratio
        └─→ Queue depth
```

### Auto-Scaling Rules
```
API Gateway:
  Metric: requests/sec
  Rule 1: > 1000 req/sec → Scale from 3 to 5 instances (+ 2 min delay)
  Rule 2: < 300 req/sec  → Scale from 5 to 3 instances (+ 10 min delay)

Database:
  Metric: CPU
  Rule: > 75% → Scale replicas (read-only)

Cache:
  Metric: Memory usage
  Rule: > 85% → Add Redis cluster shard

WebSocket:
  Metric: Active connections
  Rule: > 400 per instance → Add instance
```

---

## 11. Disaster Recovery Scaling

### Multi-Region Deployment
```
Primary Region (US-East)
    ├─→ API Gateway (3 instances)
    ├─→ Database (1 primary + 2 replicas)
    ├─→ Cache (3 node cluster)
    └─→ WebSocket Hubs (2 instances)

Secondary Region (US-West) [Standby]
    ├─→ API Gateway (1 instance) [cold]
    ├─→ Database replica (read-only)
    ├─→ Cache replica
    └─→ Failover trigger: Primary health check fails 3x
        └─→ DNS switch to secondary
        └─→ Scale secondary to full capacity
```

### Failover Process
```
Primary health check fails
    ├─→ Cloudflare DNS reroutes to secondary
    ├─→ Secondary scales up (Kubernetes)
    ├─→ Database replication lag checked
    ├─→ If lag < 5 min: Promote secondary
    ├─→ If lag > 5 min: Manual intervention
```

---

## Capacity Planning

### Current Targets
| Component | Capacity | Scaling Point |
|-----------|----------|---|
| API Instances | 3 | 1000 req/sec |
| Database | Single primary + 2 replicas | 10K req/sec |
| Cache (Redis) | 10GB | 1 million keys |
| WebSocket Hubs | 2 (600 connections) | 400 conn/instance |
| AI Workers | 4 | 100 pending jobs |

### Future Growth (1 Year)
| Component | Projected | Scaling |
|-----------|-----------|---------|
| API Instances | 10+ | 5000+ req/sec |
| Database | Sharded (3-4 shards) | Multi-region |
| Cache | 50GB+ | Cluster mode |
| WebSocket | 10+ hubs | Global CDN + edge |
| AI Workers | 20+ | GPU-accelerated |

