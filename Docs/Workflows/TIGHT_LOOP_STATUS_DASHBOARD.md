# Tight Loop Workflow - Status Dashboard

**Purpose:** Monitor and track tight loop iterations in real-time

---

## Session Dashboard Template

### Current Session Status

```text
╔════════════════════════════════════════════════════════════════════╗
║                    TIGHT LOOP SESSION STATUS                      ║
╚════════════════════════════════════════════════════════════════════╝

📊 SESSION SUMMARY
├─ Session ID: [TIMESTAMP]
├─ Start Time: [HH:MM:SS]
├─ Current Iteration: N
├─ Status: [ACTIVE | PAUSED | COMPLETE]
└─ Total Duration: [MM:SS]

🎯 ITERATION PROGRESS

Iteration 1 (COMPLETE)
├─ Plan: ✓ 3 subtasks created
├─ Execute: ✓ 3/3 subtasks done
├─ Review: ✓ 2 improvements found
└─ Discovered: "Rate limiting", "Error logging"

Iteration 2 (IN PROGRESS)
├─ Plan: ○ Analyzing task...
├─ Execute: [NOT STARTED]
├─ Review: [NOT STARTED]
└─ Discovered: [PENDING]

Iteration 3 (QUEUED)
├─ Plan: [PENDING]
├─ Execute: [PENDING]
├─ Review: [PENDING]
└─ Discovered: [TBD]

📈 AGGREGATED METRICS
├─ Total Tasks Started: 7
├─ Tasks Completed: 6
├─ Tasks Failed: 0
├─ Tasks In Progress: 1
├─ Tasks Pending: 2
├─ Improvements Discovered: 4
├─ Duplicates Prevented: 2
└─ Total Time Spent: [MM:SS]

⏱️ PHASE TIMING
├─ Plan Phase (Avg): [SS]
├─ Execute Phase (Avg): [SS]
├─ Review Phase (Avg): [SS]
└─ Loop Cycle (Avg): [SS]

🔄 CURRENT PHASE
├─ Active: Smart Plan
├─ Step: 3 of 9 (Analyze Vagueness)
├─ Time in Phase: [SS]
├─ Next Decision: Ready to execute? [AWAITING USER]
└─ Estimated Time: [SS remaining]

📋 USER INTERACTION LOG
├─ [HH:MM:SS] Clicked: 🎯 Plan Phase
├─ [HH:MM:SS] Confirmed: Vagueness score = 0.6
├─ [HH:MM:SS] Answered: Clarifying question about scope
├─ [HH:MM:SS] Confirmed: Ready to execute? [YES]
├─ [HH:MM:SS] Confirmed: OAuth setup done? [YES]
├─ [HH:MM:SS] Confirmed: JWT management done? [YES]
├─ [HH:MM:SS] Confirmed: Ready for review? [YES]
├─ [HH:MM:SS] Confirmed: Add improvements? [YES]
├─ [HH:MM:SS] Confirmed: Continue loop? [YES]
└─ [HH:MM:SS] WAITING FOR: Plan phase confirmation...

🎯 NEXT ACTIONS
├─ Immediate: User must confirm plan ready for execution
├─ Decision Point: "Ready to execute? [YES/NO]"
├─ If YES: Auto-handoff to Smart Execute
├─ If NO: Return to planning (refine subtasks)
└─ Estimated wait: < 1 minute
```

---

## Iteration Timeline

### Session Overview

