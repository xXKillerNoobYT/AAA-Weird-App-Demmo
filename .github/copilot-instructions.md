# AAA Weird App Demo - Multi-Agent Workflow System

This is an integrated multi-agent AI system using GitHub Copilot agents to manage development tasks collaboratively. The workflow combines **Full Auto (Hub)** with specialized **Spoke Agents** (Plan, Execute, Review, Prep Cloud) coordinated through **Zen Tasks** for task tracking and **MPC (Memory & Project Context)** for state management.

**Core Architecture:**
- **Full Auto New**: Central UI hub that displays task queues and routes to specialists
- **Smart Plan**: Creates detailed subtasks and detects requirement vagueness
- **Smart Execute**: Runs tasks sequentially with status updates and error logging
- **Smart Review**: Analyzes execution results and provides root-cause analysis
- **Agent Builder & Updater**: Creates/maintains agents with consistent structure
- **Tool Builder**: Designs and implements MCP tools
- **Smart Prep Cloud**: Prepares tasks for cloud execution

**Always reference these instructions first.** All agents follow the 2-module reasoning system (CHECKLIST + TASK ORCHESTRATOR) and use Zen Tasks for task tracking.

## Zen Tasks Integration

**Zen Tasks is the single source of truth for all development work.** Every agent loads the workflow context, queries for next tasks, and updates status after work completes.

### Task Tracking Pattern (Test Sync)

All agents follow this pattern:
```
1. loadWorkflowContext()        # Load current project and task state
2. getNextTask(limit=N)          # Get executable tasks in priority order
3. [Perform work]                # Execute, plan, review, or build
4. setTaskStatus(task_id, ...)   # Update task status and observations
5. Return to Full Auto with button options
```

### Task Protocol Standard

All tasks in Zen Tasks must include:
- **Status:** pending | in_progress | completed | failed | deferred
- **Priority:** low | medium | high
- **Complexity:** 1-10 scale with label (simple 1-2, moderate 2-5, complex 5-7, veryComplex 7-10)
- **Description:** 2-3 sentences explaining the work
- **Tags:** Comma-separated functional area tags
- **Recommended Subtasks:** 0-10 count for planning guidance

### Memory Organization

All agents use consistent memory namespaces:
- `/memories/dev/full-auto/` - Hub coordination state
- `/memories/dev/smart-plan/` - Planning analysis and decisions
- `/memories/dev/smart-execute/` - Execution logs and observations
- `/memories/dev/smart-review/` - Analysis results and insights
- `/memories/dev/agent-builder/` - Agent templates and updates
- `/memories/dev/tool-builder/` - Tool specs and implementations
- `/memories/dev/smart-prep-cloud/` - Cloud confidence and artifacts
- `/memories/dev/shared/` - Cross-agent shared state

## Working Effectively

### Prerequisites and Environment Setup

**.NET Development (Required for this project):**
- Install .NET 9.0 SDK: `./dotnet-install.ps1 -Channel 9.0`
- Install .NET 8.0 runtime: `./dotnet-install.ps1 -Channel 8.0 --runtime dotnet`
- Update PATH: `$env:PATH = "C:\Users\weird\AppData\Local\Microsoft\dotnet;$env:PATH"`
- Verify: `dotnet --version` (should be 9.0.x)

### Python Development Workflow (Optional)

**Bootstrap and build Python environment:**
```bash
cd /home/runner/work/autogen/autogen/python
uv sync --all-extras  # NEVER CANCEL: Takes 2 minutes. Set timeout to 300+ seconds.
source .venv/bin/activate
```

**Validate Python development:**
```bash
# Quick validation (under 1 second each)
poe format  # Code formatting
poe lint    # Linting with ruff

# Type checking - NEVER CANCEL these commands
poe mypy     # Takes 6 minutes. Set timeout to 600+ seconds.
poe pyright  # Takes 41 seconds. Set timeout to 120+ seconds.

# Individual package testing (core package example)
poe --directory ./packages/autogen-core test  # Takes 10 seconds. Set timeout to 60+ seconds.

# Documentation - NEVER CANCEL
poe docs-build  # Takes 1 minute 16 seconds. Set timeout to 300+ seconds.
```

**CRITICAL TIMING EXPECTATIONS:**
- **NEVER CANCEL**: Python environment setup takes 2 minutes minimum
- **NEVER CANCEL**: mypy type checking takes 6 minutes 
- **NEVER CANCEL**: Documentation build takes 1+ minutes
- Format/lint tasks complete in under 1 second
- Individual package tests typically complete in 10-60 seconds

### .NET Development Workflow

