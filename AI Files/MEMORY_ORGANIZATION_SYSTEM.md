# Memory Organization System - Multi-Agent Architecture

## 🎯 Purpose

This document defines the **structured memory organization system** for the WeirdToo Parts System, designed to prevent memory overlap between multiple AI agents (both development agents and AutoGen production agents).

## 🏗️ Architecture Overview

### Two Agent Ecosystems

**1. Development Agents** (GitHub Copilot - Smart Plan, Execute, Review, Full Auto)
- Purpose: Build and maintain the WeirdToo application code
- Memory namespace: `/memories/dev/`
- Tools: MCP Docker tools, file operations, Python/VS Code extensions

**2. Production Agents** (AutoGen - PartsSpecialist, SupplierMatcher, OrderGenerator)
- Purpose: Run the WeirdToo application business logic (part matching, supplier selection, order generation)
- Memory namespace: `/memories/autogen/`
- Tools: MCP Docker tools, database access, cloud file I/O

### Memory Isolation Strategy

```
/memories/
├── dev/                          # Development agent memory (Copilot agents)
│   ├── smart-plan/
│   │   ├── session_YYYY-MM-DD.md
│   │   ├── clarifications.md
│   │   └── plan_patterns.md
│   ├── smart-execute/
│   │   ├── execution_logs.md
│   │   ├── tool_learnings.md
│   │   └── error_solutions.md
│   ├── smart-review/
│   │   ├── review_insights.md
│   │   └── prompt_updates.md
│   ├── full-auto/
│   │   └── orchestration_logs.md
│   └── shared/
│       ├── weirdtoo_constraints.md
│       ├── weirdtoo_environment.md
│       └── user_preferences.md
│
├── autogen/                      # Production agent memory (AutoGen agents)
│   ├── parts-specialist/
│   │   ├── part_mappings.md
│   │   ├── nec_standards.md
│   │   └── specification_learnings.md
│   ├── supplier-matcher/
│   │   ├── supplier_preferences.md
│   │   ├── pricing_patterns.md
│   │   └── availability_heuristics.md
│   ├── order-generator/
│   │   ├── template_variations.md
│   │   └── validation_rules.md
│   └── shared/
│       ├── parts_catalog_patterns.md
│       ├── inventory_insights.md
│       └── user_request_patterns.md
│
└── system/                       # Cross-agent system memory
    ├── tool_registry.md          # Available MCP tools catalog
    ├── error_glossary.md         # Common errors across all agents
    └── integration_patterns.md   # How dev and prod agents coordinate
```

## 🛡️ Backend Isolation Configuration

**User-configured backend** will enforce namespace isolation:

```python
# Backend configuration (managed by user)
MEMORY_ISOLATION_RULES = {
    "smart_plan_agent": {
        "allowed_paths": ["/memories/dev/smart-plan/", "/memories/dev/shared/"],
        "read_only_paths": ["/memories/system/"],
        "denied_paths": ["/memories/autogen/"]
    },
    "smart_execute_agent": {
        "allowed_paths": ["/memories/dev/smart-execute/", "/memories/dev/shared/"],
        "read_only_paths": ["/memories/system/", "/memories/dev/smart-plan/"],
        "denied_paths": ["/memories/autogen/"]
    },
    "parts_specialist_agent": {
        "allowed_paths": ["/memories/autogen/parts-specialist/", "/memories/autogen/shared/"],
        "read_only_paths": ["/memories/system/"],
        "denied_paths": ["/memories/dev/"]
    },
    # ... more agent rules
}
```

**Enforcement**: Backend MCP server checks paths before `memory()` tool execution.

## 🧬 Structured Memory Format

### Standard Memory File Structure

```markdown
# [Topic] - [Agent Name]

**Created:** YYYY-MM-DD HH:MM
**Last Updated:** YYYY-MM-DD HH:MM
**Agent:** [smart-plan / smart-execute / parts-specialist / etc.]
**Status:** [active / archived / deprecated]

## Quick Summary (1-3 sentences)

[TL;DR of what this memory contains]

---

## Current Context

[Active information agents should reference]

### Key Points
- Point 1
- Point 2
- Point 3

### Recent Learnings (Most Recent First)

#### 2025-12-21: [Brief title]
[What was learned, what worked, what didn't]

#### 2025-12-20: [Previous learning]
[Details...]

---

## Historical Context

[Older information kept for pattern analysis]

### Deprecated Approaches (Don't Use These)
- ❌ Approach A - Reason it failed
- ❌ Approach B - Reason it failed

### Archived Patterns (Still Valid But Superseded)
- 🗄️ Pattern A - Why replaced
- 🗄️ Pattern B - Why replaced

---

## Cross-References

**Related Dev Memories:**
- [Link to related dev agent memory]

**Related AutoGen Memories:**
- [Link to related production agent memory]

**Related System Memories:**
- [Link to system-wide knowledge]

---

## Metadata

**Tags:** [tag1, tag2, tag3]
**Category:** [environment / implementation / learnings / patterns]
**Confidence:** [high / medium / low] - How reliable is this info?
**Expiry:** [Never / YYYY-MM-DD] - When to review/deprecate
```

