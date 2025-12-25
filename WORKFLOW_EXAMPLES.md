# Workflow Examples - Phase-Gated Execution with TaskSync

This document provides step-by-step examples of how the phase-gated workflow operates with user confirmation checkpoints.

## Example 1: Simple API Endpoint Implementation

**Goal:** Create a new REST API endpoint for user authentication

### Phase 1: PLANNING

**User Interaction:**
```
User: @Full Auto
User clicks: 🎯 Plan Phase

Full Auto routes to Smart Plan with:
  Goal: "Create REST API endpoint for user authentication"
  Context: Current project structure, existing endpoints
  Mode: TASKSYNC ENABLED
```

**Smart Plan Execution:**

```
Step 1: Load Workflow Context
  └─ loadWorkflowContext() 
     → Discovers: 3 pending features, 5 in-progress tasks
     
Step 2: Analyze Goal for Vagueness
  Goal Analysis:
    - Scope: Specific ✓
    - Metrics: "user authentication" (needs clarification)
    - Requirements: Not listed
  → Vagueness Score: 0.6 (MEDIUM - needs QA)

Step 3: Conduct QA Survey
  ❓ "What authentication method? (OAuth, JWT, Basic Auth?)"
  User: "JWT with refresh tokens"
  
  ❓ "What endpoints needed? (Login, Logout, Refresh?)"
  User: "Login, Logout, and Refresh"
  
  ❓ "Error handling requirements?"
  User: "Return 401 for bad credentials, 500 for server errors"

Step 4: Create Subtasks via parseRequirements()
  Goal (refined): "Create JWT-based authentication API with 
                   Login, Logout, Refresh endpoints + error handling"
  
  Generated Subtasks:
  1. subtask-1: "Set up JWT middleware"
     Priority: high | Complexity: 5 | Status: pending
     
  2. subtask-2: "Implement /auth/login endpoint"
     Priority: high | Complexity: 6 | Status: pending
     
  3. subtask-3: "Implement /auth/logout endpoint"
     Priority: high | Complexity: 4 | Status: pending
     
  4. subtask-4: "Implement /auth/refresh endpoint"
     Priority: high | Complexity: 5 | Status: pending
     
  5. subtask-5: "Add error handling for all endpoints"
     Priority: high | Complexity: 4 | Status: pending
     
  6. subtask-6: "Write unit tests for auth endpoints"
     Priority: medium | Complexity: 6 | Status: pending
     
  7. subtask-7: "Document API endpoints in OpenAPI spec"
     Priority: medium | Complexity: 3 | Status: pending

Step 5: Display Subtasks & Ask for Confirmation
  
  📋 PLANNING COMPLETE
  
  I've created 7 subtasks for JWT authentication implementation:
  
  HIGH PRIORITY:
  • subtask-1: Set up JWT middleware (Complexity: 5)
  • subtask-2: Implement /auth/login endpoint (Complexity: 6)
  • subtask-3: Implement /auth/logout endpoint (Complexity: 4)
  • subtask-4: Implement /auth/refresh endpoint (Complexity: 5)
  • subtask-5: Add error handling (Complexity: 4)
  
  MEDIUM PRIORITY:
  • subtask-6: Write unit tests (Complexity: 6)
  • subtask-7: Document API endpoints (Complexity: 3)
  
  Confirm adding these subtasks to execution queue?
  [✓ YES - Start Execution] [✗ NO - Revise Planning]
```

**User Decision:** ✓ YES

```
Smart Plan logs observation:
  Type: planning
  Event: subtasks_created
  Count: 7
  User Confirmation: YES
  
Smart Plan returns to Full Auto with message:
  "📋 PHASE COMPLETE - Planning Done"
```

---

### Phase 2: EXECUTION

