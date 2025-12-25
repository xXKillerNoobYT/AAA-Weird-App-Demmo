# WeirdToo Parts System – Requirements

## Overview

This document consolidates implementation requirements for the WeirdToo Parts System to enable easy task selection, clear accountability, and traceability from plan to execution.

## Purpose

- Provide a single source of truth for what must be built
- Organize requirements by features and workflows
- Align developers and AI assistants on scope and acceptance criteria

## Scope

Full-stack coverage:

- Server File Watcher & Request Processor
- PostgreSQL Database
- Mobile Device Apps (React/Vue.js)
- AI Orchestration (Microsoft Autogen)
- Cloud Storage Integration (SharePoint/Google Drive)

## Audience

- Developers (task selection and acceptance criteria)
- AI assistants (orchestration and automation)
- Project managers (status and prioritization)

## Cross-References

- 01_SYSTEM_ARCHITECTURE.md
- 02_DESIGN_DOCUMENT.md
- 03_TECHNICAL_SPECIFICATION.md
- 04_WORKFLOW_DIAGRAMS.md
- SYSTEM_DECISIONS_LOG.md

## Implementation Features and Requirements

### 1. Server File Watcher & Request Processor

- Watch local cloud-synced folders for new request JSON files (creation events)
- Validate request file schema and naming convention
- Route requests by type to processors (get_parts, update_inventory, etc.)
- Generate response JSON files atomically in device-specific folders
- Implement 90-day archival and 6-month archive deletion policy
- Log all events and failures with timestamps

Acceptance Criteria:

- New file detection occurs within ~1s of local sync
- Invalid schema produces detailed error responses
- All writes are atomic (temp + rename) to avoid partial files
- Archival jobs run daily at 2:00 AM server time

### 2. PostgreSQL Database Schema & Migrations

- Create core entity tables: users, roles, departments
- Parts hierarchy (5+ levels), measurement units, parts, variants
- Supplier-brand-part relationships and availability
- Inventory: warehouses, trucks, job sites, and audit triggers
- Parts lists and supplier orders with approval states
- Migration framework (versioned) and indexing strategy

Acceptance Criteria:

- UUID primary keys; FKs enforced; essential indexes present
- JSONB used where specified; GIN indexes on searchable JSONB fields
- Baseline migration creates full schema reproducibly

### 3. Mobile Device Apps (React/Vue.js)

- PWA app with retro CRT theme (black+green)
- OAuth2 auth (Google & Microsoft)
- SQLite local cache mirroring key server schema
- Smart polling (2min/1hr/12hr) + offline request queue
- Parts catalog browser with hierarchical navigation
- Inventory management UI and parts list creation workflow

Acceptance Criteria:

- Auth succeeds with both providers and refresh logic works
- Offline mode queues requests and syncs successfully when online
- Catalog usable offline; inventory updates reflect responses

### 4. AI Orchestration (Microsoft Autogen)

- RequestRouter agent (role/department/cost-based approval routing)
- PartsSpecialist agent (variant selection with construction heuristics)
- SupplierMatcher agent (optimal supplier by cost/lead time/availability)
- OrderGenerator agent (PO consolidation across projects)

Acceptance Criteria:

- Routing decisions logged with rationale and approver targets
- Variant recommendations include spec links and tradeoffs
- Supplier selection accounts for bulk discounts and delivery windows

### 5. Cloud Storage Integration

- OAuth2 credentials configured for SharePoint and Drive
- Folder structure standardized (/Cloud/Requests, /Responses, /SpecSheets, /Archive)
- Multi-provider abstraction: upload, download, list, delete
- No direct cloud polling by server; relies on local sync + watcher

Acceptance Criteria:

- Devices have scoped access (own subfolders) and can read/write
- Server reads Requests and writes Responses across devices
- Provider swap configurable via environment settings

## Traceability

- MPC Project: WeirdToo Parts System - MVP Implementation (ID: 19c890be-070f-4d4c-afcb-fdded65de71d)
- Features: 5 total; Tasks: 31 total

## Definition of Done

- All feature acceptance criteria met
- Tasks completed and logged in MPC
- Cross-references updated; workflows validated end-to-end
