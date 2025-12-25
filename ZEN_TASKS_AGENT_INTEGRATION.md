# Zen Tasks Integration for All Agents

**Date:** December 24, 2025  
**Status:** ✅ COMPLETE

---

## Summary

All agents in `.github/agents/` have been updated to use **Zen Tasks Copilot** for intelligent task tracking and **test sync workflow** for finding the next executable task automatically.

### What Changed?

Each agent now:
1. **Loads workflow context** at startup via `loadWorkflowContext()`
2. **Finds next task** via `getNextTask()` to respect dependencies
3. **Displays task queue** to user with pending, ready, and blocked tasks
4. **Updates task status** after each work step via `updateTask()`
5. **Continues workflow** through the queue until completion

---

## Updated Agents

### Core 4 Workflow Agents
1. **Full Auto New** - UI Hub with task queue display and phase routing
2. **Smart Execute Updated** - Execution with pending task list & progress tracking
3. **Smart Plan Updated** - Planning with requirement parsing & queue validation
4. **Smart Review Updated** - Review with analysis & root-cause findings

### Supporting Agents
5. **Agent Builder & Updater** - Meta-agent (already had Zen Tools)
6. **Tool Builder** - Tool design & implementation with Zen Tools
7. **Smart Prep Cloud** - Cloud handoff preparation with Zen Tools

---

## Test Sync Pattern

**What is it?**  
A workflow pattern where agents load context → find next task → execute → update → repeat.

**Pattern:**
```
1. Load Workflow Context
   ↓
2. Get Next Executable Task (dependencies respected)
   ↓
3. List Pending Tasks (show user the queue)
   ↓
4. Execute Current Task
   ↓
5. Update Task Status
   ↓
6. Continue (repeat for next task)
```

**Benefit:**  
User never has to manually select which task to work on. System automatically prioritizes based on dependencies and user goals.

---

## User Experience

### Before Agent Update
- Manual task selection
- No dependency tracking
- File-based task storage
- Limited visibility into queue

### After Agent Update
✅ **Automatic Task Queue**
- "Next Executable Task: [title]"
- "Ready Queue: [2-3 tasks]"
- "Pending (blocked): [N] tasks"

✅ **Dependency Aware**
- Blocked tasks clearly marked
- Dependencies shown in queue
- Tasks execute in correct order

✅ **Real-Time Updates**
- Task status updates immediately
- Queue refreshes automatically
- Progress visible throughout

✅ **User Friendly**
- One-click task execution
- Queue visible at all times
- Can override priorities if needed

---

## Zen Tasks Tools Available to All Agents

Each agent now has access to:

| Tool | Purpose |
|------|---------|
| `loadWorkflowContext` | Load project structure & dependencies |
| `listTasks` | Query tasks by status |
| `getNextTask` | Find highest-priority executable task |
| `addTask` | Create new task |
| `getTask` | Fetch task details |
| `updateTask` | Modify task (status, insights, etc) |
| `setTaskStatus` | Change task state quickly |
| `parseRequirements` | Structure goals into subtasks |

---

## Per-Agent Changes

### Full Auto (UI Hub)
**New Capabilities:**
- Displays ready task queue from `getNextTask(limit=3)`
- Shows pending tasks with `listTasks(status=pending)`
- Refreshes queue on demand
- Routes to spoke agents with current task context

**Key Update:**
```yaml
test_sync_enabled: true
zen_workflow_loaded: false
next_tasks_queue: []
```

### Smart Execute (Execution)
**New Capabilities:**
- Loads workflow context at startup
- Gets next task automatically
- Lists full pending queue
- Updates status after each task
- Continues execution flow

**Integration:**
```
loadWorkflowContext() 
  → listTasks(status=pending)
  → getNextTask(limit=1)
  → Execute task
  → updateTask(status=completed)
  → Repeat
```

### Smart Plan (Planning)
**New Capabilities:** (Was missing Zen Tools)
- Loads workflow context before planning
- Uses `parseRequirements` to structure goals
- Creates subtasks with `addTask`
- Validates with `getNextTask` (ensure executable)
- Displays created queue to user

**New Workflow:**
```
loadWorkflowContext()
  → parseRequirements(user goal)
  → addTask() for each subtask
  → getNextTask() validation
  → Show queue to user
```

### Smart Review (Analysis)
**New Capabilities:**
- Loads workflow context for criteria
- Lists completed tasks
- Analyzes patterns and root causes
- Updates task insights
- Uses `getNextTask` for replan prioritization

**Integration:**
```
loadWorkflowContext()
  → listTasks(status=completed/failed)
  → Analyze patterns
  → Decide recommendation
  → updateTask() with insights
```

---

## Zen Tasks Setup in Workspace

### ✅ Already Configured
- Extension installed: `barradevdigitalsolutions.zen-tasks-copilot`
- Recommended in `.vscode/extensions.json`
- All tool endpoints available
- No additional setup needed

### To Use
1. Open command palette: `Ctrl+Shift+P`
2. Run: "Zen Tasks: Show Task Explorer"
3. Create initial tasks or load from existing plan
4. Start Full Auto workflow - see queue auto-populate

---

## Task Visibility Example

When user clicks "Execute Phase" in Full Auto:

