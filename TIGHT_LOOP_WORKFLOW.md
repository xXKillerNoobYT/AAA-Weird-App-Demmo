# Tight Loop Workflow - Plan→Execute→Review→Loop

## Overview

The three agents (Smart Plan, Smart Execute, Smart Review) now form a **tightly-integrated loop** that runs without returning to Full Auto between phases. The loop continues automatically until the user explicitly says "DONE" to break back to the hub.

**Workflow Pattern:**
```
User: @Full Auto
User clicks: 🎯 Plan Phase

                        ↓
        
     Smart Plan (Phase 1)
     Create subtasks
     User confirms: [YES]
     ↓ (CHAIN: Handoff → Execute)
     
     Smart Execute (Phase 2)
     Run each task with per-task confirmation
     User confirms each: [YES]
     When all done → ↓ (CHAIN: Handoff → Review)
     
     Smart Review (Phase 3)
     Analyze & discover issues
     User confirms discoveries: [YES]
     ↓ (AUTO LOOP)
     
     ? CONTINUE LOOP or BREAK?
     
     IF Continue Loop → ↓ (CHAIN: Handoff → Plan)
     
     Smart Plan (Phase 1 again - Iteration 2)
     Plan discovered tasks / next iteration
     Loop cycles...
     
     IF User says DONE → ↓ (CHAIN: Handoff → Full Auto)
     Full Auto displays session results
```

## Loop Lifecycle

### Entry Point: Full Auto

```
Full Auto (User's entry point)
  User clicks: 🎯 Plan Phase
  
  → Routes to Smart Plan with context
  
  Note: Full Auto is NOT called again until user breaks loop
```

### Loop Iteration 1: Plan → Execute → Review

**Smart Plan - First Iteration**
```
Step 1: Load Zen workflow context
Step 2: Analyze goal and create subtasks
Step 3: Ask user: "Confirm subtasks? [YES/NO]"

If [YES]:
  → CHAIN: Auto-handoff to Smart Execute
  
If [NO]:
  → Wait for user to provide feedback
  → Revise subtasks
  → Ask confirmation again
```

**Smart Execute - First Iteration**
```
Step 1: Load Zen workflow context
Step 2: Get pending subtasks from plan
Step 3: Loop through each task:
  - Execute task
  - Ask: "Task complete - confirm? [YES/NO/REVIEW]"
  - If [YES]: Mark done and continue
  - If [NO]: Debug or skip
  - If [REVIEW]: Ask for clarification
Step 4: When all tasks done:
  → CHAIN: Auto-handoff to Smart Review
```

**Smart Review - First Iteration**
```
Step 1: Load Zen workflow context
Step 2: Analyze completed/failed tasks
Step 3: Discover new issues/tasks
Step 4: Ask: "Add discovered tasks? [YES/NO/EDIT]"

If [YES] or [EDIT then YES]:
  → CHAIN: Auto-handoff back to Smart Plan (LOOP)
  
If [NO]:
  → Ask: "Done with this feature? [YES/NO]"
  
  If [YES]: CHAIN: Break to Full Auto
  If [NO]: Wait for user direction
```

### Loop Iteration 2+: Plan → Execute → Review (again)

**Smart Plan - Second+ Iteration**
```
Receives discovered tasks from previous Review
Step 1: Load Zen workflow context
Step 2: Analyze discovered tasks as new goal
Step 3: Create new subtasks for discovered issues
Step 4: Ask: "Confirm [iteration N] subtasks? [YES/NO]"

If [YES]:
  → CHAIN: Auto-handoff to Smart Execute
  
Continue looping...
```

**Smart Execute - Second+ Iteration**
```
Executes subtasks for discovered issues
Same as iteration 1: execute → confirm → next
When done:
  → CHAIN: Auto-handoff to Smart Review
```

**Smart Review - Second+ Iteration**
```
Analyzes new execution results
May discover additional issues
If issues found:
  → CHAIN: Loop back to Smart Plan
  
If no issues found:
  → Recommend: "Feature complete?"
  → User decides: Continue or Done
  → Route accordingly
```

### Exit Point: Break Loop to Full Auto

**When Loop Breaks:**

```
Smart Review determines feature is complete:
  - No critical issues
  - All planned work done
  - User confirms: "Mark Done"
  
  → CHAIN: Auto-handoff to Full Auto
  
Full Auto receives:
  "LOOP BROKEN - Session Complete"
  Shows: ✓ Session Ended
  
  Displays:
  [New Session?] [View Results?] [Edit Tasks?] [Exit]
```

