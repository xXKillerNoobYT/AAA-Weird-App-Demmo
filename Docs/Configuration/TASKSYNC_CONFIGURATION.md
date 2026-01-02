# TaskSync Configuration for Phase-Gated Workflows

## Overview

This document configures TaskSync queue mode for autonomous, phase-gated workflow execution with user confirmation checkpoints.

**Key Principle:** Each phase (Plan, Execute, Review) runs to completion, then **returns to Full Auto** for confirmation before proceeding to the next phase.

## Phase Gates & Confirmation Workflow

### Phase 1: Plan Phase (Smart Plan)
```
TRIGGER: User clicks "🎯 Plan Phase"
STATE: Full Auto routes to Smart Plan with TASKSYNC ENABLED

Smart Plan Execution:
1. Call zen-tasks_000_workflow_context() — Load workflow
2. Analyze goal for vagueness (QA survey if needed)
3. Create subtasks via addTask() — Auto-generated list
4. Collect all created subtasks

COMPLETION CHECKPOINT:
📋 PHASE COMPLETE - Planning Done
"I created these subtasks: [LIST]
Confirm adding these to execution queue? [YES / NO]"

RETURN PATH: Full Auto hub
- If [YES]: Tasks ready for execution, enable Execute Phase button
- If [NO]: Return to planning for revision
```

### Phase 2: Execute Phase (Smart Execute)
```
TRIGGER: User clicks "⚡ Execute Phase" (after Plan confirmation)
STATE: Full Auto routes to Smart Execute with TASKSYNC ENABLED

Smart Execute Execution:
1. Call zen-tasks_000_workflow_context() — Load workflow
2. Loop through pending tasks via getNextTask()
3. For each task:
   a. Run task execution (terminal, files, tools)
   b. Log observations and results
   c. **CONFIRMATION CHECKPOINT**:
      "✅ TASK COMPLETE - Confirm before marking done?
      [Task: {name} | Status: Completed | Observations: ...]
      [YES - Mark Done] [NO - Review First]"
   d. Only call setTaskStatus("completed") if [YES]
   e. Log user confirmation status to observations
4. Collect EXECUTED_TASKS and FAILED_TASKS with summaries

COMPLETION CHECKPOINT:
📋 PHASE COMPLETE - Execution Done
"Executed Tasks: [COUNT]
Failed Tasks: [COUNT]
Observations: [SUMMARY]
Ready for review phase? [YES / NO]"

RETURN PATH: Full Auto hub
- If [YES]: Proceed to Review Phase
- If [NO]: Return to execution for continuation or modification
```

### Phase 3: Review Phase (Smart Review)
```
TRIGGER: User clicks "🔍 Review Phase" (after Execute confirmation)
STATE: Full Auto routes to Smart Review with TASKSYNC ENABLED

Smart Review Execution:
1. Call zen-tasks_000_workflow_context() — Load workflow
2. Analyze completed and failed tasks
3. Perform root-cause analysis
4. Identify discovered tasks/issues
5. Update task insights

DISCOVERY CHECKPOINT:
📋 DISCOVERED_TASKS - Review found these issues:
"- [Issue 1] (Priority: {P}, Complexity: {C})
 - [Issue 2] (Priority: {P}, Complexity: {C})
 
Add these to backlog for next cycle? [YES / NO / EDIT]"

IF [EDIT]:
"Modify task details before adding:
- Issue 1: [Priority selector] [Complexity selector]
- Issue 2: [Priority selector] [Complexity selector]
[SAVE / CANCEL]"

6. Create confirmed discovered tasks via addTask()

COMPLETION CHECKPOINT:
📋 PHASE COMPLETE - Review Done
"Analysis Complete:
- Patterns Found: [COUNT]
- Root Causes: [COUNT]
- Discovered Tasks: [COUNT]

Recommendation: [Continue Loop / Mark Done]
Continue loop (Plan→Execute→Review) again? [YES / NO]"

RETURN PATH: Full Auto hub
- If [YES]: Reset cycle, return to Plan Phase with new discovered tasks
- If [NO]: Mark workflow complete, archive observations
```

## TaskSync Queue Mode Configuration

### MCP Server Setup
```json
{
  "server": "localhost:3579",
  "mode": "queue",
  "features": [
    "batch_prompts",
    "file_references",
    "tool_call_tracking",
    "observation_logging"
  ]
}
```

