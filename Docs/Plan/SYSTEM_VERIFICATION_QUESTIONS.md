# WeirdToo Parts System - Verification Questions

**Document Purpose**: Comprehensive verification of all system design decisions  
**Total Questions**: 75 questions across 5 sessions  
**Format**: Multiple choice (A/B/C/D) with option for custom answer  
**Status**: Sessions 1-5 completed; concise decisions logged in `SYSTEM_DECISIONS_LOG.md` (see Q tags); full answers remain below for reference

---

## PROGRESS TRACKER

| Session | Topic Area | Questions | Status |
| -------- | ----------- | ----------- | ------ |
| 1 | Core Architecture & Communication | Q1-15 | ✅ COMPLETED |
| 2 | Parts Management & Inventory | Q16-30 | ✅ COMPLETED |
| 3 | User Roles & Permissions | Q31-45 | ✅ COMPLETED |
| 4 | AI & Automation | Q46-60 | ✅ COMPLETED |
| 5 | Deployment & Operations | Q61-75 | ✅ COMPLETED |

**Traceability Note**: For concise summaries with Q-number tags, see `SYSTEM_DECISIONS_LOG.md` → “Verification Decisions (Traceability)”. Long-form narratives stay in this document.

---

## SESSION 1: CORE ARCHITECTURE & COMMUNICATION ✅

### Q1: How should devices check for responses from the server?

- **A)** Direct HTTP polling to server API every 5 minutes
- **B)** WebSocket connection for real-time updates
- **C)** Smart polling of cloud storage folder (2min when active, 1hr work hours, 12hr off-hours)
- **D)** Custom answer: _______________

**✅ ANSWER: C** - Smart polling with variable intervals based on app state and time of day

---

### Q2: Which cloud storage provider should be used for production?

- **A)** Dropbox
- **B)** SharePoint (primary) with Google Drive for beta testing
- **C)** Amazon S3
- **D)** Custom answer: _______________

**✅ ANSWER: B** - SharePoint primary, Google Drive beta, OneDrive tertiary

---

### Q3: How long can devices operate offline?

- **A)** No offline support - always require internet
- **B)** 24 hours with local SQLite cache
- **C)** Unlimited offline with request queue, sync when online
- **D)** Custom answer: _______________

**✅ ANSWER: C** - Unlimited offline with SQLite caching and request queuing

---

### Q4: When should old request files be deleted from cloud storage?

- **A)** Immediately after processing
- **B)** After 90 days (moved to archive first)
- **C)** Never delete - keep all historical data
- **D)** Custom answer: _______________

**✅ ANSWER: B** - 90 days retention, then archive, then delete archives after 6 months

---

### Q5: What database should be used for the server?

- **A)** MySQL
- **B)** PostgreSQL 14+
- **C)** MongoDB
- **D)** Custom answer: _______________

**✅ ANSWER: B** - PostgreSQL for server authority database

---

### Q6: How should users authenticate to the system?

- **A)** Username/password stored in WeirdToo database
- **B)** OAuth2 via cloud storage provider (Google/Microsoft)
- **C)** Active Directory integration
- **D)** Custom answer: _______________

**✅ ANSWER: B** - OAuth2 through cloud storage APIs (no passwords in system)

---

### Q7: Where should the server be hosted?

- **A)** Cloud VM (AWS/Azure)
- **B)** On-premises at company office
- **C)** Not yet decided - need requirements analysis
- **D)** Custom answer: _______________

**✅ ANSWER: C** - Not yet decided, requires evaluation of company infrastructure

---

### Q8: How many warehouses will the system support initially?

- **A)** Single warehouse only
- **B)** Multiple warehouses with department-based separation
- **C)** Not yet decided - depends on company structure
- **D)** Custom answer: _______________

**✅ ANSWER: D** - Single warehouse compatible multi warehouse options with multiple points for delivery and pickup in returns. Be able to select whether or not a part has to be returned to the specific warehouse or not. Arts management forget things to where they belong. In handling shipping. Or personally gonna start out with building this for a single warehouse, but. We want multiple warehouses dash shops. To be optional. As many as needed.

---

### Q9: How many trucks need mobile access?

