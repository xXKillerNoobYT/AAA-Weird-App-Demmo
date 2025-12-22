# WeirdToo Parts System – Technical Specification

## 1. Critical Design Constraint: Local-Only, Cloud File I/O

### The Architecture

**All devices run locally. No direct internet communication between apps. Cloud storage is the ONLY inter-app messenger.**

```
Device 1 (Truck)     ──┐
Device 2 (Warehouse) ──┤──> Cloud Storage (SharePoint/Drive) <──┐
Device 3 (Office)    ──┘                                         │
                                                                 │
                                                    Server (Local Machine)
```

**Explicitly Prohibited**:
- ❌ HTTP/REST API calls from device to server
- ❌ WebSocket connections
- ❌ gRPC communication
- ❌ Direct database connections from devices
- ❌ Message queues (RabbitMQ, Kafka)
- ❌ Event streaming services

**Explicitly Allowed**:
- ✅ JSON files in cloud storage (read/write/poll)
- ✅ Local SQLite caching on each device
- ✅ Local database on server machine
- ✅ Offline-first operation

---

## 2. Technology Stack

| Component | Primary | Fallback | Notes |
|-----------|---------|----------|-------|
| **Frontend** | React.js or Vue.js | HTML/CSS/JS | Single Page Apps on each device |
| **Server Agent** | Node.js + Express OR Python + FastAPI | - | Polls cloud storage for requests |
| **Authoritative DB** | PostgreSQL 14+ | SQL Server 2019+ | Central database on server machine |
| **Device Cache** | SQLite 3.x | - | Local offline-capable cache |
| **AI Orchestration** | Microsoft Autogen | LangChain | Python-based agent framework |
| **Cloud Storage** | Microsoft SharePoint | Google Drive | Exclusive inter-app communication |
| **Optional Caching** | Redis 7.x | memcached | Server-side optimization only |

---

## 3. API Specifications: Cloud File Protocol

### Request/Response Pattern

All communication happens through JSON files in cloud storage:

**Device Workflow**:
1. Write request JSON to cloud storage `/Cloud/Requests/{device-id}/` folder
2. Cloud service (SharePoint/Google Drive desktop app) syncs to cloud
3. Poll cloud `/Cloud/Responses/{device-id}/` folder for matching request_id
   - Smart polling intervals:
     - 2 minutes while app is open/active
     - 1 hour during work hours when app is closed/backgrounded
     - 12 hours after work hours
   - Immediate re-poll after submitting a new request
4. Read response, process, delete files

**Server Workflow** (Cloud Service Syncs to Local Hard Drive):
1. Cloud service (SharePoint/Google Drive desktop sync) syncs cloud to local drive
2. Server watches local `/Cloud/Requests/` folders for NEW FILE events (not polling cloud)
3. Each new request file triggers processing immediately
4. Process requests in order (with dependency checking)
5. Write response JSON to local `/Cloud/Responses/{device-id}/`
6. Cloud service syncs responses back to cloud automatically

### 3.1 Parts Catalog APIs

#### GET: Fetch All Parts with Variants

**Request File**: `/Cloud/Requests/{device-id}/get-parts-{uuid}.json`

```json
{
  "request_type": "get_parts",
  "request_id": "req-get-parts-001",
  "timestamp": "2025-05-15T14:30:00Z",
  "device_id": "truck-005",
  "filters": {
    "category_id": "cat-electrical-wire",
    "is_active": true
  },
  "include_variants": true,
  "include_pricing": true,
  "include_availability": true
}
```

**Response File**: `/Cloud/Responses/truck-005/req-get-parts-001.json`

```json
{
  "request_id": "req-get-parts-001",
  "status": "success",
  "data": {
    "parts": [
      {
        "part_id": "part-wire-001",
        "part_name": "Romex Wire 12 AWG",
        "part_code": "WIR-12-ROM",
        "category_id": "cat-electrical-wire",
        "variants": [
          {
            "variant_id": "var-001",
            "variant_name": "250ft Roll",
            "variant_sku": "WIR-12-ROM-250",
            "quantity_per_unit": 250,
            "specifications": {
              "gauge": "12 AWG",
              "material": "copper",
              "insulation": "THHN",
              "voltage_rating": "600V"
            },
            "suppliers": [
              {
                "supplier_id": "sup-001",
                "supplier_name": "Home Depot Supply",
                "brand_id": "brand-001",
                "brand_name": "Southwire",
                "list_price": 89.99,
                "cost_price": 65.00,
                "stock_level": 24,
                "lead_time_days": 1
              }
            ]
          }
        ]
      }
    ],
    "total_count": 150,
    "page": 1,
    "page_size": 10
  },
  "timestamp": "2025-05-15T14:30:45Z",
  "response_time_ms": 342
}
```

