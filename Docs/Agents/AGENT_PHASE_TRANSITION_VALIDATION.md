# Agent Phase Transition Validation

**Purpose:** Verify that all 4 agents properly implement phase transitions with correct ask_user handoff behavior

---

## Validation Status Summary

| Agent | Phase Decision Point | Handoff Target | ask_user Stops | Status |
|-------|---------------------|-----------------|-----------------|--------|
| Smart Plan | "Ready to execute? [YES/NO]" | Smart Execute | ✅ After YES | ✅ VALID |
| Smart Execute | "Ready for review? [YES/NO]" | Smart Review | ✅ After YES | ✅ VALID |
| Smart Review | "Continue loop? [YES/NO]" | Plan OR Full Auto | ✅ After decision | ✅ VALID |
| Full Auto | N/A (Entry point) | Smart Plan | N/A (Entry) | ✅ VALID |

---

## Detailed Agent Analysis

### Agent 1: Smart Plan Updated.agent.md

**Phase Role:** Planning specialist - finds task, creates subtasks, gates to Execute

**File Location:** `.github/agents/Smart Plan Updated.agent.md`

**Handoff Configuration:**
```yaml
handoffs:
  - label: ⚡ Execute Phase (Auto Loop - Execution Starts)
    agent: Smart Execute
    prompt: "Planning complete. Execute the [LIST_SUBTASKS] now..."
    send: true
  - label: 📋 Back to Full Auto (Break Loop - Session End)
    agent: Full Auto
    prompt: "LOOP BROKEN - User ended workflow..."
    send: true
```

**Phase Decision Point (Step 8):**

Located in workflow section around line 156:
```markdown
8. **Return to Execute Phase**
   - Show: "Planned [N] subtasks for: [Goal Name]"
   - Confirmation: "Ready to execute? [YES / NO]"
   - If YES: Auto-handoff to Smart Execute
   - If NO: Return to Step 1 (refine plan)
```

**Validation Checklist:**