**User Interaction:**
```
Full Auto displays:
  Current Phase: Planning Complete ✓
  Ready for Execution: 7 pending subtasks
  
User clicks: ⚡ Execute Phase

Full Auto routes to Smart Execute with:
  Task Queue: 7 pending subtasks (JWT auth implementation)
  Mode: TASKSYNC ENABLED
```

**Smart Execute Execution:**

```
Step 1: Load Workflow Context
  └─ loadWorkflowContext()
     → Loads: 7 pending subtasks, dependencies, project context

Step 2: Get Next Task
  └─ getNextTask(limit=1)
     → Returns: subtask-1 (Set up JWT middleware)
     → Status: pending | Priority: high | Complexity: 5

Step 3: Execute Task 1 - Set up JWT middleware

  Execution Steps:
  1. npm install jsonwebtoken dotenv
  2. Create src/middleware/auth.ts
  3. Implement JWT verification logic
  4. Add middleware to Express app
  5. Test with simple token validation
  
  Terminal Output:
  $ npm install jsonwebtoken dotenv
  added 8 packages, removed 0 packages, audited 15 packages
  
  $ cat > src/middleware/auth.ts << 'EOF'
  import jwt from 'jsonwebtoken';
  
  export const authMiddleware = (req, res, next) => {
    const token = req.headers.authorization?.split(' ')[1];
    if (!token) return res.status(401).json({ error: 'No token' });
    
    try {
      const decoded = jwt.verify(token, process.env.JWT_SECRET!);
      (req as any).user = decoded;
      next();
    } catch (error) {
      res.status(401).json({ error: 'Invalid token' });
    }
  };
  EOF
  
  ✓ File created successfully
  ✓ Middleware implemented
  ✓ Dependencies installed

Step 4: Task Completion Confirmation

  ✅ TASK COMPLETE
  
  Task: subtask-1 (Set up JWT middleware)
  Status: ✓ Success
  
  Observations:
  • JWT middleware implemented in src/middleware/auth.ts
  • Dependencies installed: jsonwebtoken, dotenv
  • Middleware validates JWT tokens and attaches user to request
  • Error handling: Returns 401 for missing/invalid tokens
  
  Confirm before marking done?
  [✓ YES - Mark Done] [✗ NO - Review First]
```

**User Decision:** ✓ YES

```
Smart Execute:
  ✓ Call setTaskStatus(subtask-1, "completed")
  ✓ Log observation:
    Type: execution
    Task: subtask-1
    Result: success
    User Confirmed: YES

  Next → getNextTask() → subtask-2
```

```
Step 5: Execute Task 2 - Implement /auth/login endpoint

  Execution Steps:
  1. Create src/routes/auth.ts
  2. Implement POST /auth/login with validation
  3. Return JWT token on success
  4. Handle errors (invalid credentials, etc)
  
  Code Created:
  export const loginHandler = async (req, res) => {
    const { email, password } = req.body;
    
    // Validate input
    if (!email || !password) {
      return res.status(400).json({ error: 'Email and password required' });
    }
    
    // Query database for user
    const user = await db.users.findOne({ email });
    if (!user || !comparePassword(password, user.passwordHash)) {
      return res.status(401).json({ error: 'Invalid credentials' });
    }
    
    // Generate JWT
    const token = jwt.sign(
      { userId: user.id, email: user.email },
      process.env.JWT_SECRET!,
      { expiresIn: '1h' }
    );
    
    return res.json({ 
      token,
      user: { id: user.id, email: user.email }
    });
  };
  
  ✓ Login endpoint implemented
  ✓ Password validation working
  ✓ JWT generation successful
  ✓ Error handling in place

Step 6: Task 2 Confirmation

  ✅ TASK COMPLETE
  
  Task: subtask-2 (Implement /auth/login endpoint)
  Status: ✓ Success
  
  Observations:
  • Login endpoint created at POST /auth/login
  • Password validation against database
  • JWT token generated with 1h expiration
  • Error handling: 400 for missing fields, 401 for invalid credentials
  
  Confirm before marking done?
  [✓ YES - Mark Done] [✗ NO - Review First]
```