---

## Handoff Configuration

### Smart Plan Handoffs (Phase 1)

```yaml
Handoff 1: To Smart Execute
  Label: ⚡ Execute Phase (Auto Loop - Execution Starts)
  Prompt: "Planning complete. Execute the [LIST_SUBTASKS] now. 
           After each subtask completes and is user-confirmed, 
           get next subtask. When ALL subtasks done or user stops, 
           auto-transition to review without returning to hub. 
           Keep looping (Plan→Execute→Review→Loop) until user says DONE."
  
Handoff 2: To Full Auto (Emergency Break)
  Label: 📋 Back to Full Auto (Break Loop - Session End)
  Prompt: "LOOP BROKEN - User ended workflow. Show '✓ Session Ended' 
           and present: [New Session?] [View Results?] [Edit Tasks?]"
```

### Smart Execute Handoffs (Phase 2)

```yaml
Handoff 1: To Smart Review
  Label: 🔍 Review Phase (Auto Loop - Analysis Starts)
  Prompt: "Execution complete. Completed tasks: [EXECUTED_TASKS_LIST]. 
           Failed tasks: [FAILED_TASKS_LIST]. Analyze these results. 
           Discover issues. After user confirms discovered tasks, 
           auto-transition back to planning without returning to hub. 
           Keep looping (Plan→Execute→Review→Loop) until user says DONE."
  
Handoff 2: To Full Auto (Emergency Break)
  Label: 📋 Back to Full Auto (Break Loop - Session End)
  Prompt: "LOOP BROKEN - User ended workflow. Show '✓ Session Ended' 
           and present: [New Session?] [View Results?] [Edit Tasks?]"
```

### Smart Review Handoffs (Phase 3)

```yaml
Handoff 1: To Smart Plan
  Label: 🎯 Plan Next Phase (Auto Loop - Continue)
  Prompt: "Review complete. Discovered tasks [DISCOVERED_TASKS_LIST] 
           have been confirmed and added. Analyze these discovered tasks 
           and plan next iteration subtasks. Auto-transition to execution. 
           Keep looping (Plan→Execute→Review→Loop) without returning to hub 
           until user says DONE."
  
Handoff 2: To Full Auto (Break Loop)
  Label: 📋 Back to Full Auto (Break Loop - Session End)
  Prompt: "LOOP BROKEN - User ended workflow. Show '✓ Session Ended' 
           and present: [New Session?] [View Results?] [Edit Tasks?]"
```

---

## User Confirmation Points in Loop

### Iteration 1

| Phase | Confirmation | Options |
|-------|--------------|---------|
| Plan | Confirm subtasks? | [YES - Execute] [NO - Revise] |
| Execute | Task complete? | [YES - Done] [NO - Fix] |
| Review | Add discovered tasks? | [YES - Loop] [NO - Skip] |
| Loop Decision | Continue loop? | [YES - Plan Again] [NO - End] |

### Iteration 2+

Same confirmation pattern as Iteration 1, but with discovered tasks as new goal

---

## Loop Control Points

### User Can Control Loop With:

1. **[NO] on Confirmation**
   - Plan: Revise subtasks before executing
   - Execute: Debug or skip task
   - Review: Don't add discovered tasks

2. **[BREAK] Signal**
   - Any phase: User types "BREAK LOOP"
   - Smart Plan/Execute/Review detects and routes to Full Auto
   - Loop terminates, results returned

3. **[DONE] Signal**
   - Review phase: User confirms "Feature complete"
   - Auto-routes to Full Auto
   - Loop terminates gracefully

4. **[CONTINUE] Decision**
   - After Review: "Want to keep improving? [YES - Loop] [NO - Done]"
   - User controls whether to iterate

---

## Loop Behavior Examples

### Example 1: Linear Loop (Plan → Execute → Review → Loop → Plan... → Done)

```
Iteration 1:
  Smart Plan → Creates 5 subtasks
  User: [YES]
  ↓
  Smart Execute → Runs 5 tasks
  User: [YES] on each
  ↓
  Smart Review → Finds 2 improvements
  User: [YES] add to backlog
  ↓ (Auto-loop back)

Iteration 2:
  Smart Plan → Creates 2 new subtasks
  User: [YES]
  ↓
  Smart Execute → Runs 2 tasks
  User: [YES] on each
  ↓
  Smart Review → No new issues found
  User: [YES] mark feature complete
  ↓ (Break loop)

Full Auto → Shows: ✓ Session Complete
Results: 7 tasks done, 0 failed
```

