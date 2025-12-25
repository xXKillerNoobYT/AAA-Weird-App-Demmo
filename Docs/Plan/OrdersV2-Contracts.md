# Orders v2 – Contracts and Validation

## Scope

- Endpoints covered: GET /api/v2/orders/{orderId}, PATCH /api/v2/orders/{orderId}, POST /api/v2/orders/{orderId}/approve, GET /api/v2/orders.
- Applies to OrdersControllerV2 Wave 5 implementation.
- Audience: backend devs, QA, API consumers.

## Authentication & Authorization

- All endpoints require authentication (`[Authorize]`).
- Recommended roles/claims (align with policy naming when added):
  - orders.read → GET single/list
  - orders.write → PATCH, approve, create
  - orders.approve → POST approve (separate from write if desired)
- Dev bypass: LocalDevApiKeyMiddleware in Development may allow key-based inject; still keep contracts identical.

## Status Model

- Allowed statuses: pending, approved, shipped, delivered, cancelled.
- Transitions (enforced today in PATCH):
  - pending → approved | cancelled
  - approved → shipped | cancelled
  - shipped → delivered
  - delivered → (terminal)
  - cancelled → (terminal)
- Approve endpoint forces pending → approved.
- Timestamps: shippedAt set on first transition to shipped; deliveredAt set on first transition to delivered.

## Data Contracts

### OrderResponse (GET/PATCH/approve/creation)

- id (Guid)
- requestId (Guid?)
- status (string, lowercase)
- totalAmount (decimal)
- createdAt (DateTime)
- shippedAt (DateTime?)
- deliveredAt (DateTime?)
- availabilityWarnings (string[], optional; informational)
- items: array of OrderItemResponse

### OrderItemResponse

- id (Guid)
- partId (Guid)
- partCode (string?)
- partName (string?)
- locationId (Guid?)
- locationName (string?)
- quantity (int, >0)
- unitPrice (decimal, ≥0)
- lineAmount (decimal, = quantity * unitPrice)

### OrderSummary (list item)

- id, requestId, status, totalAmount, itemCount, createdAt, shippedAt, deliveredAt

### PaginationMetadata

- page (int ≥1)
- pageSize (int 1–100)
- totalItems (int ≥0)
- totalPages (int ≥1 when totalItems>0 else 1)

## Requests & Validation

### GET /api/v2/orders/{orderId}

- Path: orderId (Guid, required)
- Responses:
  - 200 OrderResponse
  - 404 NOT_FOUND if missing

### PATCH /api/v2/orders/{orderId}

- Path: orderId (Guid, required)
- Body: UpdateOrderRequest
  - status (string?) — must follow transitions above; lowercase
  - notes (string?) — stored in history entry detail
- Validation:
  - order must exist (404)
  - status, if provided, must be in allowed set and allowed from current status (400 INVALID_OPERATION)
- Effects:
  - Updates status and timestamps (shippedAt, deliveredAt) as applicable
  - Adds OrderHistory event "status_changed_{newStatus}" with notes
- Responses: 200 OrderResponse | 400 INVALID_OPERATION | 404 NOT_FOUND

### POST /api/v2/orders/{orderId}/approve

- Path: orderId (Guid, required)
- Body: ApproveOrderRequest
  - notes (string?)
- Validation:
  - order must exist (404)
  - current status must be pending (400 INVALID_OPERATION)
  - authenticated user id required (401)
- Effects:
  - Sets status to approved
  - Creates OrderApproval record (status=approved, approverId, timestamps, notes)
  - Adds OrderHistory event "approved"
- Responses: 200 OrderResponse | 400 INVALID_OPERATION | 401 UNAUTHORIZED | 404 NOT_FOUND

### GET /api/v2/orders

- Query params (optional):
  - status (string; filter exact, lowercase)
  - startDate (DateTime; createdAt >=)
  - endDate (DateTime; createdAt <=)
  - page (int ≥1; default 1)
  - pageSize (int 1–100; default 20)
- Response 200 OrderListResponse (orders[], pagination)
- Validation: clamp page/pageSize bounds; invalid values default
- Future: add userId/customer filters, sort, richer filter set

## Error Model

- Shape: { message, code, status }
- Codes in use: VALIDATION_ERROR, NOT_FOUND, INVALID_OPERATION, UNAUTHORIZED, INTERNAL_ERROR.
- HTTP mapping: 400/401/404/500 as indicated per endpoint.

## Tests to add

- Approve: cannot approve non-pending; 401 when user missing; 404 missing order; success sets approval + history.
- Patch: transition matrix enforcement; timestamps set; history entry contains notes; 404 missing order.
- Get single: 404 missing; 200 with items and amounts.
- List: pagination bounds; status filter; page/count math.

## Open questions / future items

- Authorization policies/claims names to be finalized.
- Concurrency/ETag for PATCH to prevent lost updates.
- Additional filters (customer, location, status set, date ranges) and sorting options.
- Include approvals/history in GET order response? (currently not returned; consider adding lightweight summaries.)