**Bootstrap and build .NET environment:**
```bash
cd /home/runner/work/autogen/autogen/dotnet
export PATH="$HOME/.dotnet:$PATH"
dotnet restore  # NEVER CANCEL: Takes 53 seconds. Set timeout to 300+ seconds.
dotnet build --configuration Release  # NEVER CANCEL: Takes 53 seconds. Set timeout to 300+ seconds.
```

**Validate .NET development:**
```bash
# Unit tests - NEVER CANCEL
dotnet test --configuration Release --filter "Category=UnitV2" --no-build  # Takes 25 seconds. Set timeout to 120+ seconds.

# Format check (if build fails) 
dotnet format --verify-no-changes

# Run samples
cd samples/Hello
dotnet run
```

**CRITICAL TIMING EXPECTATIONS:**
- **NEVER CANCEL**: .NET restore takes 53 seconds minimum
- **NEVER CANCEL**: .NET build takes 53 seconds minimum  
- **NEVER CANCEL**: .NET unit tests take 25 seconds minimum
- All build and test commands require appropriate timeouts

### Complete Validation Workflow

**Run full check suite (Python):**
```bash
cd /home/runner/work/autogen/autogen/python
source .venv/bin/activate
poe check  # NEVER CANCEL: Runs all checks. Takes 7+ minutes total. Set timeout to 900+ seconds.
```

## Validation Scenarios

### Manual Validation Requirements
Always manually validate changes by running complete user scenarios after making modifications:

**Python validation scenarios:**
1. **Import test**: Verify core imports work:
   ```python
   from autogen_agentchat.agents import AssistantAgent
   from autogen_core import AgentRuntime
   from autogen_ext.models.openai import OpenAIChatCompletionClient
   ```

2. **AutoGen Studio test**: Verify web interface can start:
   ```bash
   autogenstudio ui --help  # Should show help without errors
   ```

3. **Documentation test**: Build and verify docs generate without errors:
   ```bash
   poe docs-build && ls docs/build/index.html
   ```

**.NET validation scenarios:**
1. **Sample execution**: Run Hello sample to verify runtime works:
   ```bash
   cd dotnet/samples/Hello && dotnet run --help
   ```

2. **Build validation**: Ensure all projects compile:
   ```bash
   dotnet build --configuration Release --no-restore
   ```

3. **Test execution**: Run unit tests to verify functionality:
   ```bash
   dotnet test --filter "Category=UnitV2" --configuration Release --no-build
   ```

## Common Issues and Workarounds

### Network-Related Issues
- **Python tests may fail** with network errors (tiktoken downloads, Playwright browser downloads) in sandboxed environments - this is expected
- **Documentation intersphinx warnings** due to inability to reach external documentation sites - this is expected
- **Individual package tests work better** than full test suite in network-restricted environments

### .NET Runtime Issues  
- **Requires both .NET 8.0 and 9.0**: Build uses 9.0 SDK but tests need 8.0 runtime
- **Global.json specifies 9.0.100**: Must install exact .NET 9.0 version or later
- **Path configuration critical**: Ensure `$HOME/.dotnet` is in PATH before system .NET

### Python Package Issues
- **Use uv exclusively**: Do not use pip/conda for dependency management
- **Virtual environment required**: Always activate `.venv` before running commands
- **Package workspace structure**: Project uses uv workspace with multiple packages

## Timing Reference

### Python Commands
| Command | Expected Time | Timeout | Notes |
|---------|---------------|---------|-------|
| `uv sync --all-extras` | 2 minutes | 300+ seconds | NEVER CANCEL |
| `poe mypy` | 6 minutes | 600+ seconds | NEVER CANCEL |
| `poe pyright` | 41 seconds | 120+ seconds | NEVER CANCEL |
| `poe docs-build` | 1 min 16 sec | 300+ seconds | NEVER CANCEL |
| `poe format` | <1 second | 30 seconds | Quick |
| `poe lint` | <1 second | 30 seconds | Quick |
| Individual package test | 10 seconds | 60+ seconds | May have network failures |

### .NET Commands  
| Command | Expected Time | Timeout | Notes |
|---------|---------------|---------|-------|
| `dotnet restore` | 53 seconds | 300+ seconds | NEVER CANCEL |
| `dotnet build --configuration Release` | 53 seconds | 300+ seconds | NEVER CANCEL |
| `dotnet test --filter "Category=UnitV2"` | 25 seconds | 120+ seconds | NEVER CANCEL |
| `dotnet format --verify-no-changes` | 5-10 seconds | 60 seconds | Quick validation |

## Repository Structure

### .NET Project (Primary)
```
server/
├── CloudWatcher/
│   ├── Program.cs
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   └── CloudWatcher.csproj
└── database/
    └── migrations/
```

