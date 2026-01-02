# WeirdToo Parts System - Requirements Document

**Project:** CloudWatcher Parts Management System MVP  
**Last Updated:** December 25, 2025  
**Task Management:** Zen Tasks for Copilot Extension

---

## Project Overview

CloudWatcher is a comprehensive parts management system designed for field operations with offline-first mobile capabilities, AI-powered request routing, and multi-cloud storage integration. The system enables technicians to request parts via mobile devices, with intelligent approval workflows and real-time inventory tracking.

### Key Features

- **Server File Watcher**: Automated request/response file processing
- **PostgreSQL Database**: Complete relational schema with RBAC and audit trails
- **Mobile Apps**: React/Vue.js PWA with offline SQLite caching
- **AI Orchestration**: Microsoft Autogen agents for intelligent routing
- **Cloud Storage**: Multi-provider integration (SharePoint, Google Drive, Azure, S3)

---

## Technical Stack

### Backend
- **.NET 9.0**: Server API with file watching
- **PostgreSQL 14+**: Primary database with EF Core migrations
- **Entity Framework Core**: ORM and migrations

### Frontend
- **React/Vue.js**: Mobile-first Progressive Web App
- **SQLite**: Client-side offline cache
- **OAuth2**: Authentication (Google, Microsoft)

### AI & Orchestration  
- **Microsoft Autogen**: Multi-agent orchestration framework
- **RequestRouter**: Intelligent approval routing
- **PartsSpecialist**: Part variant selection
- **SupplierMatcher**: Optimal supplier selection
- **OrderGenerator**: Purchase order consolidation

### Cloud Integration
- **SharePoint**: Document storage
- **Google Drive**: Cloud file storage
- **Azure Blob Storage**: Cloud archive
- **AWS S3**: Optional cloud storage

---

## High-Priority Tasks (Priority: High)

### Server & File Processing (7 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv87l8-6olcz | Initialize server project with file watching dependencies | 7 | Pending |
| TASK-mjlv87ox-60yom | Build file watcher with event-driven request detection | 8 | Pending |
| TASK-mjlv87uc-z503h | Implement request file JSON schema validation | 6 | Pending |
| TASK-mjlv87yv-szq7t | Create request processor with type-based routing logic | 9 | Pending |
| TASK-mjlv8lm8-4ksf3 | Add file naming convention validation and collision detection | 7 | Pending |

**Tags:** `server`, `file-processing`, `validation`, `event-driven`

### Database Schema (5 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv8lq6-q8ali | Install and configure PostgreSQL 14+ database | 8 | Pending |
| TASK-mjlv8lua-1hyd5 | Create user, role, and department schema tables | 9 | Pending |
| TASK-mjlv8lyc-s3uah | Create parts hierarchy and variant schema tables | 10 | Pending |
| TASK-mjlv94uw-3evzw | Create supplier, brand, and availability schema tables | 8 | Pending |
| TASK-mjlv94ym-ckodj | Create inventory and location schema tables | 9 | Pending |

**Tags:** `database`, `postgresql`, `schema`, `migrations`

### Mobile App Core (3 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv95fk-645sb | Initialize mobile app project with React/Vue.js and PWA | 8 | Pending |
| TASK-mjlv95jv-2x0ai | Implement OAuth2 authentication with Google and Microsoft | 9 | Pending |
| TASK-mjlv95pf-gvzog | Set up SQLite local caching database | 8 | Pending |
| TASK-mjlvcert-vwyft | Build smart polling service with variable intervals | 9 | Pending |
| TASK-mjlvcewk-i9uhw | Create request file generator with offline queue | 7 | Pending |

**Tags:** `mobile`, `pwa`, `react`, `vue`, `offline`, `oauth2`

### AI Orchestration (1 task)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlvcfsy-53zmm | Implement RequestRouter agent with approval routing logic | 10 | Pending |

**Tags:** `ai`, `autogen`, `agents`, `routing`

### Cloud Storage (3 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlvcst9-m5syf | Configure OAuth2 credentials for SharePoint and Google Drive | 7 | Pending |
| TASK-mjlvcsyq-5euz3 | Create cloud folder structure with proper permissions | 8 | Pending |
| TASK-mjlvct50-0n5jh | Build cloud storage abstraction layer for multi-provider support | 8 | Pending |

**Tags:** `cloud`, `sharepoint`, `google-drive`, `azure`, `s3`

---

## Medium-Priority Tasks (Priority: Medium)

### File Processing (2 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv882i-9qg8c | Build response file generator with atomic writes | 5 | Pending |
| TASK-mjlv8lik-phtci | Implement 90-day archival and cleanup service | 6 | Pending |

**Tags:** `server`, `archival`, `file-processing`

### Database (2 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv952v-75mgk | Create parts list and order workflow schema tables | 8 | Pending |
| TASK-mjlv9578-4jxvp | Configure database migration framework and versioning | 6 | Pending |

**Tags:** `database`, `migrations`, `ef-core`

### Mobile UI (3 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlvcf22-xzveq | Build parts catalog browser with category navigation | 8 | Pending |
| TASK-mjlvcf9h-387d0 | Create inventory management and consumption tracking interface | 8 | Pending |
| TASK-mjlvcfgk-rwf29 | Build parts list creation and approval workflow interface | 9 | Pending |

**Tags:** `mobile`, `ui`, `inventory`, `parts`, `workflow`

### AI Agents (4 tasks)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlvcfm6-qylj0 | Install and configure Microsoft Autogen framework | 9 | Pending |
| TASK-mjlvcg0h-4kc1r | Create PartsSpecialist agent for part variant selection | 8 | Pending |
| TASK-mjlvcsif-b09l1 | Create SupplierMatcher agent for optimal supplier selection | 8 | Pending |
| TASK-mjlvcsnv-jy0hv | Create OrderGenerator agent for PO consolidation | 9 | Pending |

