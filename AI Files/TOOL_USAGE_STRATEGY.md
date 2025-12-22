# Tool Usage Strategy - WeirdToo Parts System

**Version:** 1.0.0  
**Date:** 2025-12-21  
**Purpose:** Strategic guide for using available AI tools effectively

---

## Overview of Available Tools

| Tool | Complexity | Scope | Best Use Case |
|------|------------|-------|---------------|
| **Todo List** | Low | Session-scoped | Quick inline tracking during active work |
| **SimpleCheckList** | Medium | Project-scoped | Detailed task breakdown for features |
| **Task Orchestrator** | High | Enterprise-scoped | Complex multi-agent workflows with dependencies |
| **Memory (Reference)** | Medium | Persistent | Cross-session knowledge retention |

---

## Tool 1: Todo List (Inline Session Tracking)

### When to Use
- **Active coding sessions** with 3-10 sequential steps
- **Short-lived tasks** that complete in single session
- **Real-time progress** visibility during execution
- **Quick pivots** when user changes direction mid-task

### When NOT to Use
- Multi-day projects (use SimpleCheckList instead)
- Complex dependency chains (use Task Orchestrator)
- Cross-session tracking (won't persist)

### Best Practices
```markdown
✅ DO:
- Create todo at start of multi-step work
- Mark in-progress when starting a step
- Mark completed immediately after finishing
- Keep titles concise (3-7 words)
- Use 3-10 items max per list

❌ DON'T:
- Batch-update multiple completions
- Create 20+ item todo lists
- Use for long-term planning
- Duplicate with other tracking systems
```

### Example Pattern (WeirdToo)
```json
[
  {"id": 1, "status": "completed", "title": "Create Python venv"},
  {"id": 2, "status": "in-progress", "title": "Install server dependencies"},
  {"id": 3, "status": "not-started", "title": "Scaffold server/watcher.py"},
  {"id": 4, "status": "not-started", "title": "Test file watching"}
]
```

---

## Tool 2: SimpleCheckList (Project-Level Task Breakdown)

### When to Use
- **Feature development** with 10-50 tasks
- **Multi-day projects** requiring persistence
- **Milestone tracking** across sessions
- **Team coordination** where others need visibility

### When NOT to Use
- Quick 5-minute fixes (use todo list)
- Enterprise workflows with 100+ tasks (use Task Orchestrator)
- Simple linear work without dependencies

### Best Practices
```markdown
✅ DO:
- Group related tasks under headings
- Add acceptance criteria per task
- Include time estimates
- Mark dependencies clearly
- Review and update regularly

❌ DON'T:
- Create tasks without clear outcomes
- Mix multiple features in one checklist
- Forget to mark progress
- Over-engineer simple work
```

### Example Pattern (WeirdToo Server Implementation)
```markdown
## Server Implementation Checklist

### Phase 1: Environment Setup
- [x] Install Python 3.12 (Estimated: 10 min)
- [x] Create virtual environment (Estimated: 2 min)
- [x] Install watchdog, jsonschema (Estimated: 3 min)
- [ ] Install PostgreSQL 14+ (Estimated: 20 min)
  - Acceptance: `psql --version` shows 14+

### Phase 2: File Watcher
- [ ] Create server/watcher.py (Estimated: 30 min)
  - Acceptance: Detects new .json in Cloud/Requests/
- [ ] Implement JSON schema validation (Estimated: 20 min)
  - Acceptance: Invalid JSON rejected with clear error
- [ ] Write response to Cloud/Responses/ (Estimated: 15 min)
  - Acceptance: Round-trip <2 seconds

### Phase 3: Database Integration
- [ ] Define PARTS table schema (Estimated: 45 min)
- [ ] Implement get_parts request handler (Estimated: 60 min)
- [ ] Add audit logging (Estimated: 30 min)
```

---

## Tool 3: Task Orchestrator (Enterprise Workflow Management)

### When to Use
- **Complex projects** with 50+ tasks and dependencies
- **Multi-agent workflows** (AI agents coordinating)
- **Long-term roadmaps** (3+ months)
- **Template-driven development** (reusable task structures)

### When NOT to Use
- Simple 1-2 day projects (SimpleCheckList sufficient)
- Proof-of-concept work
- Personal scripts with no dependencies

### Key Features
- **Templates**: Reusable task patterns (Technical Approach, Testing Strategy, etc.)
- **Dependencies**: BLOCKS/IS_BLOCKED_BY/RELATES_TO relationships
- **Features**: Group tasks under higher-level features
- **Projects**: Top-level organization
- **Sections**: Rich documentation per task (markdown, code, JSON)

### Best Practices
```markdown
✅ DO:
- Start with list_templates to see available patterns
- Apply templates during create_task for consistency
- Use features to group 3+ related tasks
- Define dependencies to prevent ordering issues
- Add sections for requirements, technical approach, testing

❌ DON'T:
- Create tasks without templates (loses structure)
- Skip feature organization (orphaned tasks)
- Ignore dependencies (causes rework)
- Over-document simple tasks
```

### Example Pattern (WeirdToo Full Implementation)

#### Step 1: List Available Templates
```
Tool: mcp_mcp_docker_list_templates
Filter: targetEntityType = "TASK"
Result: Shows Technical Approach, Testing Strategy, Requirements Specification, etc.
```

#### Step 2: Create Feature
```json
{
  "name": "Server File Watcher",
  "summary": "Python server that watches Cloud/Requests/ and processes incoming JSON",
  "status": "planning",
  "priority": "high",
  "templateIds": ["context-background-uuid", "requirements-spec-uuid"]
}
```

#### Step 3: Create Tasks Under Feature
```json
{
  "title": "Implement watchdog file monitoring",
  "summary": "Use Python watchdog to detect new .json files in Cloud/Requests/",
  "featureId": "server-watcher-feature-uuid",
  "templateIds": ["technical-approach-uuid", "testing-strategy-uuid"],
  "status": "pending",
  "priority": "high",
  "complexity": 5
}
```

#### Step 4: Add Dependencies
```
Task A: "Setup PostgreSQL schema" 
Task B: "Implement get_parts handler" (depends on A)

Tool: mcp_mcp_docker_create_dependency
{
  "fromTaskId": "task-A-uuid",
  "toTaskId": "task-B-uuid",
  "type": "BLOCKS"
}
```

#### Step 5: Get Overview
```
Tool: mcp_mcp_docker_get_overview
Result: Hierarchical view of features → tasks with status/priority
```

### Template Combination Strategies

| Scenario | Recommended Templates |
|----------|----------------------|
| **New Feature** | Context & Background + Requirements Specification + Testing Strategy |
| **Implementation Task** | Technical Approach + Task Implementation Workflow + Testing Strategy |
| **Bug Fix** | Bug Investigation Workflow + Technical Approach + Definition of Done |
| **Complex Feature** | Requirements + Technical Approach + Local Git Branching + GitHub PR + Testing + DoD |

---

## Tool 4: Memory (Cross-Session Knowledge Retention)

### When to Use
- **ANY insight worth remembering** - don't overthink it
- **Every stage of development** - incremental updates are fine
- **Messy notes during active work** - organize later
- **Quick observations** - even single-line learnings
- **Grouping related info** - merge files as patterns emerge

### Storage Structure
```
/memories/
  /project_context/
    weirdtoo_constraints.md       # File-only protocol, no HTTP, etc.
    weirdtoo_architecture.md      # System design decisions
  /user_preferences/
    tooling_preferences.md        # Minimal third-party tools
    workflow_preferences.md       # PowerShell-first, Windows focus
  /learnings/
    python_environment_setup.md   # Python 3.12 works, 3.11 missing
    dotnet_gaps.md                # SDK missing, only runtimes
  /patterns/
    success_patterns.md           # What worked well
    failure_patterns.md           # What to avoid
```

### Best Practices (Incremental & Messy Approach)
```markdown
✅ DO:
- Write memories IMMEDIATELY when you learn something
- Update memories at EVERY stage (setup, dev, testing, debugging)
- Append to existing files freely - don't worry about structure
- Create multiple small files - merge/group later
- Dump raw notes, observations, errors, solutions
- Let memories grow organically
- Clean up and consolidate weekly/monthly (not daily)

✅ MESSY IS OKAY:
- Duplicate info across files → consolidate later
- Unorganized notes → refactor when patterns emerge
- Temporary observations → keep them; context helps future sessions
- Quick one-liners → better than forgetting

❌ DON'T:
- Wait for "perfect" moment to write memory
- Spend time organizing before capturing
- Delete "trivial" info (it might matter later)
- Hesitate because file structure isn't ideal
```

### Example Patterns (WeirdToo)

#### Memory 1: Project Constraints
```markdown
File: /memories/weirdtoo_constraints.md

# WeirdToo Parts System - Hard Constraints

## Communication Protocol
- ✅ ONLY file-based JSON in Cloud/ folders
- ❌ NO HTTP/REST/WebSocket/gRPC
- ❌ NO direct network between apps
- Cloud storage (SharePoint/Google Drive) is sole messenger

## Architecture
- Server is ONLY database writer
- Devices use local SQLite caches
- Offline-first design
- Polling: 2min active, 1hr backgrounded, 12hr off-hours

## Tooling
- Minimal third-party dependencies
- Built-in venv/pip for Python
- Built-in dotnet CLI for .NET
- Avoid uv/poetry/npm unless approved
```

#### Memory 2: Environment Learnings
```markdown
File: /memories/weirdtoo_env_setup.md

# WeirdToo Environment Setup Learnings

## Python
- User has Python 3.12 (not 3.11)
- py launcher doesn't detect 3.11
- venv at C:\Users\weird\AAA Weird App Demmo\.venv
- pip 25.3 installed

## .NET
- Only .NET 6.0 runtimes installed
- No SDK present (blocks device app development)
- Need SDK 9.0 from https://aka.ms/dotnet-download
- Estimated install time: 5-10 minutes

## Batch Files Created
- setup.bat: Automated environment setup
- run-server.bat: Start Python server
- run-device.bat: Start .NET device app
```

#### Memory 3: User Preferences
```markdown
File: /memories/user_preferences.md

# User Preferences - WeirdToo Project

## Development Environment
- OS: Windows
- Shell: PowerShell (primary), Git Bash (secondary)
- Editor: VS Code

## Tooling Philosophy
- Minimize third-party tools
- Use built-in package managers only
- Avoid cloud/SaaS dependencies
- Prefer CLI over GUI

## Workflow
- Batch files for easy execution
- Clear error messages
- Auto-environment activation
- Keep up to date automatically

## Memory Usage Preference (Added 2025-12-21)
- Okay with messy/incremental memory updates
- Don't worry about perfect organization
- Update at every stage of development
- Group and cleanup later when patterns emerge
- Prefer capturing info over organizing it
```

#### Memory 4: Daily Dev Notes (Messy/Incremental Style)
```markdown
File: /memories/weirdtoo_daily_notes.md

# WeirdToo Daily Development Notes

## 2025-12-21 Morning - Environment Setup
- Python 3.11 missing → using 3.12 (works fine)
- venv created at .venv/
- pip 25.3 installed

## 2025-12-21 Afternoon - .NET Issues
- dotnet --info shows only 6.0 runtimes
- Need SDK 9.0 from https://aka.ms/dotnet-download
- Blocks device app dev

## 2025-12-21 Evening - Batch Files
- Created setup.bat, run-server.bat, run-device.bat
- Auto-activates venv
- Checks prerequisites before running

## Random Observations
- User wants minimal third-party tools
- File-based protocol only (no HTTP)
- PowerShell is primary shell
- Okay with messy memory updates!

## TODO Later
- Group these notes into organized files
- Merge env notes with architecture notes
- Create separate file for batch script patterns
```

### Memory Tool Commands

```javascript
// Create memory (do this often!)
memory({
  command: "create",
  path: "/memories/weirdtoo_constraints.md",
  file_text: "# Content here..."
})

// Quick append (add to end of file)
memory({
  command: "insert",
  path: "/memories/weirdtoo_notes.md",
  insert_line: 999999,  // Appends to end
  insert_text: "\n## New observation\n- Thing I just learned\n"
})

// Read memory
memory({
  command: "view",
  path: "/memories/weirdtoo_constraints.md"
})

// Update memory
memory({
  command: "str_replace",
  path: "/memories/weirdtoo_constraints.md",
  old_str: "old text",
  new_str: "updated text"
})

// List all memories
memory({
  command: "view",
  path: "/memories"
})

// Rename/group memories (cleanup phase)
memory({
  command: "rename",
  old_path: "/memories/temp_notes_1.md",
  new_path: "/memories/weirdtoo_implementation_notes.md"
})

// Delete old/merged memories
memory({
  command: "delete",
  path: "/memories/outdated_notes.md"
})
```

### Memory Cleanup & Grouping Patterns

**Incremental Workflow (Recommended)**:
```
Day 1-5: Write freely, create many small files
  /memories/weirdtoo_day1_notes.md
  /memories/python_env_issue.md
  /memories/dotnet_sdk_notes.md
  /memories/file_watcher_gotcha.md

Week 1 Cleanup: Group by topic
  Merge → /memories/weirdtoo_environment.md (env + SDK notes)
  Merge → /memories/weirdtoo_implementation.md (file watcher + dev notes)
  Delete temp files after merging

Month 1 Cleanup: Consolidate by category
  /memories/weirdtoo/
    architecture.md (all design decisions)
    environment.md (setup + tools)
    learnings.md (gotchas + solutions)
    user_preferences.md (workflow preferences)
```

**Grouping Triggers** (when to consolidate):
- 5+ files on same topic → merge into one
- Duplicate info across files → consolidate
- Weekly review shows patterns → create organized structure
- File count >20 → group into subdirectories

**Never Delete** (keep for context):
- Error messages and solutions
- "Weird" edge cases
- Failed approaches (document why they failed)
- Time estimates vs actuals (improve future planning)
- User feedback/corrections

---

## Integrated Tool Usage Strategy (WeirdToo Example)

### Scenario: Implement Server File Watcher Feature

#### Phase 1: Planning (Task Orchestrator)
1. **List templates** → Identify "Technical Approach" + "Testing Strategy"
2. **Create feature** "Server File Watcher" with requirements template
3. **Create tasks** under feature:
   - Setup watchdog (complexity 3)
   - Implement JSON validation (complexity 4)
   - Write response handler (complexity 5)
   - Add integration test (complexity 3)
4. **Set dependencies** → Validation depends on watchdog setup

#### Phase 2: Active Development (Todo List)
```json
[
  {"id": 1, "status": "completed", "title": "Install watchdog via pip"},
  {"id": 2, "status": "in-progress", "title": "Create server/watcher.py scaffold"},
  {"id": 3, "status": "not-started", "title": "Add FileSystemEventHandler"},
  {"id": 4, "status": "not-started", "title": "Test with sample JSON"}
]
```

#### Phase 3: Documentation (Memory - Updated at EVERY Stage)

**Stage 1: Before Starting** (capture intent)
```markdown
File: /memories/weirdtoo_watcher_notes.md

# File Watcher - Starting Implementation
- Need to watch Cloud/Requests/ for new .json files
- Plan: Python watchdog library
- Target latency: <2 seconds
```

**Stage 2: During Setup** (capture issues)
```markdown
[Append to same file]

## Setup Issues
- pip install watchdog → worked fine
- Needed to create Cloud/Requests/ folder first
```

**Stage 3: During Coding** (capture decisions)
```markdown
[Append to same file]

## Implementation Decisions
- Using FileSystemEventHandler not PollingObserver
- Added 500ms stabilityThreshold for Windows file locks
- Response folder check added (fails silently otherwise)
```

**Stage 4: During Testing** (capture results)
```markdown
[Append to same file]

## Test Results
- Detection latency: 200-400ms ✓
- JSON validation: 10-50ms ✓
- Round-trip: 1.8 seconds ✓ (meets <2s requirement)

## Gotchas Found
- Windows file locks need awaitWriteFinish option
- Schema validation must happen before processing
- Empty response folder = silent failure (now checks/creates)
```

**Stage 5: Weekly Cleanup** (optional consolidation)
```markdown
Merge into organized file:
File: /memories/weirdtoo_implementation.md

# WeirdToo Implementation Notes

## File Watcher (Consolidated)
### Setup
- Python watchdog library
- Cloud/Requests/ folder required

### Implementation
- FileSystemEventHandler approach
- 500ms stabilityThreshold for Windows
- Auto-creates response folders

### Performance
- Detection: 200-400ms
- Validation: 10-50ms  
- Total: <2s ✓

### Gotchas
- Windows file locks: use awaitWriteFinish
- Validate schema first
- Check/create response folder
```

#### Phase 4: Progress Tracking (SimpleCheckList for Larger Context)
If this is part of a 2-week sprint:
```markdown
## Sprint 1: Core Server Functionality

### Week 1
- [x] Environment setup
- [x] File watcher implementation
- [ ] Database schema design
- [ ] get_parts handler
- [ ] Audit logging

### Week 2
- [ ] Device client stub
- [ ] End-to-end testing
- [ ] Performance optimization
- [ ] Documentation
```

---

## Decision Matrix: Which Tool When?

| Criteria | Todo List | SimpleCheckList | Task Orchestrator | Memory |
|----------|-----------|-----------------|-------------------|--------|
| **Duration** | <1 day | 1-7 days | 1+ weeks | Permanent |
| **Task Count** | 3-10 | 10-50 | 50+ | N/A |
| **Complexity** | Low | Medium | High | N/A |
| **Dependencies** | None/Few | Some | Many | N/A |
| **Persistence** | Session | Project | Enterprise | Forever |
| **Team Visibility** | No | Yes | Yes | Yes |

### Quick Selection Guide

```
START HERE:
  ├─ Is this a quick fix (<1 hour)?
  │  └─ YES → Use TODO LIST
  │  └─ NO → Continue
  │
  ├─ Does it need to persist across sessions?
  │  └─ NO → Use TODO LIST
  │  └─ YES → Continue
  │
  ├─ Are there <10 tasks with few dependencies?
  │  └─ YES → Use SIMPLE CHECKLIST
  │  └─ NO → Continue
  │
  ├─ Are there 50+ tasks or complex dependencies?
  │  └─ YES → Use TASK ORCHESTRATOR
  │  └─ NO → Use SIMPLE CHECKLIST
  │
  └─ Should this knowledge persist forever?
     └─ YES → Store in MEMORY
```

---

## Best Practices Summary

### 1. Start Every Session
```
1. List all memories: memory({command: "view", path: "/memories"})
2. Read recent/relevant ones (even if messy): 
   - /memories/project_name_daily_notes.md
   - /memories/last_session_notes.md
3. Check Task Orchestrator overview: mcp_mcp_docker_get_overview()
4. Create session todo list for immediate work
```

### 2. During Development
```
1. Update todo list as you progress
2. Mark SimpleCheckList items when milestones reached
3. Add observations to Task Orchestrator tasks
4. Write to memory IMMEDIATELY when you learn something:
   - Hit an error? → memory append
   - Found a solution? → memory append
   - Made a decision? → memory append
   - Changed approach? → memory append
   Don't wait! Messy notes > forgotten knowledge
```

### 3. End of Session
```
1. Complete all in-progress todos or mark stalled
2. Update SimpleCheckList with final status
3. Dump ALL remaining notes to Memory (even rough/messy):
   - Create /memories/session_YYYY-MM-DD.md if needed
   - Append anything not yet captured
   - Include todos, observations, questions, ideas
4. Update Task Orchestrator task sections with implementation notes
5. Don't organize - just capture!
```

### 4. Weekly Review (Memory Cleanup Time)
```
1. Review Task Orchestrator features for progress
2. Update priorities based on learnings
3. MEMORY CLEANUP:
   - List all memories: memory({command: "view", path: "/memories"})
   - Identify grouping opportunities (5+ files on same topic)
   - Merge related files:
     * day1_notes.md + day2_notes.md → environment_setup.md
     * watcher_notes.md + server_notes.md → implementation.md
   - Rename for clarity
   - Delete only if merged elsewhere (keep originals if unsure)
   - Create subdirectories for 20+ files
4. Archive completed SimpleChecklists
```

---

## WeirdToo-Specific Recommendations

### For Environment Setup
- **Tool**: Todo List (quick, 5 steps)
- **Memory**: Save environment state to `/memories/weirdtoo_env.md`

### For Server Implementation
- **Tool**: Task Orchestrator Feature with 5-8 tasks
- **Templates**: Technical Approach + Testing Strategy
- **Memory**: Document file-watching patterns

### For Device App Development
- **Tool**: Separate Task Orchestrator Feature
- **SimpleCheckList**: Use for .NET scaffolding subtasks
- **Memory**: Save .NET project structure decisions

### For Full System Integration
- **Tool**: Task Orchestrator Project with 2-3 Features
- **Dependencies**: Link server/device tasks appropriately
- **Memory**: Cross-reference architecture decisions

---

## Action Plan: Immediate Next Steps

1. **Store project constraints in Memory** (5 min)
   ```
   File: /memories/weirdtoo_constraints.md
   Content: Communication protocol, architecture rules, tooling preferences
   ```

2. **Create Task Orchestrator Project** (10 min)
   ```
   Project: "WeirdToo Parts System MVP"
   Features: 
     - Server File Watcher
     - Device Client Stub
     - Schema Validation
   ```

3. **Apply templates to first task** (5 min)
   ```
   Task: "Implement server/watcher.py"
   Templates: Technical Approach + Testing Strategy
   ```

4. **Use todo list for today's work** (ongoing)
   ```
   - Complete .NET SDK installation
   - Scaffold server/ directory
   - Create watcher.py prototype
   ```

5. **Save learnings at end of session** (10 min)
   ```
   File: /memories/weirdtoo_session_1.md
   Content: What worked, gotchas, performance notes
   ```

---

**Total Setup Time**: ~30 minutes  
**ROI**: 10x faster context recovery across sessions, 5x better task organization, zero forgotten requirements
