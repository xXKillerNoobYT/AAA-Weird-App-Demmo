# Zen Tasks Base Configuration

## System Information

**Installation Date:** December 24, 2025  
**Version:** 1.0.0  
**Project:** AAA Weird App Demo  

---

## Core Principles

### 1. Single Source of Truth
All tasks live in Zen Tasks. No internal task lists, no duplicate tracking systems.

### 2. Dependency-Driven Development
Tasks execute in dependency order. getNextTask() respects all dependencies.

### 3. Hub-Spoke Pattern
Full Auto routes to specialists. Specialists return to Full Auto. No direct chaining.

### 4. Observation Logging
Log everything: success, failure, attempts, solutions. Smart Review needs complete data.

### 5. Test Sync Workflow
Load context → Get next → Execute → Update → Repeat

---

## Task Categories

### By Priority
- **HIGH**: Infrastructure, blockers, critical features
- **MEDIUM**: Standard features, enhancements, integration
- **LOW**: Documentation, cleanup, nice-to-have features

### By Type
- **Feature**: New functionality implementation
- **Bug**: Error fixes and corrections
- **Refactor**: Code restructuring without functionality change
- **Documentation**: Docs, README, comments
- **Testing**: Unit tests, integration tests, validation
- **Infrastructure**: Build, deploy, CI/CD, environment
- **Database**: Schema, migrations, seeding

---

## Tagging Convention

Use consistent tags for filtering:

### Functional Area
- `api` - API endpoints
- `database` - Database operations
- `ui` - User interface
- `testing` - Test code
- `documentation` - Docs
- `architecture` - System design
- `migration` - Database migrations
- `seeding` - Data seeding
- `validation` - Input validation
- `authentication` - Auth/security

### Technology
- `dotnet` - .NET code
- `csharp` - C# specific
- `react` - React frontend
- `vue` - Vue.js frontend
- `postgresql` - Database
- `entity-framework` - EF Core
- `swagger` - OpenAPI/Swagger

### Wave/Phase
- `wave1` - Wave 1 features
- `wave2` - Wave 2 features  
- `wave3` - Wave 3 features
- `wave4` - Wave 4 features
- `wave5` - Wave 5 features

---

## File Structure

```
AAA Weird App Demmo/
├── _ZENTASKS/              # Task storage
│   ├── tasks.json          # All tasks
│   └── TASK-*.md           # Individual task files
├── .github/
│   └── agents/             # Copilot agents
│       ├── Full Auto.agent.md
│       ├── Smart Execute Updated.agent.md
│       ├── Smart Plan Updated.agent.md
│       └── Smart Review Updated.agent.md
├── server/
│   └── CloudWatcher/       # .NET API server
│       ├── Controllers/
│       ├── Models/
│       ├── Services/
│       └── Data/
├── device/                 # Device applications
├── prompts/               # Workflow context files
│   ├── zen_tasks_workflow.md
│   └── base.md
├── Docs/
│   └── Plan/              # Architecture docs
└── shared/                # Shared code
```

---

## Workflow States

### zen_workflow_loaded
- `true`: Workflow context loaded, dependency graph available
- `false`: Not loaded, need to call loadWorkflowContext()

### Task Status Values
- `pending`: Not started, may have dependencies
- `in-progress`: Currently being worked on
- `completed`: Successfully finished
- `failed`: Attempted but encountered blocking errors
- `deferred`: Postponed for later

### Agent States (in TASK_ORCHESTRATOR)
- `idle`: No active task
- `planning`: Creating subtasks
- `executing`: Running task operations
- `reviewing`: Analyzing results
- `routing`: Returning to Full Auto

---

## Tool Usage Guidelines

### Core Zen Tools

**loadWorkflowContext()**
- Call once per session
- Required before other Zen tools
- Sets zen_workflow_loaded = true

**listTasks(status, priority)**
- Query tasks by filters
- Returns array of tasks
- Use for queue display

**getNextTask(limit)**
- Finds executable tasks
- Respects dependencies
- Returns highest priority first

**addTask(...)**
- Creates new task
- Requires title, description, priority
- Optional: dependencies, complexity, tags

**getTask(taskId)**
- Retrieves single task
- Includes full details
- Use for task examination

