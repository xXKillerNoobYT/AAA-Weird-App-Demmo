# Cloud/Archive Folder

## Purpose

This folder stores historical request/response pairs and archived specification documents. It serves as a long-term audit trail for compliance, troubleshooting, and business intelligence.

## Archival Strategy

### Automatic Archival

Files are automatically moved from `/Cloud/Requests/` and `/Cloud/Responses/` to Archive after:
- **Age:** >30 days old
- **Status:** Processing complete (success or error)
- **Size:** Total folder size >1 GB (triggers cleanup)

### Manual Archival

Periodically (monthly):
1. Move old requests/responses to Archive
2. Compress files for long-term storage
3. Verify audit logs are complete
4. Update index for searchability

## Folder Structure

```
Cloud/Archive/
├── 2025/
│   ├── 2025-12/           # Year-month folders
│   │   ├── requests/      # All requests from December 2025
│   │   ├── responses/     # All responses from December 2025
│   │   └── index.json     # Index of this month's files
│   ├── 2025-11/
│   │   ├── requests/
│   │   ├── responses/
│   │   └── index.json
│   └── catalog/           # Old specification sheets
├── 2024/
│   └── [Similar structure]
└── index-all.json         # Master archive index
```

## File Organization

### Request/Response Archives

Organized by year-month to support date-range queries:

```
Cloud/Archive/2025/2025-12/
├── requests/
│   ├── truck-001/
│   │   ├── req-ping-2025-12-01T08-00-00.json
│   │   ├── req-get_parts-2025-12-02T14-30-00.json
│   │   └── req-update_inventory-2025-12-03T10-15-00.json
│   ├── truck-002/
│   │   └── req-echo_test-2025-12-04T16-45-00.json
│   └── device-mobile-001/
│       └── req-ping-2025-12-05T09-30-00.json
├── responses/
│   ├── truck-001/
│   │   ├── resp-ping-2025-12-01T08-00-00.json
│   │   ├── resp-get_parts-2025-12-02T14-30-00.json
│   │   └── resp-update_inventory-2025-12-03T10-15-00.json
│   ├── truck-002/
│   │   └── resp-echo_test-2025-12-04T16-45-00.json
│   └── device-mobile-001/
│       └── resp-ping-2025-12-05T09-30-00.json
└── index.json
```

### Specification Archives

Old versions of catalogs and technical specs:

```
Cloud/Archive/catalog/
├── 2024/
│   ├── BoltCo-FastenerCatalog-2024-12.pdf
│   ├── SteelSupply-BeamSpecs-2024-09.pdf
│   └── parts-specs-2024-12-backup.zip
└── 2023/
    └── [Old catalogs]
```

## Monthly Index Format

Each month includes an `index.json` for quick lookups:

```json
{
  "archive_month": "2025-12",
  "archive_date": "2025-12-31T23:59:59Z",
  "summary": {
    "total_requests": 245,
    "total_responses": 245,
    "success_count": 242,
    "error_count": 3,
    "unique_devices": 5,
    "date_range": {
      "start": "2025-12-01T00:00:00Z",
      "end": "2025-12-31T23:59:59Z"
    }
  },
  "requests_by_device": {
    "truck-001": {
      "count": 120,
      "file_path": "requests/truck-001/"
    },
    "truck-002": {
      "count": 85,
      "file_path": "requests/truck-002/"
    },
    "device-mobile-001": {
      "count": 40,
      "file_path": "requests/device-mobile-001/"
    }
  },
  "request_types": {
    "ping": 85,
    "get_parts": 95,
    "update_inventory": 45,
    "echo_test": 15,
    "get_inventory": 5
  },
  "errors": [
    {
      "date": "2025-12-15T14:30:00Z",
      "device": "truck-002",
      "error_code": "invalid_schema",
      "file_path": "requests/truck-002/req-get_parts-2025-12-15T14-30-00.json"
    }
  ],
  "compression": {
    "compressed_filename": "2025-12-archive.zip",
    "size_bytes": 2458752,
    "compression_ratio": 0.65
  }
}
```

## Master Archive Index

The `index-all.json` provides quick access to all months:

