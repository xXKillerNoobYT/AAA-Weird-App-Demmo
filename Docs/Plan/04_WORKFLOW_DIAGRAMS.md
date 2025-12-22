# WeirdToo Parts System – Workflow Diagrams

## 1. Parts List Creation and Approval Workflow

### 1.1 Three-Stage Approval Process

```mermaid
graph TD
    A["Office Manager<br/>Creates Parts List"]
    B["Parts List: Draft"]
    C["Submit for Review"]
    D["Construction Manager<br/>Reviews & Annotates"]
    E["Parts List: Pending Review"]
    F["Flag Issues?"]
    G["Send Back to Draft<br/>With Comments"]
    H["Approve for Procurement"]
    I["Parts List: Approved"]
    J["Trigger AI: SupplierMatcher"]
    K["Generate PO Recommendations"]
    
    A -->|Create| B
    B -->|Edit items| B
    B -->|Submit| C
    C -->|Process| E
    E -->|Review specs| D
    D -->|Check NEC codes, quantities| F
    F -->|Yes| G
    G -->|Return to draft| B
    F -->|No| H
    H -->|Approve| I
    I -->|Auto-trigger| J
    J -->|Process| K
    
    style B fill:#fff9c4
    style E fill:#ffe0b2
    style I fill:#c8e6c9
    style G fill:#ffccbc
```

### 1.2 Parts List State Machine

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> PendingReview: Submit for Review
    Draft --> [*]: Cancel
    
    PendingReview --> Draft: Request Changes
    PendingReview --> Approved: Approve
    PendingReview --> [*]: Reject
    
    Approved --> Ordered: Create PO
    Approved --> [*]: Archive
    
    Ordered --> [*]: Complete
    
    note right of Draft
        User can edit items,
        delete items, change
        quantities freely
    end note
    
    note right of PendingReview
        Awaiting approval from
        Construction Manager
    end note
    
    note right of Approved
        Ready for procurement,
        triggers AI matching
    end note
```

---

## 2. Supplier Order Consolidation Workflow

### 2.1 Consolidation Process (AI-Driven)

```mermaid
graph TD
    A["Approved Parts Lists<br/>Queue"]
    B["SupplierMatcher AI<br/>Receives List"]
    C["Group by Supplier"]
    D["Consolidate Variants<br/>Remove Duplicates"]
    E["Check Lead Times"]
    F["Within Deadline?"]
    G["Alert: Long Lead Time"]
    H["Calculate Bulk Discounts"]
    I["Generate Cost Analysis"]
    J["Recommend Best Suppliers"]
    K["OrderGenerator AI<br/>Creates POs"]
    L["Purchase Orders<br/>Ready for Submission"]
    
    A -->|Pop from queue| B
    B -->|Analyze| C
    C -->|Consolidate| D
    D -->|Query DB| E
    E -->|Check| F
    F -->|No| G
    F -->|Yes| H
    G -->|Continue| H
    H -->|Calculate savings| I
    I -->|Compare options| J
    J -->|Generate recommendations| K
    K -->|Create POs| L
    
    style A fill:#fff9c4
    style K fill:#bbdefb
    style L fill:#c8e6c9
    style G fill:#ffccbc
```

### 2.2 Consolidation Example: Two Projects, One Supplier

```
PROJECT A - Parts List:
├── Wire 12AWG (250ft roll)  x2
├── Outlets (Single Gang)     x8
└── Switches (3-Way)          x4

PROJECT B - Parts List:
├── Wire 12AWG (250ft roll)  x1
├── Wire 10AWG (250ft roll)  x2
└── Outlets (Single Gang)     x4

CONSOLIDATION BY SUPPLIER (Home Depot Supply):
├── Wire 12AWG (250ft roll)  x3  ← Combined: 2+1
├── Wire 10AWG (250ft roll)  x2
├── Outlets (Single Gang)     x12 ← Combined: 8+4
└── Switches (3-Way)          x4