- **A)** 5-10 trucks
- **B)** 20+ trucks
- **C)** Not yet decided - depends on company fleet size
- **D)** Custom answer: _______________

**✅ ANSWER: D** - Not yet decided, system designed to scale for any fleet size Anywhere from 1+ or more trucks. Each truck will be assigned to a user. And each user will have their own account. One user can have. Several trucks.

---

### Q10: How many simultaneous job sites should be supported?

- **A)** 1-5 active job sites
- **B)** 10+ active job sites
- **C)** Unlimited - system scales based on department structure
- **D)** Custom answer: _______________

**✅ ANSWER: C** - Unlimited via department assignment (each job site is a department) Active jobs may number anywhere from one. To easily. Anything avove that. Unlimited. As many as needed.

---

### Q11: How should file naming conflicts be prevented?

- **A)** Timestamp-based naming (risk of collision)
- **B)** Random number + device + user + timestamp (virtually collision-proof)
- **C)** Sequential numbering managed by server
- **D)** Custom answer: _______________

**✅ ANSWER: B** - 8-char random + device + user + timestamp format

---

### Q12: What happens if device loses internet mid-request?

- **A)** Request fails and user must retry
- **B)** Request queued locally, auto-uploads when reconnected
- **C)** Request stored temporarily, requires manual re-submission
- **D)** Custom answer: _______________

**✅ ANSWER: B** - SQLite queue holds requests, cloud sync service uploads when online

---

### Q13: How long should devices wait for response before timeout?

- **A)** 30 seconds (real-time requirement)
- **B)** 5 minutes (standard API timeout)
- **C)** No fixed timeout - poll until response appears (with 2min/1hr/12hr intervals)
- **D)** Custom answer: _______________

**✅ ANSWER: C** - Smart polling continues until response found (no timeout) It sends out the request and then looks for the responding Response. In the updated public database. Appropriately for that location. I wonder what the request is. I think it should check for a response every so often. Based on whether or not the app is active. If the app is active, it should check every two minutes. If the app is in the background during work hours, it should check every hour. If the app is in the background outside of work hours, it should check every 12 hours.

---

### Q14: How should the AI prioritize incoming requests?

- **A)** First-in, first-out (FIFO) processing
- **B)** Priority based on keywords (emergency, urgent), user role, location, and cost Deadline. And time waiting in queue.
- **C)** Random processing
- **D)** Custom answer: _______________

**✅ ANSWER: B** - P5 > P4 > P3 > P2 > P1 > P0 LOW based on AI analysis. Keywords, role, location, cost, deadline, time waiting I wanted to add weights to all of this. Keywords such as emergency and urgent. In the notes not the arts themselves, but in notes for the arts list should have higher priority. Kick up by two over standard or something. We should have a time weight average mechanism. That way if we have an item. For each of the priority levels, and they're all the same priority baseline. The one that's been there the longest will be a higher priority. And then a. Deadline mechanism. We want everything processed a day before the deadline, if possible. So a day before the deadline that there is a key factor in this. 6 minutes. And we should also have deadline time hit but task has not been finished yet thing so like for example. If the task. Involves sorting these parts into an order list for one of the suppliers. And that email has not been sent out to the supplier yet by the user. To continue adding parts there until the email has been set out and. The next deadline for the order has been set. We want intelligent deadline management. That changes based off of what's happened. What's happening. And how long it's been there, the longer it's been there. That should add weight to the factor. Kind of averaged out over all the tasks there. So if a task is a priority one. But it's been there the longest. Of the priority once, move it up to a priority two. Rinse and repeat all the way through. Let's be intelligent. We want to keep track of the original riority and do time analysis. In accordance with. Wait processing. You have the after results priority, so there's the task priority, as in the individual task by itself, no weights, and then the weighted priority. And we want processing done off of the weighted priority.

---

### Q15: How many category levels should the parts hierarchy support?

- **A)** Fixed 3 levels (sufficient for most use cases)
- **B)** Fixed 5 levels (as originally requested)
- **C)** Unlimited levels with full_path tracking
- **D)** Custom answer: _______________

**✅ ANSWER: C** - 5+ unlimited levels with VARCHAR(1000) full_path field

---

## SESSION 2: PARTS MANAGEMENT & INVENTORY ✅

### Q16: Should "brand" and "manufacturer" be separate fields?

