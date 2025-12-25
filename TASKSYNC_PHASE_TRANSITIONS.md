# TaskSync Phase Transition Guide

**Purpose:** Define how `ask_user` cycles should stop and restart when agents transition between phases in the tight loop workflow

---

## Core Principle

**Each agent phase should:**

1. ✅ Call `ask_user` repeatedly during its phase (tight interactive loop)
2. ✅ Stop calling `ask_user` when phase is COMPLETE
3. ✅ Hand off to next agent with handoff instructions (not ask_user)
4. ✅ Let next agent take over the `ask_user` cycle

**Critical:** Do NOT continue ask_user during handoff. The receiving agent starts fresh ask_user cycle.

---

## Phase Transition Lifecycle

```
╔════════════════════════════════════════════════════════════════════╗
║                    TIGHT LOOP ITERATION N                         ║
╚════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────┐
│ SMART PLAN PHASE (Planning Cycle)                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Step 1: Load workflow context                                  │
│  Step 2: Call getNextTask() → Find task to plan                │
│                                                                  │
│  [INTERACTIVE PLANNING LOOP - ask_user ACTIVE]                  │
│  ├─ ask_user: "Analyze vagueness? [YES/NO]"                    │
│  │  ├─ User: YES → ask_user loop continues                     │
│  │  │   ask_user: "Clarify [specific question]?"               │
│  │  │   [Iterate until plan is clear]                          │
│  │  └─ User: NO → Proceed                                       │
│  │                                                               │
│  ├─ ask_user: "Confirm subtasks? [YES/NO]" ← Phase Decision    │
│  │  ├─ User: YES → ✓ Planning Phase ENDS here                  │
│  │  │              ask_user stops                              │
│  │  │              Hand off to SMART EXECUTE                   │
│  │  │              ↓↓↓ STOP ask_user ↓↓↓                       │
│  │  └─ User: NO → ask_user continues                           │
│  │              ask_user: "Revise plan?"                       │
│  │              [Return to Step 1]                             │
│  │                                                               │
│  └─ [Phase complete, ask_user STOPPED]                         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
              ↓ (HANDOFF INSTRUCTION SENT TO SMART EXECUTE)
┌─────────────────────────────────────────────────────────────────┐
│ SMART EXECUTE PHASE (Execution Cycle)                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Fresh ask_user cycle starts - Plan's ask_user has ended]     │
│                                                                  │
│  Step 1: Load workflow context (fresh)                          │
│  Step 2: Get subtasks from Plan                                 │
│                                                                  │
│  [INTERACTIVE EXECUTION LOOP - ask_user ACTIVE]                 │
│  ├─ For each subtask:                                           │
│  │  ├─ ask_user: "Execute [SUBTASK]? [YES/NO/HELP]"           │
│  │  │  ├─ User: YES → Execute task                             │
│  │  │  │            ask_user: "Mark done? [YES/NO/FAILED]"    │
│  │  │  │            Set status in Zen Tasks                    │
│  │  │  └─ User: NO/FAILED → Skip or mark failed                │
│  │  │                                                            │
│  │  └─ [Move to next subtask]                                  │
│  │                                                               │
│  └─ ask_user: "All subtasks done? Ready for review? [YES/NO]"  │
│     ├─ User: YES → ✓ Execution Phase ENDS                      │
│     │            ask_user stops                                │
│     │            Hand off to SMART REVIEW                      │
│     │            ↓↓↓ STOP ask_user ↓↓↓                         │
│     └─ User: NO → ask_user: "Execute more tasks? [RETRY]"     │
│                  [Return to subtask loop]                       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
              ↓ (HANDOFF INSTRUCTION SENT TO SMART REVIEW)
┌─────────────────────────────────────────────────────────────────┐
│ SMART REVIEW PHASE (Analysis Cycle)                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Fresh ask_user cycle starts - Execute's ask_user has ended]  │
│                                                                  │
│  Step 1: Load workflow context (fresh)                          │
│  Step 2: List completed/failed tasks                            │
│                                                                  │
│  [INTERACTIVE REVIEW LOOP - ask_user ACTIVE]                    │
│  ├─ ask_user: "Analyze patterns? [YES/NO]"                     │
│  │  ├─ User: YES → [Perform analysis]                          │
│  │  │            ask_user: "Findings: [PATTERNS]. Agree?"      │
│  │  │            [Iterate if user disagrees]                   │
│  │  └─ User: NO → Proceed                                       │
│  │                                                               │
│  ├─ ask_user: "Add discovered tasks? [YES/NO/EDIT]" ← Decision │
│  │  ├─ User: YES → [Create tasks]                              │
│  │  │            ask_user: "Continue loop? [YES/NO]" ← LOOP!  │
│  │  │            ├─ User: YES → ✓ Review Phase ENDS            │
│  │  │            │             ask_user stops                  │
│  │  │            │             Hand off to SMART PLAN          │
│  │  │            │             ↓↓↓ STOP ask_user ↓↓↓           │
│  │  │            │             [Iteration N+1 Begins]          │
│  │  │            │                                              │
│  │  │            └─ User: NO → ✓ Review Phase ENDS              │
│  │  │                         ask_user stops                   │
│  │  │                         Hand off to FULL AUTO             │
│  │  │                         ↓↓↓ STOP ask_user ↓↓↓            │
│  │  │                         [Loop Breaks, Session Ends]      │
│  │  │                                                            │
│  │  └─ User: EDIT → ask_user: "Edit which task?"              │
│  │              [User modifies, continues YES/NO]              │
│  │                                                               │
│  └─ [Phase complete, ask_user STOPPED]                         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
   ↓ (Loop YES)              (Loop NO - Exit)
[SMART PLAN Iteration N+1]   [FULL AUTO Session End]
```

