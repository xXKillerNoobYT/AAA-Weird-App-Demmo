# WeirdToo Parts System – Backlog

This backlog lists executable tasks organized by feature. Prioritize high first; maintain progress in MPC.

Project: WeirdToo Parts System - MVP Implementation (ID: 19c890be-070f-4d4c-afcb-fdded65de71d)

## Server File Watcher & Request Processor (7)

- Initialize server project with file watching dependencies [high, 7]
- Build file watcher with event-driven request detection [high, 8]
- Implement request file JSON schema validation [high, 6]
- Create request processor with type-based routing logic [high, 9]
- Build response file generator with atomic writes [medium, 5]
- Implement 90-day archival and cleanup service [medium, 6]
- Add file naming convention validation and collision detection [high, 7]

## PostgreSQL Database Schema & Migrations (8)

- Install and configure PostgreSQL 14+ database [high, 8]
- Create user, role, and department schema tables [high, 9]
- Create parts hierarchy and variant schema tables [high, 10]
- Create supplier, brand, and availability schema tables [high, 8]
- Create inventory and location schema tables [high, 9]
- Create parts list and order workflow schema tables [medium, 8]
- Configure database migration framework and versioning [medium, 6]
- Implement comprehensive database indexing strategy [low, 7]

## Mobile Device Apps (React/Vue.js) (8)

- Initialize mobile app project with React/Vue.js and PWA [high, 8]
- Implement OAuth2 authentication with Google and Microsoft [high, 9]
- Set up SQLite local caching database [high, 8]
- Build smart polling service with variable intervals [high, 9]
- Create request file generator with offline queue [high, 7]
- Build parts catalog browser with category navigation [medium, 8]
- Create inventory management and consumption tracking interface [medium, 8]
- Build parts list creation and approval workflow interface [medium, 9]

## AI Orchestration (Microsoft Autogen) (5)

- Install and configure Microsoft Autogen framework [medium, 9]
- Implement RequestRouter agent with approval routing logic [high, 10]
- Create PartsSpecialist agent for part variant selection [medium, 8]
- Create SupplierMatcher agent for optimal supplier selection [medium, 8]
- Create OrderGenerator agent for PO consolidation [medium, 9]

## Cloud Storage Integration (3)

- Configure OAuth2 credentials for SharePoint and Google Drive [high, 7]
- Create cloud folder structure with proper permissions [high, 8]
- Build cloud storage abstraction layer for multi-provider support [high, 8]

---

## Execution Notes

- Update statuses in MPC as tasks progress
- Log observations for successes/failures
- Continue even after failures; Smart Review will analyze issues

## Next Up

- Start with Server File Watcher tasks (High priority)
- Then Database schema tasks to unblock downstream work