BULK DISCOUNT APPLIED:
├── > 10 items: 5% discount applied
├── Total Cost: $450 (before discount)
└── Final Cost: $427.50 (after 5% discount)
```

---

## 3. Inventory Movement: Warehouse to Job Site

### 3.1 Complete Inventory Movement Workflow

```mermaid
graph TD
    A["Approved PO<br/>Received in Warehouse"]
    B["Stock Added to<br/>Warehouse Inventory"]
    C["Job Site Manager<br/>Needs Supplies"]
    D["Create Pickup Request<br/>via Office App"]
    E["Request Sent to Cloud"]
    F["Warehouse Manager<br/>Receives Notification"]
    G["Generate Pick List"]
    H["Warehouse Worker<br/>Picks Parts"]
    I["Verify Quantity<br/>Check Serial #s"]
    J["Load onto Truck"]
    K["Update Truck Inventory<br/>in Cloud"]
    L["Driver En Route<br/>to Job Site"]
    M["Arrive at Job Site"]
    N["Deliver Parts<br/>to Manager"]
    O["Job Site Manager<br/>Verifies Receipt"]
    P["Confirm Delivery<br/>in App"]
    Q["Update Job Site<br/>Inventory"]
    R["Consumption<br/>Phase Begins"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    K --> L
    L --> M
    M --> N
    N --> O
    O --> P
    P --> Q
    Q --> R
    
    style A fill:#c8e6c9
    style B fill:#c8e6c9
    style G fill:#ffe0b2
    style K fill:#bbdefb
    style Q fill:#bbdefb
    style R fill:#f8bbd0
```

### 3.2 Truck Inventory Consumption at Job Site

```mermaid
graph TD
    A["Parts Arrive<br/>at Job Site"]
    B["Stored in Staging Area"]
    C["Construction Worker<br/>Uses Parts"]
    D["Updates Job App<br/>Mark as Consumed"]
    E["Quantity Available<br/>Decreases"]
    F["Low Stock Alert?"]
    G["Alert: Request More<br/>Send to Warehouse"]
    H["Order More<br/>from Truck"]
    I["Truck Makes<br/>Return Trip"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F -->|< 20% remaining| G
    F -->|adequate| H
    G --> H
    H --> I
    
    style A fill:#bbdefb
    style E fill:#fff9c4
    style G fill:#ffccbc
    style I fill:#c8e6c9
```

---

## 4. Wire Selection Workflow: Construction Worker Perspective

### 4.1 Wire Selection Decision Tree

```mermaid
graph TD
    A["Worker: Need Wire<br/>for Project"]
    B["Job Specs Require:"]
    C["Load Parts List<br/>in Job Site App"]
    D["Search: Wire"]
    E["Filter Results<br/>by Gauge"]
    F["12 AWG Selected"]
    G["View Spec Sheet<br/>📄 PDF"]
    H["Spec Sheet shows:<br/>- Voltage Rating: 600V<br/>- Insulation: THHN<br/>- NEC Approved: Yes"]
    I["Choose Packaging"]
    J["250ft Roll<br/>or<br/>1000ft Roll?"]
    K["250ft Roll<br/>Good for small sections<br/>$89.99 per roll"]
    L["1000ft Roll<br/>Good for large runs<br/>$325.00 per roll"]
    M["Estimate Footage Needed"]
    N["Calculate Rolls Required"]
    O["Add to Parts List<br/>Quantity: X rolls"]
    P["Parts Added<br/>Ready to Order"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    J --> L
    K --> M
    L --> M
    M --> N
    N --> O
    O --> P
    
    style A fill:#fff9c4
    style G fill:#bbdefb
    style H fill:#e1f5ff
    style K fill:#c8e6c9
    style L fill:#c8e6c9
    style P fill:#c8e6c9
```

---

## 5. Wire Roll Variant Decision Logic

### 5.1 Wire Roll Selection: Cost-Benefit Analysis

```mermaid
graph TD
    A["Project Requires Wire"]
    B["Calculate Total<br/>Footage Needed"]
    C["Project = 500 feet"]
    D["Option A:<br/>250ft Rolls"]
    E["Option B:<br/>1000ft Roll"]
    F["250ft: Qty=2<br/>Cost=2 x $89.99<br/>= $179.98"]
    G["1000ft: Qty=1<br/>Cost=$325.00"]
    H["250ft: $0.36/ft"]
    I["1000ft: $0.325/ft"]
    J["Waste Analysis"]
    K["250ft: ~0 waste<br/>Uses exactly 2 rolls"]
    L["1000ft: 500ft waste<br/>Future projects?"]
    M["Decision Matrix"]
    N["Small projects &<br/>Limited budget<br/>→ 250ft Roll"]
    O["Large projects &<br/>Multiple uses<br/>→ 1000ft Roll"]
    
    A --> B
    B --> C
    C --> D
    C --> E
    D --> F
    E --> G
    F --> H
    G --> I
    H --> J
    I --> J
    J --> K
    J --> L
    K --> M
    L --> M
    M --> N
    M --> O
    
    style C fill:#fff9c4
    style F fill:#ffccbc
    style G fill:#c8e6c9
    style N fill:#c8e6c9
    style O fill:#c8e6c9
```

---

## 6. AI Task Processing Workflow

### 6.1 Master Queue and Dependency Chain

```mermaid
graph TD
    A["Parts Lists<br/>Submitted as Files"]
    B["Server Watches Folder<br/>NEW FILE EVENT"]
    C["SupplierMatcher Task<br/>ID: task-001"]
    D["Dependencies?"]
    E["None - can start"]
    F["Analyze parts lists"]
    G["Generate PO recommendations"]
    H["Complete task-001"]
    I["OrderGenerator Task<br/>ID: task-002"]
    J["Dependencies?"]
    K["Needs: task-001 output"]
    L["task-001 complete?"]
    M["Yes - can start"]
    N["Create purchase orders"]
    O["Submit to suppliers"]
    P["Complete task-002"]
    Q["Watch for Next File<br/>or Wait"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    K --> L
    L -->|Yes| M
    L -->|No| Q
    M --> N
    N --> O
    O --> P
    P --> Q
    
    style C fill:#e1f5ff
    style I fill:#e1f5ff
    style L fill:#fff9c4
    style H fill:#c8e6c9
    style P fill:#c8e6c9
```

### 6.2 Task Execution with Error Handling

```mermaid
graph TD
    A["Task Start:<br/>ProcessPartsList"]
    B["Step 1: Validate<br/>All parts exist?"]
    C["Validation Passed"]
    D["Step 2: Check<br/>Supplier Availability"]
    E["Data Retrieved"]
    F["Step 3: Consolidate<br/>by Supplier"]
    G["Orders Generated"]
    H["Step 4: Calculate<br/>Bulk Discounts"]
    I["Discount Applied"]
    J["Step 5: Generate<br/>Report"]
    K["Task Complete"]
    L["Validation Failed"]
    M["Log Error:<br/>Missing parts"]
    N["Alert Manager"]
    O["Task Failed"]
    P["Supplier Data<br/>Unavailable"]
    Q["Retry: Wait 30s"]
    R["Retry count<br/>< 3?"]
    S["Yes: Retry"]
    T["No: Escalate"]
    
    A --> B
    B -->|Pass| C
    B -->|Fail| L
    C --> D
    D -->|Success| E
    D -->|Failure| P
    P --> Q
    Q --> R
    R -->|Yes| S
    R -->|No| T
    S --> D
    T --> O
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    L --> M
    M --> N
    N --> O
    
    style K fill:#c8e6c9
    style O fill:#ffccbc
    style P fill:#fff9c4
    style R fill:#fff9c4
```

---

## 7. Device Cloud Sync Sequence Diagram

### 7.1 Request/Response File Watching Cycle

```mermaid
sequenceDiagram
    participant Truck as Truck App<br/>SQLite Cache
    participant Cloud as Cloud Storage<br/>SharePoint
    participant Sync as Cloud Sync Service<br/>Desktop App
    participant LocalFS as Local Server<br/>Hard Drive
    participant Server as Server Agent<br/>PostgreSQL
    
    Truck->>Truck: User requests parts list
    Truck->>Truck: Check local cache (SQLite)
    Truck->>Cloud: Write request JSON<br/>get_parts.json
    Cloud->>Sync: Sync to server drive
    Sync->>LocalFS: Write to local folder<br/>/Cloud/Requests/truck-001/
    LocalFS->>Server: FILE CREATED EVENT
    Server->>LocalFS: Read new request file
    Server->>Server: Process request
    Server->>Server: Query PostgreSQL
    Server->>LocalFS: Write response JSON<br/>/Cloud/Responses/truck-001/
    LocalFS->>Sync: Detect new file
    Sync->>Cloud: Sync response to cloud
    Truck->>Cloud: Poll for response<br/>(every 2s)
    Truck->>Cloud: Read response
    Truck->>Truck: Update local cache (SQLite)
    Truck->>Truck: Display to user
    
    Note over Truck,Server: Server watches files, not polling cloud<br/>Cloud service handles sync automatically
```

### 7.2 Multi-Step Request with Dependencies

```mermaid
graph TD
    A["Device: Create Parts List<br/>request_id: req-001"]
    B["Write to Cloud:<br/>/Requests/truck-001/"]
    C["Server Polls<br/>Reads req-001"]
    D["Process & Validate"]
    E["Write Response<br/>parts_list_id: pl-001"]
    F["Device Polls<br/>Reads Response"]
    G["User: Submit List<br/>for Approval<br/>request_id: req-002"]
    H["Write to Cloud:<br/>Includes parts_list_id"]
    I["Server Polls<br/>Reads req-002"]
    J["Process: Mark Approved"]
    K["Trigger AI: SupplierMatcher<br/>request_id: task-001"]
    L["Write Response<br/>Ready for PO"]
    M["Device Polls<br/>Reads Response"]
    N["User Sees:<br/>Ready to Order"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    K --> L
    L --> M
    M --> N
    
    style E fill:#c8e6c9
    style L fill:#c8e6c9
    style N fill:#c8e6c9
```

---

## 8. Return and Credit Processing Workflow

### 8.1 Return Request to Supplier

```mermaid
graph TD
    A["Defective Part<br/>Received at Job Site"]
    B["Worker Reports<br/>via Job App"]
    C["Photo Evidence<br/>+ Notes"]
    D["Return Request<br/>Created"]
    E["Sent to Cloud"]
    F["Warehouse Manager<br/>Reviews"]
    G["Inspect Part<br/>Confirm Defect"]
    H["Initiate Return<br/>with Supplier"]
    I["Generate RMA<br/>Return Number"]
    J["Ship Part<br/>Back"]
    K["Track Return"]
    L["Supplier Receives"]
    M["Inspection by Supplier"]
    N["Issue Credit<br/>or Replacement"]
    O["Update Inventory"]
    P["Return Complete"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    K --> L
    L --> M
    M --> N
    N --> O
    O --> P
    
    style D fill:#fff9c4
    style I fill:#ffe0b2
    style N fill:#c8e6c9
    style P fill:#c8e6c9
```

---

## 9. System-Wide Swimlane: Complete Parts-to-Delivery Flow

```mermaid
graph TD
    A["📋 OFFICE PHASE"]
    B["Manager: Create Parts List"]
    C["Office App → Cloud"]
    D["Submit for Review"]
    E["Manager: Review & Annotate"]
    F["Approve"]
    
    G["🤖 AI PHASE"]
    H["SupplierMatcher Analyzes"]
    I["Finds Best Suppliers"]
    J["Calculates Bulk Discounts"]
    
    K["📦 PROCUREMENT PHASE"]
    L["OrderGenerator Creates POs"]
    M["Submit to Suppliers"]
    N["Suppliers Confirm"]
    O["Set Delivery Date"]
    
    P["🏭 WAREHOUSE PHASE"]
    Q["Receive Stock"]
    R["Add to Warehouse Inventory"]
    S["Job Site Requests Items"]
    T["Pick and Pack"]
    U["Load onto Truck"]
    
    V["🚚 DELIVERY PHASE"]
    W["Truck in Transit"]
    X["Arrive at Job Site"]
    Y["Unload Parts"]
    
    Z["🏗️ JOB SITE PHASE"]
    AA["Manager Verifies Delivery"]
    AB["Parts Added to Job Inventory"]
    AC["Workers Use Parts"]
    AD["Track Consumption"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    K --> L
    L --> M
    M --> N
    N --> O
    O --> P
    P --> Q
    Q --> R
    R --> S
    S --> T
    T --> U
    U --> V
    V --> W
    W --> X
    X --> Y
    Y --> Z
    Z --> AA
    AA --> AB
    AB --> AC
    AC --> AD
    
    style A fill:#f8bbd0
    style G fill:#e1f5ff
    style K fill:#ffe0b2
    style P fill:#c8e6c9
    style V fill:#bbdefb
    style Z fill:#f8bbd0
```

---

## 10. User Request Approval Workflow

### 10.1 RequestRouter AI Decision Flow

```mermaid
graph TD
    A["Device Request File<br/>Created in Cloud"]
    B["Server Detects<br/>New File Event"]
    C["RequestRouter AI<br/>Analyzes Request"]
    D["Get User Role<br/>and Permissions"]
    E["Request Type?"]
    F["User Edit Request"]
    G["Inventory/Part Request"]
    H["Check Edit Permission"]
    I["Permission Granted?"]
    J["Requires Approval?"]
    K["Auto-Approve"]
    L["Find Approvers"]
    M["Route to Manager"]
    N["Manager Approves?"]
    O["Process Request"]
    P["Create Response"]
    Q["Auto-Reject<br/>Insufficient Permission"]
    R["Check Cost Threshold"]
    S["Under Threshold?"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    E --> G
    F --> H
    H --> I
    I -->|Yes| J
    I -->|No| Q
    J -->|No| K
    J -->|Yes| L
    L --> M
    M --> N
    N -->|Yes| O
    N -->|No| Q
    G --> R
    R --> S
    S -->|Yes| K
    S -->|No| L
    K --> O
    O --> P
    Q --> P
    
    style K fill:#c8e6c9
    style Q fill:#ffccbc
    style P fill:#bbdefb
```

### 10.2 Tree Hierarchy Approval Routing

**Scenario: Supervisor Edits Same-Level User in Different Branch**

```mermaid
graph TB
    subgraph "Role Tree Structure"
        L0["Admin (Level 0)"]
        L1A["Project Manager A (L1)"]
        L1B["Warehouse Manager (L1)"]
        L2A1["Site Supervisor - Commercial (L2)"]
        L2A2["Site Supervisor - Residential (L2)"]
        L2B["Warehouse Worker (L2)"]
        
        L0 --> L1A
        L0 --> L1B
        L1A --> L2A1
        L1A --> L2A2
        L1B --> L2B
    end
    
    REQ["Site Supervisor Commercial<br/>Requests to Edit:<br/>Site Supervisor Residential"]
    CHECK["Check: Same Level (L2),<br/>Same Tree (both under PM A)"]
    PERM["Role Setting:<br/>can_edit_same_level=true"]
    APPR["requires_approval=true"]
    ROUTE["Route to Common Ancestor:<br/>Project Manager A"]
    DECIDE["PM A Reviews Request"]
    EXEC["Execute Edit"]
    DENY["Deny Request"]
    
    REQ --> CHECK
    CHECK --> PERM
    PERM -->|Granted| APPR
    APPR -->|Needs Approval| ROUTE
    ROUTE --> DECIDE
    DECIDE -->|Approve| EXEC
    DECIDE -->|Reject| DENY
    
    style REQ fill:#fff9c4
    style PERM fill:#c8e6c9
    style ROUTE fill:#bbdefb
    style EXEC fill:#c8e6c9
    style DENY fill:#ffccbc
```

### 10.3 Exception-Based Approval Bypass

**Scenario: Worker with Special Exception**

```mermaid
graph LR
    A["Worker (Level 3)<br/>Requests Edit"]
    B["Check Exception List"]
    C["Exception Found?"]
    D["Exception Active<br/>Not Expired?"]
    E["Grant Permission"]
    F["Check Normal Permissions"]
    G["Denied<br/>Workers Cannot Edit"]
    H["Process Request"]
    
    A --> B
    B --> C
    C -->|Yes| D
    C -->|No| F
    D -->|Yes| E
    D -->|No| F
    E --> H
    F --> G
    
    style C fill:#fff9c4
    style E fill:#c8e6c9
    style G fill:#ffccbc
    style H fill:#bbdefb
```

### 10.4 Cost-Based Approval Escalation

**Scenario: Request Exceeds Role's Approval Threshold**

```mermaid
graph TD
    A["User Request<br/>for $8000 Part"]
    B["Get User Role"]
    C["approval_threshold_amount<br/>= $5000"]
    D["Request Cost > Threshold?"]
    E["Find Parent Role<br/>in Tree"]
    F["Parent Can Approve<br/>Subordinate Requests?"]
    G["Route to Parent"]
    H["Parent Approves?"]
    I["Process Request"]
    J["Deny Request"]
    K["Escalate to<br/>Next Level Up"]
    L["Auto-Approve<br/>Under Threshold"]
    
    A --> B
    B --> C
    C --> D
    D -->|Yes $8000 > $5000| E
    D -->|No| L
    E --> F
    F -->|Yes| G
    F -->|No| K
    G --> H
    H -->|Yes| I
    H -->|No| J
    L --> I
    
    style D fill:#fff9c4
    style L fill:#c8e6c9
    style I fill:#c8e6c9
    style J fill:#ffccbc
```

### 10.5 Multi-Stage Approval Workflow

**Complex Request Requiring Multiple Approvals**

```mermaid
sequenceDiagram
    participant User as Worker (Level 3)
    participant AI as RequestRouter AI
    participant Sup as Supervisor (Level 2)
    participant Mgr as Manager (Level 1)
    participant DB as Database
    
    User->>AI: Submit High-Value Request ($15,000)
    AI->>AI: Check user role settings
    AI->>AI: $15,000 > worker threshold
    AI->>Sup: Route to Supervisor
    Note over Sup: Reviews request details
    Sup->>AI: Approve (but $15,000 > supervisor threshold too)
    AI->>AI: Check supervisor threshold ($10,000)
    AI->>AI: Still exceeds, escalate
    AI->>Mgr: Route to Manager
    Note over Mgr: Final approval authority
    Mgr->>AI: Approve request
    AI->>DB: Execute request
    DB->>AI: Confirm completion
    AI->>User: Response: Approved & Processed
```

### 10.6 Approval Timeout & Auto-Escalation

**Handling Delayed Approvals**

```mermaid
graph TD
    A["Request Submitted"]
    B["Route to Approver"]
    C["Start 24-Hour Timer"]
    D["Approver Responds?"]
    E["Within 24 Hours?"]
    F["Process Approval"]
    G["Timeout Reached"]
    H["Send Reminder<br/>Notification"]
    I["Start 12-Hour<br/>Extension Timer"]
    J["Still No Response?"]
    K["Auto-Escalate to<br/>Next Level Up"]
    L["Notify Original Approver<br/>of Escalation"]
    
    A --> B
    B --> C
    C --> D
    D -->|Yes| F
    D -->|No| E
    E -->|Yes| F
    E -->|No| G
    G --> H
    H --> I
    I --> J
    J -->|No Response| K
    J -->|Responded| F
    K --> L
    
    style G fill:#fff9c4
    style K fill:#ffe0b2
    style F fill:#c8e6c9
```

---

## 11. Error Handling and Escalation Workflow

### 10.1 Common Error Scenarios

```mermaid
graph TD
    A["Request to Server"]
    B["Server Processes"]
    C["Success?"]
    D["Return Response"]
    E["Device Displays<br/>Result"]
    F["Error Occurred"]
    G["Error Type?"]
    H["Validation Error<br/>e.g., part not found"]
    I["Temporarily Unavailable<br/>e.g., supplier data"]
    J["System Error<br/>e.g., DB connection"]
    K["Return Error<br/>with Details"]
    L["Device Shows<br/>Error Message"]
    M["Retry Logic"]
    N["Retry 3 times<br/>with 5s delay"]
    O["Succeeded?"]
    P["Continue"]
    Q["Alert Manager<br/>Escalate"]
    R["Manager Reviews<br/>Cloud Log Files"]
    S["Determine Root Cause"]
    T["Resolve"]
    
    A --> B
    B --> C
    C -->|Yes| D
    C -->|No| F
    D --> E
    F --> G
    G --> H
    G --> I
    G --> J
    H --> K
    I --> M
    J --> Q
    K --> L
    M --> N
    N --> O
    O -->|Yes| P
    O -->|No| Q
    Q --> R
    R --> S
    S --> T
    
    style D fill:#c8e6c9
    style K fill:#fff9c4
    style Q fill:#ffccbc
    style T fill:#c8e6c9
```

---

## 11. Offline Workflow: No Cloud Connectivity

### 11.1 Device Operating Without Cloud Access

```mermaid
graph TD
    A["Network Connection Lost"]
    B["Device Operating<br/>in Offline Mode"]
    C["SQLite Cache<br/>Has Data"]
    D["User: View Parts List"]
    E["Load from<br/>Local Cache"]
    F["Display Last<br/>Synced Data"]
    G["User: Create<br/>Offline Note"]
    H["Store Locally<br/>in Cache"]
    I["Connection Restored"]
    J["Sync Queue Processes"]
    K["Send All Pending<br/>Requests"]
    L["Receive Responses"]
    M["Update Cache<br/>with Results"]
    N["Notify User<br/>Sync Complete"]
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    I --> J
    J --> K
    K --> L
    L --> M
    M --> N
    
    style B fill:#fff9c4
    style F fill:#bbdefb
    style N fill:#c8e6c9
```

---

## 12. Glossary: Workflow Terms

| Term | Definition |
|------|-----------|
| **Parts List** | Collection of parts needed for a specific project, submitted for approval |
| **Variant** | Specific configuration of a part (e.g., 250ft vs 1000ft wire roll) |
| **SKU** | Stock Keeping Unit; unique identifier for a variant |
| **Lead Time** | Days from order to delivery from supplier |
| **Consolidation** | Combining multiple parts lists into fewer orders for bulk pricing |
| **RMA** | Return Merchandise Authorization; reference number for returning items |
| **Bulk Discount** | Percentage discount applied when ordering quantities above threshold |
| **Spec Sheet** | PDF documentation of part specifications and certifications |
| **Device Sync** | Process of device writing request to cloud and polling for response |
| **Cloud Storage** | SharePoint or Google Drive used as inter-app message broker |
| **Offline Mode** | Device operating without cloud connectivity, using local cache |
| **AI Agent** | Autonomous subprocess (PartsSpecialist, SupplierMatcher, OrderGenerator) |
| **Task Dependency** | Condition that one task must complete before another can start |
| **Poll** | Device/Server repeatedly checking cloud folder for new files |
| **Request/Response** | JSON file pair: device writes request, server writes response |