**updateTask(taskId, ...)**
- Modifies task properties
- Partial updates allowed
- Use for status/priority changes

**setTaskStatus(taskId, status)**
- Quick status update
- Logs observations
- Use after task completion

**parseRequirements(requirements)**
- Converts text to tasks
- Creates structured task list
- Use in planning phase

---

## Agent-Specific Guidelines

### Full Auto
- Load context on startup
- Display queue: getNextTask(limit=3)
- Show backlog: listTasks(status=pending)
- Route based on phase button clicks

### Smart Plan
- Detect vagueness (0.0-1.0 scale)
- Ask clarifying questions if >0.5
- Create subtasks via addTask()
- Return with "Ready to Execute?" button

### Smart Execute
- Get pending: listTasks(status=pending)
- Execute: One task at a time
- Update: setTaskStatus() after each
- Log: All terminal output, file changes
- Continue on errors

### Smart Review
- List completed: listTasks(status=completed)
- List failed: listTasks(status=failed)
- Analyze patterns
- Update insights: updateTask()
- Recommend action

---

## Error Handling

### If Workflow Load Fails
1. Check prompts/ folder exists
2. Verify zen_tasks_workflow.md present
3. Check file permissions
4. Retry loadWorkflowContext()

### If No Tasks Found
1. Verify _ZENTASKS/tasks.json exists
2. Check task status filters
3. Review dependencies for deadlock
4. Use listTasks() without filters

### If Status Update Fails
1. Verify task ID is valid UUID
2. Check status value is valid enum
3. Ensure Zen Tasks tools activated
4. Retry with correct parameters

---

## Performance Optimization

### Efficient Task Loading
- Use status filters to reduce results
- Set appropriate limit values
- Cache workflow context in memory

### Batch Operations
- Group related addTask() calls
- Bulk update when possible
- Minimize tool invocations

### Memory Usage
- Clear old observations periodically
- Archive completed tasks
- Use task IDs, not full objects

---

## Validation Rules

### Task Creation
- Title: 3-100 characters
- Description: 10-500 characters
- Priority: high | medium | low
- Status: pending (default)
- Complexity: 1-10 integer
- Tags: comma-separated, no spaces

### Task Updates
- ID: Required, valid UUID
- Status: Valid enum value
- Dependencies: Array of valid task IDs
- No circular dependencies

### Workflow State
- Load context before operations
- Update status after every task
- Log observations for review
- Return to hub after completion

---

## Integration Points

### With GitHub
- Issues sync to tasks
- PRs link to tasks
- Branch names include task IDs

### With Memory System
- Observations stored in /memories/
- State persists across sessions
- Shared memory for coordination

### With Docker MCP
- Tools activated dynamically
- Resources cleaned up after use
- Server selection via mcp-find

---

## Success Metrics

### Well-Organized Workflow
✅ All tasks in Zen Tasks  
✅ Clear dependencies  
✅ Regular status updates  
✅ Complete observations  
✅ Hub-spoke routing maintained  

### Efficient Execution
✅ Test sync pattern followed  
✅ Dependencies respected  
✅ Minimal tool invocations  
✅ Proper error handling  
✅ Clean handoffs  

### Quality Outputs
✅ Tasks completed correctly  
✅ Code passes validation  
✅ Tests written and passing  
✅ Documentation updated  
✅ Audit trail complete  

---

## Troubleshooting

### Common Issues

**"Workflow context not found"**
→ Create prompts/ folder and zen_tasks_workflow.md

**"No executable tasks"**
→ Check dependencies with listTasks(), resolve deadlocks

**"Task status not updating"**
→ Verify task ID, check tool activation, retry

**"Duplicate tasks"**
→ Use task IDs to identify, remove via file operations

**"Circular dependencies"**
→ Review dependency graph, break cycles, reorder

---

## References

- Main workflow: `prompts/zen_tasks_workflow.md`
- Integration guide: `ZEN_TASKS_AGENT_INTEGRATION.md`
- Quick reference: `QUICK_REFERENCE_ZEN_TASKS.md`
- Workflow summary: `WORKFLOW_INTEGRATION_SUMMARY.md`
- Agents directory: `.github/agents/`
- Tasks directory: `_ZENTASKS/`