#### GET: Fetch Part Variants by SKU

**Request**: `/Cloud/Requests/{device-id}/get-variant-{uuid}.json`

```json
{
  "request_type": "get_variant",
  "request_id": "req-get-var-001",
  "timestamp": "2025-05-15T14:31:00Z",
  "device_id": "truck-005",
  "variant_sku": "WIR-12-ROM-250"
}
```

**Response**: `/Cloud/Responses/truck-005/req-get-var-001.json` (with variant details as above)

#### GET: Fetch Spec Sheet URL

**Request**: `/Cloud/Requests/{device-id}/get-spec-sheet-{uuid}.json`

```json
{
  "request_type": "get_spec_sheet",
  "request_id": "req-spec-001",
  "timestamp": "2025-05-15T14:30:00Z",
  "device_id": "truck-005",
  "variant_sku": "WIR-12-ROM-250"
}
```

**Response**: `/Cloud/Responses/truck-005/req-spec-001.json`

```json
{
  "request_id": "req-spec-001",
  "status": "success",
  "data": {
    "variant_sku": "WIR-12-ROM-250",
    "spec_sheet_url": "https://sharepoint.com/sites/specs/Electrical/Southwire/WIR-12-250.pdf",
    "file_path": "/SpecSheets/Electrical/Southwire/WIR-12-250.pdf",
    "file_size_kb": 245,
    "last_updated": "2025-04-20T10:00:00Z"
  },
  "timestamp": "2025-05-15T14:30:45Z"
}
```

---

### 3.2 Inventory APIs

#### GET: Warehouse Stock

**Request**: `/Cloud/Requests/{device-id}/get-warehouse-stock-{uuid}.json`

```json
{
  "request_type": "get_warehouse_stock",
  "request_id": "req-inv-001",
  "timestamp": "2025-05-15T14:30:00Z",
  "device_id": "truck-005",
  "warehouse_id": "warehouse-central",
  "variant_skus": ["WIR-12-ROM-250", "WIR-12-ROM-1000", "OUT-SG-001"]
}
```

**Response**: `/Cloud/Responses/truck-005/req-inv-001.json`

```json
{
  "request_id": "req-inv-001",
  "status": "success",
  "data": {
    "warehouse_id": "warehouse-central",
    "timestamp": "2025-05-15T14:30:00Z",
    "inventory": [
      {
        "variant_sku": "WIR-12-ROM-250",
        "quantity_on_hand": 24,
        "quantity_reserved": 4,
        "quantity_available": 20,
        "location_zone": "A",
        "location_bin": "A-15"
      },
      {
        "variant_sku": "WIR-12-ROM-1000",
        "quantity_on_hand": 8,
        "quantity_reserved": 2,
        "quantity_available": 6,
        "location_zone": "A",
        "location_bin": "A-16"
      }
    ]
  },
  "timestamp": "2025-05-15T14:30:45Z"
}
```

#### PUT: Update Truck Inventory (Consumption)

**Request**: `/Cloud/Requests/{device-id}/update-truck-inventory-{uuid}.json`

```json
{
  "request_type": "update_truck_inventory",
  "request_id": "req-truck-inv-001",
  "timestamp": "2025-05-15T16:45:00Z",
  "device_id": "truck-005",
  "truck_id": "truck-005",
  "updates": [
    {
      "variant_sku": "WIR-12-ROM-250",
      "quantity_consumed": 1,
      "job_site_id": "job-site-204",
      "location_on_truck": "Section-A"
    }
  ]
}
```

**Response**: `/Cloud/Responses/truck-005/req-truck-inv-001.json`

```json
{
  "request_id": "req-truck-inv-001",
  "status": "success",
  "data": {
    "truck_id": "truck-005",
    "updates_applied": 1,
    "inventory_after_update": [
      {
        "variant_sku": "WIR-12-ROM-250",
        "quantity_on_truck": 1,
        "last_verified": "2025-05-15T16:45:30Z"
      }
    ]
  },
  "timestamp": "2025-05-15T16:45:30Z"
}
```

---

### 3.3 Parts List & Supplier Order APIs