- **A)** Yes - keep separate for detailed tracking
- **B)** No - merge into single "brand" field (Milwaukee, DeWalt, etc.)
- **C)** Depends on reporting requirements
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Merge into single `brand`; manufacturer captured as optional alias/normalized brand entry.

---

### Q17: How should part numbers be structured?

- **A)** Use manufacturer's part numbers only
- **B)** System-generated internal SKU (WTP-{CATEGORY}-{SEQ}) + cross-reference table
- **C)** User-defined custom part numbers
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Internal SKU `WTP-{CATEGORY}-{SEQ}` plus PART_NUMBER_XREF for manufacturer/supplier numbers.

---

### Q18: Should the system track part location within warehouse?

- **A)** Yes - aisle, shelf, bin location tracking
- **B)** No - warehouse-level tracking is sufficient
- **C)** Optional feature for future enhancement
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Track aisle/shelf/bin; make per-part toggleable for simpler sites.

---

### Q19: How should measurement unit conversions be handled?

- **A)** Store only in standard metric units
- **B)** Store in entered units + display preference per user
- **C)** No conversion - use entered units only
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Store canonical metric + entered unit; display respects user/company preference (default imperial allowed).

---

### Q20: Should the system support multiple suppliers for same part?

- **A)** Yes - track all suppliers with pricing
- **B)** No - single primary supplier only
- **C)** Primary supplier + backup supplier
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Support many suppliers with pricing/lead times; allow “brand-agnostic” price-first selection.

---

### Q21: How should part images be stored?

- **A)** In PostgreSQL database as BYTEA
- **B)** In cloud storage with URL references in database
- **C)** No image support needed
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Store images in cloud storage; DB keeps stable path/URL (deterministic folders/filenames).

---

### Q22: Should parts have revision/version tracking?

- **A)** Yes - full version history with change log
- **B)** No - always use current version only
- **C)** Simple modified_at timestamp tracking
- **D)** Custom answer: _______________

**✅ ANSWER: C** — No formal versioning; track `modified_at`, use replacement parts pattern.

---

### Q23: How should discontinued parts be handled?

- **A)** Delete from database entirely
- **B)** Mark as inactive/discontinued but keep historical data
- **C)** Move to separate archive database
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Mark discontinued/inactive but retain history for reporting/audit.

---

### Q24: Should the system track part warranties?

- **A)** Yes - warranty expiration and claim tracking
- **B)** No - not needed for parts management
- **C)** Future enhancement after core features
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Track part warranty expirations plus job warranty windows (job close/reopen dates).

---

### Q25: How should inventory counts be updated?

- **A)** Real-time updates on every transaction
- **B)** Batch updates at end of day
- **C)** Server-only updates via request/response files
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Server-only updates via request/response files; real-time per transaction with server as authority.

---

### Q26: Should the system support kits/assemblies?

- **A)** Yes - kits containing multiple parts
- **B)** No - individual parts only
- **C)** Future enhancement
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Support kits/assemblies; kit inventory rolls up from components.

---

### Q27: How should minimum stock levels be managed?

- **A)** Global min/max levels per part
- **B)** Different levels per warehouse/department
- **C)** No automatic reorder tracking
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Min levels per warehouse/department/truck/job site (not global only).

---

### Q28: Should the system track part costs/pricing?

- **A)** Yes - purchase cost and sell price
- **B)** Purchase cost only (no selling)
- **C)** No pricing - quantities only
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Track purchase cost and optional sell price; keep pricing history when available.

---

### Q29: How should bulk import of parts be handled?

- **A)** CSV upload via web interface
- **B)** Excel file processing
- **C)** Request-based import (file uploaded to cloud, server processes)
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Request-based import of CSV/Excel via cloud upload; server processes (app UI may upload file).

---

### Q30: Should barcodes/QR codes be supported?

- **A)** Yes - barcode scanning for quick lookup
- **B)** No - manual search only
- **C)** Future enhancement
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Support barcode/QR scanning and generation using internal SKU or manufacturer number.

---

## SESSION 3: USER ROLES & PERMISSIONS ✅

### Q31: How many role hierarchy levels should be supported?