### Agent-Specific Queue Configuration

#### Full Auto (Hub Coordinator)
```yaml
tasksync_mode: "normal" (interactive mode only - no queue)
tools_enabled:
  - loadWorkflowContext (load only, no state changes)
  - listTasks (read task list)
  - getNextTask (find ready tasks)
  - getTask (task details)
  - memory (state tracking)
handoffs_configured: 3 (Plan, Execute, Review)
confirmation_required: "user click for phase selection"
observations_logged: "phase routing decisions"
```

#### Smart Plan (Planning Specialist)
```yaml
tasksync_mode: "queue"
queue_config:
  batch_size: 1 (one planning session per queue invocation)
  timeout: 300 (5 minutes for planning completion)
  auto_confirm: false (require user confirmation before returning)
tools_enabled:
  - loadWorkflowContext (required first step)
  - parseRequirements (parse goals into subtasks)
  - addTask (create subtasks)
  - listTasks (view created subtasks)
  - memory (log planning decisions)
confirmation_workflow:
  trigger: "after all subtasks created"
  display: "List of created subtasks"
  user_action: "Confirm add to queue? [YES/NO]"
  logging: "Plan decisions + user confirmation"
observations_logged: "vagueness score, QA survey results, subtasks created, user confirmation"
```

#### Smart Execute (Execution Specialist)
```yaml
tasksync_mode: "queue"
queue_config:
  batch_size: 1 (one task per queue invocation)
  timeout: 600 (10 minutes per task)
  auto_confirm: false (require user confirmation before marking done)
  error_continuation: true (continue on task failures)
tools_enabled:
  - loadWorkflowContext (required first step)
  - getNextTask (get pending tasks)
  - setTaskStatus (mark task in-progress/completed/failed)
  - listTasks (view pending/completed)
  - memory (log execution details)
  - terminal tools (execute commands)
  - file tools (read/write files)
confirmation_workflow:
  per_task: "After execution, show task completion summary"
  trigger: "after each task execution"
  display: "Task: {name} | Result: {success/failure} | Observations: {details}"
  user_action: "Confirm done? [YES/NO] or [Review First]"
  logging: "Task execution log + user confirmation status"
phase_completion:
  trigger: "all pending tasks processed or user stops"
  display: "Executed: {count} | Failed: {count}"
  user_action: "Ready for review? [YES/NO]"
observations_logged: "task execution logs, confirmations, command outputs, errors, solutions"
```

#### Smart Review (Review Specialist)
```yaml
tasksync_mode: "queue"
queue_config:
  batch_size: 1 (one review cycle per queue invocation)
  timeout: 300 (5 minutes for analysis)
  auto_confirm: false (require user confirmation for discovered tasks)
tools_enabled:
  - loadWorkflowContext (required first step)
  - listTasks (fetch completed/failed tasks)
  - updateTask (update task insights)
  - addTask (create discovered tasks)
  - getTask (get task details)
  - memory (log analysis results)
confirmation_workflow:
  discovered_tasks: "After creating discovered tasks"
  trigger: "if new issues/tasks identified"
  display: "📋 DISCOVERED_TASKS - [list with priority/complexity]"
  user_action: "Add to backlog? [YES/NO/EDIT]"
  logging: "Discovered tasks + user decision"
  edit_workflow: "Allow user to modify priority/complexity before confirmation"
loop_decision:
  trigger: "after analysis and discoveries complete"
  display: "Recommendation: [Continue Loop / Mark Done]"
  user_action: "[YES - Continue Loop] [NO - Mark Done]"
  logging: "Review findings + user loop decision"
observations_logged: "pattern analysis, root causes, discovered tasks, loop decision"
```

## User Confirmation Checkpoints Summary

| Phase | Checkpoint | User Action | System Response |
|-------|-----------|------------|-----------------|
| Plan | After subtask creation | "Confirm add? [YES/NO]" | Add to queue if YES |
| Execute | After each task | "Confirm done? [YES/NO]" | Mark status if YES |
| Review | After discovery analysis | "Add to backlog? [YES/EDIT/NO]" | Create tasks if YES |
| Hub | After phase complete | Phase-specific button | Route or complete |

## Observation Logging Format

