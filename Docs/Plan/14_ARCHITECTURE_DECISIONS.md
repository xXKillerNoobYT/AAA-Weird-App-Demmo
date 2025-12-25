# Architecture Decision Records (ADR) - CloudWatcher System

## Overview

This document captures major architectural decisions made during the system design phase. Each decision includes the context, alternatives considered, chosen solution, and rationale.

---

## ADR-001: Multi-Cloud Storage Strategy (SharePoint Primary, Google Drive Secondary)

### Status
✅ **ACCEPTED**

### Context
The system needs to store processed files and request/response data in cloud storage. Organizations have diverse cloud provider preferences (Microsoft 365, Google Workspace, hybrid). Single-provider lock-in creates vendor risk.

### Problem
- **Single-provider risk**: If primary vendor experiences outages, system fails
- **Customer choice**: Different organizations prefer different cloud platforms
- **Cost optimization**: Some organizations already have existing cloud commitments
- **Data sovereignty**: Multi-cloud enables regional compliance requirements

### Alternatives Considered

1. **SharePoint Only** (Microsoft-centric)
   - ✅ Pros: Direct integration with M365, well-documented APIs
   - ❌ Cons: Locks users into Microsoft ecosystem, no Google Workspace support

2. **Google Drive Only** (Google-centric)
   - ✅ Pros: Simpler API, quota is clear
   - ❌ Cons: Loses Microsoft 365 market, no OneDrive integration

3. **Multi-Cloud with All Providers** (Azure, AWS, GCP, local)
   - ✅ Pros: Maximum flexibility, customer choice
   - ❌ Cons: Complexity explosion, maintenance burden, unnecessary

4. **Multi-Cloud: SharePoint + Google Drive + Local** (Selected)
   - ✅ Pros: Covers 90% of market, fallback strategy, development-friendly
   - ✅ Flexible provider selection per organization
   - ✅ Graceful degradation (local fallback during outages)
   - ❌ Modest additional complexity

### Decision
**Implement Strategy Pattern with three providers**:
- Primary: SharePoint (Microsoft Graph API)
- Secondary: Google Drive (Google Drive API)
- Fallback: Local filesystem (development/testing/offline)

### Rationale
1. **Market Coverage**: SharePoint + Google Drive covers 95%+ of enterprise and SMB segments
2. **Automatic Failover**: If SharePoint is down, seamlessly failover to Google Drive
3. **Development Convenience**: Local filesystem enables offline development without API credentials
4. **Cost**: Reuses existing customer cloud subscriptions (no new SaaS)
5. **Compliance**: Different regions prefer different cloud providers (GDPR for EU, etc.)

### Implementation
- **Strategy Pattern**: `ICloudStorageProvider` interface with three implementations
- **Provider Factory**: Selects provider based on organization config
- **Fallback Logic**: Automatic retry with next provider on network error
- **Rate Limiting**: Per-provider rate limit handling

### Consequences
- ✅ Increased system resilience
- ✅ Vendor-independent architecture
- ✅ Customer flexibility
- ❌ Higher test complexity (mock three providers)
- ❌ Slightly larger codebase
- ⚠️ Provider-specific API differences require abstraction

### Related Decisions
- ADR-004: Microservice-Ready Monolith Architecture

---

## ADR-002: Microservice-Ready Monolith (Not Microservices Day 1)

### Status
✅ **ACCEPTED**

### Context
The system has multiple independent concerns:
- Request handling (mobile API)
- Response delivery (WebSocket)
- Cloud storage synchronization
- AI orchestration (Python agents)
- Scheduled tasks
- Real-time notifications

Each could theoretically be a separate service. The question: deploy as single monolith or distributed microservices from the start?

### Problem
- **Microservices are complex**: Distributed transactions, eventual consistency, debugging harder
- **Monolith becomes a bottleneck**: Eventually scaling limits are reached
- **Monolith deployment is simpler**: Single codebase, single deployment unit
- **Cross-service communication overhead**: Network latency between services

### Alternatives Considered