#### POST: Create Parts List

**Request**: `/Cloud/Requests/{device-id}/create-parts-list-{uuid}.json`

```json
{
  "request_type": "create_parts_list",
  "request_id": "req-pl-001",
  "timestamp": "2025-05-15T10:00:00Z",
  "device_id": "office-001",
  "parts_list": {
    "project_id": "proj-unit-204",
    "project_name": "Unit 204 Renovation",
    "created_by": "john_smith",
    "items": [
      {
        "line_item_id": "item-001",
        "variant_sku": "WIR-12-ROM-250",
        "quantity": 2,
        "unit_type": "ROLL"
      },
      {
        "line_item_id": "item-002",
        "variant_sku": "OUT-SG-001",
        "quantity": 8,
        "unit_type": "COUNT"
      }
    ]
  }
}
```

**Response**: `/Cloud/Responses/office-001/req-pl-001.json`

```json
{
  "request_id": "req-pl-001",
  "status": "success",
  "data": {
    "parts_list_id": "pl-001",
    "project_id": "proj-unit-204",
    "status": "draft",
    "items_created": 2,
    "total_line_items": 2
  },
  "timestamp": "2025-05-15T10:00:30Z"
}
```

#### POST: Submit Parts List for Approval

**Request**: `/Cloud/Requests/{device-id}/submit-parts-list-{uuid}.json`

```json
{
  "request_type": "submit_parts_list",
  "request_id": "req-pl-submit-001",
  "timestamp": "2025-05-15T11:00:00Z",
  "device_id": "office-001",
  "parts_list_id": "pl-001",
  "submitted_by": "john_smith"
}
```

**Response**: Status changes to "pending_review"; notification sent to approvers

#### POST: Approve Parts List

**Request**: `/Cloud/Requests/{device-id}/approve-parts-list-{uuid}.json`

```json
{
  "request_type": "approve_parts_list",
  "request_id": "req-pl-approve-001",
  "timestamp": "2025-05-15T14:00:00Z",
  "device_id": "office-002",
  "parts_list_id": "pl-001",
  "approved_by": "project_manager",
  "approval_notes": "Approved for procurement"
}
```

**Response**: Status changes to "approved"; triggers SupplierMatcher AI agent

#### POST: Generate Consolidated Purchase Order

**Request**: `/Cloud/Requests/{device-id}/generate-po-{uuid}.json`

```json
{
  "request_type": "generate_purchase_order",
  "request_id": "req-po-001",
  "timestamp": "2025-05-15T15:00:00Z",
  "device_id": "office-001",
  "parts_list_ids": ["pl-001", "pl-002"],
  "consolidation_strategy": "supplier_first",
  "apply_bulk_discounts": true
}
```

**Response**: `/Cloud/Responses/office-001/req-po-001.json`

```json
{
  "request_id": "req-po-001",
  "status": "success",
  "data": {
    "purchase_orders": [
      {
        "po_id": "po-001",
        "supplier_id": "sup-001",
        "supplier_name": "Home Depot Supply",
        "po_date": "2025-05-15T15:00:30Z",
        "line_items": 5,
        "total_cost": 450.00,
        "lead_time_days": 1,
        "estimated_arrival": "2025-05-16T15:00:00Z"
      }
    ],
    "total_pos": 1,
    "total_cost_all_pos": 450.00,
    "bulk_discount_applied": 5.2
  },
  "timestamp": "2025-05-15T15:00:30Z"
}
```

---

## 4. Database Schema with Indexing

### Core Tables