- **A)** Fixed 4 levels (Admin, Manager, Supervisor, Worker)
- **B)** Unlimited levels with customizable tree
- **C)** No hierarchy - flat role structure
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Flexible tree; can be flat by configuration (no forced fixed levels).

---

### Q32: Can users have multiple roles simultaneously?

- **A)** Yes - multiple role assignment
- **B)** No - single role per user
- **C)** Single role + permission exceptions
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Users can hold multiple roles; permissions are unioned.

---

### Q33: Should same-level users be able to edit each other?

- **A)** Never allowed
- **B)** Always allowed
- **C)** Configurable checkbox per role
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Same-level edits configurable per role (`can_edit_same_level`).

---

### Q34: Can users edit across different tree branches?

- **A)** Never allowed
- **B)** Always allowed
- **C)** Configurable checkbox per role with approval required
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Cross-branch edits configurable per role with approval.

---

### Q35: How should temporary permission grants be handled?

- **A)** No temporary permissions - only permanent role changes
- **B)** Exception list with optional expiration dates
- **C)** Time-limited role assignments
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Temporary grants via exception list with optional expiry; supports user-submitted change requests for review.

---

### Q36: Should users be able to view all inventory or only their department?

- **A)** All users see all inventory
- **B)** Only assigned department inventory
- **C)** Configurable per role (can_view_all_inventory checkbox)
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Inventory visibility configurable per role (`can_view_all_inventory`); defaults can allow “see all.”

---

### Q37: Can users be assigned to multiple departments?

- **A)** Yes - multi-department with primary flag
- **B)** No - single department only
- **C)** Not needed - users are department-agnostic
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Users may belong to multiple departments with a primary flag.

---

### Q38: How should approval thresholds be configured?

- **A)** Global threshold for all users ($500)
- **B)** Per-role approval_threshold_amount field
- **C)** No thresholds - all requests require approval
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Approval thresholds configured per role (`approval_threshold_amount`).

---

### Q39: Should workers ever be able to approve requests?

- **A)** No - workers cannot approve anything
- **B)** Yes - if can_approve_subordinate_requests checkbox enabled
- **C)** Only for specific request types (configurable)
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Workers may approve subordinate requests if flag enabled.

---

### Q40: How should admin override privileges work?

- **A)** Admins can do anything without approval
- **B)** Admins still require approval for high-value changes
- **C)** Configurable - admin role has requires_approval checkbox
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Admins can do anything; maintain full audit logging.

---

### Q41: Should user creation be restricted to admins only?

- **A)** Yes - only admins can create users
- **B)** Managers can create users in their tree
- **C)** Configurable via can_create_users checkbox
- **D)** Custom answer: _______________

**✅ ANSWER: A** — User creation is admin-only; managers may request creation for admin approval.

---

### Q42: Can users delete other users or only deactivate?

- **A)** Delete allowed with can_delete_users permission
- **B)** Deactivate only - never delete users
- **C)** Soft delete with retention period
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Deactivate only; deletion not allowed. Deactivation requests follow approval chain.

---

### Q43: How should role changes be tracked?

- **A)** Audit log table with all role assignments/changes
- **B)** Simple modified_at timestamp
- **C)** No tracking - current role only
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Track all role assignments/changes in an audit log.

---

### Q44: Should there be read-only user roles?

- **A)** Yes - reporting/viewing access only
- **B)** No - all users can submit requests
- **C)** Configurable via permission checkboxes
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Provide read-only/report roles with optional commenting, shareable read-only access, optional expiry, and log purge after expiry.

---

### Q45: How should cross-department approvals work?

- **A)** Each department has independent approval chains
- **B)** Cross-department requests route to common manager
- **C)** Not applicable - no cross-department edits allowed
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Each department keeps independent approval chains; cross-dept requests use normal comms, not shared chain.

---

## SESSION 4: AI & AUTOMATION ✅

### Q46: Which AI framework should be used?

- **A)** Custom AI implementation
- **B)** Microsoft Autogen with RequestRouter agent
- **C)** OpenAI API directly
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Microsoft Autogen with RequestRouter agent.

---

### Q47: Should AI automatically approve low-value requests?

- **A)** Yes - requests under threshold auto-approved
- **B)** No - all requests require human approval
- **C)** Configurable per role via requires_approval checkbox
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Auto-approval thresholds configurable per role (`requires_approval`, threshold amount).

