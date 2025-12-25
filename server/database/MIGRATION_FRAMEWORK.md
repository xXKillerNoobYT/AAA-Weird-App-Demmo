# Database Migration Framework

## Overview

The WeirdToo Parts System uses a versioned migration approach for PostgreSQL schema management. All migrations are numbered sequentially and tracked in the `schema_versions` table.

## Migration Files

### V1: Initial Schema (setup.sql)

- **Created:** 2025-12-24
- **Status:** Foundation - Ready for PostgreSQL deployment
- **Tables:** 25 core tables covering users, roles, departments, parts hierarchy, suppliers, inventory, jobs, orders, and audit logging
- **Indexes:** 25+ indexes for query optimization
- **Features:**
  - UUID primary keys
  - JSONB columns for flexible specifications and pricing
  - Automatic updated_at timestamp triggers
  - Audit logging for compliance
  - GIN indexes on JSONB for full-text search
  - Schema versioning table for migration tracking

### V2: Placeholder (migrations/002_*.sql)

- Status: TBD - Reserved for future enhancements

## Deployment Instructions

### Prerequisites

- PostgreSQL 14+
- psql CLI tool
- Environment variable: POSTGRES_PASSWORD

### Deploy Initial Schema

```bash
cd server/database
psql -h localhost -U postgres -d weirdtoo_parts -f setup.sql
```

### Check Migration Status

```bash
SELECT * FROM app.schema_versions ORDER BY version_id DESC;
```

### Rollback (if needed)

```bash
-- Drop entire schema (WARNING: deletes all data)
DROP SCHEMA IF EXISTS app CASCADE;
```

## Migration Naming Convention

- Filename: `{version_id:03d}_{description}.sql`
- Example: `001_initial_schema.sql`, `002_add_return_tracking.sql`
- Version tracking: Automatic via `schema_versions` table

## Key Design Decisions

1. **Single File for V1:** Initial schema contains all core tables (simplified deployment)
2. **Future Migrations:** Each enhancement gets its own numbered SQL file
3. **No Down Migrations:** Rollback requires dropping schema; focus on forward compatibility
4. **Audit Trail:** All changes logged in `audit_log` table with user/timestamp/action

## Next Steps

1. Deploy setup.sql to PostgreSQL instance
2. Test connection and verify schema creation
3. Create migration tracking mechanism in CI/CD
4. Begin adding application layers (Node.js/Python API)