**User Decision:** ✓ YES

```
Continue with subtasks 3-7...
(Similar confirmation flow for each task)

After Task 5 (Error handling):
✓ Subtask 1 completed
✓ Subtask 2 completed
✓ Subtask 3 completed
✓ Subtask 4 completed
✓ Subtask 5 completed
⏳ Subtask 6 (Unit tests) - in progress
⏳ Subtask 7 (Documentation) - pending

After Task 7 (Final documentation):

✅ EXECUTION COMPLETE

Summary:
  Executed Tasks: 7 ✓
  Failed Tasks: 0
  
  All subtasks completed successfully:
  • JWT middleware ✓
  • /auth/login endpoint ✓
  • /auth/logout endpoint ✓
  • /auth/refresh endpoint ✓
  • Error handling ✓
  • Unit tests ✓
  • API documentation ✓
  
  Ready for review phase?
  [✓ YES - Review] [✗ NO - Continue]
```

**User Decision:** ✓ YES

```
Smart Execute returns to Full Auto:
  "📋 PHASE COMPLETE - Execution Done"
  
  Executed Tasks:
  • subtask-1: Set up JWT middleware ✓
  • subtask-2: Implement /auth/login ✓
  • subtask-3: Implement /auth/logout ✓
  • subtask-4: Implement /auth/refresh ✓
  • subtask-5: Add error handling ✓
  • subtask-6: Write unit tests ✓
  • subtask-7: Document API endpoints ✓
```

---

### Phase 3: REVIEW

**User Interaction:**
```
Full Auto displays:
  Current Phase: Execution Complete ✓
  
User clicks: 🔍 Review Phase

Full Auto routes to Smart Review with:
  Completed Tasks: 7 (all successful)
  Failed Tasks: 0
  Mode: TASKSYNC ENABLED
```

**Smart Review Execution:**

```
Step 1: Load Workflow Context
  └─ loadWorkflowContext()
     → All tasks marked completed

Step 2: Analyze Results
  
  Pattern Analysis:
  ✓ Success Clustering: All tasks completed in dependency order
  ✓ No Failures: 100% success rate
  ✓ Timing: Average 45 minutes per task
  ✓ Dependencies: All prerequisites met
  
  Conclusion: Excellent execution flow

Step 3: Review Each Task
  
  subtask-1 (JWT middleware):
    Status: ✓ Completed
    Quality: High - proper error handling, clean code
    Insight: Well-structured, reusable middleware
    
  subtask-2 (Login endpoint):
    Status: ✓ Completed
    Quality: Good - includes validation and error handling
    Insight: Consider adding rate limiting to prevent brute force
    
  subtask-3 (Logout endpoint):
    Status: ✓ Completed
    Quality: Good - token blacklist implemented
    Insight: Works well with login flow
    
  ...
  (Review all 7 tasks)

Step 4: Discover Issues

  During review, discovered potential improvements:
  
  Issue D1: "Add rate limiting to login endpoint"
    Priority: medium | Complexity: 4
    Description: Prevent brute force attacks on login endpoint
    
  Issue D2: "Improve JWT token expiration handling"
    Priority: low | Complexity: 3
    Description: Add automatic token refresh before expiration
    
  Issue D3: "Add password reset endpoint"
    Priority: medium | Complexity: 5
    Description: Implement secure password reset workflow
    
  Issue D4: "Setup OpenAPI/Swagger documentation"
    Priority: low | Complexity: 4
    Description: Auto-generate API docs from code

Step 5: Discovered Tasks Confirmation

  📋 DISCOVERED_TASKS
  
  Review found 4 improvements that could enhance the implementation:
  
  MEDIUM PRIORITY:
  • Issue D1: Add rate limiting to login endpoint (Complexity: 4)
  • Issue D3: Add password reset endpoint (Complexity: 5)
  
  LOW PRIORITY:
  • Issue D2: Improve JWT token expiration handling (Complexity: 3)
  • Issue D4: Setup OpenAPI/Swagger documentation (Complexity: 4)
  
  Add these to backlog for next iteration?
  [✓ YES - Add All] [✎ EDIT - Modify] [✗ NO - Skip]
```

