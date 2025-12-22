# WeirdToo Parts System - Design Decisions Log

**Document Version**: 1.0  
**Last Updated**: December 21, 2025  
**Status**: Active Design Reference

This document captures all major design decisions made during the system architecture phase. It serves as the authoritative reference for implementation and provides context for future maintenance.

---

## TABLE OF CONTENTS

1. [Platform & Deployment Decisions](#platform--deployment-decisions)
2. [Data Retention & Cleanup Policies](#data-retention--cleanup-policies)
3. [Conflict Resolution Strategy](#conflict-resolution-strategy)
4. [AI Organization & Request Routing](#ai-organization--request-routing)
5. [Parts Management & SKU Strategy](#parts-management--sku-strategy)
6. [Authentication & Authorization](#authentication--authorization)
7. [User Roles & Permission System](#user-roles--permission-system)
8. [Edit Permission Rules](#edit-permission-rules)
9. [Approval Workflow Customization](#approval-workflow-customization)
10. [Department Structure](#department-structure)
11. [Implementation Roadmap](#implementation-roadmap)
12. [Verification Decisions (Traceability)](#verification-decisions-traceability)
13. [Appendix: Full Answer References](#appendix-full-answer-references)

---

## PLATFORM & DEPLOYMENT DECISIONS

### Target Platform
**Decision**: Mobile devices only  
**Rationale**: Construction environment requires portable access at job sites, warehouses, and trucks  
**Impact**: Optimizes UI/UX for mobile form factors, allows offline-first architecture

### Cloud Storage Provider Strategy

**Primary (Production)**: SharePoint  
- Industry standard for enterprise construction companies
- Robust authentication and compliance features
- Existing infrastructure integration

**Secondary (Beta Testing)**: Google Drive  
- Testing alternative providers for flexibility
- Lower cost option for smaller operations
- Familiar interface for non-enterprise users

**Tertiary**: OneDrive  
- Microsoft ecosystem integration
- Fallback option if SharePoint unavailable
- Personal device support

**Beta Testing Approach**:
- Run parallel systems with subset of users
- Compare reliability, performance, user satisfaction
- Decision point after 90-day beta period
- No automatic migration - manual cutover if Drive selected

### Device Types & Locations

**Office Devices**: Desktop management interfaces (web-based mobile view)  
**Warehouse Devices**: Tablets and mobile devices for inventory management  
**Truck Devices**: Rugged mobile devices with offline capability  
**Job Site Devices**: Personal and company mobile devices with dust/water resistance

---

## DATA RETENTION & CLEANUP POLICIES

### Request Files (Cloud Storage)

**Retention Period**: 90 days from creation  
**Rationale**: Sufficient for debugging, audit trail, and pattern analysis  
**Cleanup Process**:
1. Automated script runs daily at 2:00 AM server time
2. Identifies request files older than 90 days
3. Moves to `/Cloud/Archive/` folder with timestamp
4. Archives compressed weekly into single `.zip` files
5. Archive files deleted after 6 months

**Example Cleanup**:
```
Original: /Cloud/Processed/a7f3k9m2_truck-005_john_smith_20250915143045.json
Archive: /Cloud/Archive/2025-09/week-37_processed-requests.zip
```

### Database Records (PostgreSQL)

**Retention Period**: 6 years (company policy + legal compliance)  
**Rationale**: Construction industry audit requirements, warranty tracking, legal disputes  
**Deletion Process**:
1. System identifies records older than 6 years
2. Admin receives notification list requiring review
3. Admin manually approves deletion batch
4. System creates permanent backup before deletion
5. Deletion executed with full audit trail

**Exception Cases**:
- Active legal holds: Indefinite retention
- Ongoing warranty claims: Retain until claim resolved
- Historical analysis projects: Custom retention extension

---

## CONFLICT RESOLUTION STRATEGY

### Single Write Authority Pattern

**Decision**: Server database is the only write authority  
**Rationale**: Eliminates race conditions, simplifies conflict resolution, ensures data consistency  
**Implementation**: All devices submit requests; server processes and updates database

### Request File Naming Convention

**Format**: `{RANDOM_NUMBER}_{DEVICE_NAME}_{USERNAME}_{TIMESTAMP_METADATA}.json`  
**Example**: `a7f3k9m2_truck-005_john_smith_20251221143045.json`

**Component Details**:
- **RANDOM_NUMBER**: 8-character alphanumeric (lowercase), collision probability: 1 in 2.8 trillion
- **DEVICE_NAME**: Registered device identifier (e.g., `truck-005`, `warehouse-02`, `jobsite-oak-street`)
- **USERNAME**: Sanitized username with underscores replacing spaces
- **TIMESTAMP**: YYYYMMDDHHmmss format in UTC

**Collision Prevention**:
- Random number ensures uniqueness even if device/user/timestamp identical
- Cloud storage providers reject duplicate filenames
- Server validates uniqueness before processing

### Request Processing Flow

1. **Device creates request file** → `/Cloud/Requests/{device-id}/`
2. **Cloud sync service** syncs to server's local watched folder
3. **Server detects new file** via file watch event (chokidar library)
4. **Server validates** JSON structure and authentication
5. **Server processes** request and updates database
6. **Server creates response file** → `/Cloud/Responses/{device-id}/`
7. **Cloud sync service** syncs response back to cloud
8. **Device detects response** via smart polling
9. **Device updates local SQLite cache**
10. **Server moves request** → `/Cloud/Processed/` for archival

**No Race Conditions Because**:
- Only server writes to PostgreSQL
- Each request file is unique and processed once
- Response files use same naming convention with `_response` suffix
- File system operations are atomic

---

## AI ORGANIZATION & REQUEST ROUTING

### Microsoft Autogen Framework

**Primary Agent**: RequestRouter  
**Function**: Analyzes incoming requests and routes to appropriate approval workflow  
**Input**: Request JSON file with user context, requested changes, affected records  
**Output**: Routing decision with assigned approvers and priority

### RequestRouter Logic (Pseudocode)

```python
class RequestRouter:
    def route_request(self, request_file):
        request = parse_json(request_file)
        user = get_user_details(request.user_id)
        
        # Determine approval requirements
        if user.role.requires_approval == False:
            return auto_approve(request)
        
        # Check edit permissions
        if request.type == "USER_EDIT":
            target_user = get_user_details(request.target_user_id)
            permission = check_edit_permission(user, target_user)
            
            if permission == "DENIED":
                return auto_reject(request, "Insufficient permissions")
            elif permission == "ALLOWED_NO_APPROVAL":
                return auto_approve(request)
            else:  # REQUIRES_APPROVAL
                approvers = get_approvers_for_user_edit(user, target_user)
                return route_to_approvers(request, approvers)
        
        # Standard part/inventory requests
        if request.estimated_cost > user.role.approval_threshold:
            approvers = get_hierarchical_approvers(user)
            return route_to_approvers(request, approvers)
        else:
            return auto_approve(request)
    
    def get_approvers_for_user_edit(self, requesting_user, target_user):
        # Find common manager up the tree
        requester_path = get_role_tree_path(requesting_user)
        target_path = get_role_tree_path(target_user)
        
        common_ancestor = find_common_ancestor(requester_path, target_path)
        return [common_ancestor]
    
    def get_hierarchical_approvers(self, user):
        # Walk up tree until finding role with approval authority
        current_role = user.role
        approvers = []
        
        while current_role.parent_role_id:
            parent_role = get_role(current_role.parent_role_id)
            if parent_role.can_approve_subordinate_requests:
                approvers.append(get_users_with_role(parent_role.role_id))
                break
            current_role = parent_role
        
        return approvers
```

### AI Task Prioritization

**Priority Levels**:
1. **CRITICAL**: Safety issues, job site emergencies, equipment failures
2. **HIGH**: Active job site requests, deadline-sensitive orders
3. **MEDIUM**: Standard inventory requests, routine updates
4. **LOW**: Historical data corrections, reporting requests

**AI Priority Assignment Factors**:
- Keywords in request description (emergency, urgent, broken, safety)
- Requesting user's role and location (job site > truck > warehouse > office)
- Time sensitivity (delivery deadlines, project schedules)
- Cost implications (high-value parts require faster approval)

---

## PARTS MANAGEMENT & SKU STRATEGY

### Brand vs Manufacturer Consolidation

**Decision**: Merge into single "BRAND" field  
**Rationale**: In construction, brand identity is what matters (Milwaukee, DeWalt, etc.)  
**Schema Impact**: PARTS table has `brand_name` field; BRANDS table for standardization

**Example**:
- Milwaukee Electric Tool Corporation → "Milwaukee"
- Stanley Black & Decker → "DeWalt" (brand that matters)
- Hilti Corporation → "Hilti"

### Internal SKU Strategy

**Decision**: System-generated internal SKU + cross-reference table for external numbering  
**Format**: `WTP-{CATEGORY}-{SEQUENCE}`  
**Example**: `WTP-ELEC-00001` (WeirdToo Parts, Electrical category, sequence 1)

**Cross-Reference Table Schema**:

```sql
CREATE TABLE PART_NUMBER_XREF (
    xref_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    part_id UUID NOT NULL REFERENCES PARTS(part_id),
    external_system VARCHAR(100) NOT NULL,  -- "QuickBooks", "Manufacturer", "Supplier_ACE"
    external_sku VARCHAR(100) NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES USERS(user_id),
    notes TEXT,
    UNIQUE(part_id, external_system, external_sku)
);

CREATE INDEX idx_xref_external_lookup ON PART_NUMBER_XREF(external_system, external_sku);
CREATE INDEX idx_xref_part_lookup ON PART_NUMBER_XREF(part_id);
```

**Usage Example**:
```json
{
  "internal_sku": "WTP-ELEC-00042",
  "cross_references": [
    {"system": "QuickBooks", "sku": "QB-12345", "is_primary": true},
    {"system": "Manufacturer", "sku": "MIL-48-22-8424", "is_primary": false},
    {"system": "Supplier_Grainger", "sku": "GR-2V891", "is_primary": false}
  ]
}
```

**Benefits**:
- Internal SKU remains stable regardless of external system changes
- Easy migration between accounting systems
- Support multiple supplier/manufacturer numbering systems
- Search works across all numbering systems

### Measurement Units

**Standardized Units**:
- **LENGTH**: inches, feet, yards, meters, millimeters
- **VOLUME**: gallons, liters, cubic feet, cubic yards
- **WEIGHT**: pounds, ounces, kilograms, grams
- **QUANTITY**: each, box, case, pallet, roll
- **AREA**: square feet, square meters, square yards

**Database Storage**: Store in standard unit (metric) + display unit preference per user/company

---

## AUTHENTICATION & AUTHORIZATION

### Authentication Method

**Decision**: OAuth2 via cloud storage provider APIs  
**Providers**:
- **Google OAuth2**: For Google Drive users
- **Microsoft OAuth2**: For SharePoint/OneDrive users

**Authentication Flow**:
1. User launches mobile app
2. App detects configured cloud storage (from company settings)
3. App initiates OAuth2 flow with appropriate provider
4. User authenticates with cloud provider credentials
5. App receives OAuth2 token with cloud storage access
6. Server validates token and retrieves user profile
7. Server checks user exists in USER table with matching email
8. Session established with JWT token for subsequent requests

**Token Refresh**:
- OAuth2 tokens refreshed automatically before expiration
- If refresh fails, user prompted to re-authenticate
- Session JWT tokens valid for 24 hours
- Offline mode uses last known authentication state

**Security Considerations**:
- No passwords stored in WeirdToo system
- Cloud provider handles MFA and security policies
- Server validates OAuth tokens with provider on each request
- User email from OAuth must match USER table email exactly

### User Registration Process

**New User Onboarding**:
1. Admin creates user account in WeirdToo system (USER table)
2. Admin assigns role, department, permissions
3. Admin provides user with company's cloud storage folder access
4. User launches app and authenticates via OAuth2
5. System matches OAuth email to USER record
6. User completes profile setup (display name, device registration)

**No Self-Registration**: All users must be pre-created by administrators to control access

---

## USER ROLES & PERMISSION SYSTEM

### Tree Hierarchy Design

**Decision**: Fully customizable tree-based role hierarchy  
**Rationale**: Different companies have different org structures; flexibility is essential

**Role Tree Structure**:

```
Level 0 (Admin)
├── Level 1 (Project Manager)
│   ├── Level 2 (Site Supervisor - Commercial)
│   │   └── Level 3 (Worker - Commercial)
│   └── Level 2 (Site Supervisor - Residential)
│       └── Level 3 (Worker - Residential)
└── Level 1 (Warehouse Manager)
    └── Level 2 (Warehouse Worker)
```

**Schema Design**:

```sql
CREATE TABLE USER_ROLES (
    role_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name VARCHAR(100) NOT NULL,
    role_level INT NOT NULL,  -- 0=Admin, 1=Manager, 2=Supervisor, 3=Worker, etc.
    parent_role_id UUID REFERENCES USER_ROLES(role_id),
    description TEXT,
    
    -- Permission Checkboxes (Customizable per Role)
    can_edit_same_level BOOLEAN DEFAULT FALSE,
    can_edit_different_tree BOOLEAN DEFAULT FALSE,
    can_edit_subordinates BOOLEAN DEFAULT TRUE,
    requires_approval BOOLEAN DEFAULT TRUE,
    can_approve_subordinate_requests BOOLEAN DEFAULT FALSE,
    approval_threshold_amount DECIMAL(10,2),
    can_create_users BOOLEAN DEFAULT FALSE,
    can_delete_users BOOLEAN DEFAULT FALSE,
    can_manage_roles BOOLEAN DEFAULT FALSE,
    can_view_all_inventory BOOLEAN DEFAULT FALSE,
    can_edit_all_inventory BOOLEAN DEFAULT FALSE,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES USERS(user_id),
    
    UNIQUE(role_name)
);

CREATE INDEX idx_role_tree ON USER_ROLES(parent_role_id, role_level);
```

**Tree Navigation Functions**:
- `get_role_ancestors(role_id)`: Returns all parent roles up to root
- `get_role_descendants(role_id)`: Returns all child roles recursively
- `get_role_siblings(role_id)`: Returns roles with same parent
- `find_common_ancestor(role_id_1, role_id_2)`: Finds lowest common manager

---

## EDIT PERMISSION RULES

### Permission Decision Matrix

| Scenario | Requester | Target | Permission | Requires Approval | Notes |
|----------|-----------|--------|------------|-------------------|-------|
| 1 | Admin (L0) | Anyone | ALWAYS ALLOWED | Configurable | Admin checkbox overrides all |
| 2 | Manager (L1) | Subordinate (L2/L3) in same tree | ALWAYS ALLOWED | Configurable | Direct hierarchy path |
| 3 | Manager (L1) | Different tree user (L1/L2/L3) | CONFIGURABLE | Yes | Requires `can_edit_different_tree=true` |
| 4 | Supervisor (L2) | Same level (L2) same tree | CONFIGURABLE | Yes | Requires `can_edit_same_level=true` |
| 5 | Supervisor (L2) | Worker (L3) in own tree | ALWAYS ALLOWED | Configurable | Direct subordinate |
| 6 | Supervisor (L2) | Different tree Supervisor (L2) | CONFIGURABLE | Yes | Requires `can_edit_different_tree=true` |
| 7 | Worker (L3) | Anyone | NEVER | N/A | Workers cannot edit users unless exception granted |
| 8 | Any role | Higher level in hierarchy | NEVER | N/A | Cannot edit upward |
| 9 | Exception list | Specific user | ALWAYS ALLOWED | Configurable | Manual exception grants |

### Exception List Management

**User Edit Exceptions Table**:

```sql
CREATE TABLE USER_EDIT_EXCEPTIONS (
    exception_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES USERS(user_id),  -- Who gets the exception
    target_user_id UUID NOT NULL REFERENCES USERS(user_id),  -- Who they can edit
    granted_by UUID NOT NULL REFERENCES USERS(user_id),  -- Admin who granted
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP,  -- Optional expiration
    reason TEXT,
    UNIQUE(user_id, target_user_id)
);
```

**Use Cases**:
- Temporary project lead needs to edit team members outside normal tree
- Cross-department collaboration requires temporary edit access
- Training scenarios where worker needs to edit test accounts
- Special assignments (e.g., safety officer can edit all users)

### Edit Permission Check Logic

```python
def check_edit_permission(requesting_user, target_user):
    # Check if admin override
    if requesting_user.role.can_manage_roles:
        return "ALLOWED_NO_APPROVAL" if not requesting_user.role.requires_approval else "REQUIRES_APPROVAL"
    
    # Check exception list
    if has_edit_exception(requesting_user.user_id, target_user.user_id):
        return "ALLOWED_NO_APPROVAL"
    
    # Cannot edit upward
    if target_user.role.role_level < requesting_user.role.role_level:
        return "DENIED"
    
    # Check if same tree
    requester_path = get_role_tree_path(requesting_user.role_id)
    target_path = get_role_tree_path(target_user.role_id)
    
    if is_same_tree(requester_path, target_path):
        if target_user.role.role_level > requesting_user.role.role_level:
            # Subordinate edit
            return "REQUIRES_APPROVAL" if requesting_user.role.requires_approval else "ALLOWED_NO_APPROVAL"
        elif target_user.role.role_level == requesting_user.role.role_level:
            # Same level edit
            if requesting_user.role.can_edit_same_level:
                return "REQUIRES_APPROVAL"
            else:
                return "DENIED"
    else:
        # Different tree
        if requesting_user.role.can_edit_different_tree:
            return "REQUIRES_APPROVAL"
        else:
            return "DENIED"
```

---

## APPROVAL WORKFLOW CUSTOMIZATION

### Workflow Routing Based on Role Settings

**Principle**: Approval workflow is determined by the role settings of both requester and target, not hardcoded workflows

### Example Approval Scenarios

**Scenario 1: Worker Requests Part**
```
Role: Worker (Level 3)
Settings:
  - requires_approval: true
  - approval_threshold_amount: $0 (requires approval for any cost)

Workflow:
1. Worker creates request file
2. RequestRouter checks: requires_approval=true
3. AI finds parent role (Supervisor Level 2)
4. Check supervisor's can_approve_subordinate_requests: true
5. Route to supervisor for approval
6. If approved, process request; if denied, notify worker
```

**Scenario 2: Supervisor Edits Same-Level User**
```
Role: Site Supervisor - Commercial (Level 2)
Settings:
  - can_edit_same_level: true
  - requires_approval: true

Target: Site Supervisor - Residential (Level 2)

Workflow:
1. Supervisor creates edit request for peer
2. RequestRouter checks: can_edit_same_level=true (permission granted)
3. Check requires_approval: true
4. Find common ancestor: Project Manager (Level 1)
5. Route to Project Manager for approval
6. If approved, execute edit; if denied, notify requester
```

**Scenario 3: Manager with High Approval Threshold**
```
Role: Project Manager (Level 1)
Settings:
  - approval_threshold_amount: $5000
  - requires_approval: false (for requests under threshold)
  - can_approve_subordinate_requests: true

Workflow for $3000 part request:
1. Manager creates request
2. RequestRouter checks cost: $3000 < $5000
3. Check requires_approval: false
4. Auto-approve and process immediately

Workflow for $8000 part request:
1. Manager creates request
2. RequestRouter checks cost: $8000 > $5000
3. Find parent role: Admin (Level 0)
4. Route to Admin for approval
```

### Configurable Workflow Settings Per Company

**Company-Level Settings Table**:

```sql
CREATE TABLE COMPANY_WORKFLOW_SETTINGS (
    setting_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES COMPANIES(company_id),
    
    -- Global thresholds
    default_approval_threshold DECIMAL(10,2) DEFAULT 100.00,
    high_value_threshold DECIMAL(10,2) DEFAULT 5000.00,
    critical_threshold DECIMAL(10,2) DEFAULT 25000.00,
    
    -- Timeout settings
    approval_timeout_hours INT DEFAULT 48,
    auto_escalate_after_hours INT DEFAULT 24,
    
    -- Notification settings
    notify_on_approval BOOLEAN DEFAULT TRUE,
    notify_on_rejection BOOLEAN DEFAULT TRUE,
    notify_on_timeout BOOLEAN DEFAULT TRUE,
    
    UNIQUE(company_id)
);
```

---

## DEPARTMENT STRUCTURE

### Department Types

**Four Primary Departments**:
1. **Office**: Administrative, management, accounting roles
2. **Warehouse**: Inventory management, receiving, shipping
3. **Truck**: Mobile workers, delivery personnel, service technicians
4. **Job Site**: Active construction site workers, supervisors

### Multi-Department Assignment

**Decision**: Users can be assigned to multiple departments  
**Rationale**: Supervisors may manage both warehouse and job site; managers oversee multiple locations

**Schema**:

```sql
CREATE TABLE DEPARTMENTS (
    department_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    department_name VARCHAR(100) NOT NULL,
    department_type VARCHAR(50) NOT NULL,  -- 'Office', 'Warehouse', 'Truck', 'Job Site'
    location_name VARCHAR(200),  -- e.g., "Main Warehouse", "Oak Street Job Site"
    address TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(department_name)
);

CREATE TABLE USER_DEPARTMENTS (
    user_id UUID NOT NULL REFERENCES USERS(user_id),
    department_id UUID NOT NULL REFERENCES DEPARTMENTS(department_id),
    is_primary BOOLEAN DEFAULT FALSE,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID REFERENCES USERS(user_id),
    PRIMARY KEY (user_id, department_id)
);
```

**Multi-Department Example**:
```json
{
  "user_id": "uuid-john-smith",
  "departments": [
    {"name": "Main Warehouse", "type": "Warehouse", "is_primary": true},
    {"name": "Oak Street Job Site", "type": "Job Site", "is_primary": false}
  ]
}
```

**Department Visibility Rules**:
- Users see inventory from all assigned departments
- Requests default to primary department
- Approvers notified based on their department assignments
- Reports can filter by department

---

## IMPLEMENTATION ROADMAP

### 5-Phase Implementation Plan (20 Weeks)

**Phase 1: Core Infrastructure (Weeks 1-4)**

Week 1-2: Database & Server Foundation
- Set up PostgreSQL database with all schema (USERS, ROLES, DEPARTMENTS, PARTS, etc.)
- Implement cloud sync service (local folder watching)
- Create file watching system with chokidar library
- Set up OAuth2 authentication flow (Google + Microsoft)

Week 3-4: Basic Request Processing
- Build RequestRouter AI agent skeleton
- Implement request file validation
- Create response file generation
- Set up file archival system (90-day retention)

**Phase 2: Mobile App Foundation (Weeks 5-8)**

Week 5-6: React/Vue.js Mobile App
- Create app shell with authentication screens
- Implement SQLite local caching
- Build smart polling system (2min/1hr/12hr intervals)
- Create offline mode with queue management

Week 7-8: Core UI Features
- Parts catalog browsing (with 5+ level categories)
- Search functionality across all SKUs (internal + cross-reference)
- Basic request submission forms
- Notification system for responses

**Phase 3: User Management & Permissions (Weeks 9-12)**

Week 9-10: Role & Permission System
- Implement tree hierarchy navigation
- Build edit permission check logic
- Create exception list management UI (admin)
- Develop role configuration interface

Week 11-12: Approval Workflows
- Implement workflow routing based on role settings
- Create approval UI for managers/supervisors
- Build notification system for pending approvals
- Add timeout and escalation logic

**Phase 4: Advanced Features (Weeks 13-16)**

Week 13-14: Parts Management
- Multi-level category management UI
- PART_NUMBER_XREF cross-reference system
- Brand/supplier management
- Measurement unit conversions

Week 15-16: Reporting & Analytics
- Inventory reports by department
- Usage analytics by user/role
- Approval workflow metrics
- Cost tracking and budgeting

**Phase 5: Testing & Beta Deployment (Weeks 17-20)**

Week 17-18: Internal Testing
- Unit testing for all critical paths
- Integration testing with cloud storage providers
- Load testing with simulated multi-device scenarios
- Security audit of OAuth2 implementation

Week 19-20: Beta Deployment
- Deploy to beta users (Google Drive group)
- Monitor performance and error rates
- Collect user feedback on workflows
- Iterate on UI/UX based on feedback
- Prepare for production rollout decision

---

## REVISION HISTORY

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-21 | System Architect | Initial comprehensive design decisions document |

---

## DOCUMENT CONTROL

**Approval Required For Changes**: System Architect, Project Manager  
**Review Cycle**: Every 30 days during implementation  
**Distribution**: Development team, stakeholders, beta testers

**Related Documents**:
- [01_SYSTEM_ARCHITECTURE.md](01_SYSTEM_ARCHITECTURE.md)
- [02_DESIGN_DOCUMENT.md](02_DESIGN_DOCUMENT.md)
- [03_TECHNICAL_SPECIFICATION.md](03_TECHNICAL_SPECIFICATION.md)
- [04_WORKFLOW_DIAGRAMS.md](04_WORKFLOW_DIAGRAMS.md)
- [SYSTEM_VERIFICATION_QUESTIONS.md](SYSTEM_VERIFICATION_QUESTIONS.md)

---

## VERIFICATION DECISIONS (TRACEABILITY)

**Purpose**: Concise answers to Q16–Q75 with Q-number tags. Long-form reasoning remains in `SYSTEM_VERIFICATION_QUESTIONS.md` (see Q IDs) and can be copied to an appendix if ever needed.

### Session 2: Parts Management & Inventory (Q16–Q30)
- **Q16 – Brand vs manufacturer**: Merge into single `brand` field; manufacturer captured as optional alias/normalized brand entry. **Notes**: Keep BRANDS table for dedupe and aliases.
- **Q17 – Part numbers**: Internal SKU `WTP-{CATEGORY}-{SEQ}` plus PART_NUMBER_XREF for manufacturer/supplier numbers. **Notes**: Internal SKU is authoritative; external numbers are searchable cross-refs.
- **Q18 – Location tracking**: Track aisle/shelf/bin; make per-part toggleable (optional for simple sites).
- **Q19 – Units & conversion**: Store canonical metric + entered unit; display respects user/company preference (default imperial allowed). **Notes**: Preserve original unit for audit, convert on-the-fly.
- **Q20 – Multiple suppliers**: Support many suppliers per part with pricing/lead time; allow “brand-agnostic” option for price-first selection. **Notes**: Price comparison UI should allow brand-not-important checkbox.
- **Q21 – Part images**: Store files in cloud storage; DB stores stable path/URL. **Notes**: Prefer deterministic folder + filename to avoid expiring links.
- **Q22 – Revisions**: No formal versioning; use `modified_at` and replacement parts pattern. **Notes**: If revisioning needed later, add PART_REVISIONS table.
- **Q23 – Discontinued**: Mark inactive, keep history for reporting/audit.
- **Q24 – Warranties**: Track part warranty expirations and job warranty windows. **Notes**: Need fields for job close/reopen dates to align warranty coverage.
- **Q25 – Inventory counts**: Real-time updates per transaction via request/response; server remains single source of truth.
- **Q26 – Kits/assemblies**: Support kits composed of parts; kit inventory rolls up from components.
- **Q27 – Min stock levels**: Configure per warehouse/department/truck/job site (not global only).
- **Q28 – Costs/pricing**: Track purchase cost and optional sell price; keep pricing history when available. **Notes**: Sell price optional but schema should allow future integrations.
- **Q29 – Bulk import**: Request-based import of CSV/Excel uploaded to cloud; server processes. **Notes**: App UI may upload file; processing stays server-side.
- **Q30 – Barcodes/QR**: Support scanning and generation (internal SKU or manufacturer number). **Notes**: Provide barcode/QR tooling within parts management.

### Session 3: User Roles & Permissions (Q31–Q45)
- **Q31 – Role structure**: Flexible tree; allow flat use by configuring roles without hierarchy constraints. **Notes**: Keep tree schema but don’t force strict level semantics.
- **Q32 – Multiple roles**: Users can hold multiple roles; permissions unioned.
- **Q33 – Same-level edits**: Configurable per role (`can_edit_same_level`).
- **Q34 – Cross-branch edits**: Configurable checkbox with approval required.
- **Q35 – Temporary grants**: Exception list with optional expiration; supports user-submitted change requests for review.
- **Q36 – Inventory visibility**: Configurable per role (`can_view_all_inventory`), default can be “see all”; onboarding wizard to confirm defaults.
- **Q37 – Multi-department**: Users can be in multiple departments with a primary flag.
- **Q38 – Approval thresholds**: Per-role `approval_threshold_amount` drives routing.
- **Q39 – Worker approvals**: Workers may approve subordinate requests if flag enabled.
- **Q40 – Admin override**: Admins can do anything; retain full audit logging.
- **Q41 – User creation**: Admin-only creation; managers may request creation for admin approval.
- **Q42 – Deactivate vs delete**: Deactivate only; deletion not allowed. **Notes**: Deactivation requests follow approval chain (level-above rule).
- **Q43 – Role change audit**: Full audit log of role assignments/changes.
- **Q44 – Read-only roles**: Provide view/report roles with optional commenting; allow shareable read-only access; optional expiry; purge logs 2 years after expiry.
- **Q45 – Cross-department approvals**: Each department keeps its own approval chain; cross-dept requests routed through standard comms, not shared chain.

### Session 4: AI & Automation (Q46–Q60)
- **Q46 – AI framework**: Microsoft Autogen with RequestRouter agent.
- **Q47 – Auto-approval thresholds**: Configurable per role (`requires_approval`, threshold amount).
- **Q48 – AI prioritization**: Weighted priority using keywords (emergency/urgent), role, location, cost, deadline, and wait time. **Notes**: Maintain base priority + weighted priority for routing.
- **Q49 – Alternatives**: Suggest substitute parts when inventory is low.
- **Q50 – Ambiguous requests**: Ask for clarification; if best-guess applied, flag for human review.
- **Q51 – Learning**: Enable ML to improve over time (pattern learning).
- **Q52 – Emergencies**: Highest priority with immediate notifications and CRITICAL flag.
- **Q53 – Part validation**: Validate against external catalogs; use PART_NUMBER_XREF for cross-refs.
- **Q54 – Duplicates**: Detect/flag duplicates or high-similarity requests for review (no silent auto-reject).
- **Q55 – Reports**: Automated scheduled reports (daily/weekly) with configurable cadence.
- **Q56 – Invalid requests**: Auto-fix common issues; escalate to human if unresolved.
- **Q57 – Responses**: Return both JSON and human-readable summary.
- **Q58 – Multi-step workflows**: Single request with workflow state tracking; internal steps may surface as sub-steps.
- **Q59 – AI optionality**: AI-enabled but manual fallback available.
- **Q60 – AI downtime**: Queue until AI available; process when restored.

### Session 5: Deployment & Operations (Q61–Q75)
- **Q61 – Mobile platforms**: Support iOS and Android (start with iOS-heavy user base, but both required).
- **Q62 – Web interface**: Mobile-only for now; future web optional.
- **Q63 – Updates**: OTA for config/content; major versions via app stores; manual download only as fallback.
- **Q64 – Tenancy**: Single-company deployment; revisit multi-tenant later.
- **Q65 – Backups**: Real-time replication to backup server plus periodic snapshots/time-machine style restore points.
- **Q66 – Logging**: Detailed full request/response audit trail.
- **Q67 – Disaster recovery**: Backup server with manual failover (delegation optional later).
- **Q68 – Certificates**: Custom server TLS certificates; rely on provider encryption for cloud file sync.
- **Q69 – Staging**: Separate staging environment mirroring production.
- **Q70 – Performance metrics**: Use APM tool for metrics and tracing.
- **Q71 – Browser support**: No web UI required; only open shared links in default browser as needed.
- **Q72 – Offline reports**: Server-generated PDFs cached on device; on-demand requests allowed.
- **Q73 – Security scanning**: Automated dependency scanning (GitHub security features) with triage.
- **Q74 – Data export**: Support CSV/Excel exports and API access.
- **Q75 – Documentation**: Admin guide + user manual + API docs; script video tutorials once dev stabilizes.

---

## APPENDIX: FULL ANSWER REFERENCES

Long-form narratives and detailed rationale remain in `SYSTEM_VERIFICATION_QUESTIONS.md` under the original question numbers (Q1–Q75). Use those IDs for deep dives; this log stays concise for implementation traceability.