```json
{
  "archive_version": "1.0",
  "last_updated": "2025-12-24T22:30:00Z",
  "total_months": 3,
  "months": [
    {
      "archive_month": "2025-12",
      "path": "2025/2025-12",
      "requests": 245,
      "responses": 245,
      "index_file": "2025/2025-12/index.json"
    },
    {
      "archive_month": "2025-11",
      "path": "2025/2025-11",
      "requests": 312,
      "responses": 312,
      "index_file": "2025/2025-11/index.json"
    },
    {
      "archive_month": "2025-10",
      "path": "2025/2025-10",
      "requests": 287,
      "responses": 287,
      "index_file": "2025/2025-10/index.json"
    }
  ],
  "storage_summary": {
    "total_files": 1088,
    "total_size_bytes": 156847232,
    "total_size_gb": 0.156,
    "oldest_file": "2025-10-01T00:00:00Z",
    "newest_file": "2025-12-31T23:59:59Z"
  },
  "compliance": {
    "retention_years": 7,
    "retention_until": "2033-12-31T23:59:59Z",
    "audit_certified": true,
    "last_audit_date": "2025-12-20T10:00:00Z"
  }
}
```

## Retention Policy

### Legal Requirements
- **Minimum retention:** 7 years (business/compliance requirement)
- **Deletion:** Only after 7-year retention period expires
- **Encryption:** All archived data must be encrypted at rest
- **Access logging:** All access to archives must be logged

### Archive Lifecycle

```
Day 1-30:   In active folders (/Cloud/Requests, /Cloud/Responses)
Day 31:     Move to Archive with current month folder
Day 30-365: Store in monthly folders for quick access
1-7 years:  Keep in Archive folder (compressed)
7+ years:   Can be securely deleted (per legal requirements)
```

## Compression

Archives >1 month old should be compressed:

```bash
# Monthly compression (run on 1st day of next month)
Compress /Cloud/Archive/2025/2025-11/ → 2025-11-archive.zip
Keep index.json uncompressed for searchability
```

### Compression Format

- **Format:** ZIP with password protection
- **Encryption:** AES-256
- **Password:** Stored in secure credential manager
- **Size:** Typically 60-70% of original (good compression ratio)

## Backup & Disaster Recovery

Archives should be:
1. **Backed up** to secondary cloud storage daily
2. **Replicated** to geographic backup region weekly
3. **Tested** for recovery quarterly (restore random month, verify integrity)
4. **Audited** annually for compliance

## Access Control

Archive access should be restricted to:
- **Data Administrators:** Full access for maintenance
- **Compliance Officers:** Read-only access for audits
- **Legal Team:** Read-only access for litigation holds
- **Systems:** Automated read access for reporting

No user-facing access to archives; use dashboards/reports instead.

## Cleanup Procedures

### Monthly (1st of month)
```bash
# 1. Move requests/responses >30 days old to Archive
# 2. Create index.json for previous month
# 3. Compress previous month's data
# 4. Verify index integrity
# 5. Update master index-all.json
```

### Quarterly
```bash
# 1. Verify backup integrity
# 2. Test recovery of random archive
# 3. Update retention policy if needed
# 4. Generate compliance report
```

### Annually
```bash
# 1. Full archive audit
# 2. Review and delete 7+ year old archives
# 3. Update disaster recovery plan
# 4. Certify compliance with retention policy
```

## Searching Archives

Use the monthly indexes to find specific requests/responses:

```bash
# Find all requests from truck-001 in December 2025
cd Cloud/Archive/2025/2025-12/requests/truck-001/
ls req-*

# Find specific error
grep -r "invalid_schema" Cloud/Archive/2025/2025-12/

# Find requests in date range
find Cloud/Archive/2025/ -name "*2025-12-15*"
```

## Cloud Storage Sync

This folder is synced to cold storage:
- **Primary:** SharePoint cold storage tier (infrequent access)
- **Secondary:** Google Drive Archive (business tier)

Files are synced daily but moved to cold storage after 90 days (cost optimization).

## See Also

- `/Cloud/Requests/` - Active request upload folder
- `/Cloud/Responses/` - Active response folder
- `/Cloud/SpecSheets/` - Current specification documents
- Database audit tables for additional compliance data