---

### Q48: How should AI determine priority levels?

- **A)** User-specified priority in request
- **B)** AI analyzes keywords, role, location, cost
- **C)** All requests same priority (FIFO)
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Weighted priority using keywords, role, location, cost, deadline, and wait time (base + weighted priority).

---

### Q49: Should AI suggest alternative parts when inventory low?

- **A)** Yes - proactive suggestions
- **B)** No - only process exact requests
- **C)** Future enhancement
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Suggest alternative parts when inventory is low.

---

### Q50: How should AI handle ambiguous requests?

- **A)** Reject and request clarification
- **B)** Make best guess and flag for review
- **C)** Route to human for interpretation
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Ask for clarification; if best-guess applied, flag for human review.

---

### Q51: Should AI track patterns and learn over time?

- **A)** Yes - machine learning for common patterns
- **B)** No - rule-based processing only
- **C)** Future enhancement after core features stable
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Enable ML to learn patterns over time.

---

### Q52: How should AI route emergency requests?

- **A)** Highest priority, immediate approval notification
- **B)** Standard routing with CRITICAL flag
- **C)** Direct escalation to admin level
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Highest priority with immediate notifications; include CRITICAL flag for visibility.

---

### Q53: Should AI validate part numbers against external systems?

- **A)** Yes - check manufacturer catalogs
- **B)** No - only internal validation
- **C)** Use PART_NUMBER_XREF cross-reference table
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Validate part numbers against external catalogs; use PART_NUMBER_XREF for cross-refs.

---

### Q54: How should AI handle duplicate/similar requests?

- **A)** Process independently (duplicates allowed)
- **B)** Detect and flag duplicates for review
- **C)** Auto-reject obvious duplicates
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Detect/flag duplicate or similar requests for review (no silent auto-reject).

---

### Q55: Should AI generate reports automatically?

- **A)** Yes - daily/weekly automated reports
- **B)** No - reports only on demand
- **C)** Configurable scheduled reports
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Automated scheduled reports (daily/weekly configurable cadence).

---

### Q56: How should AI handle invalid requests?

- **A)** Reject immediately with error message
- **B)** Attempt to fix common issues automatically
- **C)** Route to human for manual correction
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Auto-fix common issues; escalate to human if unresolved.

---

### Q57: Should AI provide natural language responses?

- **A)** Yes - conversational response messages
- **B)** No - structured JSON responses only
- **C)** Both - JSON + human-readable summary
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Return JSON plus human-readable summary.

---

### Q58: How should AI handle multi-step workflows?

- **A)** Each step as separate request
- **B)** Single request with workflow state tracking
- **C)** Not supported - simple requests only
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Single request with workflow state tracking (sub-steps managed internally).

---

### Q59: Should AI integration be optional or required?

- **A)** Required - system depends on AI routing
- **B)** Optional - fallback to manual processing
- **C)** Configurable per company
- **D)** Custom answer: _______________

**✅ ANSWER: B** — AI-enabled but manual fallback available.

---

### Q60: How should AI handle timeout/unavailability?

- **A)** Queue requests until AI available
- **B)** Fallback to simple rule-based routing
- **C)** Reject requests with error
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Queue requests until AI is available, then process.

---

## SESSION 5: DEPLOYMENT & OPERATIONS ✅

### Q61: What mobile platforms should be supported?

- **A)** iOS only
- **B)** Android only
- **C)** Both iOS and Android
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Support both iOS and Android (start iOS-heavy but both required).

---

### Q62: Should there be a web interface for office users?

- **A)** Yes - full web application
- **B)** No - mobile-only for consistency
- **C)** Read-only web interface for reporting
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Mobile-only for now; future web optional.

---

### Q63: How should app updates be distributed?

- **A)** App store updates only
- **B)** Over-the-air updates for content/config
- **C)** Manual download and install
- **D)** Custom answer: _______________

**✅ ANSWER: B** — OTA for config/content; major versions via app stores; manual download only as fallback.

---

### Q64: Should the system support multiple companies/tenants?

- **A)** Yes - multi-tenant architecture
- **B)** No - single company deployment
- **C)** Future enhancement
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Single-company deployment; revisit multi-tenant later.