### Example 2: Loop with Debugging (Execute revisits same task)

```
Iteration 1:
  Smart Plan → Creates 3 subtasks
  User: [YES]
  ↓
  Smart Execute → Task 1 succeeds
                  Task 2 fails (test failing)
                  User: [NO] - wants to debug same task
                  
  Continues with Task 2:
                  Execute fix
                  User: [YES] now passes
                  
                  Task 3 succeeds
  ↓
  Smart Review → Analyzes all tasks
                 Issues found: "Add error handling"
  User: [YES] add discovered task
  ↓ (Auto-loop)

Iteration 2:
  Smart Plan → Plans error handling task
  ... (continues loop)
```

### Example 3: User Breaks Loop Early

```
Iteration 1:
  Smart Plan → Creates 5 subtasks
  User: [YES]
  ↓
  Smart Execute → Task 1, 2, 3 complete
                  Task 4 encounters error
                  User: [BREAK LOOP]
  ↓ (Immediately route to Full Auto)

Full Auto → Shows: ⚠️ Loop Interrupted
Results: 3 completed, 1 error, 1 pending
Options: [Continue Loop] [Edit Tasks] [Exit]
```

---

## Technical Implementation

### State Management Across Loop

Each agent preserves state:

**Smart Plan:**
- Stores: Current iteration number, goal being planned
- Receives: Previous iteration's discovered tasks (if looping)
- Passes to Execute: List of subtasks with status=pending

**Smart Execute:**
- Stores: Execution log for each task, completion times
- Receives: Subtask list from Plan
- Passes to Review: Completed/failed task summaries

**Smart Review:**
- Stores: Analysis results, discovered issues
- Receives: Execution results from Execute
- Passes to Plan (if looping): Discovered tasks as new goal

### Observation Logging

All confirmations logged with loop iteration info:

```markdown
Observation (Loop Iteration 2):
  Type: planning
  Iteration: 2
  Goal: "Implement discovered error handling"
  Subtasks Created: 2
  User Confirmed: YES
  Timestamp: 2025-05-11T14:32:00Z
```

---

## Breaking the Loop Safely

### Methods to Exit Loop:

1. **[NO] on "Continue Loop?"** (recommended)
   ```
   Review asks: "Continue loop? [YES / NO]"
   User: [NO]
   → Routes to Full Auto
   → Loop terminates gracefully
   ```

2. **User Types "BREAK LOOP"**
   ```
   Any phase: Detects "BREAK LOOP" in user input
   → Immediately routes to Full Auto
   → Session status: "Interrupted"
   ```

3. **Error Condition**
   ```
   Any phase: Unexpected error
   → Logs error to observations
   → Routes to Full Auto for recovery
   ```

4. **Feature Complete**
   ```
   Review confirms: "No issues, feature complete"
   User: [YES - Done]
   → Routes to Full Auto
   → Loop terminates, results displayed
   ```

---

## Advantages of Tight Loop

✅ **User stays in context** - No switching back to Full Auto repeatedly
✅ **Faster iterations** - Discovered tasks automatically feed back to planning
✅ **Continuous improvement** - Loop refines until feature complete
✅ **Error recovery** - Can fix issues within loop before review
✅ **Explicit control** - User chooses when to continue or stop
✅ **Minimal latency** - No hub overhead between phases

---

## Migration from Hub-Spoke Model

**Previous Model (Hub-Spoke):**
```
Full Auto → Plan → (return to Full Auto)
Full Auto → Execute → (return to Full Auto)
Full Auto → Review → (return to Full Auto)
Full Auto → Plan again → ...
```

**New Model (Tight Loop):**
```
Full Auto → Plan → Execute → Review → Plan → Execute → Review → ... → Full Auto
```

**Key Difference:**
- **Old:** User manually clicks buttons for each phase (3+ clicks per iteration)
- **New:** Agents auto-transition within loop (1 click, multiple iterations)

---

## Configuration Checklist

- [ ] Smart Plan handoff 1 points to Smart Execute
- [ ] Smart Execute handoff 1 points to Smart Review
- [ ] Smart Review handoff 1 points back to Smart Plan
- [ ] All handoffs include "Keep looping..." language
- [ ] Emergency break handoffs to Full Auto configured
- [ ] Confirmation workflow enforces user decisions
- [ ] Iteration counter tracked in observations
- [ ] Discovery tasks fed as goal to next Plan phase
- [ ] Loop termination criteria clear (no more issues OR user decides)

