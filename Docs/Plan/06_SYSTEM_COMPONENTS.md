# System Components Architecture

## Component Inventory

### Backend Services Layer

#### 1. **API Gateway** (ASP.NET Core)
- Entry point for all client requests
- Request/response routing
- Rate limiting and throttling
- Request validation
- Response serialization

#### 2. **Authentication Service**
- OAuth2 token validation
- Azure AD integration
- Session management
- Token refresh logic

#### 3. **Request Processor Service**
- Validates incoming request JSON schemas
- Type-based request routing
- Business logic orchestration
- Error handling and validation

#### 4. **Response Generator Service**
- Formats response data into JSON
- Writes to cloud storage
- Atomic file operations
- Response metadata tracking

#### 5. **Cloud Storage Service**
- ICloudStorageProvider abstraction layer
- Multi-provider support (SharePoint, Google Drive, Local)
- File upload/download/delete operations
- Folder structure management
- Retry logic with exponential backoff

#### 6. **WebSocket Hub** (SignalR)
- Real-time device connections
- Connection state management
- Message broadcasting
- Graceful disconnection handling
- Heartbeat/keep-alive

#### 7. **File Watcher Service**
- Monitors /Cloud/Requests folders
- Event-driven request detection
- Watches for response readiness
- Triggers processing pipeline

#### 8. **Notification Service**
- Email notifications (SMTP)
- Slack notifications
- SMS notifications (Twilio)
- Webhook dispatching

### Data Access Layer

#### 9. **Database Service**
- Entity Framework Core ORM
- PostgreSQL integration
- Connection pooling
- Query optimization

#### 10. **Cache Layer** (Redis)
- Session caching
- Request result caching
- Device state caching
- TTL-based expiration

### AI/Agent Layer

#### 11. **AI Orchestrator** (Python + AutoGen)
- Multi-agent coordination
- Agent lifecycle management
- Context passing between agents
- Decision logging

#### 12. **RequestRouter Agent**
- Analyzes request content
- Routes to appropriate approvers
- Permission checking
- Escalation logic

#### 13. **OrderGenerator Agent**
- Consolidates parts lists
- Optimizes supplier selection
- PO generation

#### 14. **SupplierMatcher Agent**
- Supplier database queries
- Cost optimization
- Availability checking
- Logistics optimization

#### 15. **PartsSpecialist Agent**
- Parts variant matching
- Construction specifications
- Quality assurance
- Equivalency checking

### Mobile/Client Applications

#### 16. **Truck App** (React/React Native)
- Device request generation
- Offline request queue
- Smart sync with backoff
- Real-time response polling

#### 17. **Warehouse App** (React/React Native)
- Inventory management interface
- Parts list management
- Receiving workflows
- Stock updates

#### 18. **Office App** (Web - Vue.js/React)
- Parts list creation
- Approval workflows
- Order management
- Reporting dashboards

#### 19. **Job Site App** (React Native)
- Equipment deployment tracking
- Parts usage logging
- Status updates
- Photo/document capture

### Support Services

#### 20. **Monitoring & Observability**
- Application Performance Monitoring (APM)
- Structured logging
- Distributed tracing
- Metrics collection

#### 21. **Configuration Service**
- Environment-specific settings
- Feature flags
- Rate limiting policies
- Provider credentials management

#### 22. **Database Migration Service**
- Schema migrations
- Data transformation
- Rollback capabilities
- Version control

## Component Dependencies Map

```
Client Applications (Web/Mobile)
    ↓
API Gateway + Authentication Service
    ↓
Request Processor Service → WebSocket Hub
    ↓
Cloud Storage Service (Multi-provider)
    ↓
File Watcher Service
    ↓
Response Generator Service
    ↓
AI Orchestrator (AutoGen)
    ├→ RequestRouter Agent
    ├→ OrderGenerator Agent
    ├→ SupplierMatcher Agent
    └→ PartsSpecialist Agent
    ↓
Database Service (PostgreSQL)
    ↓
Cache Layer (Redis)
    ↓
Notification Service (Email/Slack/SMS)
```

## Component Communication Patterns

### Synchronous
- Client → API Gateway (HTTP REST)
- API Gateway → Authentication Service
- Request Processor → Response Generator
- AI Orchestrator ↔ Agents (In-process calls)

### Asynchronous
- File Watcher → Request Processor (Event-driven)
- WebSocket Hub → Connected Clients (Broadcast)
- Notification Service → External Systems (Background tasks)
- Database Service → Cache Layer (Invalidation)

### Event-Driven
- Request uploaded → File Watcher detects
- Response ready → WebSocket broadcasts
- Status changed → Notifications dispatched
- Processing complete → Archive triggered

## Component Boundaries

### Microservice Candidates
These components could become separate microservices:
1. Cloud Storage Service (high scalability needs)
2. AI Orchestrator (Python-based, separate runtime)
3. Notification Service (async, independent)
4. Authentication Service (security focus)

### Monolith Components
These work well in monolithic design:
1. API Gateway
2. Request Processor
3. Response Generator
4. Database Service
5. Cache Layer

## Component Scalability Characteristics

| Component | Scaling Strategy | Load Driver | Bottleneck |
|-----------|------------------|-------------|-----------|
| API Gateway | Horizontal (Replicas) | Request rate | Load balancer |
| Request Processor | Horizontal + Queue | Processing rate | Database writes |
| Cloud Storage | Multi-provider | File I/O | Provider limits |
| WebSocket Hub | Horizontal + Session affinity | Connection count | Memory |
| Database | Vertical + Read replicas | Transaction volume | IOPS |
| Cache (Redis) | Cluster mode | Hot data access | Memory |
| AI Orchestrator | Horizontal | Agent requests | Inference latency |
| File Watcher | Horizontal + Partitioning | File count | Network |

## Component Deployment Strategy

### Tier 1: Core Services (Always Required)
- API Gateway
- Authentication Service
- Database Service
- Cache Layer

### Tier 2: Processing Services (Scaled as needed)
- Request Processor
- Response Generator
- Cloud Storage Service

### Tier 3: Optional Services (Scale by demand)
- WebSocket Hub
- File Watcher
- Notification Service
- AI Orchestrator

### Tier 4: Client Applications
- Truck App
- Warehouse App
- Office App
- Job Site App
