# Quick Start Guide - Tight Loop Workflow Testing

**Purpose:** Test the integrated tight loop (Plan→Execute→Review→Loop) workflow with Zen Tasks

---

## Prerequisites

Before you start, ensure:

- [ ] VS Code with GitHub Copilot agents installed
- [ ] Zen Tasks MCP Server running on `localhost:3579`
- [ ] The 4 agents available in `.github/agents/`:
  - [ ] Full Auto New.agent.md
  - [ ] Smart Plan Updated.agent.md
  - [ ] Smart Execute Updated.agent.md
  - [ ] Smart Review Updated.agent.md
- [ ] Task backlog populated in Zen Tasks (28 tasks from migration)
- [ ] Read the AGENT_VERIFICATION_REPORT.md for full technical details

---

## Starting Your Test Session

### Step 1: Open Full Auto Hub

1. Open your workspace in VS Code
2. Open the **Full Auto New** agent
3. You should see a welcome message with the tight loop workflow diagram
4. You'll see buttons for three options (or ask for a goal)

### Step 2: Select Planning Phase

**Click: "🎯 Plan Phase"** (or mention it in conversation)

**What happens:**
- Full Auto hands off to Smart Plan
- Smart Plan loads Zen Tasks workflow context
- Smart Plan calls `getNextTask(limit=1)` to find the highest-priority pending task
- You'll see a task title, description, priority, complexity

**Smart Plan will:**
1. Show you the task it found: "Planning for: [TASK TITLE]"
2. Ask if there's any vagueness: "Any unclear requirements? [YES/NO]"
3. If YES: Ask clarifying questions
4. If NO: Proceed to create subtasks
5. Show created subtasks with priorities and complexity
6. Ask: "Ready to execute? [YES/NO]"

**⏱️ Expected Time:** 1-2 minutes

### Step 3: Execute Subtasks