```text
Session Start
     │
     ├─ 00:00-00:30 ITERATION 1 (Plan Phase)
     │  ├─ Load context: 2s
     │  ├─ Call getNextTask(): 1s
     │  ├─ Analyze vagueness: 5s
     │  ├─ Ask clarifications: 12s (user interaction)
     │  ├─ Create subtasks: 5s
     │  └─ Confirm plan: 5s (user: YES)
     │
     ├─ 00:30-01:15 ITERATION 1 (Execute Phase)
     │  ├─ Load context: 2s
     │  ├─ Get subtasks: 1s
     │  ├─ Execute Task 1: 15s
     │  ├─ Confirm Task 1: 3s (user: YES)
     │  ├─ Execute Task 2: 12s
     │  ├─ Confirm Task 2: 3s (user: YES)
     │  ├─ Execute Task 3: 12s
     │  ├─ Confirm Task 3: 3s (user: YES)
     │  └─ Confirm review ready: 7s (user: YES)
     │
     ├─ 01:15-02:00 ITERATION 1 (Review Phase)
     │  ├─ Load context: 2s
     │  ├─ List tasks: 2s
     │  ├─ Analyze patterns: 15s
     │  ├─ Root-cause analysis: 12s
     │  ├─ Discover improvements: 8s
     │  ├─ Check duplicates: 3s
     │  └─ Confirm add tasks: 3s (user: YES)
     │
     ├─ 02:00-02:15 ITERATION 1 (Loop Decision)
     │  └─ Continue? (user: YES)
     │
     ├─ 02:15-02:35 ITERATION 2 (Plan Phase)
     │  ├─ Load context: 2s
     │  ├─ Call getNextTask(): 1s (finds different task)
     │  ├─ Analyze: 8s
     │  ├─ Create subtasks: 4s
     │  └─ Confirm: 6s (user: YES)
     │
     ├─ 02:35-03:30 ITERATION 2 (Execute Phase)
     │  └─ [Similar pattern]
     │
     ├─ 03:30-04:20 ITERATION 2 (Review Phase)
     │  └─ [Similar pattern]
     │
     ├─ 04:20-04:25 ITERATION 2 (Loop Decision)
     │  └─ Continue? (user: NO ← Loop Breaks)
     │
     └─ 04:25-04:30 SESSION SUMMARY
        ├─ Total time: 4:30
        ├─ Iterations: 2
        ├─ Tasks completed: 6
        └─ Improvements: 4
```

---

## Metrics & Analytics

### Performance Metrics

```text
TIMING ANALYSIS

Average Phase Duration:
├─ Plan Phase: 30s (range: 20-45s)
├─ Execute Phase: 45s (range: 30-75s)
├─ Review Phase: 45s (range: 35-60s)
└─ Loop Cycle: 120s (2 minutes per iteration)

Bottleneck Analysis:
├─ User wait times: 18% of session
├─ System processing: 82% of session
├─ Slowest component: Execute (waiting for per-task confirmation)
├─ Fastest component: Plan (automated analysis)
└─ Recommendation: Batch similar tasks to reduce confirmation overhead

Task Completion Efficiency:
├─ Avg subtasks per iteration: 3
├─ Completion rate: 100% (0 failures)
├─ Rework rate: 0% (no tasks returned from Review)
├─ Quality score: 95% (discoveries per task)
```

### Quality Metrics

```text
DUPLICATE PREVENTION EFFECTIVENESS

Iteration 1:
├─ Discovered improvements: 4
├─ Checked for duplicates: 4
├─ Duplicates found: 0
└─ New tasks added: 4

Iteration 2:
├─ Discovered improvements: 3
├─ Checked for duplicates: 3
├─ Duplicates found: 1 ("Add rate limiting" already exists)
│  └─ Action: Skipped
└─ New tasks added: 2

Duplicate Prevention Summary:
├─ Total improvements discovered: 7
├─ Duplicates prevented: 1
├─ Prevention rate: 14%
├─ Effectiveness: ✅ WORKING
└─ Note: Prevents task backlog pollution

Error Handling:
├─ Tasks attempted: 6
├─ Tasks succeeded: 6
├─ Tasks failed: 0
├─ Failure rate: 0%
├─ All failures handled by Review
└─ Recommendation: Continue current approach
```

### User Interaction

```text
CONFIRMATION PATTERNS

Decision Points per Iteration:
├─ Smart Plan:
│  └─ "Ready to execute?" [1 decision point]
├─ Smart Execute:
│  └─ "✅ Task done?" [N decision points = N subtasks]
└─ Smart Review:
   └─ "Continue loop?" [1 decision point]

Iteration 1 Confirmations:
├─ Plan phase: 1 (Ready? YES)
├─ Execute phase: 4 (3 tasks + overall ready)
├─ Review phase: 2 (Add tasks? YES → Continue loop? YES)
└─ Total: 7 confirmations in 4:30 (1 per 38 seconds)

User Response Time:
├─ Quick decisions (< 5s): 5 confirmations (71%)
├─ Medium decisions (5-15s): 2 confirmations (29%)
├─ Slow decisions (> 15s): 0 confirmations (0%)
└─ Avg response time: 4.2 seconds
```