## 🔄 Memory Lifecycle

### 1. Creation (Immediate)

```javascript
// Any agent, any time during work
memory({
  command: "create",
  path: "/memories/dev/smart-execute/session_2025-12-21.md",
  file_text: "# Session Notes 2025-12-21\n\n## Quick Summary\nWorking on environment setup...\n\n"
})
```

### 2. Incremental Updates (Throughout Session)

```javascript
// Append new observations as they happen
memory({
  command: "insert",
  path: "/memories/dev/smart-execute/session_2025-12-21.md",
  insert_line: 999999,  // End of file
  insert_text: "\n### Python 3.12 Works Fine\n- Expected 3.11 but 3.12 is fully compatible\n- No changes needed\n\n"
})
```

### 3. Weekly Cleanup (Consolidation)

```javascript
// Merge daily sessions into topical files
memory({
  command: "str_replace",
  path: "/memories/dev/shared/weirdtoo_environment.md",
  old_str: "## Python Setup\n\n[Old content]",
  new_str: "## Python Setup\n\n### Confirmed Working\n- Python 3.12.2 (venv at .venv/)\n- pip 25.3\n- watchdog, jsonschema dependencies\n\n### .NET Status\n- Only 6.0 runtimes present\n- Need SDK 9.0 from https://aka.ms/dotnet-download\n"
})

// Archive old daily notes
memory({
  command: "rename",
  old_path: "/memories/dev/smart-execute/session_2025-12-21.md",
  new_path: "/memories/dev/smart-execute/archive/session_2025-12-21.md"
})
```

### 4. Monthly Consolidation (Restructure)

```javascript
// Group related memories into comprehensive files
// Example: Merge all environment notes into single authoritative file

// After consolidation, mark old files as deprecated
memory({
  command: "str_replace",
  path: "/memories/dev/shared/old_env_notes.md",
  old_str: "**Status:** active",
  new_str: "**Status:** deprecated\n**Replaced By:** /memories/dev/shared/weirdtoo_environment.md"
})
```

## 🎯 Agent-Specific Memory Patterns

### Development Agents (Copilot)

**Smart Plan:**
- Sessions: Daily planning sessions with user goals
- Clarifications: QA survey responses and refined goals
- Plan Patterns: Successful planning strategies and anti-patterns

**Smart Execute:**
- Execution Logs: What steps were executed, tools used, results
- Tool Learnings: Which MCP tools worked, which failed, workarounds
- Error Solutions: Error messages and their solutions

**Smart Review:**
- Review Insights: Pattern analysis from execution results
- Prompt Updates: Track applied prompt edits and their effectiveness

**Full Auto:**
- Orchestration Logs: Chain execution flows, handoff timing, loop iterations

### Production Agents (AutoGen)

**PartsSpecialist:**
- Part Mappings: User descriptions → correct part variant mappings
- NEC Standards: Electrical code requirements and how to apply them
- Specification Learnings: Material specs, gauge requirements, insulation types

**SupplierMatcher:**
- Supplier Preferences: User preferences for suppliers (price vs quality)
- Pricing Patterns: Historical pricing trends, bulk discount thresholds
- Availability Heuristics: Which suppliers stock what, lead times

**OrderGenerator:**
- Template Variations: PO format variations across suppliers
- Validation Rules: Order validation logic that caught errors

## 🔍 Memory Search Strategy

### Cross-Agent Search (Read-Only Access)

Development agents can **read** (but not write) AutoGen agent memories for context:

```javascript
// Smart Plan checking if AutoGen agents have learnings about part specs
memory({
  command: "view",
  path: "/memories/autogen/parts-specialist/specification_learnings.md"
})

// Smart Execute checking system-wide error solutions
memory({
  command: "view",
  path: "/memories/system/error_glossary.md"
})
```

### MCP Search for Semantic Queries

```javascript
// Use MCP Docker search tools for semantic queries across memories
mcp_mcp_docker_search_nodes({
  query: "Python environment setup issues solutions"
})

// Filter results by namespace if needed
results.filter(r => r.path.startsWith("/memories/dev/"))
```

## 🚨 Conflict Resolution

### What If Memories Overlap Despite Isolation?

**Scenario:** Backend isolation fails; two agents write to same file.

**Detection:**
- File modification timestamp conflicts
- MCP tool reports write conflicts
- User observes unexpected memory content

**Resolution Strategy:**

1. **Automatic Merge Markers** (Backend adds conflict markers)
   ```markdown
   <<<<<<< smart-plan-agent (2025-12-21 14:30)
   Content from Smart Plan agent
   =======
   Content from Parts Specialist agent
   >>>>>>> parts-specialist-agent (2025-12-21 14:32)
   ```

2. **Manual Resolution** (User or agent resolves)
   - Review both versions
   - Merge valid content from both
   - Create separate files if needed
   - Update isolation rules to prevent recurrence