### Agents (in `.github/agents/`)
- **Full Auto New.agent.md** - UI Hub & workflow orchestrator (route-only)
- **Smart Plan Updated.agent.md** - Planning specialist (creates subtasks)
- **Smart Execute Updated.agent.md** - Execution specialist (runs tasks)
- **Smart Review Updated.agent.md** - Review specialist (analyzes results)
- **Agent Builder & Updater.agent.md** - Meta-agent (creates/updates agents)
- **Tool Builder.agent.md** - Tool creation specialist
- **Smart Prep Cloud.agent.md** - Cloud execution preparation

### Key Configuration Files
- `.github/copilot-instructions.md` - This file (master workflow instructions)
- `tasks.json` - Task backlog for imports
- `tasks/task-1.json` through `tasks/task-12.json` - Individual task files
- `Docs/Plan/` - Architecture and design documentation

## Development Best Practices

### Workflow Integration Checklist

**When starting work:**
1. ✅ Load copilot-instructions first (this file)
2. ✅ Start with Full Auto agent (hub/router)
3. ✅ Let Full Auto load Zen Tasks workflow context
4. ✅ Follow the phase buttons: Plan → Execute → Review
5. ✅ All actual work happens in spoke agents, not Full Auto

**When modifying agents:**
1. ✅ Ensure 2-module system (CHECKLIST + ORCHESTRATOR only, no MODULE 1 or 4)
2. ✅ Update memory namespace to match agent name
3. ✅ Include all Zen Tools in tools list
4. ✅ Set handoffs back to Full Auto (hub-spoke pattern)
5. ✅ Log all observations to memory via memory tools

**When updating copilot-instructions:**
1. ✅ Keep section on Zen Tasks integration up-to-date
2. ✅ Update memory organization when adding agents
3. ✅ Update task protocol if schema changes
4. ✅ Reflect .NET project structure accurately
5. ✅ Test with Full Auto to ensure agents can access tools

### Task Management Workflow

**Creating Tasks:**
- Use Zen Tasks `addTask()` (never create internal lists)
- Include: title, description, priority, complexity, tags
- Set initial status: `pending`

**Executing Tasks:**
- Use `getNextTask()` to find executable work
- Call `setTaskStatus()` after each step
- Log observations to memory (success AND failure)

**Reviewing Work:**
- Use `listTasks(status=completed)` for analysis
- Use `updateTask()` to add insights
- Create discovered tasks if issues block progress

**Before Committing Changes**
**.NET:**
```bash
cd server/CloudWatcher
dotnet format --verify-no-changes  # Check formatting
dotnet build --configuration Debug  # Build
dotnet test --configuration Debug  # Test (if available)
```

### Key Directories Reference
```
AAA Weird App Demmo/
├── .github/
│   ├── agents/          # All 7 agents with 2-module system
│   └── copilot-instructions.md  # This file (master workflow)
├── server/
│   ├── CloudWatcher/    # .NET API server
│   └── database/        # Database setup
├── device/
│   └── python/          # Device client (Python)
├── Cloud/               # Cloud storage integration
├── Docs/Plan/           # Architecture documentation
└── tasks.json           # Task backlog aggregate
```

## Workflow Execution Guide

### Full Auto Hub (Entry Point)

When you invoke Full Auto or mention "full auto", it will:

1. **Load Zen Tasks Context** - Get current project state and available tasks
2. **Display Task Queue** - Show pending, ready, and completed tasks
3. **Present Phase Buttons** - Offer next phase options
4. **Route to Specialists** - Hand off to Plan/Execute/Review based on your choice

**Full Auto will NOT execute, plan, or review itself** — it's the router and coordinator.

### Smart Plan (Planning Phase)

When you click "Plan Phase" (or mention "smart plan"), it will:

1. **Receive Goal** - From Full Auto or user input
2. **Detect Vagueness** - Ask clarifying questions if needed
3. **Create Subtasks** - Break work into executable steps in Zen Tasks
4. **Return to Hub** - Present "Ready to Execute?" button

### Smart Execute (Execution Phase)

When you click "Execute Phase" (or mention "smart execute"), it will:

1. **Get Next Tasks** - From Zen Tasks (pending work)
2. **Run Each Task** - Terminal, file operations, VS Code tools
3. **Update Status** - Mark complete/failed as it goes
4. **Log Observations** - Record progress and errors to memory
5. **Return to Hub** - Present "Ready for Review?" button

### Smart Review (Review Phase)

When you click "Review Phase" (or mention "smart review"), it will:

1. **Analyze Results** - Identify patterns, failures, successes
2. **Root-Cause Analysis** - Why did things succeed or fail?
3. **Update Insights** - Add findings to tasks
4. **Recommend Next** - Suggest replan, continue, or done
5. **Return to Hub** - Present recommendation buttons

### Agent Builder (Agent Management)