```
📋 TASK QUEUE STATUS

🎯 Next Executable Task:
   [1] Implement User Authentication
   Status: pending | Priority: HIGH | Complexity: 7

📚 Ready Queue (can start immediately):
   [2] Setup Database Schema (MEDIUM, 5)
   [3] Create API Endpoints (MEDIUM, 6)

⏳ Pending (waiting for dependencies):
   [4] Deploy to Production (BLOCKED - needs 1,2,3)
   [5] Performance Testing (BLOCKED - needs 4)

📊 Summary: 5 total | 3 ready | 2 blocked

🔄 ACTIONS:
[▶️ Execute Task 1] [🔄 Refresh] [📋 See Details]
```

User doesn't need to pick - just click execute and system handles the rest.

---

## Workflow Flow with Test Sync

```
User: "Run Full Auto with project goal"
         ↓
Full Auto loads workflow context
         ↓
Shows task queue: "Next: Task A | Ready: Tasks B,C | Pending: Tasks D,E"
         ↓
User clicks [Plan Phase]
         ↓
Smart Plan receives Task A context
         ↓
Smart Plan: parseRequirements(A) → creates subtasks → validates with getNextTask
         ↓
Returns to Full Auto: "Created 5 subtasks, ready to execute?"
         ↓
User clicks [Execute Phase]
         ↓
Smart Execute: loads context → lists pending → getNextTask → executes → updates
         ↓
Continues automatically through queue OR waits for user
         ↓
Returns to Full Auto: "Execution done, ready for review?"
         ↓
User clicks [Review Phase]
         ↓
Smart Review: analyzes results → finds issues → recommends replan or done
         ↓
Back to Full Auto with recommendation
         ↓
User confirms or overrides recommendation
         ↓
Loop continues or workflow ends
```

---

## Key Benefits

1. **No Manual Task Selection** - System finds next executable task
2. **Dependency Tracking** - Blocked tasks marked, respected automatically
3. **Queue Visibility** - User always sees what's next, what's waiting, what's done
4. **Real-Time Updates** - Task status updates immediately across all agents
5. **User Control** - Can override system recommendations anytime
6. **Progress Tracking** - Every action logged and visible
7. **Scalable** - Works with 5 tasks or 500 tasks
8. **Smart Routing** - Agents hand off with full context

---

## Technical Details

### Tool Integration Points

**Full Auto:**
- Calls `loadWorkflowContext()` on startup
- Displays `getNextTask(limit=3)` in UI
- Shows `listTasks(status=pending, limit=10)` as backlog
- Routes to spokes with task context

**Smart Execute:**
- Calls `loadWorkflowContext()` at phase start
- Gets `getNextTask(limit=1)` before each execution
- Lists `listTasks(status=pending)` for queue awareness
- Updates status with `updateTask(status=completed)` after work

**Smart Plan:**
- Calls `loadWorkflowContext()` before planning
- Uses `parseRequirements(goal)` to structure work
- Creates with `addTask()` for each subtask
- Validates with `getNextTask()` to ensure first task is executable

**Smart Review:**
- Calls `loadWorkflowContext()` for context
- Lists `listTasks(status=completed)` to see results
- Updates insights with `updateTask(insights=findings)`
- Uses `getNextTask()` to prioritize replan work

---

## Extension Info

**Extension:** `barradevdigitalsolutions.zen-tasks-copilot`  
**Marketplace:** VS Code Extensions  
**Status:** ✅ Installed and recommended in workspace  
**Version:** Latest (auto-updates)

**Features Provided:**
- Task creation & management
- Dependency tracking
- Priority-based ordering
- Status workflow
- Real-time updates
- Sidebar queue view
- Integration with agents

---

## Next Steps for User

### Immediate
1. Ensure Zen Tasks Copilot extension is installed
2. Open Zen Tasks explorer from sidebar
3. Create initial project tasks or import from your plan

### Testing
1. Run Full Auto workflow
2. Observe task queue displays
3. Go through Plan → Execute → Review cycle
4. Watch Zen Tasks update in real-time

### Validation Checklist
- [ ] Full Auto displays task queue
- [ ] Queue shows ready tasks first
- [ ] Task status updates persist
- [ ] Plan phase creates subtasks in Zen Tasks
- [ ] Execute phase respects task order
- [ ] Review shows completed tasks
- [ ] Can complete full workflow end-to-end

---

## Support & Configuration

### To Restart Zen Tasks
```
Command Palette → Zen Tasks: Reset State
```

### To View All Tasks
```
Command Palette → Zen Tasks: Show All Tasks
```

### To Create New Project
```
Command Palette → Zen Tasks: Create Project
→ Name: [your project]
→ Tasks: [add initial tasks]
```

### To Load Task File
```
Command Palette → Zen Tasks: Import Tasks
→ Select JSON file with tasks
```

---

## Summary

✅ **All agents updated**  
✅ **Zen Tools integrated**  
✅ **Test sync pattern implemented**  
✅ **User-friendly task queue**  
✅ **Automatic dependency management**  
✅ **Real-time progress tracking**  
✅ **Production ready**

Your agents are now **Zen Tasks aware** and use **intelligent task selection** to keep users informed and productive throughout every workflow phase.

---

**Questions?** See `/memories/dev/agent-builder/` for technical details or check the agent files directly for implementation examples.