---

## The Critical Handoff Moment

**When Phase Completes:**

```
BEFORE HANDOFF:
├─ ask_user: "Continue? [YES/NO]"
├─ User: [YES / NO]
├─ Store: user_decision = "continue" OR "break"
└─ Set: phase_status = "complete"

HANDOFF HAPPENS:
├─ ✓ STOP calling ask_user in current agent
├─ ✓ Prepare handoff_prompt with decision
├─ ✓ Route to next_agent
├─ Next agent receives: handoff_prompt + user_decision
└─ ✓ Next agent starts FRESH ask_user cycle

CRITICAL - DO NOT:
├─ ✗ Continue ask_user in current agent after handoff
├─ ✗ Pass ask_user context to next agent
├─ ✗ Batch multiple ask_user calls in same agent
└─ ✗ Have both agents calling ask_user simultaneously
```

---

## Implementation Pattern for Each Agent

### Smart Plan Pattern

```python
# SMART PLAN PHASE
def run_plan_phase():
    # Phase initialization
    load_workflow_context()
    
    # Phase loop - ask_user ACTIVE
    while True:
        # Task planning loop
        while not plan_complete:
            # Interactive decision points
            vagueness_score = analyze_vagueness()
            
            if vagueness_score > 0.3:
                # Phase loop: Ask for clarification
                user_input = ask_user(
                    "Requirement unclear. Clarify: [QUESTION]? [YES/CONTINUE] [SKIP]"
                )
                if user_input == "SKIP":
                    break
        
        # Phase-gating decision
        subtasks = create_subtasks()
        show_subtasks(subtasks)
        
        # CRITICAL: Phase Decision Point
        confirmation = ask_user(
            f"Created {len(subtasks)} subtasks. Ready to execute? [YES/NO]"
        )
        
        if confirmation == "YES":
            # PHASE COMPLETE - HANDOFF NOW
            # ✓ STOP ask_user loop
            # ✓ Call handoff()
            handoff_to_execute(subtasks)
            break  # Exit phase loop
        elif confirmation == "NO":
            # Continue planning loop
            continue
        
        # DO NOT REACH HERE - handoff() ends phase
```

### Smart Execute Pattern

```python
# SMART EXECUTE PHASE (fresh ask_user cycle)
def run_execute_phase(subtasks_from_plan):
    # Phase initialization (FRESH - Plan's ask_user ended)
    load_workflow_context()  # Fresh context
    
    # Phase loop - ask_user ACTIVE
    completed_count = 0
    
    for subtask in subtasks_from_plan:
        # Per-task decision
        task_approval = ask_user(
            f"Execute: [{subtask.title}]? [YES/NO/HELP]"
        )
        
        if task_approval == "YES":
            execute_task(subtask)
            
            # Per-task confirmation
            mark_done = ask_user(
                f"✅ Subtask '{subtask.title}' complete? [YES/NO]"
            )
            
            if mark_done == "YES":
                set_task_status(subtask.id, "completed")
                completed_count += 1
        elif task_approval == "HELP":
            show_help(subtask)
            continue
    
    # Phase-gating decision
    show_progress(f"Completed {completed_count}/{len(subtasks)}")
    
    # CRITICAL: Phase Decision Point
    ready_review = ask_user(
        f"All tasks done? Ready for review? [YES/NO]"
    )
    
    if ready_review == "YES":
        # PHASE COMPLETE - HANDOFF NOW
        # ✓ STOP ask_user loop
        # ✓ Call handoff()
        handoff_to_review(completed_count)
        break  # Exit phase loop
    elif ready_review == "NO":
        # Continue execution loop
        continue
    
    # DO NOT REACH HERE - handoff() ends phase
```