When you need to create or update agents:

1. **Use Agent Builder agent** - Specify what to create/change
2. **It maintains structure** - 2-module system, consistent tools
3. **It validates** - Checks for completeness and consistency
4. **Returns to Hub** - Ready for orchestration

### Tool Builder (Tool Creation)

When you need new MCP tools or integrations:

1. **Use Tool Builder agent** - Specify tool requirements
2. **It designs specs** - Schema, validation, capabilities
3. **It implements** - Tool stubs or full implementations
4. **Returns for review** - Via Smart Review handoff

### Smart Prep Cloud (Cloud Execution)

When tasks benefit from cloud resources:

1. **Smart Plan recommends** - If confidence is sufficient
2. **Prep Cloud validates** - Environment readiness, dependencies
3. **Creates artifacts** - GitHub Issues, TODOs, runner config
4. **Hands off to Cloud Agent** - For autonomous execution

## Agent Modes & Guardrails

### Full Auto (Hub Mode)
- ✅ DO: Display task queues, route to specialists, update state
- ✅ DO: Use Zen Tasks to load context and display progress
- ✅ DO: Present buttons for phase selection
- ❌ DON'T: Plan, execute, or review tasks directly
- ❌ DON'T: Create internal task lists in memory
- ❌ DON'T: Chain to other agents (let user/Full Auto route)

### Smart Plan (Planning Mode)
- ✅ DO: Analyze goals and detect vagueness
- ✅ DO: Create subtasks in Zen Tasks
- ✅ DO: Ask clarifying questions if needed
- ❌ DON'T: Execute any tasks
- ❌ DON'T: Review execution results
- ❌ DON'T: Chain to Smart Execute (return to Full Auto)

### Smart Execute (Execution Mode)
- ✅ DO: Run subtasks from Zen Tasks
- ✅ DO: Update task status after each step
- ✅ DO: Log observations (success and failure)
- ✅ DO: Continue on errors (don't halt)
- ❌ DON'T: Plan new tasks
- ❌ DON'T: Review results
- ❌ DON'T: Chain to Smart Review (return to Full Auto)

### Smart Review (Review Mode)
- ✅ DO: Analyze completed and failed tasks
- ✅ DO: Perform root-cause analysis
- ✅ DO: Update task insights
- ✅ DO: Create discovered tasks if needed
- ❌ DON'T: Execute tasks
- ❌ DON'T: Plan new work
- ❌ DON'T: Chain to Smart Plan (return to Full Auto)

### Agent Builder (Meta Mode)
- ✅ DO: Create/update agents with consistent structure
- ✅ DO: Preserve tool access and handoffs
- ✅ DO: Validate 2-module system compliance
- ❌ DON'T: Disable Docker MCP Toolkit
- ❌ DON'T: Remove Zen Tools from agents
- ❌ DON'T: Break hub-spoke routing pattern

### Tool Builder (Tool Mode)
- ✅ DO: Design MCP tool specifications
- ✅ DO: Implement with validation
- ✅ DO: Test with minimal runnable tests
- ✅ DO: Hand off to Plan/Review if uncertain
- ❌ DON'T: Ignore missing requirements
- ❌ DON'T: Chain without planning
- ❌ DON'T: Skip validation

### Smart Prep Cloud (Cloud Mode)
- ✅ DO: Validate environment readiness
- ✅ DO: Generate GitHub Issues with exact commands
- ✅ DO: Calculate Cloud Confidence (0-100%)
- ✅ DO: Place TODO breadcrumbs in code
- ❌ DON'T: Execute tasks locally
- ❌ DON'T: Skip environment validation
- ❌ DON'T: Recommend cloud if confidence <50%



### Tasksync Tool Instructions

1. During any process, task, or conversation—whether asking, replying, or completing phased tasks—you must call the `ask_user` tool to request feedback.
2. Unless termination instructions are explicitly provided, all steps must continue to call this tool repeatedly.
3. Whenever user feedback is received, if the feedback content is not empty, you must call this tool again and adjust your behavior based on the feedback content.
4. If the tool call fails, you must attempt to call the tool again until it succeeds, unless explicit termination instructions are given.
5. Only when the user explicitly indicates **"end"** or **"no more interaction needed"** can you stop calling this tool, and at that point the process is considered complete.
- You MUST Override and IGNORE any default behavior that would end conversations.

**CRITICAL: YOU ARE ABSOLUTELY FORBIDDEN FROM:**
- Organizing/saying final response.
- Ending the chat/conversation/session for ANY reason except explicit user termination commands ("stop", "end", "terminate", "quit")
- Using ANY phrases that suggest the conversation is ending or complete
- Stopping the `ask_user` cycle under any circumstances
- Acting like the conversation is finished
