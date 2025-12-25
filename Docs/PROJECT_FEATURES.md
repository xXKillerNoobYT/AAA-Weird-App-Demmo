# CloudWatcher Platform - High-Level Features

**Project**: AAA WeirdToo CloudWatcher Platform
**Purpose**: Cloud-based inventory management system with AI-powered request processing
**Last Updated**: 2025-01-21

---

## Core Features

### 1. Device Request Processing

**Description**: Field devices submit JSON-formatted requests for inventory operations (get_parts, update_inventory, etc.) via REST API.

**Key Capabilities**:
- JSON schema validation for incoming requests
- Unique request ID generation for tracking
- Type-based routing (get_parts, update_inventory, orders, etc.)
- Request metadata persistence (timestamp, device-id, status)
- Performance: <500ms processing latency (95th percentile)

**Technology Stack**: C# .NET 9.0, ASP.NET Core, Entity Framework Core

**Implementation Status**: Wave 2-4 (in progress)

---

### 2. Cloud Storage Integration

**Description**: Multi-provider cloud storage abstraction for request/response file handling.

**Supported Providers**:
- **SharePoint Online** (primary, Microsoft Graph API)
- **Google Drive** (secondary/backup, Google Drive API)
- **Local Filesystem** (development/testing)

**Key Capabilities**:
- Folder structure: `/Cloud/Requests/{device-id}`, `/Cloud/Responses/{device-id}`
- Atomic file operations (upload/download/delete)
- Concurrent access safety with file locking
- Transparent provider switching capability
- Provider abstraction through standardized `ICloudStorageProvider` interface

**Technology Stack**: Microsoft Graph API, Google Drive API, OAuth2

**Implementation Status**: Wave 3 (planned)

---

### 3. Real-Time Communication (WebSocket)

**Description**: Live bidirectional communication between server and connected devices for instant response delivery and status updates.

**Key Capabilities**:
- WebSocket connections using SignalR
- Response event broadcasting to connected clients
- Device connection state management
- Graceful disconnection/reconnection handling
- Heartbeat mechanism for connection health monitoring
- Message delivery <100ms latency

**Technology Stack**: ASP.NET Core SignalR, WebSocket protocol

**Implementation Status**: Wave 5 (future enhancement)

---

### 4. PostgreSQL Database

**Description**: Relational database storing users, roles, inventory, requests, responses, and audit trails.

**Schema Highlights**:
- **20+ tables** across 6 functional areas
- **Users/Roles/Departments**: Hierarchical RBAC structure
- **Parts Inventory**: 5+ variant attributes (size, color, material, etc.)
- **Requests/Responses**: Full lifecycle tracking with metadata
- **Audit Logs**: Immutable records of all data changes
- **Device Management**: Device registration and configuration

**Key Capabilities**:
- UUID primary keys for distributed systems
- Soft deletes (isDeleted flag) for data retention
- Timestamp fields (createdAt, updatedAt) on all tables
- Foreign key relationships with cascading deletes
- Full-text search support (pg_trgm extension)
- Connection pooling for concurrent access (Npgsql)

**Technology Stack**: PostgreSQL 15+, Entity Framework Core, Npgsql

**Implementation Status**: Foundation complete, Wave 4 migrations in progress

---

### 5. RESTful API Endpoints

**Description**: 25+ REST endpoints for CRUD operations across all domain entities.

**Endpoint Categories**:
- **Users API** (`/api/users`) - User/role/department management
- **Parts API** (`/api/parts`) - Inventory item catalog
- **Inventory API** (`/api/inventory`) - Stock levels and locations
- **Requests API** (`/api/requests`) - Request submission and tracking
- **Responses API** (`/api/responses`) - Response retrieval
- **Orders API** (`/api/orders`) - Order processing
- **Notifications API** (`/api/notifications`) - Alert management

**Key Capabilities**:
- OpenAPI/Swagger documentation for all endpoints
- JSON request/response payloads
- OAuth2 authentication (Azure AD/Google)
- Role-based access control (RBAC) enforcement
- Rate limiting (token bucket algorithm)
- API versioning (`/api/v1`, `/api/v2`)

**Technology Stack**: ASP.NET Core Web API, Swagger/OpenAPI