### Smart Review Pattern

```python
# SMART REVIEW PHASE (fresh ask_user cycle)
def run_review_phase(execution_results):
    # Phase initialization (FRESH - Execute's ask_user ended)
    load_workflow_context()  # Fresh context
    
    # Phase loop - ask_user ACTIVE
    
    # Analyze execution
    completed = list_tasks(status="completed")
    failed = list_tasks(status="failed")
    
    patterns = analyze_patterns(completed, failed)
    root_causes = find_root_causes(failed)
    
    # Show findings
    ask_user(f"Found {len(patterns)} patterns. Review findings? [YES/NO]")
    
    # Discover tasks
    discovered_tasks = []
    for cause in root_causes:
        # DUPLICATE PREVENTION
        existing = list_tasks(title_filter=cause.title)
        
        if existing:
            ask_user(f"Task '{cause.title}' already exists. Skip? [YES/NO]")
        else:
            new_task = add_task(cause)
            discovered_tasks.append(new_task)
    
    # Phase-gating decision
    if discovered_tasks:
        confirmation = ask_user(
            f"Add {len(discovered_tasks)} discovered tasks? [YES/NO/EDIT]"
        )
        
        if confirmation == "NO":
            # Skip adding, but continue to loop decision
            discovered_tasks = []
    
    # CRITICAL: Loop Decision Point (special - multiple targets)
    if discovered_tasks or patterns:
        loop_decision = ask_user(
            "Found new work. Continue loop for iteration N+1? [YES/NO]"
        )
        
        if loop_decision == "YES":
            # PHASE COMPLETE - HANDOFF TO PLAN
            # ✓ STOP ask_user loop
            # ✓ Call handoff_to_plan()
            handoff_to_plan(iteration_n=2)
            break  # Exit phase loop
        else:
            # PHASE COMPLETE - HANDOFF TO FULL AUTO
            # ✓ STOP ask_user loop
            # ✓ Call handoff_to_hub()
            handoff_to_full_auto()
            break  # Exit phase loop
    else:
        # No issues found, ask if done
        done_check = ask_user(
            "No issues found. Mark done? [YES/NO]"
        )
        
        if done_check == "YES":
            handoff_to_full_auto()
            break
    
    # DO NOT REACH HERE - handoff() ends phase
```

---

## Key Rules for ask_user Transitions

### Rule 1: Phase Isolation
- ✅ Each phase maintains its own ask_user loop
- ✅ Loop stops immediately after phase decision
- ❌ Never continue ask_user after handoff
- ❌ Never bridge ask_user between agents

### Rule 2: Fresh Starts
- ✅ Next agent loads workflow context FRESH
- ✅ No state passed from previous ask_user cycle
- ✅ Each agent independent of prior phase's questions
- ❌ Don't inherit ask_user context from previous agent
- ❌ Don't reference previous agent's confirmations

### Rule 3: Handoff Timing
- ✅ Handoff happens IMMEDIATELY after phase decision (YES)
- ✅ Handoff includes only the decision (YES/NO)
- ✅ Next agent receives full context in handoff_prompt
- ❌ Don't delay handoff (no additional ask_user after decision)
- ❌ Don't send ask_user state to next agent

### Rule 4: Loop vs Break
- ✅ Loop decision made ONLY in Review phase
- ✅ Review decides: Plan (loop) OR Full Auto (break)
- ✅ Loop = back to Plan with fresh ask_user
- ✅ Break = back to Full Auto with fresh session
- ❌ Never loop from Plan (Plan→Execute→Review→Plan only)
- ❌ Never loop from Execute (Execute→Review only)