**User Decision:** ✎ EDIT

```
Edit Mode:
  
  D1 (Rate limiting):
    Current: Priority=medium, Complexity=4
    Change to: Priority=HIGH, Complexity=4
    Reason: Security critical
  
  D3 (Password reset):
    Current: Priority=medium, Complexity=5
    Skip this one (not needed yet)
  
  D2 & D4: Keep as-is (low priority)
  
  [✓ SAVE CHANGES]
```

**User Decision:** ✓ SAVE

```
Smart Review:
  ✓ Create issue D1 with Priority=HIGH, Complexity=4
  ✓ Skip D3 (user chose not to add)
  ✓ Create D2 with Priority=LOW, Complexity=3
  ✓ Create D4 with Priority=LOW, Complexity=4
  
  Log observations:
    Type: review
    Discovered Tasks: 3 (D1, D2, D4)
    Tasks Skipped: 1 (D3)
    User Modified: D1 priority to HIGH
    User Confirmed: YES (with edits)

Step 6: Final Recommendation

  ✅ REVIEW COMPLETE
  
  Analysis Summary:
  • All 7 planned subtasks completed successfully
  • Zero failures, excellent execution quality
  • 3 improvement tasks discovered and added to backlog
  • D1 (Rate limiting) marked HIGH priority for next iteration
  
  Discovered tasks added:
  • D1: Add rate limiting (HIGH, Complexity: 4) ✓
  • D2: Improve JWT expiration (LOW, Complexity: 3) ✓
  • D4: Setup Swagger docs (LOW, Complexity: 4) ✓
  
  Recommendation: CONTINUE LOOP
  
  Reason: Authentication API complete, but rate limiting (HIGH priority)
          discovered during review should be addressed next.
  
  Continue loop (Plan→Execute→Review) for next feature?
  [✓ YES - Continue Loop] [✗ NO - Mark Feature Done]
```

**User Decision:** ✓ YES - CONTINUE LOOP

```
Smart Review returns to Full Auto:
  "📋 PHASE COMPLETE - Review Done"
  
  Recommendation: Continue Loop
  Next task: Plan rate limiting for login endpoint
  
Full Auto Now Displays:
  ═══════════════════════════════════════════════════════
  WORKFLOW LOOP - ITERATION 2
  ═══════════════════════════════════════════════════════
  
  ✓ COMPLETED (Iteration 1):
    - Plan: JWT Authentication (7 subtasks)
    - Execute: All subtasks completed
    - Review: Discovered 3 improvements
  
  ⏳ NEXT (Iteration 2):
    - Plan: Rate Limiting & Improvements
    - Execute: New security features
    - Review: Verify security implementation
  
  Ready to start Planning Phase for Iteration 2?
  [🎯 PLAN PHASE] [⚡ EXECUTE] [🔍 REVIEW] [✓ DONE]
```

User clicks: 🎯 PLAN PHASE (for iteration 2, starting with rate limiting)

```
...Workflow loops back to Planning Phase...
```

---

## Example 2: Database Schema Migration (Shorter Version)

**Goal:** Add user roles and permissions to database schema

### Phase 1: PLANNING
```
Smart Plan analyzes goal
Vagueness Score: 0.3 (low-medium)
Creates 4 subtasks:
  1. Design role model (roles, permissions, user_roles)
  2. Create database migration file
  3. Implement role-based access control
  4. Write tests for RBAC

User confirms: ✓ YES

Planning Complete → Back to Full Auto
```