```sql
-- ============================================
-- PARTS MANAGEMENT TABLES
-- ============================================

CREATE TABLE PART_CATEGORIES (
    category_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_category_id UUID REFERENCES PART_CATEGORIES(category_id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category_depth INT NOT NULL,  -- Supports 0 (root) to 5+ levels (unlimited)
    full_path VARCHAR(1000),      -- 'Electrical > Wire > Copper > Romex > 12 AWG'
    display_order INT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_depth CHECK (category_depth >= 0)
);
CREATE INDEX idx_parent_category ON PART_CATEGORIES(parent_category_id);
CREATE INDEX idx_category_active ON PART_CATEGORIES(is_active);
CREATE INDEX idx_category_depth ON PART_CATEGORIES(category_depth);
CREATE INDEX idx_category_path ON PART_CATEGORIES(full_path);

CREATE TABLE MEASUREMENT_UNITS (
    unit_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    unit_name VARCHAR(100) NOT NULL UNIQUE,
    unit_type ENUM('COUNT', 'LINEAR', 'ROLL', 'CUSTOM') NOT NULL,
    base_unit VARCHAR(50),
    conversion_factor DECIMAL(10, 4),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_unit_type ON MEASUREMENT_UNITS(unit_type);
CREATE INDEX idx_unit_active ON MEASUREMENT_UNITS(is_active);

CREATE TABLE PARTS (
    part_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id UUID NOT NULL REFERENCES PART_CATEGORIES(category_id),
    part_name VARCHAR(255) NOT NULL,
    part_code VARCHAR(50) NOT NULL UNIQUE,
    manufacturer VARCHAR(255),
    base_unit_id UUID REFERENCES MEASUREMENT_UNITS(unit_id),
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_part_code ON PARTS(part_code);
CREATE INDEX idx_part_category ON PARTS(category_id);
CREATE INDEX idx_part_active ON PARTS(is_active);

CREATE TABLE PART_VARIANTS (
    variant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    part_id UUID NOT NULL REFERENCES PARTS(part_id),
    variant_name VARCHAR(255) NOT NULL,
    variant_sku VARCHAR(100) NOT NULL UNIQUE,
    variant_unit_id UUID REFERENCES MEASUREMENT_UNITS(unit_id),
    quantity_per_unit INT,
    specifications JSONB,
    is_primary BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_variant_sku ON PART_VARIANTS(variant_sku);
CREATE INDEX idx_variant_part_id ON PART_VARIANTS(part_id);
CREATE INDEX idx_variant_active ON PART_VARIANTS(is_active);
CREATE INDEX idx_variant_primary ON PART_VARIANTS(is_primary) WHERE is_primary = TRUE;

-- ============================================
-- SUPPLIER & BRAND TABLES
-- ============================================

CREATE TABLE SUPPLIERS (
    supplier_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supplier_name VARCHAR(255) NOT NULL UNIQUE,
    supplier_code VARCHAR(50) NOT NULL UNIQUE,
    contact_email VARCHAR(255),
    contact_phone VARCHAR(20),
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(50),
    zip_code VARCHAR(20),
    country VARCHAR(100),
    payment_terms VARCHAR(100),
    lead_time_days INT,
    minimum_order_value DECIMAL(10, 2),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_supplier_code ON SUPPLIERS(supplier_code);
CREATE INDEX idx_supplier_active ON SUPPLIERS(is_active);
CREATE INDEX idx_supplier_lead_time ON SUPPLIERS(lead_time_days);

CREATE TABLE BRANDS (
    brand_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    brand_name VARCHAR(255) NOT NULL UNIQUE,
    brand_code VARCHAR(50) NOT NULL UNIQUE,
    manufacturer_country VARCHAR(100),
    website VARCHAR(500),
    technical_support_email VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_brand_code ON BRANDS(brand_code);
CREATE INDEX idx_brand_active ON BRANDS(is_active);

CREATE TABLE SUPPLIER_BRAND_MAPPING (
    mapping_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supplier_id UUID NOT NULL REFERENCES SUPPLIERS(supplier_id),
    brand_id UUID NOT NULL REFERENCES BRANDS(brand_id),
    supplier_brand_code VARCHAR(100),
    lead_time_days INT,
    minimum_order_qty INT,
    discount_percentage DECIMAL(5, 2),
    contract_expires DATE,
    is_preferred BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(supplier_id, brand_id)
);
CREATE INDEX idx_supplier_brand_mapping ON SUPPLIER_BRAND_MAPPING(supplier_id, brand_id);
CREATE INDEX idx_supplier_brand_preferred ON SUPPLIER_BRAND_MAPPING(is_preferred);

CREATE TABLE PART_SUPPLIER_AVAILABILITY (
    availability_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    variant_id UUID NOT NULL REFERENCES PART_VARIANTS(variant_id),
    supplier_id UUID NOT NULL REFERENCES SUPPLIERS(supplier_id),
    brand_id UUID NOT NULL REFERENCES BRANDS(brand_id),
    supplier_sku VARCHAR(100),
    list_price DECIMAL(10, 2),
    cost_price DECIMAL(10, 2),
    stock_level INT,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_current BOOLEAN DEFAULT TRUE,
    UNIQUE(variant_id, supplier_id, brand_id)
);
CREATE INDEX idx_part_availability_variant ON PART_SUPPLIER_AVAILABILITY(variant_id);
CREATE INDEX idx_part_availability_supplier ON PART_SUPPLIER_AVAILABILITY(supplier_id);
CREATE INDEX idx_part_availability_current ON PART_SUPPLIER_AVAILABILITY(is_current);

-- ============================================
-- INVENTORY TABLES
-- ============================================

CREATE TABLE WAREHOUSE_INVENTORY (
    inventory_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    warehouse_id UUID NOT NULL,
    variant_id UUID NOT NULL REFERENCES PART_VARIANTS(variant_id),
    quantity_on_hand INT DEFAULT 0,
    quantity_reserved INT DEFAULT 0,
    quantity_available INT DEFAULT 0,
    location_zone VARCHAR(50),
    location_bin VARCHAR(50),
    last_stock_check TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(warehouse_id, variant_id)
);
CREATE INDEX idx_warehouse_variant ON WAREHOUSE_INVENTORY(warehouse_id, variant_id);
CREATE INDEX idx_warehouse_available ON WAREHOUSE_INVENTORY(quantity_available);

CREATE TABLE TRUCK_INVENTORY (
    inventory_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    truck_id UUID NOT NULL,
    variant_id UUID NOT NULL REFERENCES PART_VARIANTS(variant_id),
    quantity_on_truck INT DEFAULT 0,
    storage_location VARCHAR(100),
    last_verified TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(truck_id, variant_id)
);
CREATE INDEX idx_truck_variant ON TRUCK_INVENTORY(truck_id, variant_id);

-- ============================================
-- PARTS LIST & ORDER TABLES
-- ============================================

CREATE TABLE PARTS_LISTS (
    parts_list_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NOT NULL,
    project_name VARCHAR(255),
    created_by VARCHAR(255),
    status ENUM('draft', 'pending_review', 'approved', 'rejected') DEFAULT 'draft',
    approval_notes TEXT,
    approved_by VARCHAR(255),
    approval_timestamp TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_parts_list_status ON PARTS_LISTS(status);
CREATE INDEX idx_parts_list_project ON PARTS_LISTS(project_id);
CREATE INDEX idx_parts_list_created_at ON PARTS_LISTS(created_at);

CREATE TABLE PARTS_LIST_ITEMS (
    item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parts_list_id UUID NOT NULL REFERENCES PARTS_LISTS(parts_list_id),
    variant_id UUID NOT NULL REFERENCES PART_VARIANTS(variant_id),
    quantity_requested INT NOT NULL,
    quantity_fulfilled INT DEFAULT 0,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_parts_list_items_list ON PARTS_LIST_ITEMS(parts_list_id);
CREATE INDEX idx_parts_list_items_variant ON PARTS_LIST_ITEMS(variant_id);

CREATE TABLE PURCHASE_ORDERS (
    po_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supplier_id UUID NOT NULL REFERENCES SUPPLIERS(supplier_id),
    po_number VARCHAR(50) UNIQUE,
    po_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status ENUM('draft', 'submitted', 'confirmed', 'shipped', 'received', 'cancelled') DEFAULT 'draft',
    total_cost DECIMAL(12, 2),
    estimated_arrival TIMESTAMP,
    actual_arrival TIMESTAMP,
    created_by VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_po_supplier ON PURCHASE_ORDERS(supplier_id);
CREATE INDEX idx_po_status ON PURCHASE_ORDERS(status);
CREATE INDEX idx_po_estimated_arrival ON PURCHASE_ORDERS(estimated_arrival);

CREATE TABLE PURCHASE_ORDER_ITEMS (
    po_item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    po_id UUID NOT NULL REFERENCES PURCHASE_ORDERS(po_id),
    variant_id UUID NOT NULL REFERENCES PART_VARIANTS(variant_id),
    quantity_ordered INT NOT NULL,
    quantity_received INT DEFAULT 0,
    unit_price DECIMAL(10, 2),
    line_total DECIMAL(12, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_po_items ON PURCHASE_ORDER_ITEMS(po_id);

-- ============================================
-- AUDIT TABLES
-- ============================================

CREATE TABLE PART_AUDIT_LOG (
    audit_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    variant_id UUID NOT NULL REFERENCES PART_VARIANTS(variant_id),
    action VARCHAR(50),
    changed_by_user VARCHAR(255),
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    old_values JSONB,
    new_values JSONB,
    request_file_id VARCHAR(255),
    notes TEXT
);
CREATE INDEX idx_audit_variant ON PART_AUDIT_LOG(variant_id);
CREATE INDEX idx_audit_timestamp ON PART_AUDIT_LOG(changed_at DESC);
CREATE INDEX idx_audit_user ON PART_AUDIT_LOG(changed_by_user);

-- ============================================
-- USER MANAGEMENT & PERMISSION TABLES
-- ============================================

CREATE TABLE USERS (
    user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    display_name VARCHAR(255) NOT NULL,
    role_id UUID REFERENCES USER_ROLES(role_id),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES USERS(user_id),
    last_login TIMESTAMP,
    oauth_provider VARCHAR(50),  -- 'google' or 'microsoft'
    oauth_subject_id VARCHAR(255)  -- OAuth provider's user ID
);
CREATE INDEX idx_user_email ON USERS(email);
CREATE INDEX idx_user_role ON USERS(role_id);
CREATE INDEX idx_user_active ON USERS(is_active);
CREATE INDEX idx_user_oauth ON USERS(oauth_provider, oauth_subject_id);

CREATE TABLE USER_ROLES (
    role_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name VARCHAR(100) NOT NULL UNIQUE,
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
    created_by UUID REFERENCES USERS(user_id)
);
CREATE INDEX idx_role_tree ON USER_ROLES(parent_role_id, role_level);
CREATE INDEX idx_role_level ON USER_ROLES(role_level);
CREATE INDEX idx_role_name ON USER_ROLES(role_name);

CREATE TABLE USER_EDIT_EXCEPTIONS (
    exception_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES USERS(user_id),  -- Who gets the exception
    target_user_id UUID NOT NULL REFERENCES USERS(user_id),  -- Who they can edit
    granted_by UUID NOT NULL REFERENCES USERS(user_id),  -- Admin who granted
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP,  -- Optional expiration
    reason TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(user_id, target_user_id)
);
CREATE INDEX idx_exception_user ON USER_EDIT_EXCEPTIONS(user_id);
CREATE INDEX idx_exception_target ON USER_EDIT_EXCEPTIONS(target_user_id);
CREATE INDEX idx_exception_active ON USER_EDIT_EXCEPTIONS(is_active) WHERE is_active = TRUE;

CREATE TABLE DEPARTMENTS (
    department_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    department_name VARCHAR(100) NOT NULL UNIQUE,
    department_type VARCHAR(50) NOT NULL,  -- 'Office', 'Warehouse', 'Truck', 'Job Site'
    location_name VARCHAR(200),  -- e.g., "Main Warehouse", "Oak Street Job Site"
    address TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_department_type ON DEPARTMENTS(department_type);
CREATE INDEX idx_department_active ON DEPARTMENTS(is_active);

CREATE TABLE USER_DEPARTMENTS (
    user_id UUID NOT NULL REFERENCES USERS(user_id),
    department_id UUID NOT NULL REFERENCES DEPARTMENTS(department_id),
    is_primary BOOLEAN DEFAULT FALSE,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID REFERENCES USERS(user_id),
    PRIMARY KEY (user_id, department_id)
);
CREATE INDEX idx_user_dept_user ON USER_DEPARTMENTS(user_id);
CREATE INDEX idx_user_dept_primary ON USER_DEPARTMENTS(user_id, is_primary) WHERE is_primary = TRUE;

CREATE TABLE USER_ROLE_AUDIT (
    audit_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES USERS(user_id),
    old_role_id UUID REFERENCES USER_ROLES(role_id),
    new_role_id UUID REFERENCES USER_ROLES(role_id),
    changed_by UUID REFERENCES USERS(user_id),
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reason TEXT
);
CREATE INDEX idx_role_audit_user ON USER_ROLE_AUDIT(user_id);
CREATE INDEX idx_role_audit_timestamp ON USER_ROLE_AUDIT(changed_at DESC);

-- ============================================
-- PART NUMBER CROSS-REFERENCE TABLE
-- ============================================

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
CREATE INDEX idx_xref_primary ON PART_NUMBER_XREF(part_id, is_primary) WHERE is_primary = TRUE;
```

