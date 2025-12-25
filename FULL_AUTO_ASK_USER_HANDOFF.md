# Full Auto Agent - ask_user Phase Transition Rules

**Updated:** Applied proper TaskSync phase transition behavior to Full Auto agent

---

## Critical ask_user Handoff Rules for Full Auto

### Rule 1: Single Decision Point Per Entry

**Location:** Full Auto Step 4 - "Present Phase Decision with ask_user"

```
ask_user("Choose next phase: [PLAN / EXECUTE / REVIEW]")
```

**Behavior:**
- ✅ Full Auto calls ask_user ONCE per session entry
- ✅ User must choose ONE of three options
- ✅ Only then does handoff happen
- ❌ No additional ask_user calls before routing

---

### Rule 2: ask_user STOPS on Handoff

**When user clicks a button [PLAN / EXECUTE / REVIEW]:**

```
BEFORE HANDOFF:
├─ ask_user: "Choose next phase: [PLAN / EXECUTE / REVIEW]"
├─ User: clicks [PLAN]
├─ Decision recorded: "plan_selected"
└─ Set: handoff_triggered = true

DURING HANDOFF:
├─ ✓ STOP calling ask_user in Full Auto
├─ ✓ Prepare routing to Smart Plan
├─ ✓ Include full context in handoff_prompt
└─ ✓ Send handoff with send: true

AFTER HANDOFF:
├─ Full Auto waits for Smart Plan to complete
├─ Smart Plan starts FRESH ask_user cycle
├─ No overlap between Full Auto and Smart Plan ask_user
└─ Full Auto's ask_user is PAUSED (not ended)
```

---

### Rule 3: ask_user RESTARTS on Loop Break

**When loop breaks (Smart Review → Full Auto):**

```
Smart Review completes loop:
├─ User says [NO - Done]
├─ Smart Review hands off to Full Auto with session_end signal
└─ ask_user STOPS in Smart Review

Full Auto receives signal:
├─ ✓ ask_user RESTARTS (fresh cycle, not resumption)
├─ Show session summary (tasks done, discovered issues)
├─ ask_user: "Start new session? [PLAN / EXECUTE / REVIEW]"
└─ Cycle repeats
```

---

### Rule 4: Each Phase Gets Fresh Context

**When Full Auto hands off to any agent:**

```
Full Auto hands off to Smart Plan:
├─ Prompt: "Start planning phase..."
├─ Context: task_id, title, complexity, priority
├─ NOT included: Full Auto's ask_user state
└─ Smart Plan loads: loadWorkflowContext() FRESH

Smart Plan receives handoff:
├─ Loads workflow context (FRESH, not inherited)
├─ Starts ask_user cycle (FRESH, not continued from Full Auto)
├─ Independent decision-making
└─ No reference to Full Auto's state
```

---

## Full Auto Handoff Implementation

### Handoff 1: Full Auto → Smart Plan

```yaml
label: 🎯 Plan Phase (with TaskSync Queue)
agent: Smart Plan
prompt: |
  "Start planning phase in TASKSYNC QUEUE MODE.
  Step 1: Call zen-tasks_000_workflow_context() to load current state.
  Step 2: Call getNextTask(limit=1) to find task to plan for.
  Step 3: Analyze goal and detect vagueness.
  Step 4: Create subtasks via addTask().
  Step 5: Ask user: 'Confirm subtasks? [YES/NO]'.
  When YES: Auto-handoff to Smart Execute (tight loop begins).
  Do NOT return to Full Auto hub until loop breaks."
send: true
```

**ask_user Behavior:**
- ✅ Full Auto: Calls ask_user once ("Choose phase?")
- ✅ Full Auto: User clicks [Plan]
- ✓ Full Auto: STOPS ask_user
- ✅ Smart Plan: Starts FRESH ask_user ("Vagueness? [YES/NO]")

---

### Handoff 2: Full Auto → Smart Execute