### Phase 2: EXECUTION
```
Task 1: Design role model
  ✓ Creates database schema
  User confirms: ✓ YES

Task 2: Create migration
  ✓ Migration file created
  User confirms: ✓ YES

Task 3: Implement RBAC
  ✓ Middleware created
  User confirms: ✓ YES

Task 4: Write tests
  ✓ Tests passing
  User confirms: ✓ YES

All 4 tasks completed
User confirms ready for review: ✓ YES

Execution Complete → Back to Full Auto
```

### Phase 3: REVIEW
```
Smart Review analyzes:
  ✓ All tests passing
  ✓ Schema properly designed
  ✓ RBAC functional

Discovered Issues:
  D1: Add admin role seeds
  D2: Document permission model
  
User adds both: ✓ YES

Recommendation: DONE (no critical issues)

User confirms: ✓ DONE - Feature Complete
```

**Result:** Feature complete, 2 follow-up tasks added for future work

---

## Example 3: Failed Task Recovery

**Goal:** Fix failing unit tests in authentication module

### Phase 2: EXECUTION (Failing Task Example)

```
Task 1: Fix JWT token validation tests
  ✓ Executed
  ✓ 5 tests pass
  ✗ 1 test fails: "Token expiration not enforced"
  
  Error Output:
  AssertionError: Expected token to expire after 1 hour
  Actual: Token still valid after 5 hours
  
  ✅ TASK COMPLETE (with errors)
  
  Observations:
  • 5/6 tests passing
  • JWT expiration validation broken
  • Likely cause: Missing expiration check in middleware
  
  Confirm before marking done?
  [✓ YES - Mark Done (Partial)] [✗ NO - Continue Fixing]
```

**User Decision:** ✗ NO - CONTINUE FIXING

```
Smart Execute (continues with same task):
  
  Additional Debug:
  ✓ Found issue in authMiddleware
  ✓ JWT.verify wasn't checking expiration
  ✓ Fixed by using jwt.verify with options
  
  Result: 6/6 tests now passing ✓
  
  ✅ TASK COMPLETE (Fixed)
  
  Confirm before marking done?
  [✓ YES - Mark Done] [✗ NO - Review First]
```

**User Decision:** ✓ YES

```
Smart Execute marks task as:
  Status: completed
  Observations: "Task required debugging; JWT expiration validation was missing"
  User Confirmed: YES
  
  Continue to next task...
```

---

## Phase Workflow Summary

| Phase | Action | Confirmation | Return |
|-------|--------|--------------|--------|
| **Plan** | Create subtasks | "Confirm subtasks?" | To Full Auto |
| **Execute** | Run each task | "Confirm done?" per task | To Full Auto |
| **Review** | Analyze results | "Confirm discovered tasks?" | To Full Auto |
| **Loop** | Continue or Done | "Continue loop?" | Back to Plan or End |

---

## Key Success Patterns

✅ **Successful Workflow:**
1. Clear planning with user confirmation
2. Methodical execution with per-task confirmation
3. Thorough review with discovered task confirmation
4. Explicit loop decision with user control

✅ **User Control Points:**
- Plan confirmation: approve subtask breakdown
- Execution confirmation: verify task completion quality
- Discovery confirmation: choose which improvements to tackle
- Loop confirmation: decide to continue or mark done

✅ **Error Recovery:**
- Task failures don't halt workflow (continue on error)
- User can ask for review or more debugging before marking done
- Review phase discovers improvements for next iteration
- Discovered tasks become next planning cycle

---

## Tips for Using the Workflow

1. **Be Specific in Goals** - Reduces vagueness score, fewer QA questions
2. **Review Task Results** - Use [NO] option if you want more details before confirming
3. **Monitor Observations** - Check logs in Zen Tasks for complete execution history
4. **Use Discovered Tasks** - Review phase finds improvements automatically
5. **Loop Strategically** - Continue loop for related work, mark done when feature complete