---

## Live Dashboard View (Terminal Output)

When running the tight loop, your dashboard might look like:

```text
═══════════════════════════════════════════════════════════════════
                   TIGHT LOOP WORKFLOW DASHBOARD
                    Session ID: 2024-12-15-14:32
═══════════════════════════════════════════════════════════════════

📊 REAL-TIME STATUS
│
├─ Iteration: 1 of [TBD]
├─ Current Phase: EXECUTE (Step 2/5)
├─ Time Elapsed: 01:32
├─ Last Update: 14:33:45
│
├─ Active Task: "Implement OAuth endpoints"
├─ Status: IN PROGRESS
├─ Completion: ████████░░ 80%
└─ Awaiting: User confirmation: "✅ OAuth setup done? [YES/NO]"

───────────────────────────────────────────────────────────────────

📈 AGGREGATE METRICS
│
├─ Phase Summary:
│  ├─ Plan (Iteration 1):    COMPLETE (30s)
│  ├─ Execute (Iteration 1): IN PROGRESS (21s of 45s est)
│  └─ Review (Iteration 1):  QUEUED
│
├─ Work Summary:
│  ├─ Subtasks Created: 3
│  ├─ Subtasks Completed: 2 of 3
│  ├─ Subtasks Failed: 0
│  └─ Progress: 66%
│
└─ Discoveries:
   ├─ Issues Found: 2
   ├─ Duplicates Prevented: 0
   └─ New Tasks Queued: [TBD]

───────────────────────────────────────────────────────────────────

🎯 CURRENT DECISION POINT

Question: ✅ Subtask "Implement OAuth endpoints" complete?

Options:
  [Y] Mark complete, move to next subtask
  [N] Task failed, return to analysis
  [H] Help/details about this task

Status: AWAITING USER RESPONSE (max 5 minutes idle timeout)

───────────────────────────────────────────────────────────────────

📋 RECENT ACTIVITY LOG

14:32:00 │ Session Started
14:32:05 │ ✓ Loaded workflow context
14:32:08 │ ✓ Found task: "Implement Authentication System" (Priority: HIGH)
14:32:10 │ Smart Plan: Analyzing vagueness...
14:32:18 │ Smart Plan: Vagueness score = 0.6 (moderate)
14:32:22 │ Smart Plan: Ask user clarifications [INTERACTION]
14:32:34 │ ✓ User answered: Scope includes OAuth + JWT
14:32:45 │ Smart Plan: Creating subtasks...
14:32:52 │ ✓ Created 3 subtasks
14:33:02 │ ✓ User confirmed: Ready to execute? [YES]
14:33:05 │ Handoff: Smart Plan → Smart Execute
14:33:07 │ Smart Execute: Loading context...
14:33:12 │ Smart Execute: Executing Task 1 (OAuth endpoints)...
14:33:27 │ ✓ Task 1 complete
14:33:30 │ ✓ User confirmed: Mark complete? [YES]
14:33:33 │ Smart Execute: Executing Task 2 (JWT management)...
14:33:42 │ [WAITING FOR USER CONFIRMATION]

───────────────────────────────────────────────────────────────────

```text
⏱️ ESTIMATED REMAINING TIME
├─ Current task: ~8 seconds
├─ Remaining execute subtasks: ~25 seconds
├─ Review phase: ~45 seconds
├─ Loop decision: ~5 seconds
└─ Total remaining: ~1:23 minutes

═══════════════════════════════════════════════════════════════════
```

---

## Data Collection for Analysis

### What Gets Tracked

```text
Per Iteration:
├─ Task ID planned
├─ Subtask count
├─ Subtask completion status (each)
├─ Execution time per subtask
├─ User confirmations (responses + timing)
├─ Issues discovered
├─ Duplicates found and skipped
├─ Root causes identified
├─ New tasks created
└─ Loop decision (continue/break)