```yaml
label: ⚡ Execute Phase (with TaskSync Queue)
agent: Smart Execute
prompt: |
  "Start execution phase in TASKSYNC QUEUE MODE.
  Step 1: Call zen-tasks_000_workflow_context() to load current state.
  Step 2: Loop through pending tasks via getNextTask().
  Step 3: Execute each task and log observations.
  Step 4: For each task: '✅ Task complete? [YES/NO]'
  Step 5: When done: 'Ready for review? [YES/NO]'
  When YES: Auto-handoff to Smart Review (tight loop continues).
  Do NOT return to Full Auto hub until loop breaks."
send: true
```

**ask_user Behavior:**
- ✅ Full Auto: Calls ask_user once ("Choose phase?")
- ✅ Full Auto: User clicks [Execute]
- ✓ Full Auto: STOPS ask_user
- ✅ Smart Execute: Starts FRESH ask_user ("Task 1 complete? [YES/NO]")

---

### Handoff 3: Full Auto → Smart Review

```yaml
label: 🔍 Review Phase (with TaskSync Queue)
agent: Smart Review
prompt: |
  "Start review phase in TASKSYNC QUEUE MODE.
  Step 1: Call zen-tasks_000_workflow_context() to load current state.
  Step 2: Analyze all completed and failed tasks.
  Step 3: Perform root-cause analysis on failures.
  Step 4: Discover new tasks (with duplicate prevention).
  Step 5: Ask user: 'Add discovered tasks? [YES/NO/EDIT]'
  Step 6: Ask loop decision: 'Continue loop? [YES/NO]'
  If YES: Auto-handoff to Smart Plan (iteration N+1).
  If NO: Auto-handoff to Full Auto (session ends).
  Do NOT return to Full Auto until loop breaks."
send: true
```

**ask_user Behavior:**
- ✅ Full Auto: Calls ask_user once ("Choose phase?")
- ✅ Full Auto: User clicks [Review]
- ✓ Full Auto: STOPS ask_user
- ✅ Smart Review: Starts FRESH ask_user ("Analyze patterns? [YES/NO]")

---

## Loop Return: Smart Review → Full Auto

**When loop breaks (user says DONE):**

```
Smart Review Phase:
├─ Completes analysis
├─ Shows discovered tasks
├─ ask_user: "Continue loop? [YES/NO]"
├─ User: [NO - Done]
└─ Handoff to Full Auto with session_end signal

Full Auto Receives Loop Break:
├─ Smart Review's ask_user STOPS
├─ Full Auto's ask_user RESTARTS (FRESH)
├─ Show: Session summary
│  ├─ Iterations completed: N
│  ├─ Tasks done: [count]
│  ├─ Tasks failed: [count]
│  ├─ Issues discovered: [count]
│  └─ Improvements suggested: [list]
├─ ask_user: "Start new session? [PLAN / EXECUTE / REVIEW]"
├─ User clicks next action
└─ [Loop continues or exits]
```

**Key:** Full Auto's ask_user RESTARTS, not RESUMES. It's a fresh cycle.

---

## Implementation Validation

### ✅ Full Auto Step 4 Updated

```markdown
4. **Present Phase Decision with ask_user**
   - Call: `ask_user("Choose next phase: [PLAN / EXECUTE / REVIEW]")`
   - Wait for user to click one of the three buttons
   - This decision point is CRITICAL - Full Auto waits here
```

✓ Single decision point before routing ✓ Explicit ask_user call ✓ User choice required

### ✅ Full Auto Step 5 Handoff Behavior

```markdown
5. **Route to Spoke (when user clicks - ASK_USER STOPS HERE)**
   - ✓ STOP calling ask_user in Full Auto
   - Route to Smart Plan/Execute/Review with full task context
   - [Specific handoff instructions for each phase]
   - ✓ Full Auto's ask_user STOPS
   - ✓ [Next agent] starts FRESH ask_user cycle
```

✓ Explicit STOP instruction ✓ Three routing options ✓ Each has handoff prompt ✓ Next agent starts fresh

### ✅ Full Auto Step 6 Loop Return

```markdown
6. **Receive Loop Break Signal (when spoke returns after loop ends)**
   - Smart Review will hand off back to Full Auto with "session_end" signal
   - This means loop broke (user said DONE)
   - Full Auto's ask_user RESTARTS (fresh cycle)
   - Log: Session completed, update observations
   - Go to step 3 (show session summary)
```

✓ Receives signal from Smart Review ✓ ask_user RESTARTS ✓ Shows session summary ✓ Loops back to display