---

## 5. Cloud Storage Integration

### 5.1 Folder Structure

```
Cloud Storage Root (SharePoint/Google Drive)
├── /Cloud/
│   ├── /Requests/
│   │   ├── /truck-001/
│   │   ├── /truck-002/
│   │   ├── /warehouse-001/
│   │   ├── /office-001/
│   │   └── /job-site-001/
│   ├── /Responses/
│   │   ├── /truck-001/
│   │   ├── /truck-002/
│   │   ├── /warehouse-001/
│   │   ├── /office-001/
│   │   └── /job-site-001/
│   └── /Archive/
│       └── (old requests/responses, dated folders)
├── /SpecSheets/
│   ├── /Electrical/
│   │   ├── /Southwire/
│   │   ├── /Romex/
│   │   └── /other-brands/
│   ├── /Plumbing/
│   ├── /Structural/
│   └── /Hardware/
└── /Backups/
    └── (database backups, exported data)
```

### 5.2 Cloud Sync Implementation (Node.js Example)

```javascript
const SharePoint = require('@pnp/sp');
const fs = require('fs');
const path = require('path');

class CloudSyncManager {
  constructor(localSyncPath) {
    this.localSyncPath = localSyncPath; // e.g., 'C:/Users/Server/SharePoint/Cloud'
    this.processedRequests = new Set();
    this.watcher = null;
  }

  async watchForRequests() {
    const chokidar = require('chokidar');
    
    // Watch local synced folder for new files (cloud service handles sync)
    this.watcher = chokidar.watch(`${this.localSyncPath}/Requests/**/*.json`, {
      ignored: /(^|[\/\\])\../, // ignore dotfiles
      persistent: true,
      ignoreInitial: false, // Process existing files on startup
      awaitWriteFinish: {
        stabilityThreshold: 500, // Wait for file write to complete
        pollInterval: 100
      }
    });

    this.watcher.on('add', async (filePath) => {
      try {
        const fileName = path.basename(filePath);
        const deviceId = path.basename(path.dirname(filePath));
        
        if (!this.processedRequests.has(fileName)) {
          console.log(`New request file detected: ${fileName} from ${deviceId}`);
          
          const fileContent = fs.readFileSync(filePath, 'utf8');
          const request = JSON.parse(fileContent);
          
          await this.processRequest(request, deviceId);
          
          this.processedRequests.add(fileName);
          
          // Delete processed request file
          fs.unlinkSync(filePath);
        }
      } catch (error) {
        console.error('Error processing request file:', error);
      }
    });

    console.log(`Watching for new request files in: ${this.localSyncPath}/Requests`);
  }

  async processRequest(request, deviceId) {
    const response = await this.handleRequest(request);
    
    // Write response
    const responseFileName = `${request.request_id}.json`;
    const responseContent = JSON.stringify(response, null, 2);

    await this.sp.web.getFolderByServerRelativePath(
      `Cloud/Responses/${deviceId}`
    ).files.add(responseFileName, responseContent, true);
  }

  async handleRequest(request) {
    // Route to appropriate handler based on request_type
    switch(request.request_type) {
      case 'get_parts':
        return await this.handleGetParts(request);
      case 'get_warehouse_stock':
        return await this.handleGetWarehouseStock(request);
      case 'create_parts_list':
        return await this.handleCreatePartsList(request);
      default:
        return { status: 'error', message: 'Unknown request type' };
    }
  }

  async handleGetParts(request) {
    // Query database, return parts and variants
    const db = await this.getDatabase();
    const parts = await db.query(`
      SELECT p.*, pv.variant_id, pv.variant_name, pv.variant_sku
      FROM PARTS p
      LEFT JOIN PART_VARIANTS pv ON p.part_id = pv.part_id
      WHERE p.is_active = true
      LIMIT 100
    `);

    return {
      request_id: request.request_id,
      status: 'success',
      data: { parts },
      timestamp: new Date().toISOString()
    };
  }

  // Additional handlers...
}
```