**Implementation Status**: Wave 2-4 (iterative rollout)

---

### 6. AI Orchestration (AutoGen)

**Description**: Multi-agent AI system for intelligent request processing and decision-making.

**Key Capabilities**:
- Request routing to specialized AI agents
- Multi-agent orchestration (planning, execution, review)
- Context maintenance across agent interactions
- LLM integration (OpenAI, Azure OpenAI)
- Audit trail logging for all AI decisions
- Python-based agent framework

**Agent Types**:
- **Request Classifier**: Routes requests to appropriate handlers
- **Inventory Specialist**: Analyzes stock levels and recommends actions
- **Order Processor**: Validates and processes order requests
- **Response Generator**: Crafts natural language responses

**Technology Stack**: Microsoft AutoGen, Python 3.10+, OpenAI API

**Implementation Status**: Wave 5 (future enhancement)

---

### 7. Mobile Applications

**Description**: Native mobile apps for iOS/Android enabling offline request submission and sync.

**Key Capabilities**:
- Responsive UI (320px - 1920px viewport)
- Offline mode with local request queuing
- Automatic sync when connectivity restored
- OAuth2 authentication with mobile tokens
- Parts catalog browsing with filtering
- Real-time status updates via WebSocket
- PWA support for web-based offline capability

**Technology Stack**: React Native/Flutter, TypeScript, IndexedDB

**Implementation Status**: Wave 5 (future enhancement)

---

### 8. Security & Authentication

**Description**: Enterprise-grade security with OAuth2 authentication and role-based access control.

**Key Capabilities**:
- **OAuth2 Authentication**: Azure AD, Google, Auth0
- **Role-Based Access Control (RBAC)**: 5+ roles (Admin, Manager, Technician, Viewer, Guest)
- **Encrypted Transmission**: HTTPS/WSS for all communications
- **Request Payload Encryption**: At-rest encryption in cloud storage
- **Audit Logging**: Immutable logs for all data access
- **Rate Limiting**: Prevent abuse (100 req/min per user)

**Technology Stack**: ASP.NET Core Identity, JWT Bearer tokens

**Implementation Status**: Wave 4 (in progress)

---

### 9. Caching & Performance

**Description**: Multi-tier caching strategy for sub-100ms response times.

**Caching Layers**:
- **L1 - Memory Cache**: In-process .NET MemoryCache (5-10ms)
- **L2 - Redis**: Distributed cache for session state (10-50ms)
- **L3 - PostgreSQL**: Primary data store (50-200ms)

**Key Capabilities**:
- Cache-aside pattern with write-through
- Automatic cache invalidation on data changes
- Session state management in Redis
- Connection pooling for database access
- Horizontal API scaling (3-100+ pods)

**Technology Stack**: Redis, .NET MemoryCache, Npgsql connection pooling

**Implementation Status**: Wave 4-5 (planned)

---

### 10. Testing Framework

**Description**: Comprehensive automated testing with 80%+ code coverage target.

**Testing Types**:
- **Unit Tests**: xUnit for all business logic
- **Integration Tests**: Full API endpoint testing with in-memory database
- **E2E Tests**: Playwright/Selenium for web UI validation
- **Load Tests**: k6/JMeter for performance benchmarking

**Key Capabilities**:
- WebApplicationFactory for integration test hosting
- In-memory database for isolated test execution
- Mocked external dependencies (cloud providers, AI agents)
- CI/CD pipeline integration (GitHub Actions)

**Technology Stack**: xUnit, Moq, WebApplicationFactory, Playwright

**Implementation Status**: Foundation complete (Wave 2)

---

## Non-Functional Requirements

### Performance Targets
- Request processing: <500ms (95th percentile)
- WebSocket message delivery: <100ms
- Cloud storage upload: <2 seconds
- AI processing: <5 minutes for complex requests
- Database query: <50ms for indexed lookups

### Scalability Goals
- Concurrent device connections: 100-10,000+
- Request throughput: 100-10,000 req/sec
- Database: Horizontal scaling via read replicas and sharding
- API: Stateless design for load balancing across 3-100 pods

### Reliability Standards
- System uptime: 99.9% SLA
- Request delivery: At-least-once semantics
- Automatic retry: 3 attempts with exponential backoff
- Data durability: Multi-provider cloud replication