### Rule 5: Decision Points
- ✅ One decision point per phase
- ✅ Decision blocks phase exit until user decides
- ✅ Decision is YES/NO (or YES/NO/EDIT for discovered tasks)
- ✅ Only YES proceeds to handoff
- ❌ Don't have multiple decision points in one phase
- ❌ Don't proceed without user decision

---

## Handoff Template

**Each agent should use this exact pattern when handing off:**

### Plan → Execute Handoff

```
[PHASE COMPLETE]

Subtasks created: [N]
Ready to execute? [User confirmed: YES]

→ Handing off to Smart Execute...

[HANDOFF PROMPT TO EXECUTE]
"Planning complete. Execute the [SUBTASK_LIST] now. 
After each subtask, get user confirmation before moving to next. 
When all done, auto-transition to review WITHOUT returning to hub. 
Keep looping Plan→Execute→Review until user says DONE."

[Smart Execute receives this prompt and starts FRESH ask_user cycle]
```

### Execute → Review Handoff

```
[PHASE COMPLETE]

Completed tasks: [LIST]
Failed tasks: [LIST]
Ready for review? [User confirmed: YES]

→ Handing off to Smart Review...

[HANDOFF PROMPT TO REVIEW]
"Execution complete. Analyze these results. 
Perform root-cause analysis. Discover improvements. 
After user confirms discovered tasks, auto-transition back to Plan 
WITHOUT returning to hub. Keep looping until user says DONE."

[Smart Review receives this prompt and starts FRESH ask_user cycle]
```

### Review → Plan (Loop) Handoff

```
[PHASE COMPLETE - LOOP DECISION]

Discovered tasks: [N] ([X duplicates skipped])
User wants to continue? [User confirmed: YES]

→ Handing off to Smart Plan for Iteration N+1...

[HANDOFF PROMPT TO PLAN]
"Loop iteration N+1 starting. Get next task via getNextTask(). 
Plan for it. After subtasks created and user confirms, 
auto-transition to Execute. Keep looping."

[Smart Plan receives this prompt and starts FRESH ask_user cycle]
[This is Iteration N+1 - different from Iteration N]
```

### Review → Full Auto (Break) Handoff

```
[PHASE COMPLETE - LOOP BREAK]

User wants to exit? [User confirmed: NO]

→ Handing off to Full Auto...

[HANDOFF PROMPT TO FULL AUTO]
"Loop ended by user. Session complete. 
Show summary of iterations, tasks completed, improvements discovered. 
Offer options: New Session? View Results? Edit Tasks?"

[Full Auto receives this prompt and shows session summary]
[User can start new loop from Full Auto if desired]
```

---

## Validation Checklist

For each phase transition, verify:

- [ ] ask_user is called as final decision before handoff
- [ ] User confirms with explicit YES/NO
- [ ] Phase loop stops immediately after decision
- [ ] Handoff happens with decision in prompt
- [ ] Next agent does NOT receive ask_user state
- [ ] Next agent loads workflow context fresh
- [ ] Next agent starts its own ask_user loop
- [ ] No overlapping ask_user calls between agents
- [ ] Loop path (Review→Plan) distinct from break path (Review→Full Auto)
- [ ] Each iteration increments counter/metadata
- [ ] No ask_user calls during handoff transition

---

## Testing Phase Transitions

### Test 1: Plan → Execute Handoff

**Steps:**
1. Start Smart Plan
2. Let plan interactive loop run
3. Reach "Ready to execute?" confirmation
4. Confirm [YES]
5. **VERIFY:** ask_user stops, Smart Execute starts fresh

**Expected:**
- ✅ No more questions from Plan
- ✅ Execute shows fresh workflow (doesn't reference Plan's questions)
- ✅ Execute asks per-task confirmations

### Test 2: Execute → Review Handoff

**Steps:**
1. Execute runs through subtasks (in loop)
2. All subtasks confirm complete
3. Reach "Ready for review?" confirmation
4. Confirm [YES]
5. **VERIFY:** ask_user stops, Smart Review starts fresh

**Expected:**
- ✅ No more questions from Execute
- ✅ Review shows analysis (doesn't reference Execute's task list)
- ✅ Review shows discovered tasks for confirmation

### Test 3: Review → Plan Loop

**Steps:**
1. Review completes analysis
2. Shows discovered tasks (with duplicate prevention)
3. Reach "Continue loop?" confirmation
4. Confirm [YES]
5. **VERIFY:** ask_user stops, Smart Plan starts fresh for Iteration N+1