---

## 6. Microsoft Autogen Configuration

### 6.1 PartsSpecialist Agent

```python
from autogen import AssistantAgent, UserProxyAgent

parts_specialist_config = {
    "model": "gpt-4",
    "api_type": "openai",
    "api_key": os.getenv("OPENAI_API_KEY"),
    "temperature": 0.3  # Lower temperature for consistency
}

parts_specialist = AssistantAgent(
    name="PartsSpecialist",
    system_message="""You are an expert in construction materials and parts selection.
    You help identify the correct part variants based on electrical codes, material specifications,
    and construction requirements. Always reference NEC (National Electrical Code) standards.
    
    Key responsibilities:
    - Identify gauge and amperage requirements from circuit descriptions
    - Recommend insulation types based on environment (wet, dry, underground)
    - Suggest packaging (250ft vs 1000ft rolls) based on project scope
    - Compare specifications across brands
    - Ensure voltage and temperature ratings meet requirements
    
    Database access: query_parts_catalog(category, filters)
    Available functions: get_part_specifications, get_supplier_availability, compare_variants
    """,
    llm_config=parts_specialist_config
)

# Function definitions for the agent
functions = [
    {
        "name": "search_part_variants",
        "description": "Search parts by category and specifications",
        "parameters": {
            "type": "object",
            "properties": {
                "category": {"type": "string"},
                "gauge": {"type": "string"},
                "material": {"type": "string"}
            }
        }
    },
    {
        "name": "get_supplier_availability",
        "description": "Check current pricing and availability from suppliers",
        "parameters": {
            "type": "object",
            "properties": {
                "variant_sku": {"type": "string"},
                "suppliers": {"type": "array", "items": {"type": "string"}}
            }
        }
    },
    {
        "name": "compare_variants",
        "description": "Compare specifications across different wire variants",
        "parameters": {
            "type": "object",
            "properties": {
                "variant_skus": {"type": "array", "items": {"type": "string"}}
            }
        }
    }
]

parts_specialist.update_function_signature(functions)
```