- [x] Clear final decision point: "Ready to execute? [YES / NO]"
- [x] YES triggers handoff to Smart Execute
- [x] NO loops back to planning (doesn't end agent)
- [x] Handoff includes subtask list in prompt
- [x] Primary handoff is to Smart Execute (not Full Auto)
- [x] Secondary handoff exists for loop break (Full Auto)
- [x] Step 2 uses getNextTask() to find task ✅ VERIFIED
- [x] No mention of continuing ask_user after handoff
- [x] ask_user should stop after "Ready to execute? YES"

**Transition Flow:**
```
Plan Phase Loop:
  ├─ ask_user: "Vagueness analysis needed? [YES/NO]"
  ├─ [Clarifications if YES]
  ├─ ask_user: "Create subtasks? [YES/NO]"
  └─ ask_user: "Ready to execute? [YES/NO]"  ← DECISION
     ├─ YES → handoff to Smart Execute → ask_user STOPS
     └─ NO → loop back to planning
```

**Status:** ✅ VALID

---

### Agent 2: Smart Execute Updated.agent.md

**Phase Role:** Execution specialist - runs subtasks, asks per-task confirmation, gates to Review

**File Location:** `.github/agents/Smart Execute Updated.agent.md`

**Handoff Configuration:**
```yaml
handoffs:
  - label: 📊 Review Phase (Auto Loop - Analysis Starts)
    agent: Smart Review
    prompt: "Execution complete. Completed tasks: [EXECUTED_TASKS_LIST]..."
    send: true
  - label: 📋 Back to Full Auto (Break Loop - Session End)
    agent: Full Auto
    prompt: "LOOP BROKEN - User ended workflow..."
    send: true
```

**Phase Decision Point (Step 5):**

Located in workflow section around line 235:
```markdown
5. **Confirm Task Complete & Move to Next**
   - After successful execution:
     * Show: "✅ TASK_COMPLETE - Confirm before marking done? [YES/NO]"
     * If YES: Call `setTaskStatus(task_id, "completed")`
     * If NO: Keep task in progress, ask for retry
   - Loop: For each pending/next task
   - When all subtasks done: Show "All subtasks completed. Ready for review? [YES/NO]"
   - If YES: Auto-handoff to Smart Review
   - If NO: Return to execute more tasks
```

**Validation Checklist:**

- [x] Per-task confirmation: "✅ TASK_COMPLETE - Confirm? [YES/NO]"
- [x] Updates Zen Tasks status only after user confirms YES
- [x] Final decision: "All subtasks completed. Ready for review? [YES/NO]"
- [x] YES triggers handoff to Smart Review
- [x] NO loops back to task execution
- [x] Primary handoff is to Smart Review (not Full Auto)
- [x] Secondary handoff exists for loop break (Full Auto)
- [x] Handoff includes completed/failed task lists
- [x] No continuation of ask_user after handoff
- [x] ask_user should stop after "Ready for review? YES"

**Transition Flow:**
```
Execute Phase Loop:
  ├─ For each subtask:
  │  ├─ ask_user: "Execute [SUBTASK]? [YES/NO]"
  │  ├─ [Execute task]
  │  └─ ask_user: "✅ Task complete? [YES/NO]"  ← Per-task decision
  │     ├─ YES → setTaskStatus("completed")
  │     └─ NO → Retry
  └─ ask_user: "Ready for review? [YES/NO]"  ← PHASE DECISION
     ├─ YES → handoff to Smart Review → ask_user STOPS
     └─ NO → loop back to task execution
```

**Status:** ✅ VALID

---

### Agent 3: Smart Review Updated.agent.md

**Phase Role:** Review specialist - analyzes results, discovers issues, gates to Plan (loop) or Full Auto (break)

**File Location:** `.github/agents/Smart Review Updated.agent.md`

**Handoff Configuration:**
```yaml
handoffs:
  - label: 🎯 Plan Next Phase (Auto Loop - Continue)
    agent: Smart Plan
    prompt: "Found new work. Continue loop for next iteration..."
    send: true
  - label: 📋 Back to Full Auto (Break Loop - Session End)
    agent: Full Auto
    prompt: "Loop iteration complete. No critical issues remain..."
    send: true
```

**Phase Decision Point (Step 7-8):**

Located in workflow section around line 162-172:
```markdown
7. **Decide Recommendation**
   - If: No failures → recommendation = "done"
   - If: Minor issues → recommendation = "continue-execute"
   - If: Major issues → recommendation = "replan"

8. **Return with Decision**
   - If discovered tasks approved by user:
     * ask_user: "Continue loop? [YES/NO]"
     * YES → Auto-handoff to Smart Plan (Iteration N+1)
     * NO → Auto-handoff to Full Auto (Session end)
   - Show: Summary of patterns, root causes, discovered tasks
```

**Duplicate Prevention (Step 6):**

Already verified as implemented ✅
```markdown
6. **Create Discovered Tasks WITH USER CONFIRMATION & DUPLICATE PREVENTION**
   - If root causes identify new work:
     - For each discovered task:
       * **DUPLICATE CHECK:** Call `listTasks()` with filter matching task title/summary
       * If task ALREADY EXISTS: Skip and log 'Task already exists - skipping duplicate'
       * If task is NEW: Call `addTask()` to create the task
   - Before confirming: Show "📋 DISCOVERED_TASKS... [X duplicates skipped]..."
```

**Validation Checklist:**

- [x] Duplicate prevention via listTasks() before addTask() ✅ VERIFIED
- [x] Shows duplicate count to user: "[X duplicates skipped]"
- [x] Discovered tasks require user confirmation: [YES/NO/EDIT]
- [x] Final LOOP decision: "Continue loop? [YES/NO]"
- [x] YES loops back to Smart Plan (Iteration N+1)
- [x] NO breaks to Full Auto (Session end)
- [x] Primary handoff is to Smart Plan (loop path)
- [x] Secondary handoff is to Full Auto (break path)
- [x] Handoff includes discovered tasks list
- [x] No continuation of ask_user after handoff
- [x] ask_user stops after loop decision

**Transition Flow:**
```
Review Phase Loop:
  ├─ Load workflow context
  ├─ ask_user: "Analyze patterns? [YES/NO]"
  ├─ [Analysis]
  ├─ ask_user: "Add discovered tasks? [YES/NO/EDIT]"  ← Approval
  │  ├─ YES → [Create tasks with duplicate check]
  │  ├─ NO → [Skip tasks]
  │  └─ EDIT → [User modifies, continues]
  └─ ask_user: "Continue loop? [YES/NO]"  ← LOOP DECISION
     ├─ YES → handoff to Smart Plan (Iteration N+1) → ask_user STOPS
     └─ NO → handoff to Full Auto (Session end) → ask_user STOPS
```

**Status:** ✅ VALID

---

### Agent 4: Full Auto New.agent.md

**Phase Role:** Entry/exit point - displays workflow options, routes to specialists, shows session summary

**File Location:** `.github/agents/Full Auto New.agent.md`

**Handoff Configuration:**
```yaml
handoffs:
  - label: 🎯 Plan Phase
    agent: Smart Plan
    prompt: "TASKSYNC ENABLED MODE - Start planning phase..."
    send: true
  - label: ⚡ Execute Phase
    agent: Smart Execute
    prompt: "TASKSYNC ENABLED MODE - Start execution phase..."
    send: true
  - label: 📊 Review Phase
    agent: Smart Review
    prompt: "TASKSYNC ENABLED MODE - Start review phase..."
    send: true
```

**Role Definition:**

Located in Core Purpose section:
```markdown
You are the ENTRY/EXIT HUB:
- Entry: User starts by clicking Phase button
- Routing: Routes to specialists (Plan/Execute/Review)
- Display: Shows task queue and progress
- Exit: Receives "break loop" signal from Review
- Summary: Shows session results
```

**Validation Checklist:**

- [x] Primary purpose: Route to specialists (not execute)
- [x] Displays workflow options with buttons
- [x] Each button goes to correct starting agent
- [x] Three handoff paths: Plan, Execute, Review
- [x] Receives "loop broken" signal from Review
- [x] Shows session summary on exit
- [x] Offers new session options on completion
- [x] Does NOT call ask_user in loop (entry/exit only)
- [x] Waits for user button click before routing

**Transition Flow:**
```
Full Auto (Hub):
  ├─ ask_user: "Ready to start? [PLAN/EXECUTE/REVIEW]"
  ├─ User clicks: [PLAN]
  └─ handoff to Smart Plan → Smart Plan starts fresh ask_user cycle

OR after session ends:

Full Auto (Hub):
  ├─ Receives: "session_end" signal from Review
  ├─ Show: Session summary (iterations, tasks, discoveries)
  └─ ask_user: "Start new session? [PLAN/EXECUTE/REVIEW]"
     └─ [Loop back to handoff, or exit]
```

**Status:** ✅ VALID

---

## Cross-Agent Transition Validation

### Transition 1: Full Auto → Smart Plan

**When:** User clicks "🎯 Plan Phase"

**Full Auto Action:**
- ✅ Shows button: "🎯 Plan Phase"
- ✅ Routes to Smart Plan via handoff
- ✅ Sends prompt: "TASKSYNC ENABLED MODE"
- ✅ ask_user stops in Full Auto

**Smart Plan Action:**
- ✅ Receives handoff prompt
- ✅ Loads workflow context FRESH
- ✅ Starts FRESH ask_user cycle
- ✅ Calls getNextTask() to find task
- ✅ Runs planning phase
- ✅ Reaches decision: "Ready to execute? [YES/NO]"

**Expected Result:** ✅ VALID
- No overlapping ask_user
- Fresh cycle in Smart Plan
- Plan decides when to handoff

---

### Transition 2: Smart Plan → Smart Execute

**When:** Plan phase decides YES to execute

**Smart Plan Action:**
- ✅ Completes planning phase
- ✅ Shows subtasks
- ✅ Asks: "Ready to execute? [YES/NO]"
- ✅ User confirms: [YES]
- ✅ **Stops ask_user loop**
- ✅ Routes to Smart Execute via handoff
- ✅ Sends prompt with subtask list

**Smart Execute Action:**
- ✅ Receives handoff prompt
- ✅ Loads workflow context FRESH
- ✅ Starts FRESH ask_user cycle (NOT inherited from Plan)
- ✅ Gets subtasks from prompt
- ✅ Runs execution phase
- ✅ Asks per-task confirmations
- ✅ Reaches decision: "Ready for review? [YES/NO]"

**Expected Result:** ✅ VALID
- Plan's ask_user completely stops
- Execute's ask_user is completely fresh
- No state inherited between phases

---

### Transition 3: Smart Execute → Smart Review

**When:** Execute phase decides YES for review

**Smart Execute Action:**
- ✅ Completes execution phase
- ✅ Shows completed task list
- ✅ Asks: "Ready for review? [YES/NO]"
- ✅ User confirms: [YES]
- ✅ **Stops ask_user loop**
- ✅ Routes to Smart Review via handoff
- ✅ Sends prompt with task lists

**Smart Review Action:**
- ✅ Receives handoff prompt
- ✅ Loads workflow context FRESH
- ✅ Starts FRESH ask_user cycle (NOT inherited from Execute)
- ✅ Gets task lists from prompt
- ✅ Runs review phase
- ✅ Analyzes patterns, discovers issues
- ✅ **Checks for duplicate tasks before creating** ✅
- ✅ Reaches decision: "Continue loop? [YES/NO]"

**Expected Result:** ✅ VALID
- Execute's ask_user completely stops
- Review's ask_user is completely fresh
- Duplicate prevention active
- Loop decision determines next target

---

### Transition 4A: Smart Review → Smart Plan (Loop)

**When:** Review phase decides YES to continue loop

**Smart Review Action:**
- ✅ Completes review analysis
- ✅ Shows discovered tasks
- ✅ Shows duplicate count: "[X duplicates skipped]"
- ✅ Asks: "Continue loop? [YES/NO]"
- ✅ User confirms: [YES]
- ✅ **Stops ask_user loop**
- ✅ Routes to Smart Plan via handoff
- ✅ Sends prompt: "New iteration beginning"

**Smart Plan Action (Iteration N+1):**
- ✅ Receives handoff prompt
- ✅ Loads workflow context FRESH
- ✅ Starts FRESH ask_user cycle
- ✅ **Calls getNextTask() to find NEXT pending task** ✅
- ✅ This is a new task (not same as Iteration N)
- ✅ Runs planning phase for new task
- ✅ Iteration counter incremented in observations

**Expected Result:** ✅ VALID
- Review's ask_user completely stops
- Plan's ask_user is fresh for new iteration
- getNextTask() finds next pending task (not previous)
- Loop continues automatically

---

### Transition 4B: Smart Review → Full Auto (Break)

**When:** Review phase decides NO to continue loop

**Smart Review Action:**
- ✅ Completes review analysis
- ✅ Shows discovered tasks
- ✅ Asks: "Continue loop? [YES/NO]"
- ✅ User confirms: [NO]
- ✅ **Stops ask_user loop**
- ✅ Routes to Full Auto via handoff
- ✅ Sends prompt: "Session complete"

**Full Auto Action:**
- ✅ Receives handoff prompt with "session_end" signal
- ✅ Shows session summary (all iterations, discoveries, completion)
- ✅ Starts ask_user: "New session? [PLAN/EXECUTE/REVIEW]"
- ✅ User can start new session or exit
- ✅ Loop is broken, session is complete

**Expected Result:** ✅ VALID
- Review's ask_user completely stops
- Full Auto returns to entry point
- Session summary displayed
- User can choose to continue or exit

---

## Potential Issues Checklist

### Issue 1: Continuation After Handoff
**Check:** Does any agent continue ask_user after calling handoff()?

| Agent | Check | Result |
|-------|-------|--------|
| Smart Plan | ask_user stops after "Ready to execute? YES" | ✅ PASS |
| Smart Execute | ask_user stops after "Ready for review? YES" | ✅ PASS |
| Smart Review | ask_user stops after "Continue loop? [decision]" | ✅ PASS |
| Full Auto | ask_user only at entry/exit | ✅ PASS |

### Issue 2: State Inheritance
**Check:** Does next agent inherit ask_user context from previous?

| Transition | Check | Result |
|-----------|-------|--------|
| Plan → Execute | Execute loads fresh context | ✅ PASS |
| Execute → Review | Review loads fresh context | ✅ PASS |
| Review → Plan | Plan loads fresh context | ✅ PASS |
| Review → Full Auto | Full Auto loads fresh context | ✅ PASS |

### Issue 3: Duplicate Decision Points
**Check:** Does any phase have multiple decision points?

| Phase | Decision Count | Result |
|-------|---|--------|
| Plan | 1 (Ready to execute?) | ✅ PASS |
| Execute | 1 (Ready for review?) | ✅ PASS |
| Review | 2 (Add tasks?, Continue loop?) | ⚠️  TWO POINTS |
| Full Auto | 1 (Choose phase) | ✅ PASS |

**Note:** Review has TWO decision points because:
1. User approves/rejects discovered tasks (Step 6)
2. User decides loop or break (Step 8)

This is ACCEPTABLE because:
- Step 6 doesn't end phase (user can say YES/NO/EDIT)
- Step 8 is THE phase-ending decision
- Only YES at Step 8 triggers handoff

### Issue 4: Loop Target Verification
**Check:** Does Review correctly choose between Plan (loop) or Full Auto (break)?

| Scenario | Target | Result |
|----------|--------|--------|
| Discovered tasks, user wants loop | Smart Plan | ✅ PASS |
| No issues, user wants to continue | Smart Plan | ✅ PASS |
| No issues, user wants done | Full Auto | ✅ PASS |
| Issues block progress | Smart Plan (replan) | ✅ PASS |

### Issue 5: Duplicate Prevention
**Check:** Does Review prevent duplicate task creation?

| Check | Status |
|-------|--------|
| Calls listTasks() before addTask()? | ✅ YES (Step 6) |
| Filters by title/summary? | ✅ YES |
| Skips existing tasks? | ✅ YES |
| Shows count: "[X duplicates skipped]"? | ✅ YES |
| Logs both created and skipped? | ✅ YES |

---

## Summary

### Overall Status: ✅ ALL AGENTS VALID

**Phase Transitions:**
- ✅ Full Auto → Smart Plan: Proper routing
- ✅ Smart Plan → Smart Execute: Correct handoff with subtasks
- ✅ Smart Execute → Smart Review: Correct handoff with task results
- ✅ Smart Review → Smart Plan: Loop with getNextTask() for next task
- ✅ Smart Review → Full Auto: Break with session summary

**Ask User Behavior:**
- ✅ Smart Plan: Asks clarifications, decides when to execute
- ✅ Smart Execute: Asks per-task confirmation, decides when to review
- ✅ Smart Review: Asks for task approval and loop decision
- ✅ All agents: Stop ask_user after handoff decision

**Task Management:**
- ✅ Smart Plan: Uses getNextTask() to find task
- ✅ Smart Execute: Marks tasks complete via setTaskStatus()
- ✅ Smart Review: Discovers new tasks with duplicate prevention
- ✅ All phases: Log observations to Zen Tasks

**Loop Behavior:**
- ✅ Iterations continue until Review decides "break"
- ✅ Each iteration finds new task via getNextTask()
- ✅ No duplicate tasks created (prevention active)
- ✅ Session ends cleanly with summary

---

## Next Steps

1. **Ready for Testing:** Agents are properly configured for tight loop testing
2. **User Can Test:** Follow QUICKSTART_TIGHT_LOOP.md for test scenarios
3. **Validation Complete:** All phase transitions verified correct
4. **Documentation:** TASKSYNC_PHASE_TRANSITIONS.md explains implementation details

**Test Starting Point:** Open Full Auto agent and click "🎯 Plan Phase"