**Click: "Yes, start execution"** (or approve at Smart Plan's confirmation)

**What happens:**
- Smart Plan auto-chains to Smart Execute
- Smart Execute loads Zen Tasks workflow context
- Smart Execute gets the subtasks created by Plan
- For each subtask, Execute will:
  1. Show the subtask: "[TASK] - Complexity: [N]"
  2. Execute or simulate execution
  3. Ask: "✅ Task complete? [YES/NO]"
  4. You respond YES (or NO if there's an issue)
  5. Mark in Zen Tasks with `setTaskStatus(task_id, "completed")`

**Smart Execute will:**
- Loop through all pending/subtasks
- Ask per-task confirmation
- Log observations (success/failure)
- Show progress: "Completed [3/5] subtasks"
- Ask: "Ready for review? [YES/NO]"

**⏱️ Expected Time:** 2-5 minutes (depending on number of subtasks)

### Step 4: Review Results

**Click: "Yes, start review"** (or approve at Execute's confirmation)

**What happens:**
- Smart Execute auto-chains to Smart Review
- Smart Review loads Zen Tasks workflow context
- Smart Review analyzes completed/failed tasks
- **NEW: Duplicate Prevention Check**
  - For any improvements it discovers, it calls `listTasks()` to check if the issue already exists as a task
  - If found: Shows "[X duplicates skipped]"
  - If new: Adds to discovered tasks list
- Shows: "Patterns identified: [list]"
- Shows: "Root causes: [list]"
- Shows: "Discovered tasks: [list] ([X duplicates skipped])"
- Asks: "Add discovered tasks to backlog? [YES/NO/EDIT]"

**Smart Review will:**
1. List what succeeded/failed
2. Perform root-cause analysis
3. Identify improvements/issues
4. Check for duplicates (new feature ✅)
5. Present discovered tasks with edit option
6. Ask final decision: "Continue loop? [YES/NO]"

**⏱️ Expected Time:** 1-2 minutes

### Step 5: Loop or Exit

**Decision Point:**

- **"[YES] Loop to Plan"** (Click to continue)
  - Auto-chains back to Smart Plan
  - Smart Plan calls `getNextTask()` again for next iteration
  - Cycle repeats (this is the tight loop!)
  
- **"[NO] Done & Exit"** (Click to stop)
  - Returns to Full Auto
  - Shows session summary (tasks completed, discovered issues, iterations)
  - You can start new session or review results

**Loop Behavior:**
- The tight loop continues until you explicitly say DONE
- Each iteration should get faster (smaller tasks)
- Watch for patterns: what types of issues come up repeatedly?
- Loop ends when no pending tasks remain OR you approve "Done"

---

## What to Watch For During Testing

### ✅ Verify These Work Correctly

1. **Smart Plan getNextTask()**
   - Watch: Does Smart Plan immediately find and show a pending task?
   - Expected: Shows actual task from your backlog (not asking for input first)
   - Success Indicator: "Planning for: [real task from Zen Tasks]"

2. **Smart Review Duplicate Prevention**
   - Watch: When Review discovers issues, does it say "[X duplicates skipped]"?
   - Expected: If an issue was found in a previous iteration, it won't be added again
   - Success Indicator: "4 discovered tasks, 1 duplicate skipped, 3 to add"
   - If no duplicates exist yet, that's also fine (count will be 0)

3. **Tight Loop Handoffs**
   - Watch: Does each agent auto-chain to the next?
   - Expected: No return to Full Auto between Plan→Execute→Review
   - Success Indicator: You see "Handoff to Smart Execute..." or similar message
   - DO NOT manually switch agents (they should chain automatically)

4. **Per-Task Confirmation**
   - Watch: Does Execute ask confirmation for EACH task?
   - Expected: "✅ Task complete? [YES/NO]" appears for every subtask
   - Success Indicator: You can confirm or reject individual task completion
   - DO NOT let it batch multiple tasks together

5. **Observation Logging**
   - Watch: Does each agent show "Logging observations..." messages?
   - Expected: Observations include timestamps, task IDs, status changes
   - Success Indicator: Review section shows previous observations when analyzing

### 🚨 Red Flags (If You See These, Something's Wrong)

| Red Flag | Expected Behavior | What to Do |
|----------|------------------|-----------|
| Plan asks "What do you want to do?" | Plan should show found task immediately | Check Zen Tasks is running, has pending tasks |
| Execute doesn't ask per-task confirmation | Execute should ask for EACH subtask | This is a workflow bug, note line in agent |
| Review chains to Full Auto instead of Plan | Review should auto-chain back to Plan for loop | Check handoff section in Smart Review agent |
| No "[X duplicates skipped]" message | Review should show duplicate count (even if 0) | Check Step 6 of Smart Review was updated correctly |
| Duplicate task created twice | Smart Review should prevent this via listTasks() check | Duplicate prevention may not be working |
| Same issue discovered each iteration | Should eventually recognize and skip as duplicate | Indicates listTasks() filter might be too strict |

---

## Test Scenarios

### Scenario 1: Quick Single-Task Loop (15 minutes)

**Goal:** Verify basic tight loop functionality

1. Start: Full Auto → Plan
2. Expect: Plan finds 1 pending task
3. Plan: Create 1-2 simple subtasks
4. Execute: Complete 2 subtasks
5. Review: Analyze results
6. Exit: Click "No, done for now"

**Success Criteria:**
- ✅ Plan found task automatically
- ✅ Execute asked per-task confirmation
- ✅ Review showed analysis
- ✅ All tools worked without errors

### Scenario 2: Multi-Iteration Loop (30 minutes)

**Goal:** Test loop continuation across multiple cycles

1. Start: Full Auto → Plan (Iteration 1)
2. Plan: Create subtasks for Task #1
3. Execute: Complete all subtasks
4. Review: Discover issues
5. Loop: Click "Yes, continue" (Iteration 2)
6. Plan: getNextTask() should find Task #2
7. Execute: Complete Task #2 subtasks
8. Review: Identify new issues + duplicate prevention test
9. Exit: Click "No, done"

**Success Criteria:**
- ✅ Loop continues without returning to Full Auto
- ✅ Each iteration finds a different task
- ✅ Duplicate prevention activates in iteration 2+
- ✅ Shows "[X duplicates skipped]" when applicable

### Scenario 3: Duplicate Prevention Validation (20 minutes)

**Goal:** Specifically test that duplicate task creation is prevented

1. Choose a task that has common improvement opportunities
2. Plan: Create subtasks
3. Execute: Run them
4. Review: Discover issue → "Create database migration helper" task
5. Approve: Add discovered task to backlog
6. Loop: Continue (Iteration 2)
7. Plan: Get next task
8. Execute: Complete it
9. Review: Find same issue again
10. **VERIFY:** Shows "1 duplicate skipped" or "Task already exists"
11. Exit: No new task created for the same issue

**Success Criteria:**
- ✅ First iteration: Issue discovered, task created
- ✅ Second iteration: Same issue detected but NOT added (duplicate prevented)
- ✅ Review shows explicit count of skipped duplicates
- ✅ Observations log both creation and skipping

---

## Common Issues & Troubleshooting

### Issue: Plan Says "No pending tasks"

**Cause:** Zen Tasks queue is empty or all tasks completed

**Solution:**
- Check Zen Tasks: `getNextTask()` should return pending task
- Create a test task manually in Zen Tasks
- Verify task status is "pending" not "completed"

### Issue: Execute Doesn't Show Per-Task Confirmation

**Cause:** Agent workflow may not have been updated

**Solution:**
- Check Smart Execute agent Step 4-5
- Ensure "Before marking complete: Show '✅ TASK COMPLETE - Confirm? [YES/NO]'" exists
- If missing, manually update the agent

### Issue: Review Doesn't Show Duplicate Skipped Message

**Cause:** Duplicate prevention logic may not be active

**Solution:**
- Check Smart Review Step 6: "DUPLICATE CHECK"
- Verify `listTasks()` call is present before `addTask()`
- If missing, manually update Step 6
- Check AGENT_VERIFICATION_REPORT.md for exact implementation

### Issue: Loop Doesn't Auto-Chain (Keeps Returning to Full Auto)

**Cause:** Handoff configuration not updated

**Solution:**
- Check each agent's handoff section
- Plan should say "→ Smart Execute" as primary
- Execute should say "→ Smart Review" as primary
- Review should say "→ Smart Plan" for loop option
- Update any that don't match

### Issue: Tasks Getting Duplicated in Backlog

**Cause:** Duplicate prevention didn't catch the match

**Solution:**
- Check listTasks() filter in Smart Review Step 6
- Filter might be too strict (exact match) or too loose
- Verify task titles/summaries for exact matches
- May need to adjust filter logic

---

## Collecting Test Results

### What to Record

While testing, document:

1. **Timing:**
   - How long did each phase take?
   - Was it responsive?
   - Any hangs or delays?

2. **Functionality:**
   - Did each agent perform its role?
   - Were confirmations asked at right times?
   - Did handoffs work smoothly?

3. **Features:**
   - Did Plan find next task via getNextTask()?
   - Did Execute ask per-task confirmation?
   - Did Review prevent duplicates?
   - Did loop continue correctly?

4. **Errors:**
   - Any error messages?
   - Tasks that failed unexpectedly?
   - Tool calls that didn't work?

### Output Template

```markdown
# Test Session Report

**Date:** [DATE]  
**Scenario:** [SCENARIO NAME]  
**Duration:** [TIME]

## Results Summary

**Plan Phase:**
- Found task: [TASK NAME]
- Subtasks created: [N]
- Vagueness score: [0.0-1.0]
- Time: [N minutes]

**Execute Phase:**
- Completed: [N/N] subtasks
- Failed: [N]
- User confirmations requested: [YES/NO]
- Time: [N minutes]

**Review Phase:**
- Patterns identified: [LIST]
- Root causes: [LIST]
- Discovered tasks: [N]
- Duplicates skipped: [N]
- Time: [N minutes]

**Loop Result:**
- Continued: [YES/NO]
- Total iterations: [N]

## Observations

[Notes on functionality, timing, issues]

## Issues Found

- [ ] Issue 1: [Description]
- [ ] Issue 2: [Description]

## Success Criteria Met

- [x] Functionality 1
- [ ] Functionality 2
- [x] Functionality 3
```

---

## After Testing

### Recommended Actions

1. **If all tests pass:**
   - Document results
   - Create a GitHub issue with "tight-loop-validated" label
   - Ready for production use

2. **If some tests fail:**
   - Document failures
   - Create GitHub issues for each
   - Route to Agent Builder for fixes
   - Re-test after fixes

3. **If duplicate prevention isn't working:**
   - Check listTasks() filter syntax
   - Verify task titles match expected patterns
   - May need to improve filter logic
   - Create issue for Tool Builder to enhance listTasks()

4. **General feedback:**
   - Was the tight loop intuitive?
   - Did handoffs feel natural?
   - Any suggestions for improvement?
   - Document for workflow refinement

---

## Quick Reference Commands

**In Smart Plan, Execute, or Review agent, you can manually trigger:**

- Load context: Type "Load workflow context"
- Get next task: Type "Get next task"
- List tasks: Type "List pending tasks"
- Update task: Type "Mark task complete" or "Update task status"

**Zen Tasks Tools Available:**
```
loadWorkflowContext()         # Load project state
getNextTask(limit=1)          # Get next pending task
listTasks(status=pending)     # See all pending tasks
addTask(title, desc, ...)     # Create new task
setTaskStatus(id, status)     # Mark task complete
updateTask(id, {...})         # Update task details
```

---

## Need Help?

If you encounter issues or have questions:

1. Check AGENT_VERIFICATION_REPORT.md (technical details)
2. Check TIGHT_LOOP_WORKFLOW.md (architecture explanation)
3. Check individual agent files for step-by-step details
4. Create a GitHub issue with "tight-loop-testing" label
5. Share test session report and error messages

**Expected Outcome:** After testing, you should have high confidence that:
- ✅ Tight loop works without manual agent switching
- ✅ Smart Plan finds tasks via getNextTask()
- ✅ Smart Review prevents duplicate task creation
- ✅ Agents can loop multiple iterations autonomously