---

### Q65: How should database backups be handled?

- **A)** Daily automated backups with 30-day retention
- **B)** Real-time replication to backup server
- **C)** Manual backups only
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Real-time replication to backup server plus periodic snapshots/restore points.

---

### Q66: What level of monitoring/logging is needed?

- **A)** Minimal - errors only
- **B)** Standard - errors + warnings + info
- **C)** Detailed - full request/response audit trail
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Detailed full request/response audit trail.

---

### Q67: Should the system support disaster recovery?

- **A)** Yes - hot standby server
- **B)** Yes - backup server with manual failover
- **C)** No - restore from backups acceptable
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Backup server with manual failover (delegation optional later).

---

### Q68: How should SSL/TLS certificates be managed?

- **A)** OAuth2 via cloud providers handles encryption
- **B)** Custom certificates for server
- **C)** Not applicable - cloud-based file sync
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Custom server TLS certificates; rely on provider encryption for cloud file sync.

---

### Q69: Should the system have a staging environment?

- **A)** Yes - separate staging for testing
- **B)** No - test in production with beta users
- **C)** Local development environment only
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Separate staging environment mirroring production.

---

### Q70: How should performance metrics be tracked?

- **A)** Application Performance Monitoring (APM) tool
- **B)** Custom logging and metrics
- **C)** No performance tracking needed
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Use an APM tool for metrics and tracing.

---

### Q71: What browser support is needed for web interfaces?

- **A)** Modern browsers only (Chrome, Firefox, Safari, Edge)
- **B)** Include IE11 support
- **C)** No web interface needed
- **D)** Custom answer: _______________

**✅ ANSWER: C** — No web interface needed; only open shared links in default browser if required.

---

### Q72: Should the system support offline reports?

- **A)** Yes - generate and cache reports locally
- **B)** No - real-time reports only
- **C)** Export reports as PDF for offline viewing
- **D)** Custom answer: _______________

**✅ ANSWER: C** — Server-generated PDFs cached on device; on-demand requests allowed.

---

### Q73: How should security vulnerabilities be addressed?

- **A)** Monthly security audits
- **B)** Automated dependency scanning
- **C)** Ad-hoc reviews when issues reported
- **D)** Custom answer: _______________

**✅ ANSWER: B** — Automated dependency scanning (GitHub security features) with triage.

---

### Q74: Should the system support data export?

- **A)** Yes - CSV/Excel export for all data
- **B)** Yes - API for external system integration
- **C)** No - data stays in system only
- **D)** Custom answer: _______________

**✅ ANSWER: D** — Custom: Support CSV/Excel exports and API access for integrations.

---

### Q75: What documentation is needed for deployment?

- **A)** Admin guide + user manual + API docs
- **B)** README file only
- **C)** Video tutorials + FAQ
- **D)** Custom answer: _______________

**✅ ANSWER: A** — Admin guide + user manual + API docs; script video tutorials once dev stabilizes.

---

## APPENDIX: DETAILED RATIONALES (Q16–Q75)

### Session 2: Parts Management & Inventory (Q16–Q30)

- **Q16**: Brand = public name; manufacturer can be stored as alias/normalized brand entry; keep BRANDS table for dedupe/aliases.
- **Q17**: Internal SKU `WTP-{CATEGORY}-{SEQ}` is authoritative; PART_NUMBER_XREF holds manufacturer/supplier numbers for search and compatibility.
- **Q18**: Track aisle/shelf/bin; allow opt-out per part for simpler sites.
- **Q19**: Store canonical metric plus entered unit; default display may be imperial; preserve original unit for audit; convert on-the-fly.
- **Q20**: Many suppliers per part with pricing/lead times; brand-agnostic checkbox for price-first comparisons.
- **Q21**: Images stored in cloud; DB holds deterministic folder+filename/URL to avoid expiring links.
- **Q22**: No formal revisions; use `modified_at`; if revisions needed later, add PART_REVISIONS table.
- **Q23**: Discontinue by marking inactive; keep history for reporting/audit.
- **Q24**: Track part warranty expirations plus job warranty windows (job close/reopen dates).
- **Q25**: Real-time per transaction via request/response files; server is single source of truth.
- **Q26**: Support kits/assemblies; kit inventory rolls up from components.
- **Q27**: Min levels per warehouse/department/truck/job site; not global-only.
- **Q28**: Track purchase cost and optional sell price; keep pricing history for future integrations.
- **Q29**: Request-based import of CSV/Excel uploaded to cloud; server processes; app UI may upload.
- **Q30**: Barcode/QR scanning and generation using internal SKU or manufacturer number; provide tooling in parts management.