### Planning Phase Observations
```markdown
**Type:** planning
**Timestamp:** ISO-8601
**Event:** 
  - vagueness_detected: 0-1 score
  - qa_survey_conducted: true/false
  - subtasks_created: [list of task IDs]
  - user_confirmation: [YES/NO]
**Summary:** [Brief description]
```

### Execution Phase Observations
```markdown
**Type:** execution
**Timestamp:** ISO-8601
**Task ID:** [task-uuid]
**Event:**
  - execution_started: timestamp
  - execution_completed: timestamp
  - result: [success/failure]
  - observations: [detailed findings]
  - user_confirmed: [YES/NO]
  - confirmation_reason: [if NO]
**Summary:** [Brief description]
```

### Review Phase Observations
```markdown
**Type:** review
**Timestamp:** ISO-8601
**Event:**
  - patterns_identified: [count]
  - root_causes_found: [list]
  - discovered_tasks: [list of task IDs]
  - task_insights_updated: [count]
  - user_decision: [continue_loop/mark_done]
  - discovered_tasks_confirmed: [YES/NO]
**Summary:** [Brief description]
```

## TaskSync Integration Checklist

- [ ] Full Auto agent configured for interactive mode (no queue)
- [ ] Smart Plan agent configured for queue mode with planning timeout
- [ ] Smart Execute agent configured for queue mode with task confirmations
- [ ] Smart Review agent configured for queue mode with discovered task confirmations
- [ ] All agents call zen-tasks_000_workflow_context() as first step
- [ ] All observation formats match documented schema
- [ ] User confirmations captured and logged before state changes
- [ ] Phase completion checkpoints implemented in each agent
- [ ] Handoffs return to Full Auto (hub-spoke pattern enforced)
- [ ] MCP server running on localhost:3579
- [ ] Zen Tasks extension updated to latest version
- [ ] Test workflow: Plan → Execute → Review → Loop → Done

## Running the Phase-Gated Workflow

### Command Line (with TaskSync Queue Mode)

```bash
# Start MCP server for TaskSync
npm start  # or your MCP server command

# Launch VS Code and load Full Auto agent
# Then either:
# 1. Click buttons in Full Auto UI for manual phase transitions
# 2. Use TaskSync CLI for autonomous queue mode:
tasksync queue-add "Plan Phase" --agent "Smart Plan" --context "goal"
tasksync queue-add "Execute Phase" --agent "Smart Execute" --context "planned_tasks"
tasksync queue-add "Review Phase" --agent "Smart Review" --context "execution_results"

# OR in VS Code Chat:
# @Full Auto
# Start workflow: [Plan Phase] [Execute Phase] [Review Phase]
```

### Interactive Mode (Default)

1. Open Full Auto agent in VS Code
2. Click "🎯 Plan Phase"
3. After planning: Click "⚡ Execute Phase"
4. After execution: Click "🔍 Review Phase"
5. Review recommendation: "Continue Loop?" or "Done?"
6. If "Continue Loop": Return to step 2

## Success Criteria

✅ **Planning Phase:**
- Goal analyzed and vagueness detected
- Subtasks created and listed
- User confirmed before returning to hub

✅ **Execution Phase:**
- Tasks executed in dependency order
- Observations logged for each task
- User confirmed each task completion
- Failed tasks logged with error details

✅ **Review Phase:**
- Pattern analysis completed
- Root causes identified
- Discovered tasks confirmed by user
- Loop or completion recommended

✅ **Workflow Loop:**
- Phases return to hub (no direct chaining)
- User has control points at each phase boundary
- Observations preserved for analysis
- Zen Tasks remains single source of truth

## Troubleshooting

### Agents Not Following Confirmation Workflow
- Verify TaskSync version 0.1.0+ installed
- Check askUser tool is available in agent tools list
- Review agent handoffs point back to Full Auto hub

### Task Status Not Updating
- Ensure setTaskStatus called AFTER user confirmation
- Check user actually clicked [YES] in confirmation prompt
- Review observation logs to verify confirmation was recorded

### Loop Not Working
- Verify Review phase returns to Full Auto (not Smart Plan directly)
- Check Full Auto presents loop button after Review completes
- Confirm loadWorkflowContext refreshes state between phases

### Missing Observations
- Verify add_observations called with proper format
- Check memory tool configured for observation storage
- Review observation logging is happening in each agent phase