### 6.2 SupplierMatcher Agent

```python
supplier_matcher = AssistantAgent(
    name="SupplierMatcher",
    system_message="""You are a procurement expert specializing in supplier sourcing and cost optimization.
    Analyze parts lists and recommend the best supplier combinations based on:
    - Price competitiveness (compare list_price across suppliers)
    - Lead time constraints (must deliver by project deadline)
    - Supplier reliability and payment terms
    - Bulk discounts and consolidation opportunities
    
    Database access: query_supplier_brands, query_part_availability
    Available functions: consolidate_orders, calculate_lead_times, apply_discounts
    """,
    llm_config=parts_specialist_config
)
```

### 6.3 OrderGenerator Agent

```python
order_generator = AssistantAgent(
    name="OrderGenerator",
    system_message="""You are a procurement specialist responsible for creating and tracking orders.
    Create purchase orders that consolidate parts from multiple parts lists by supplier.
    Ensure:
    - All minimum order quantities are met
    - Lead times are acceptable for project schedules
    - Pricing includes applicable bulk discounts
    - Delivery dates are tracked and managed
    
    Database access: create_purchase_order, update_po_status
    Available functions: submit_order, track_delivery, process_receipts
    """,
    llm_config=parts_specialist_config
)
```

---

## 7. Security Specifications

