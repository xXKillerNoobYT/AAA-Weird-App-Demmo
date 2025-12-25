# Zen Tasks Workflow Context

## Project Overview

**Project Name:** AAA Weird App Demo  
**Type:** Cloud-enabled device management system  
**Architecture:** .NET 9.0 server + React/Vue.js devices + PostgreSQL database

### Core Components

1. **Server** (`server/CloudWatcher/`)
   - .NET 9.0 Web API
   - ASP.NET Core
   - Entity Framework Core
   - RESTful API endpoints

2. **Database**
   - PostgreSQL
   - Entity Framework migrations
   - Schema: Parts, Inventory, Locations, AuditLog

3. **Device Apps** (`device/`)
   - React/Vue.js applications
   - API integration
   - Real-time updates

4. **AI Orchestration**
   - Microsoft Autogen integration
   - Multi-agent workflows
   - GitHub Copilot agents

5. **Cloud Storage**
   - File upload/download
   - Integration modules

---

## Agent Roles & Workflow

### Hub-Spoke Pattern

**Full Auto (Hub)**
- Central router and orchestrator
- Displays task queues
- Routes to specialist agents
- Never executes, plans, or reviews directly

**Smart Plan (Planning Spoke)**
- Analyzes requirements
- Detects vagueness
- Creates subtasks in Zen Tasks
- Returns to Full Auto

**Smart Execute (Execution Spoke)**
- Gets pending tasks
- Executes using tools
- Updates task status
- Logs observations
- Returns to Full Auto

**Smart Review (Review Spoke)**
- Analyzes completed/failed tasks
- Root-cause analysis
- Updates task insights
- Recommends next steps
- Returns to Full Auto

**Agent Builder (Meta Agent)**
- Creates/updates agents
- Maintains consistent structure
- Validates Zen Tasks integration

**Tool Builder (Tool Creation)**
- Designs MCP tools
- Implements with validation
- Tests functionality

**Smart Prep Cloud (Cloud Preparation)**
- Validates environment readiness
- Generates GitHub Issues
- Calculates Cloud Confidence
- Prepares handoff artifacts

---

## Test Sync Pattern

All agents follow this workflow:

```
1. Load Workflow Context
   └─ Call: loadWorkflowContext()
   └─ Updates: zen_workflow_loaded = true

2. List Pending Tasks
   └─ Call: listTasks(status=pending)
   └─ Returns: All pending tasks

3. Get Next Executable Task
   └─ Call: getNextTask(limit=1)
   └─ Returns: Highest priority task with no blocking dependencies

4. Display Queue to User
   └─ Show: Current task, ready queue, blocked tasks

5. Execute/Plan/Review
   └─ Perform agent-specific work

6. Update Task Status
   └─ Call: setTaskStatus(task_id, "completed"|"failed")
   └─ Call: add_observations({...})

7. Continue or Return
   └─ If more tasks: Loop to step 3
   └─ If complete: Return to Full Auto
```

---

## Task Protocol

### Task Structure

Every task must have:
- **ID**: Unique UUID (e.g., TASK-mjktc463-wdtw9)
- **Title**: Clear, concise description
- **Description**: Detailed explanation
- **Status**: pending | in-progress | completed | failed | deferred
- **Priority**: high | medium | low
- **Complexity**: 1-10 (1=simplest, 10=most complex)
- **Tags**: Comma-separated categories
- **Dependencies**: Array of task IDs

### Status Lifecycle

```
pending → in-progress → completed
                     ↘ failed
                     ↘ deferred
```

### Priority Guidelines

- **HIGH**: Blockers, critical features, infrastructure
- **MEDIUM**: Standard features, enhancements
- **LOW**: Nice-to-have, documentation, cleanup

---

## Memory Organization

All agents use consistent memory namespaces:

- `/memories/dev/full-auto/` - Hub state & routing
- `/memories/dev/smart-plan/` - Planning analysis
- `/memories/dev/smart-execute/` - Execution logs
- `/memories/dev/smart-review/` - Review findings
- `/memories/dev/agent-builder/` - Agent templates
- `/memories/dev/tool-builder/` - Tool specifications
- `/memories/dev/smart-prep-cloud/` - Cloud artifacts
- `/memories/dev/shared/` - Cross-agent shared state
- `/memories/system/` - System configuration

---

## Development Stack

### Backend (.NET)
- .NET 9.0 SDK
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL provider
- Swagger/OpenAPI

### Frontend (Devices)
- React or Vue.js
- TypeScript
- State management (Redux/Vuex)
- API client libraries

### Database
- PostgreSQL 14+
- EF Core Migrations
- Audit trail tables

### AI/Automation
- Microsoft Autogen
- GitHub Copilot Agents
- MCP (Model Context Protocol)

---

## Workflow Best Practices

### For All Agents

1. **Always load workflow context first**
   - Ensures dependency awareness
   - Provides project structure

2. **Use getNextTask for task selection**
   - Respects dependencies
   - Prioritizes correctly

3. **Update status after every task**
   - Maintains accurate state
   - Enables progress tracking

4. **Log all observations**
   - Success AND failure
   - Include context and solutions

5. **Return to Full Auto for routing**
   - Hub-spoke pattern
   - No direct agent-to-agent handoffs

### For Smart Execute

- Continue on errors (don't halt)
- Log every tool usage
- Record terminal output
- Document file changes
- Note solutions attempted

### For Smart Plan

- Detect vagueness
- Ask clarifying questions
- Create detailed subtasks
- Include acceptance criteria

### For Smart Review

- Analyze patterns
- Root-cause analysis
- Update task insights
- Create discovered tasks if needed

---

## Success Criteria

A well-functioning workflow has:

✅ All tasks in Zen Tasks (single source of truth)  
✅ Dependencies clearly defined  
✅ Status updates after each step  
✅ Observations logged for review  
✅ Hub-spoke routing maintained  
✅ Test sync pattern followed  
✅ Memory organized by namespace  

---

## Common Issues

### Workflow Not Loading
- Check if prompts/ folder exists
- Verify zen_tasks_workflow.md present
- Call loadWorkflowContext() manually

### Tasks Not Found
- Verify tasks.json exists in _ZENTASKS/
- Check task IDs match
- Ensure status filter is correct

### Dependency Deadlock
- Review task dependencies
- Check for circular references
- Use getNextTask to identify blocks

### Status Not Updating
- Verify task ID is correct
- Check setTaskStatus() called
- Ensure Zen Tasks tools activated

---

## References

- Project Root: `c:\Users\weird\AAA Weird App Demmo\`
- Tasks Location: `_ZENTASKS/tasks.json`
- Agents: `.github/agents/`
- Integration Guide: `ZEN_TASKS_AGENT_INTEGRATION.md`
- Quick Reference: `QUICK_REFERENCE_ZEN_TASKS.md`
- Workflow Summary: `WORKFLOW_INTEGRATION_SUMMARY.md`