Per Session:
├─ Session ID (timestamp)
├─ Start/end time
├─ Total iterations
├─ Total tasks processed
├─ Total improvements discovered
├─ Duplicate prevention effectiveness
├─ Success rate (% completed vs failed)
├─ Average phase timing
├─ User interaction patterns
├─ Bottlenecks identified
└─ Quality metrics
```

### Storage

Dashboard data gets logged to:

- **Memory:** `/memories/dev/smart-execute/` (per-iteration tracking)
- **Zen Tasks:** Observations field (task-level details)
- **Session Summary:** Full results when loop ends

---

## Agent Loop Dashboard Integration

### Smart Execute Phase - Updates

Agents should **READ** the current dashboard state, then **APPEND** updates:

```markdown
[Execution Update - HH:MM:SS]
**Current Task:** [Title] (Status: in-progress)
**Completed:** [Task A], [Task B], [Task C]
**Failed:** [Task X] (Error: brief reason)
**Metrics:** [X tasks/min], [timing]
**Notes:** [Short observation from this task]
```

### Smart Review Phase - Updates

Agents should **READ** execution updates, then **APPEND** review results:

```markdown
[Review Update - HH:MM:SS]
**Completed Count:** [N] tasks verified
**Failed Count:** [M] tasks analyzed
**Discovered Tasks:** [K] new tasks created
**Key Findings:** [Brief pattern summary]
**Recommendation:** [Replan | Continue | Done]
**Next Step:** [What happens next]
```

### Dashboard Format Rules

- Each phase appends its section (don't overwrite)
- Keep recent task list (last 5-10 items visible)
- Observations should be short (1-2 sentences max)
- Timestamp every update (HH:MM:SS format)
- Current task always at top of Execute section
- READ current state before updating (understand context)

---

## Monitoring Checklist

During testing, watch for these indicators:

### ✅ Healthy Indicators

- [ ] Each phase starts with fresh context load
- [ ] Smart Plan finds different task each iteration (via getNextTask)
- [ ] Smart Execute asks per-task confirmation
- [ ] Smart Review shows duplicate count (even if 0)
- [ ] Handoffs happen automatically (no manual switching)
- [ ] User responses are captured correctly
- [ ] Loop continues/breaks as expected
- [ ] Session summary shows all metrics

### 🚨 Warning Indicators

- [ ] Same task planned twice (getNextTask not working)
- [ ] No per-task confirmations in Execute
- [ ] No duplicate count shown in Review
- [ ] Manual agent switching required
- [ ] User responses not captured
- [ ] Loop doesn't continue properly
- [ ] Session hangs during phase transition
- [ ] Missing data in final summary

### 🔴 Critical Issues

- [ ] askuser cycles overlap between agents
- [ ] Agent state inherited from previous phase
- [ ] Duplicate tasks created (prevention failed)
- [ ] Tasks marked complete without user confirmation
- [ ] Loop infinite or terminates unexpectedly
- [ ] Session crashes during iteration

---

## Analysis Template (Post-Session)

After testing, fill out this analysis:

```markdown
# Test Session Analysis

**Session ID:** [TIMESTAMP]
**Tester:** [NAME]
**Date:** [DATE]
**Duration:** [MM:SS]

## Observations

**✅ What Worked Well:**
- [Observation 1]
- [Observation 2]
- [Observation 3]

**⚠️ What Needs Improvement:**
- [Issue 1]: [Description] → [Recommendation]
- [Issue 2]: [Description] → [Recommendation]

**🔴 Critical Issues:**
- [Critical 1]: [Description] → [Urgency: HIGH]
- [Critical 2]: [Description] → [Urgency: HIGH]

## Metrics

| Metric | Value | Target | Status |
|--------|-------|--------|--------|
| Iterations Completed | 2 | 2+ | ✅ PASS |
| Tasks Per Iteration | 3 | 2-5 | ✅ PASS |
| Completion Rate | 100% | 95%+ | ✅ PASS |
| Duplicate Prevention | 1/7 | 10%+ | ✅ PASS |
| Avg Phase Time | 40s | < 60s | ✅ PASS |

## Recommendations

1. [Priority 1] [Action]
2. [Priority 2] [Action]
3. [Priority 3] [Action]

## Next Steps