### 7.1 Authentication & Authorization

- **Device Apps**: Local login with username/password stored in SQLite (hashed with bcrypt)
- **Server**: JWT tokens for internal operations
- **Cloud Storage Access**: OAuth2 credentials (SharePoint or Google Drive)
- **API Key Rotation**: Every 90 days

### 7.2 Data Encryption

- **In Transit**: TLS 1.3 for cloud storage API calls
- **At Rest**: PostgreSQL encryption at column level for sensitive data (pricing, supplier info)
- **Device Cache**: SQLite encryption using SQLCipher

### 7.3 Audit Logging

- All changes logged in PART_AUDIT_LOG with user, timestamp, old values, new values
- Cloud request/response files archived for compliance
- Database backup encryption enabled

---

## 8. Performance Requirements

| Metric | Target | Acceptable |
|--------|--------|-----------|
| Cloud poll latency | < 2 seconds | < 5 seconds |
| Parts search response | < 200ms | < 500ms |
| Inventory update time | < 1 second | < 3 seconds |
| Spec sheet download | < 5 seconds | < 10 seconds |
| Parts list creation | < 2 seconds | < 5 seconds |
| PO consolidation time | < 10 seconds (for 10 lists) | < 30 seconds |

---

## 9. Deployment Checklist

- [ ] PostgreSQL 14+ installed and configured on server machine
- [ ] Node.js or Python runtime installed on server machine
- [ ] Microsoft Autogen dependencies installed
- [ ] SharePoint or Google Drive OAuth credentials configured
- [ ] React/Vue build and deployment for device apps
- [ ] SQLite initialized on each device machine
- [ ] Cloud storage folders created (/Cloud/Requests, /Responses, /SpecSheets)
- [ ] Spec sheets uploaded to cloud storage
- [ ] Database schema created and indexed
- [ ] API polling service started on server
- [ ] Device apps configured with cloud storage credentials