---

## Full Workflow: ask_user Lifecycle

```
╔═══════════════════════════════════════════════════════════════╗
║            FULL AUTO ↔ TIGHT LOOP INTERACTION                ║
╚═══════════════════════════════════════════════════════════════╝

USER STARTS SESSION:
├─ Full Auto initializes
├─ ask_user: "Choose phase: [PLAN / EXECUTE / REVIEW]"
│  └─ Full Auto WAITS here for user click
└─ User clicks [PLAN]

HANDOFF 1: FULL AUTO → SMART PLAN
├─ ✓ STOP ask_user in Full Auto
├─ Route to Smart Plan with context
└─ Smart Plan receives: FRESH ask_user cycle starts

TIGHT LOOP ITERATION 1:
├─ Smart Plan: ask_user ("Vagueness? [YES/NO]")
├─ Smart Execute: ask_user ("Task 1 done? [YES/NO]")
└─ Smart Review: ask_user ("Continue loop? [YES/NO]")
   └─ User: [YES]

ITERATION 1 COMPLETE → ITERATION 2:
├─ Smart Review hands off to Smart Plan (not Full Auto)
├─ Smart Plan: FRESH ask_user cycle (getNextTask() for new task)
├─ Smart Execute: FRESH ask_user cycle
└─ Smart Review: FRESH ask_user cycle
   └─ User: [NO - Done]

LOOP BREAKS:
├─ ✓ STOP ask_user in Smart Review
├─ Smart Review hands off to Full Auto with session_end
└─ Full Auto receives: RESTART ask_user (FRESH)

FULL AUTO SESSION SUMMARY:
├─ Show: Session results (iterations, tasks, discoveries)
├─ ask_user: "New session? [PLAN / EXECUTE / REVIEW]"
│  └─ Full Auto WAITS here for user click
└─ User clicks next action or exits
```

---

## Testing Validation

### Test Case 1: Full Auto → Plan Handoff

**Scenario:** User clicks "🎯 Plan Phase"

**Expected Behavior:**

1. Full Auto asks: "Choose next phase: [PLAN / EXECUTE / REVIEW]"
2. User clicks [PLAN]
3. **VERIFY:** Full Auto stops asking questions
4. **VERIFY:** Smart Plan starts fresh ("Analyze vagueness?")
5. **VERIFY:** No Full Auto ask_user during Plan phase

**Success Criteria:** ✅
- ask_user stops in Full Auto immediately after button click
- Smart Plan's first question is about vagueness (not inherited from Full Auto)
- No simultaneous ask_user from both agents

### Test Case 2: Loop Break Return

**Scenario:** Review decides loop breaks (user says DONE)

**Expected Behavior:**

1. Smart Review asks: "Continue loop? [YES/NO]"
2. User clicks [NO - Done]
3. **VERIFY:** Smart Review stops asking questions
4. **VERIFY:** Full Auto shows session summary
5. **VERIFY:** Full Auto asks for NEW session decision
6. **VERIFY:** ask_user is FRESH (not continuation of Smart Review's)

**Success Criteria:** ✅
- Smart Review's ask_user stops after "Continue loop?"
- Full Auto shows complete session results
- Full Auto's new ask_user is fresh (not continuation)
- User can choose new phase or exit

---

## Summary

✅ **Full Auto Updated with Proper ask_user Handoff Behavior:**

1. **Entry Point:** ask_user for phase selection
2. **Handoff:** ask_user STOPS when routing to agent
3. **Agent Receive:** Fresh ask_user cycle in receiving agent
4. **Loop Return:** ask_user RESTARTS (fresh) when loop breaks
5. **Exit Point:** ask_user for next session or exit

**All 4 Agents Now Follow Same Pattern:**
- Full Auto → Smart Plan: Handoff on button click, ask_user stops
- Smart Plan → Smart Execute: Handoff on "Ready? YES", ask_user stops
- Smart Execute → Smart Review: Handoff on "Review? YES", ask_user stops
- Smart Review → Plan/Full Auto: Handoff on "Continue? [YES/NO]", ask_user stops

**Result:** Seamless tight loop with proper ask_user isolation between phases.