3. **Log Conflict** (System memory records issue)
   ```markdown
   File: /memories/system/conflict_log.md
   
   ## 2025-12-21: Memory Overlap Detected
   - **File:** /memories/dev/shared/weirdtoo_environment.md
   - **Agents:** smart-plan, parts-specialist
   - **Root Cause:** Both needed environment info, shared path not properly isolated
   - **Resolution:** Created /memories/autogen/shared/autogen_environment.md for production agents
   - **Isolation Update:** Added stricter path rules in backend config
   ```

## 🛠️ Tool Configuration for Memory Management

### Memory Tool Limits

**Per Agent:** Max 128 tools total (across all MCP servers)

**Memory-Related Tools (Always Include):**
- `memory` (core tool - view, create, str_replace, insert, delete, rename)
- `mcp_mcp_docker_search_nodes` (semantic search)
- `mcp_mcp_docker_add_observations` (structured observations)
- `mcp_mcp_docker_get_overview` (task overview)

**Additional MCP Tools (Generous Selection):**
```yaml
tools:
  # Core tools (20-30)
  - vscode, execute, read, edit, search, web, agent, todo, memory
  
  # MCP Docker wildcard (50-70 tools)
  - mcp_docker/*
  
  # Python-specific (10-15)
  - pylance-mcp-server/*
  - ms-python.python/*
  
  # GitHub-specific (15-20)
  - github.vscode-pull-request-github/*
  
  # Mermaid diagrams (3)
  - mermaidchart.vscode-mermaid-chart/*
  
  # Total: ~98-138 tools (cap at 128, prioritize by agent role)
```

### Dynamic Tool Loading

```javascript
// Check available tools at runtime
const availableTools = await discoverMCPTools();

// Load only needed tools for current session
const neededTools = [
  "mcp_mcp_docker_create_task",
  "mcp_mcp_docker_search_nodes",
  "memory"
];

// Just-in-time activation
for (const tool of neededTools) {
  if (!availableTools.includes(tool)) {
    await activateMCPServer(tool);
  }
}
```

## 📊 Monitoring and Metrics

### Memory Health Dashboard

**Track:**
- Total memory files per agent namespace
- Memory file size distribution
- Update frequency (daily, weekly, monthly)
- Conflict incidents
- Search query patterns

**Alerts:**
- Agent writing to wrong namespace (isolation breach)
- Memory file count >100 (needs cleanup)
- No memory updates in 7+ days (agent not learning)
- Frequent conflicts (isolation rules need update)

### Example Metrics

```markdown
File: /memories/system/health_metrics.md

# Memory System Health Metrics

**Last Updated:** 2025-12-21 15:00

## Per-Agent Stats

| Agent | Files | Total Size | Avg Age | Conflicts |
|-------|-------|------------|---------|-----------|
| smart-plan | 12 | 145 KB | 3 days | 0 |
| smart-execute | 24 | 320 KB | 2 days | 1 |
| parts-specialist | 8 | 95 KB | 5 days | 0 |
| supplier-matcher | 6 | 72 KB | 4 days | 0 |

## System-Wide

- **Total Memory Files:** 50
- **Total Size:** 632 KB
- **Conflicts This Week:** 1
- **Cleanup Needed:** smart-execute (24 files, consolidate recommended)
```

## 🎓 Best Practices Summary

### ✅ DO

1. **Write immediately** - Don't wait for perfect organization
2. **Use namespaces** - Always write to your agent's namespace
3. **Add metadata** - Include creation date, agent name, status
4. **Cross-reference** - Link related memories across namespaces
5. **Clean up weekly** - Merge daily notes into topical files
6. **Check read-only** - Read system and other agent memories for context
7. **Log conflicts** - Document any overlap incidents
8. **Use structured format** - Follow the standard memory template

### ❌ DON'T

1. **Don't write to other namespaces** - Backend will block, but don't try
2. **Don't duplicate** - Search before creating (might already exist)
3. **Don't delete without archiving** - Rename to archive/ instead
4. **Don't mix dev and prod** - Development learnings stay in `/dev/`, production in `/autogen/`
5. **Don't ignore conflicts** - Resolve immediately to prevent data loss
6. **Don't over-organize early** - Messy daily notes are fine, clean up later
7. **Don't forget expiry** - Mark time-sensitive info with expiry dates
8. **Don't skip cross-references** - Help future agents find related info

## 🔮 Future Enhancements

### Planned Features

1. **Automatic Conflict Detection** - Backend real-time monitoring
2. **Memory Compression** - Auto-archive files older than 30 days
3. **Semantic Deduplication** - AI-powered duplicate content detection
4. **Memory Templates** - Pre-filled templates for common memory types
5. **Access Audit Trail** - Track which agents read which memories
6. **Memory Recommendations** - AI suggests relevant memories before tasks
7. **Expiry Automation** - Auto-archive or flag expired memories

---

**Version:** 1.0.0  
**Last Updated:** 2025-12-21  
**Maintained By:** WeirdToo Development Team  
**Status:** Active