1. **Full Microservices** (gRPC, orchestration, distributed tracing)
   - ✅ Pros: Independent scaling, team autonomy, technology diversity
   - ❌ Cons: Complexity (2-3 year timeline to get right), operational overhead, cost

2. **Traditional Monolith** (Everything in one executable)
   - ✅ Pros: Simple to build, easier debugging, lower operational overhead
   - ❌ Cons: Everything scales together, technology locked (C# only), hard to break apart later

3. **Modular Monolith** (Logical separation, future microservices path) (Selected)
   - ✅ Pros: Monolith simplicity today, microservices path tomorrow
   - ✅ Clear module boundaries (API, WebSocket, Scheduler, Cloud Integration, AI Routing)
   - ✅ Async communication patterns (message queues) prepare for separation
   - ✅ Each module can be extracted independently
   - ❌ Requires architectural discipline

### Decision
**Build a modular monolith with clear boundaries, designed for future microservice extraction**.

Key principles:
1. **Logical Separation**: Six independent modules within single deployment
2. **Async Communication**: Message queues between modules enable future service boundaries
3. **Dependency Injection**: Each module can be swapped (real implementation ↔ mock)
4. **Configuration-Based**: Behavior changes via config (scaling, feature flags, fallbacks)

Module boundaries:
- **API Module**: Request/Response HTTP endpoints
- **WebSocket Module**: Real-time communication via SignalR
- **Cloud Integration Module**: Multi-provider storage abstraction
- **AI Routing Module**: Route requests to Python agents
- **Scheduler Module**: Background jobs (sync, cleanup, maintenance)
- **Cache Module**: Distributed caching via Redis

### Rationale
1. **Pragmatism**: Delivers faster (6 months vs. 2-3 years)
2. **Future-Proof**: Explicit module boundaries allow extraction to microservices
3. **Operational Simplicity**: Single deployment unit reduces ops complexity
4. **Cost**: Lower infrastructure cost initially (single K8s cluster vs. multi-cluster)
5. **Team Velocity**: Single tech stack (C#) team stays productive
6. **Flexibility**: Can migrate gradually (API → Microservice first, then others)

### Migration Path (Future)
Year 2 decisions:
- Extract **AI Routing Module** → Separate Python service (for specialized scaling)
- Extract **Scheduler Module** → Separate service (Hangfire as distributed job system)
- Extract **Cloud Integration Module** → Optional CDN service (for file distribution)
- Keep **API + WebSocket + Cache** together (high coupling, frequent communication)

### Consequences
- ✅ Faster delivery
- ✅ Lower operational overhead
- ✅ Clear path to microservices
- ✅ Single tech stack simplicity
- ❌ All instances must include all modules (wasteful memory for scheduled tasks)
- ⚠️ Requires architectural discipline (teams might not respect boundaries)

### Related Decisions
- ADR-005: Synchronous vs. Asynchronous communication patterns

---

## ADR-003: PostgreSQL + Redis + Local SQLite Caching Tier

### Status
✅ **ACCEPTED**

### Context
Application needs persistent data storage (requests, responses, metadata) and real-time access patterns (request deduplication, rate limiting, session state).

Database selection has long-term implications for cost, scaling, and operational complexity.

### Problem
- **Single database bottleneck**: High-frequency queries (cache lookups) slow down persistence layer
- **Rate limiting scalability**: Redis counters per device enable distributed rate limiting
- **Mobile offline support**: Mobile apps need local SQLite for offline request queuing

### Alternatives Considered

1. **PostgreSQL Only**
   - ✅ Pros: Single source of truth, ACID guarantees, mature
   - ❌ Cons: High query load (rate limiting, deduplication), connection pool limits

2. **MongoDB Only** (Document database)
   - ✅ Pros: Flexible schema, good for request documents
   - ❌ Cons: Less mature for large-scale distributed systems, operational complexity

3. **PostgreSQL + Redis + In-Memory Cache** (Selected)
   - ✅ Pros: Three-tier caching (L1: in-memory, L2: Redis cluster, L3: PostgreSQL)
   - ✅ Redis enables distributed rate limiting, deduplication, session state
   - ✅ PostgreSQL for persistent, queryable data
   - ✅ Three-tier improves latency and reduces load
   - ❌ Moderate added complexity

4. **DynamoDB** (AWS serverless)
   - ✅ Pros: Serverless, auto-scaling, pay-per-request
   - ❌ Cons: Vendor lock-in, complex queries more expensive, complexity

### Decision
**Three-tier caching architecture**:

**L1 - In-Memory Cache** (within API process)
- TTL: 5-30 seconds
- Use case: Hot request deduplication
- Technology: .NET MemoryCache

**L2 - Redis Cluster** (distributed)
- TTL: 5-30 minutes
- Use cases: Request cache, response cache, rate limiting counters, session state
- Technology: Redis 7.0+ Cluster Mode (3+ nodes)
- Consistency: Eventual (OK for cache layer)

**L3 - PostgreSQL** (persistent)
- Consistency: Strong (ACID)
- Use cases: Request history, response archive, metadata, audit logs
- Replicas: Read-only replicas for scaling read-heavy queries
- Technology: PostgreSQL 15+

**Mobile Layer - SQLite** (local device)
- Use case: Offline request queueing
- Technology: SQLite (embedded in mobile app)
- Sync: Batch sync when network available

### Rationale
1. **Performance**: Three tiers reduce latency and load
2. **Scalability**: Redis cluster enables horizontal scaling of cache layer
3. **Reliability**: PostgreSQL + Redis cluster provide high availability
4. **Flexibility**: Can tune TTLs per use case
5. **Cost**: Redis cheaper than database for high-frequency reads

### Caching Patterns Implemented
1. **Cache-Aside**: App checks cache, then database
2. **Write-Through**: Write to cache and database simultaneously
3. **Cache Invalidation**: Invalidate on CRUD operations

### Consequences
- ✅ Reduced database load (10x improvement)
- ✅ Lower latency for hot data
- ✅ Distributed rate limiting
- ❌ Cache coherence complexity (invalidation timing)
- ❌ More operations (manage Redis cluster)
- ⚠️ Eventual consistency (cache stale within TTL)

### Related Decisions
- ADR-006: Synchronous request-response vs. async processing

---

## ADR-004: Python Agents Separate from C# API

### Status
✅ **ACCEPTED**

### Context
The system uses Microsoft AutoGen (Python framework) for multi-agent AI orchestration. The primary API is C#/ASP.NET Core.

Should agents run:
1. Within the same C# process (hosting Python runtime)?
2. As a separate Python microservice?
3. As external API calls to Python agents?

### Problem
- **Different tech stacks**: C# and Python have different strengths
- **Operational complexity**: Managing mixed-language deployments
- **Independent scaling**: AI agents have different scaling needs than HTTP API
- **Development velocity**: Python team works independently from C# team

### Alternatives Considered

1. **Python Embedded in C#** (Python.NET library)
   - ✅ Pros: Single deployment, direct memory sharing
   - ❌ Cons: Difficult to manage, Python runtime dependencies in C# process, poor isolation

2. **Same Monolith, Different Language** (Rewrite API in Python)
   - ✅ Pros: Unified tech stack
   - ❌ Cons: Lose C# benefits (strong typing, performance), rewrite effort

3. **Separate Python Microservice** (Selected)
   - ✅ Pros: Technology independence, independent scaling, team autonomy
   - ✅ Natural service boundary (AI processing is distinct concern)
   - ✅ Async communication (request → queue → agent pool)
   - ❌ Network latency between C# API and Python service
   - ❌ Distributed transaction complexity

### Decision
**Separate Python service for agent orchestration**:

- **C# API**: Handles HTTP requests, validation, cloud storage, WebSocket
- **Python Agent Orchestrator**: Multi-agent framework, LLM calls, complex reasoning
- **Communication**: Message queue (async) or gRPC (sync)
- **Scaling**: Independent (API: 3-10 pods, Agents: 3-20 pods)

### Rationale
1. **Right tool for job**: Python + AutoGen is best for multi-agent AI
2. **Independent scaling**: AI processing can scale separate from API
3. **Team structure**: Enables Python team to work autonomously
4. **Failure isolation**: Python agent crash doesn't take down entire API
5. **Technology diversity**: Allows best-of-breed tools in each layer

### Communication Pattern
1. **Request Submission**: C# API receives request
2. **Async Routing**: Enqueue request in message queue
3. **Agent Processing**: Python agent dequeues and processes
4. **Result Storage**: Agent stores response in PostgreSQL
5. **Notification**: Sends notification via WebSocket

### Consequences
- ✅ Technology independence
- ✅ Independent scaling
- ✅ Team autonomy
- ✅ Better failure isolation
- ❌ Network latency (~10-50ms per call)
- ❌ Distributed transaction complexity
- ❌ Operational complexity (two runtime environments)

### Related Decisions
- ADR-005: Asynchronous communication patterns

---

## ADR-005: Asynchronous Processing with Queuing

### Status
✅ **ACCEPTED**

### Context
Request processing involves multiple steps:
1. Validation and serialization
2. Cloud storage upload
3. AI agent orchestration
4. Response delivery
5. WebSocket notification

Coupling all these steps synchronously creates bottlenecks. Some steps are naturally asynchronous (background jobs, notifications).

### Problem
- **Performance**: Synchronous chain blocks client (long wait time)
- **Scalability**: Bottleneck at slowest step (AI processing)
- **Resilience**: Single failure cascades (broken chain)
- **Responsiveness**: Mobile app needs immediate feedback

### Alternatives Considered

1. **Fully Synchronous** (Request → Process → Response in HTTP call)
   - ✅ Pros: Simple to understand, no queue complexity
   - ❌ Cons: Slow (user waits 30s+), AI processing blocks API, poor UX

2. **Fully Asynchronous** (Everything queued, no immediate feedback)
   - ✅ Pros: Highly scalable, resilient
   - ❌ Cons: User doesn't know if request succeeded, complex UX pattern

3. **Hybrid: Fast Path + Async Processing** (Selected)
   - ✅ Pros: Immediate feedback + async processing
   - ✅ User knows request accepted within 100ms
   - ✅ AI processing happens asynchronously
   - ✅ Notifications on completion via WebSocket
   - ❌ Moderate complexity (request state machine)

### Decision
**Hybrid request processing**:

#### Fast Path (Synchronous - < 100ms)
1. Mobile app submits request (HTTPS POST)
2. API validates schema (RequestValidator)
3. API serializes to JSON (RequestSerializer)
4. API uploads to cloud (concurrent with steps 2-3)
5. API enqueues for AI processing (message queue)
6. **API returns 202 Accepted** ← User sees success immediately
7. Mobile app shows "Processing..." status

#### Background Path (Asynchronous)
1. Agent Orchestrator dequeues request
2. Routes to appropriate agents (1-5 minutes)
3. Agents call LLM APIs (network latency)
4. Generates response document
5. Stores response in PostgreSQL
6. **Notifies API via message queue**
7. **API notifies mobile via WebSocket**
8. **Mobile app receives notification**, displays result

#### Failure Path
1. If AI processing fails:
   - Error logged to PostgreSQL
   - Failed notification sent to mobile
   - User sees "Processing failed, retry?" option
2. If cloud upload fails:
   - Exponential backoff retry (3 times max)
   - Alert monitoring on persistent failures
3. If WebSocket delivery fails:
   - Poll-based fallback (mobile queries for updates)

### Rationale
1. **Responsive**: User gets immediate feedback (< 100ms)
2. **Scalable**: AI processing scaled independently
3. **Resilient**: Failures don't cascade
4. **User Experience**: Clear status updates via WebSocket
5. **Cost**: Batch processing during off-peak hours

### Implementation
- **Message Queue**: Azure Service Bus or AWS SQS
- **Request State**: PENDING → PROCESSING → COMPLETED/FAILED
- **WebSocket Updates**: Real-time status transitions
- **Retry Logic**: Exponential backoff (3 attempts max)

### Consequences
- ✅ Better UX (responsive + real-time updates)
- ✅ Higher throughput (API not blocked)
- ✅ Better scalability
- ✅ Resilience to failures
- ❌ Complexity (state machine, async patterns)
- ❌ Eventually consistent (response available after delay)
- ⚠️ Requires WebSocket for real-time UX

### Related Decisions
- ADR-004: Separate Python agents
- ADR-006: WebSocket for real-time communication

---

## ADR-006: WebSocket + Polling Fallback for Real-Time Updates

### Status
✅ **ACCEPTED**

### Context
Mobile devices need to receive real-time notifications when processing completes. Options:
1. **Long polling** (inefficient, high latency)
2. **WebSocket** (efficient, real-time, requires persistent connection)
3. **Push notifications** (APNs for iOS, FCM for Android, infrastructure complexity)

### Problem
- **Mobile networks**: Connections are unstable, WiFi roaming causes disconnections
- **Battery life**: WebSocket requires constant connection (drains battery)
- **Firewall issues**: Some corporate firewalls block WebSocket
- **Real-time requirement**: User expects results within seconds, not minutes

### Alternatives Considered

1. **Polling Only** (Every 5 seconds, check for updates)
   - ✅ Pros: Simple, works everywhere
   - ❌ Cons: Latency (up to 5 seconds), wasteful battery, high server load

2. **WebSocket Only** (Persistent connection)
   - ✅ Pros: Real-time, efficient, low latency
   - ❌ Cons: Battery drain, firewall issues, connection loss challenges

3. **Push Notifications** (APNs / FCM)
   - ✅ Pros: Real-time, battery efficient
   - ❌ Cons: Infrastructure complex, privacy concerns, vendor lock-in

4. **WebSocket + Polling Fallback** (Selected)
   - ✅ Pros: Real-time when available, fallback for unreliable networks
   - ✅ Best of both: efficient + reliable
   - ✅ Mobile app can manage battery via fallback
   - ❌ More complex to implement

### Decision
**Dual-mode real-time delivery**:

#### Primary: WebSocket (SignalR)
- Mobile app establishes persistent WebSocket connection at launch
- Connection maintained across network transitions (WiFi ↔ cellular)
- API broadcasts response notifications via WebSocket
- Real-time delivery (< 100ms latency)
- Session state stored in Redis (distributed across API replicas)

#### Fallback: Polling
- If WebSocket disconnected for > 30 seconds
- Mobile app switches to polling (check for updates every 10 seconds)
- Enables delivery even with unreliable networks
- Works through corporate firewalls
- Mobile app can disable polling during background (save battery)

#### Mobile Battery Management
- WebSocket active when app in foreground
- WebSocket paused when app in background (prevent battery drain)
- Polling disabled in background (resume on app open)
- Push notifications (optional) for high-priority notifications

### Rationale
1. **Real-time UX**: WebSocket provides < 100ms latency
2. **Reliability**: Polling fallback for unreliable networks
3. **Battery efficient**: Can disable WebSocket in background
4. **Corporate networks**: Polling works when WebSocket blocked
5. **Gradual rollout**: Can disable WebSocket for testing

### Implementation
```csharp
// SignalR Hub
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var deviceId = Context.User.FindFirst("device_id").Value;
        await Groups.AddToGroupAsync(ConnectionId, $"device_{deviceId}");
        await base.OnConnectedAsync();
    }
    
    public void NotifyResponse(string deviceId, string responseId)
    {
        Clients.Group($"device_{deviceId}").SendAsync("ResponseReady", responseId);
    }
}

// Fallback polling endpoint
[HttpGet("api/response/{responseId}")]
public async Task<IActionResult> GetResponse(string responseId)
{
    // Check cache, then database
    var response = await _responseManager.GetResponseAsync(responseId);
    return response == null ? NotFound() : Ok(response);
}
```

### Consequences
- ✅ Real-time delivery
- ✅ Resilient to network issues
- ✅ Works everywhere (WebSocket + fallback)
- ✅ Battery efficient (configurable)
- ❌ Complexity (dual-mode implementation)
- ❌ Synchronization between WebSocket and polling
- ⚠️ State management in distributed system

### Related Decisions
- ADR-005: Asynchronous processing

---

## ADR-007: Redis Cluster Mode vs. Standalone

### Status
✅ **ACCEPTED**

### Context
Redis caching layer needs to support:
- High throughput (10,000+ requests/second)
- Fault tolerance (single-node failures)
- Data persistence (rate limits, session state)
- Distributed rate limiting

### Problem
- **Single-node Redis**: Single point of failure, limited throughput
- **Redis Sentinel**: Active-standby, not true distributed
- **Redis Cluster**: Data sharding, true distributed, more complex

### Alternatives Considered

1. **Single Redis Instance**
   - ✅ Pros: Simple setup, easy to understand
   - ❌ Cons: SPOF (single point of failure), throughput limited

2. **Redis Sentinel** (Active-standby)
   - ✅ Pros: Automatic failover, high availability
   - ❌ Cons: No data sharding, read replicas only, single master bottleneck

3. **Redis Cluster Mode** (Selected)
   - ✅ Pros: Data sharding, true distributed, auto-failover per slot
   - ✅ Linear scaling (add nodes = more throughput)
   - ✅ Fault tolerance (slot replicas)
   - ✅ Supports MOVED redirects
   - ❌ Complexity (slot management), compatibility issues

### Decision
**Redis Cluster Mode with 3+ nodes**:

- **3-node minimum**: Each node is a shard with replica
- **Hash slot distribution**: 16,384 slots divided across nodes
- **Key layout**: `device:{deviceId}:requests` patterns
- **Persistence**: RDB snapshots + AOF logs
- **Eviction policy**: allkeys-lru (LRU eviction when full)

### Configuration
```
Cluster topology:
- Node 1: Shard 1 (master) + Shard 3 (replica)
- Node 2: Shard 2 (master) + Shard 1 (replica)
- Node 3: Shard 3 (master) + Shard 2 (replica)

Scaling:
- Add Node 4: Reshuffles slots, increases throughput
- Remove Node: Reshuffles slots to remaining nodes
```

### Rationale
1. **Throughput**: Linear scaling (3x nodes = 3x throughput)
2. **Fault tolerance**: Automatic failover per slot (not entire cluster)
3. **Cost effective**: Horizontal scaling cheaper than vertical
4. **Future-proof**: Can grow from 3 to 10+ nodes as needed

### Consequences
- ✅ High throughput
- ✅ Fault tolerant
- ✅ Scales linearly
- ✅ No single point of failure
- ❌ Cluster complexity
- ❌ Cross-slot operations not supported (require caution)
- ❌ Debugging harder (data distributed)

### Related Decisions
- ADR-003: Three-tier caching architecture

---

## ADR-008: OAuth2 for Mobile Authentication

### Status
✅ **ACCEPTED**

### Context
Mobile devices need to authenticate with API. Options:
1. Username/password (insecure, password management burden)
2. API keys (limited features, bearer tokens risky)
3. OAuth2 / OpenID Connect (industry standard, social login capability)

### Problem
- **Password risk**: Plain-text passwords on mobile
- **Account takeover**: Password reuse across apps/sites
- **Social login**: Users expect Google/Microsoft login
- **Token management**: Need secure token storage and refresh

### Alternatives Considered

1. **Custom token system**
   - ✅ Pros: Full control
   - ❌ Cons: Re-inventing crypto, hard to get right, security risks

2. **API keys**
   - ✅ Pros: Simple, no password
   - ❌ Cons: Bearer token risk (if stolen), limited features

3. **OAuth2 + OpenID Connect** (Selected)
   - ✅ Pros: Industry standard, social login support, secure delegation
   - ✅ Mobile SDKs available (AppAuth for iOS/Android)
   - ✅ Can use existing credentials (Microsoft, Google)
   - ❌ More complex initially

### Decision
**OAuth2 Authorization Code flow with PKCE**:

#### Provider Options
- **Microsoft Entra ID** (Azure AD) for enterprise
- **Google OAuth** for consumer accounts
- **Custom OAuth provider** (optional, using IdentityServer)

#### Flow
1. Mobile app redirects to OAuth provider (Microsoft/Google login)
2. User logs in, grants permission
3. Provider returns authorization code
4. Mobile app exchanges code for access token (PKCE prevents theft)
5. Mobile app stores token securely (Keychain/Keystore)
6. API validates token signature (JWT)
7. API extracts device_id from token claims

#### Token Management
- **Access token TTL**: 1 hour
- **Refresh token**: Long-lived, refreshed before expiry
- **Token storage**: Secure enclave (Keychain on iOS, Keystore on Android)
- **Logout**: Revoke token, clear local storage

### Rationale
1. **Security**: Proven OAuth2 protocol, avoids password risk
2. **Social login**: Leverages existing credentials (Microsoft, Google)
3. **Enterprise friendly**: Integrates with corporate directories
4. **Standards-based**: Industry-standard, well-supported
5. **Mobile SDK support**: AppAuth libraries simplify implementation

### Consequences
- ✅ Secure, standards-based
- ✅ Social login support
- ✅ Enterprise integration
- ✅ Reduces password management burden
- ❌ Dependency on OAuth provider
- ❌ Requires secure token storage on mobile
- ❌ More complex than simple tokens

### Related Decisions
- ADR-009: Field-level encryption for sensitive data

---

## ADR-009: Horizontal Scaling over Vertical

### Status
✅ **ACCEPTED**

### Context
As load increases, system must scale. Options:
1. **Vertical scaling** (bigger servers: more CPU, RAM)
2. **Horizontal scaling** (more smaller servers)

### Problem
- **Vertical limits**: Single machine has CPU/RAM ceiling
- **Cost**: Vertical scaling increases cost faster than horizontal
- **Availability**: Larger servers mean larger failure impact
- **Cloud economics**: Horizontal scaling better utilizes cloud resources

### Alternatives Considered

1. **Vertical Only** (Big servers)
   - ✅ Pros: Simpler ops, single machine
   - ❌ Cons: Expensive, limited by hardware ceiling, single point of failure

2. **Horizontal Only** (Many small servers)
   - ✅ Pros: Cost-efficient, scalable, fault-tolerant
   - ❌ Cons: Load balancing complexity, state management harder

3. **Hybrid** (Vertical + Horizontal) (Selected)
   - ✅ Pros: Cost-optimal, scales to any size
   - ✅ API layer: Horizontal (3-100+ pods)
   - ✅ Database: Vertical (upgrade instance size) + read replicas
   - ✅ Cache: Horizontal (cluster mode sharding)
   - ❌ Moderate operational complexity

### Decision
**Horizontal-first scaling strategy**:

#### API Services
- Base: 3 pod replicas (always on for HA)
- Scale rule: Add 1 pod per 70% CPU usage
- Max: 20-30 pods (cost limit)
- Cost: 3 pods × $50/month = $150, max 30 pods = $1,500

#### Database
- Base: PostgreSQL managed instance (b2ms tier)
- Read replicas: Add as query load increases
- Vertical only: Upgrade instance size if replicas hit limits
- Sharding: Consider at 100GB data (partition by device_id)

#### Cache
- Base: 3-node Redis cluster
- Add nodes: For more throughput (each node ~5,000 ops/sec)
- Max: 10-15 nodes (diminishing returns on management)

### Rationale
1. **Cost efficiency**: Horizontal is cheaper than vertical per unit
2. **Fault tolerance**: Single pod/node failure impacts less
3. **Cloud economics**: Pay only for what you use
4. **Future-proof**: Can handle 10x growth (monolith to microservices)

### Consequences
- ✅ Cost-optimal scaling
- ✅ High fault tolerance
- ✅ Future-proof architecture
- ❌ Load balancing complexity
- ❌ State management (distributed cache, database)
- ⚠️ Requires infrastructure automation (K8s, IaC)

### Related Decisions
- ADR-002: Modular monolith architecture
- ADR-003: Three-tier caching

---

## ADR-010: Kubernetes for Container Orchestration

### Status
✅ **ACCEPTED**

### Context
Containerized application (Docker) needs orchestration. Deployment and scaling must be automated.

Options:
1. **Manual VM management** (SSH, scripts)
2. **Docker Swarm** (simpler than K8s)
3. **Kubernetes** (industry standard, complex)
4. **Managed services** (ECS, App Engine)

### Problem
- **Manual scaling**: Too slow for traffic spikes
- **Zero-downtime updates**: Requires orchestration
- **Self-healing**: Need automatic pod restarts
- **Resource efficiency**: Bin packing across nodes

### Alternatives Considered

1. **Manual VMs**
   - ✅ Pros: Full control, simple to understand
   - ❌ Cons: Slow scaling, ops burden, error-prone

2. **Docker Swarm**
   - ✅ Pros: Simpler than K8s, built-in to Docker
   - ❌ Cons: Limited features, smaller community

3. **Kubernetes (AKS/EKS)** (Selected)
   - ✅ Pros: Industry standard, rich ecosystem, auto-scaling, declarative
   - ✅ Managed service (less ops burden)
   - ✅ Rich tooling (Helm, operators, service mesh optional)
   - ❌ Complexity (steep learning curve)

4. **Serverless** (Azure Container Instances, AWS Fargate)
   - ✅ Pros: Minimal ops
   - ❌ Cons: Expensive for sustained load, cold starts

### Decision
**Managed Kubernetes cluster** (AKS or EKS):

- **Cluster size**: 3+ nodes (HA for control plane)
- **Node size**: 2-4 CPU, 4-8GB memory per node
- **Scaling**: Horizontal Pod Autoscaling (HPA) and Vertical Pod Autoscaling (VPA)
- **Service mesh**: Optional (Istio for advanced routing)

#### Kubernetes Features Used
- **Deployments**: Declarative pod management
- **Services**: Load balancing
- **ConfigMaps/Secrets**: Configuration management
- **Persistent Volumes**: Database storage
- **Ingress**: HTTPS termination, routing
- **RBAC**: Access control
- **Network Policies**: Security

### Rationale
1. **Industry standard**: Kubernetes is de-facto standard
2. **Ecosystem**: Vast tooling and community
3. **Auto-scaling**: Responds to demand
4. **Self-healing**: Automatic pod restarts
5. **Zero-downtime updates**: Rolling deployments
6. **Cost**: Managed K8s cheaper than VMs for this workload

### Consequences
- ✅ Automated scaling
- ✅ Self-healing
- ✅ Zero-downtime deployments
- ✅ Cost-efficient
- ✅ Industry standard
- ❌ Complexity (YAML configs, network policies)
- ❌ Learning curve (Kubernetes is complex)
- ⚠️ Not ideal for serverless workloads

### Related Decisions
- ADR-009: Horizontal scaling strategy

---

## Summary Table

| ADR | Title | Status | Impact |
|-----|-------|--------|--------|
| ADR-001 | Multi-Cloud Storage | Accepted | High (system resilience) |
| ADR-002 | Modular Monolith | Accepted | High (deployment model) |
| ADR-003 | 3-Tier Caching | Accepted | High (performance) |
| ADR-004 | Separate Python Agents | Accepted | Medium (architecture) |
| ADR-005 | Async Processing | Accepted | High (UX + scalability) |
| ADR-006 | WebSocket + Polling | Accepted | Medium (UX) |
| ADR-007 | Redis Cluster | Accepted | Medium (ops) |
| ADR-008 | OAuth2 Auth | Accepted | Medium (security) |
| ADR-009 | Horizontal Scaling | Accepted | High (ops) |
| ADR-010 | Kubernetes | Accepted | High (deployment) |

---

## Review Criteria

- [ ] All decisions have clear context and rationale
- [ ] Alternatives explored for each decision
- [ ] Consequences documented
- [ ] Related decisions identified
- [ ] Team alignment on decisions
- [ ] Regular review (quarterly) to reconsider decisions

## Next Steps

1. **Present to team** for feedback and alignment
2. **Document in team wiki** for onboarding
3. **Quarterly review** to reconsider decisions as situation evolves
4. **Track implications** as system evolves