**Tags:** `ai`, `autogen`, `agents`, `optimization`

### Documentation (1 task)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv491o-831c9 | Document PostgreSQL Setup Prerequisites and Troubleshooting | 3 | Pending |

**Tags:** `documentation`, `postgresql`, `setup`

---

## Low-Priority Tasks (Priority: Low)

### Database Optimization (1 task)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv95b8-08483 | Implement comprehensive database indexing strategy | 7 | Pending |

**Tags:** `database`, `performance`, `indexes`

### Data Quality (1 task)

| Task ID | Title | Complexity | Status |
|---------|-------|------------|--------|
| TASK-mjlv48ve-uzzsi | Align SQL Seed Data with C# Seeder Incoming Units | 3 | Pending |

**Tags:** `testing`, `data`, `seed`

---

## Task Categories & Tags

### By Functional Area

- **Server & Backend**: 9 tasks (`server`, `file-processing`, `validation`, `event-driven`, `archival`)
- **Database**: 9 tasks (`database`, `postgresql`, `schema`, `migrations`, `ef-core`, `performance`)
- **Mobile & Frontend**: 8 tasks (`mobile`, `pwa`, `react`, `vue`, `offline`, `ui`, `oauth2`)
- **AI & Orchestration**: 5 tasks (`ai`, `autogen`, `agents`, `routing`, `optimization`)
- **Cloud Integration**: 3 tasks (`cloud`, `sharepoint`, `google-drive`, `azure`, `s3`)
- **Testing & Documentation**: 2 tasks (`testing`, `documentation`, `setup`, `data`)

### By Complexity

- **Simple (1-2)**: 0 tasks
- **Moderate (3-5)**: 3 tasks (documentation, seed data, archival)
- **Complex (6-8)**: 20 tasks (most implementation tasks)
- **Very Complex (9-10)**: 7 tasks (core architecture, AI agents, database design)

### By Status

- **Pending**: 30 tasks
- **In Progress**: 0 tasks
- **Completed**: Multiple completed (see _ZENTASKS folder)

---

## Success Criteria

### MVP Completion Criteria

✅ **Phase 1: Foundation (Database & Server)**
- PostgreSQL database installed and configured
- Complete schema with all tables and relationships
- File watcher service operational
- Request/response file processing working

✅ **Phase 2: Mobile App**
- PWA deployed and installable
- OAuth2 authentication functional
- Offline mode with SQLite caching
- Request submission working

✅ **Phase 3: AI Orchestration**
- Autogen framework integrated
- RequestRouter operational
- Intelligent approval routing working

✅ **Phase 4: Cloud Integration**
- Multi-cloud storage abstraction complete
- SharePoint/Google Drive integration working
- File archival to cloud operational

### Quality Gates

- All high-priority tasks completed
- Integration tests passing
- Security audit passed (OAuth2, RBAC)
- Performance benchmarks met (<100ms for common queries)
- Offline mode thoroughly tested

---

## Quick Start Guide

### Using Zen Tasks

All tasks are managed through the Zen Tasks for Copilot extension:

**View Tasks:**
```
Ask agent to load workflow context first
/list_tasks - View all tasks
/list_tasks status=pending priority=high - Filter tasks
```

**Get Next Task:**
```
/next_task - Get next executable task based on dependencies
```

**Update Status:**
```
/set_status taskId="TASK-xxx" status="in-progress"
/set_status taskId="TASK-xxx" status="done"
```

**View Details:**
```
/get_task taskId="TASK-xxx" - Full task details
```

### Agent Workflow

1. **Full Auto**: Central hub - displays task queue and routes to specialists
2. **Smart Plan**: Break down complex tasks into subtasks
3. **Smart Execute**: Run tasks and update status
4. **Smart Review**: Analyze results and recommend next steps

---

## Dependencies & Prerequisites

### Development Environment

- **VS Code** with extensions:
  - GitHub Copilot
  - GitHub Copilot Chat  
  - Zen Tasks for Copilot
  - TaskSync Chat (optional)

- **.NET SDK**: 9.0 (installed)
- **PostgreSQL**: 14+ (to be installed)
- **Node.js**: 18+ (for mobile app)
- **Python**: 3.11+ (for Autogen agents)

### Cloud Accounts

- Azure AD (for OAuth2 and SharePoint)
- Google Cloud (for OAuth2 and Google Drive)
- AWS Account (optional, for S3)

---

## Project File Structure

```
AAA Weird App Demmo/
├── server/
│   └── CloudWatcher/          # .NET 9.0 server
│       ├── Program.cs
│       ├── Controllers/
│       ├── Models/
│       └── Services/
├── device/
│   └── python/                # Device client (legacy)
├── mobile/                    # React/Vue PWA (to be created)
├── Cloud/                     # Cloud integration
├── Docs/
│   ├── Plan/                  # Architecture docs
│   └── TODO/                  # Legacy task tracking
├── _ZENTASKS/                 # Zen Tasks storage (gitignored)
└── .github/
    └── agents/                # Copilot agents
```

---

## Notes for Developers

1. **Always load workflow context first** when using Zen Tasks tools
2. **Use dependency-driven development** - check task dependencies before starting
3. **Update task status** as you progress through implementation
4. **Log observations** for both successes and failures
5. **Use Full Auto hub** for workflow coordination, not direct execution

---

**For Support:** Refer to Zen Tasks extension documentation or copilot-instructions.md