### Session 3: User Roles & Permissions (Q31–Q45)

- **Q31**: Flexible tree; can operate flat by configuration (no forced fixed levels).
- **Q32**: Users may hold multiple roles; permissions are unioned.
- **Q33**: Same-level edits configurable per role (`can_edit_same_level`).
- **Q34**: Cross-branch edits configurable per role with approval.
- **Q35**: Temporary grants via exception list with optional expiry; users can submit change requests for review.
- **Q36**: Inventory visibility configurable per role (`can_view_all_inventory`); onboarding confirms defaults.
- **Q37**: Users can join multiple departments with a primary flag.
- **Q38**: Approval thresholds per role (`approval_threshold_amount`) drive routing.
- **Q39**: Workers may approve subordinate requests when enabled.
- **Q40**: Admins can do anything; retain full audit logging.
- **Q41**: User creation is admin-only; managers can request creation for admin approval.
- **Q42**: Deactivate only; deletion not allowed; deactivation requests follow level-above approval.
- **Q43**: Audit log records all role assignments/changes.
- **Q44**: Read-only/report roles with optional commenting, shareable access, optional expiry, logs purge 2 years after expiry.
- **Q45**: Each department keeps its own approval chain; cross-dept requests use standard comms.

### Session 4: AI & Automation (Q46–Q60)

- **Q46**: Microsoft Autogen with RequestRouter agent.
- **Q47**: Auto-approval thresholds configurable per role (`requires_approval`, threshold amount).
- **Q48**: Weighted priority using keywords (emergency/urgent), role, location, cost, deadline, wait time; maintain base + weighted priority.
- **Q49**: Suggest substitute parts when inventory is low.
- **Q50**: Ask for clarification on ambiguous requests; if best-guess applied, flag for human review.
- **Q51**: Enable ML to learn patterns over time.
- **Q52**: Emergencies are highest priority with immediate notifications and CRITICAL flag.
- **Q53**: Validate part numbers against external catalogs; use PART_NUMBER_XREF for cross-refs.
- **Q54**: Detect/flag duplicate or similar requests for review; avoid silent auto-rejects.
- **Q55**: Automated scheduled reports (daily/weekly) with configurable cadence.
- **Q56**: Auto-fix common issues; escalate to human if unresolved.
- **Q57**: Return JSON plus human-readable summary.
- **Q58**: Single request with workflow state tracking; internal sub-steps managed by AI.
- **Q59**: AI-enabled with manual fallback available.
- **Q60**: Queue requests until AI available, then process.

### Session 5: Deployment & Operations (Q61–Q75)

- **Q61**: Support both iOS and Android; start iOS-heavy but both required.
- **Q62**: Mobile-only for now; web optional later.
- **Q63**: OTA for config/content; major versions via app stores; manual download only as fallback.
- **Q64**: Single-company deployment; revisit multi-tenant later.
- **Q65**: Real-time replication to backup server plus periodic snapshots/restore points.
- **Q66**: Detailed full request/response audit trail.
- **Q67**: Backup server with manual failover; delegation optional later.
- **Q68**: Custom server TLS certificates; rely on provider encryption for cloud file sync.
- **Q69**: Separate staging environment mirroring production.
- **Q70**: Use an APM tool for metrics and tracing.
- **Q71**: No web UI required; only open shared links in default browser if needed.
- **Q72**: Server-generated PDFs cached on device; on-demand requests allowed.
- **Q73**: Automated dependency scanning (GitHub security features) with triage.
- **Q74**: Support CSV/Excel exports and API access for integrations.
- **Q75**: Admin guide + user manual + API docs; script video tutorials once dev stabilizes.

---

**Document Control**  
**Created**: December 21, 2025  
**Last Updated**: December 21, 2025  
**Version**: 1.0  
**Status**: Sessions 1-5 Complete