- [ ] Action 1
- [ ] Action 2
- [ ] Action 3
```

---

## Using This Dashboard

### During Testing

1. Keep this file open while running the tight loop
2. Manually update metrics as phases complete
3. Note any deviations from expected behavior
4. Record user interaction patterns

### After Testing

1. Fill out Analysis Template above
2. Identify patterns and bottlenecks
3. Create GitHub issues for improvements
4. Update documentation based on learnings

### For Multiple Sessions

1. Create a new dashboard per session
2. Compare metrics across sessions
3. Identify consistency and regressions
4. Track improvement over time

---

## Quick Reference

**Dashboard Components:**

- Session metadata (ID, time, iteration count)
- Current phase status (step, time spent, awaiting)
- Aggregated metrics (tasks, time, quality)
- User interaction log (decisions, timing)
- Activity timeline (what happened, when)
- Estimated remaining time
- Live warnings for issues

**Key Metrics to Track:**

- Phase timing (are they consistent?)
- Completion rates (are tasks getting done?)
- Duplicate prevention (is it working?)
- User interaction (are they confirming quickly?)
- Loop behavior (does it continue/break correctly?)
- Error handling (are failures caught?)
- Overall quality (are improvements being discovered?)

**Success Criteria:**

- All phases complete without manual switching
- Duplicate prevention shows positive counts (issues prevented)
- User confirmations happen naturally
- Loop continues as expected
- Session summary is complete and accurate

---

## Observations

### Agent Role Focus (CRITICAL)

Each agent focuses ONLY on its assigned job—no cross-role responsibilities:

- **Smart Plan:** Creates subtasks in Zen Tasks (plans, doesn't execute or review)
- **Smart Execute:** Executes tasks handed to it (reads task names/docs, applies properly, doesn't mark done)
- **Smart Review:** Reviews execution results, creates discovered tasks, **marks completed tasks as done** (doesn't execute)

### Task Completion Flow (CRITICAL)

Tasks must NEVER be marked complete until Smart Review reviews and confirms:

1. Smart Execute runs tasks but does NOT mark them complete
2. Smart Review analyzes results, confirms work quality, then marks tasks as done
3. This ensures accountability and prevents premature completion claims

### Use Ask User for (Developer Interaction)

- **Critical non-trivial decisions** - Architecture choices, scope changes, requirement conflicts
- **Program verification** - "Has the program been started?" / "Is the GUI accessible?"
- **Automated verification not possible** - Manual testing requirements, non-deterministic features
- **GUI/UX feedback** - "Review this page element by element, button by button—what's missing?"
- **Post-execution validation** - "Does this functionality work as expected through the UI?"
- **Clarifications when** - Info not in plan/tasks/project documentation

**Example Ask User Scenarios:**

- Execute phase: "Program started—can you verify the login flow through the GUI?"
- Review phase: "Page-by-page review needed: [URL or feature]. Anything missing or need changes?"
- Plan phase: "Requirement ambiguous—what does 'responsive design' mean in your context?"

### Proceed WITHOUT extra prompts when

- Executing clearly scoped subtasks already approved in plan
- Continuing deterministic actions within an in-progress phase
- Loading context, fetching tasks, running analysis
- Updating loop status dashboard or logging observations
- Performing automated duplicate prevention, validation, pattern analysis
- Creating discovered tasks from review observations (automatic, no prompting)


### Dashboard updates (execution + review)

- Smart Execute updates live loop dashboard metrics while executing (status, counts, confirmations)
- Smart Review updates dashboard with discoveries, failures, duplicate-prevention counts, and completion summaries
- Keep dashboard in sync each phase; no waiting for hub handoff

**note:** Goal = **Chaos coding with minimum developer interference, maximum autopilot**. Ask User only for: non-trivial decisions, GUI verification, feedback that requires human judgment. Everything else runs automatically.

- **Monitor for:**
  - Repeated tasks being planned (getNextTask issue or cycle detection failure)
  - Smart Execute marking tasks complete (should NOT do this—only Smart Review)
  - No duplicate prevention in Review phase (discovered tasks creating duplicates)
  - Manual agent switching (should route via Full Auto buttons)
  - Missing or delayed user responses to critical asks (clarifications, GUI verification)
  - Loop not continuing or breaking as expected
  - Session hanging during phase transitions
  - Incomplete data in final summary
  - Each agent has full access to previous agent's chat history for context