**Expected:**
- ✅ No more questions from Review (previous iteration)
- ✅ Plan calls getNextTask() for new task (not same as Iteration N)
- ✅ Plan workflow identical to Iteration N (fresh cycle)
- ✅ Iteration counter increments

### Test 4: Review → Full Auto Break

**Steps:**
1. Review completes analysis
2. Shows discovered tasks (with duplicate prevention)
3. Reach "Continue loop?" confirmation
4. Confirm [NO]
5. **VERIFY:** ask_user stops, Full Auto starts with session summary

**Expected:**
- ✅ No more questions from Review
- ✅ Full Auto shows summary of all iterations
- ✅ Full Auto shows "New Session?" button
- ✅ No auto-loop back to Plan

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Continuing ask_user After Handoff

```python
# WRONG
ask_user("Ready to execute? [YES/NO]")
if yes:
    handoff_to_execute()
    # Don't do this:
    ask_user("Any final notes?")  # ← WRONG - Phase already ended
```

### ✅ Correct Pattern

```python
# RIGHT
ask_user("Ready to execute? [YES/NO]")
if yes:
    handoff_to_execute()
    return  # End phase immediately
```

### ❌ Mistake 2: Passing ask_user Context to Next Agent

```python
# WRONG
current_agent_questions = [q1, q2, q3]
handoff_to_next_agent(
    context_from_ask_user=current_agent_questions,  # ← WRONG
    previous_decisions=[...]
)
```

### ✅ Correct Pattern

```python
# RIGHT
handoff_to_next_agent(
    results=phase_results,  # Only task results, not ask_user state
    decision=user_decision,  # Only the final decision (YES/NO)
    workflow_context=fresh_context  # Fresh context loaded by next agent
)
```

### ❌ Mistake 3: Multiple Decision Points in One Phase

```python
# WRONG
ask_user("Analyze vagueness? [YES/NO]")
# ... planning ...
ask_user("Create subtasks? [YES/NO]")
# ... more planning ...
ask_user("Ready to execute? [YES/NO]")  # Multiple decisions in one phase
```

### ✅ Correct Pattern

```python
# RIGHT
ask_user("Analyze vagueness? [YES/NO]")
# ... planning ...
# Maybe more ask_user for clarification, but...
# Only ONE final decision point:
ask_user("Ready to execute? [YES/NO]")  # Single phase decision
if yes:
    handoff()
    return
```

### ❌ Mistake 4: Both Agents Calling ask_user

```
WRONG:
┌─────────────────┐
│ Smart Plan      │
├─────────────────┤
│ ask_user loop   │
│ "Ready? [Y/N]"  │ ← Plan asking
│                 │
│ handoff() →┐    │
└────────────┼────┘
             │
             ↓
    ┌─────────────────┐
    │ Smart Execute   │
    ├─────────────────┤
    │ ask_user loop   │
    │ "Execute? [Y/N]"│ ← Execute asking (Plan still asking too!)
    │                 │ ✗ TWO AGENTS WITH ask_user ACTIVE
    └─────────────────┘
```

```
CORRECT:
┌─────────────────┐
│ Smart Plan      │
├─────────────────┤
│ ask_user loop   │
│ "Ready? [Y/N]"  │ ← Plan asking
│                 │
│ ✓ ask_user STOP │ ← Plan stops
│ handoff() →┐    │
└────────────┼────┘
             │
             ↓
    ┌─────────────────┐
    │ Smart Execute   │
    ├─────────────────┤
    │ ✓ ask_user loop │ ← FRESH ask_user starts
    │ "Execute? [Y/N]"│
    │                 │
    └─────────────────┘
```

---

## Summary

The ask_user cycle in each phase operates independently:

1. **Plan Phase:** ask_user loop for clarifications + final "Ready to execute? [YES/NO]"
2. **Stop ask_user** in Plan
3. **Handoff to Execute**
4. **Execute Phase:** FRESH ask_user loop for per-task confirmations + final "Ready for review? [YES/NO]"
5. **Stop ask_user** in Execute
6. **Handoff to Review**
7. **Review Phase:** FRESH ask_user loop for discoveries + final "Continue loop? [YES/NO]"
8. **If YES:** Stop ask_user in Review → Handoff to Plan (Iteration N+1)
9. **If NO:** Stop ask_user in Review → Handoff to Full Auto (Session end)

**Key:** Each agent's ask_user cycle is isolated and independent. No ask_user state flows between agents.