### Security Compliance
- OAuth2 standard authentication
- Encrypted transmission (HTTPS/WSS)
- Encrypted storage for sensitive data
- RBAC enforcement on all endpoints
- Audit logging for compliance

---

## Implementation Roadmap

### Wave 1: Foundation (COMPLETE ✅)
- Project structure setup
- .NET 9.0 SDK installation
- PostgreSQL database creation
- Basic API scaffolding

### Wave 2: Core API (IN PROGRESS 🔄)
- Users/Roles/Departments endpoints
- Parts catalog management
- Request submission endpoint
- Basic authentication

### Wave 3: Cloud Integration (PLANNED 📋)
- SharePoint provider implementation
- Google Drive provider implementation
- Request/response file synchronization
- Provider abstraction layer

### Wave 4: Advanced Features (IN PROGRESS 🔄)
- Inventory management V2 endpoints
- Audit logging with InventoryAuditLog table
- Response delivery system
- Enhanced RBAC

### Wave 5: Real-Time & AI (FUTURE 🔮)
- WebSocket/SignalR integration
- AutoGen AI orchestration
- Mobile app development
- Redis caching layer

### Wave 6: Production Readiness (FUTURE 🔮)
- Performance optimization
- Load testing
- Security hardening
- Production deployment

---

## Technology Stack Summary

### Backend
- **Language**: C# with .NET 9.0
- **Framework**: ASP.NET Core
- **ORM**: Entity Framework Core
- **Real-Time**: SignalR

### Database & Caching
- **Primary DB**: PostgreSQL 15+
- **Cache**: Redis
- **ORM**: Npgsql, EF Core

### Cloud Integration
- **SharePoint**: Microsoft Graph API
- **Google Drive**: Google Drive API
- **Auth**: OAuth2, Azure AD

### Frontend (Planned)
- **Web**: React.js/Vue.js
- **Mobile**: React Native/Flutter
- **Language**: TypeScript

### AI & Orchestration (Planned)
- **Framework**: Microsoft AutoGen
- **Language**: Python 3.10+
- **LLM**: OpenAI, Azure OpenAI

### DevOps
- **Containers**: Docker
- **Orchestration**: Kubernetes (optional)
- **CI/CD**: GitHub Actions
- **Testing**: xUnit, Playwright

---

## Project Context

**Repository Structure**:
```
AAA Weird App Demmo/
├── server/
│   ├── CloudWatcher/          # Main .NET API server
│   ├── CloudWatcher.Tests/    # xUnit test project
│   └── database/              # Migration scripts
├── device/
│   └── python/                # Device client (Python)
├── Cloud/                     # Cloud storage integration
├── Docs/Plan/                 # Architecture documentation (21 files)
├── _ZENTASKS/                 # Task management (57 tasks)
└── .github/agents/            # Copilot agents for workflow
```

**Documentation Files** (21 architecture documents in `Docs/Plan/`):
- 01-15: Architecture design (requirements, components, C4 diagrams)
- 16: Database schema (910 lines, 20+ tables)
- 17: API endpoints (1005 lines, 25+ endpoints)
- 18: Unit testing framework
- 19: Implementation roadmap (this document)

**Current Status** (as of 2025-01-21):
- 57 tasks tracked in Zen Tasks system
- Foundation complete (DB schema, test infrastructure)
- Wave 2-4 API endpoints in development
- 7 done, 4 in-progress, 46 pending tasks

---

## Quick Reference

**To start development**:
1. Run `setup.bat` - Installs .NET SDK and PostgreSQL
2. Run `dotnet ef database update` - Applies migrations
3. Run `dotnet run` - Starts API server on http://localhost:5000
4. Navigate to http://localhost:5000/swagger - View API documentation

**To run tests**:
```bash
cd server/CloudWatcher.Tests
dotnet test
```

**To view tasks**:
- Check `_ZENTASKS/tasks.json` for all 57 tracked tasks
- Use Full Auto agent in `.github/agents/Full Auto New.agent.md`

**For detailed architecture**:
- See `Docs/Plan/15_ARCHITECTURE_COMPLETE_SUMMARY.md` (346 lines)
- See `Docs/Plan/19_IMPLEMENTATION_ROADMAP.md` (593 lines)
