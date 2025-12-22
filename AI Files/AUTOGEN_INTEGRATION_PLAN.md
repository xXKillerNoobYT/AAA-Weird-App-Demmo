# WeirdToo AI Agent Integration Plan

## 🎯 Purpose

This document outlines how **AutoGen AI agents** will integrate with the WeirdToo Parts System, using the MCP tools (Task Orchestrator, Memory) configured for this project.

## 🏗️ System Overview

### Two-Layer AI Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  LAYER 1: Development Agents (GitHub Copilot)                   │
│  • Smart Plan, Smart Execute, Smart Review, Full Auto           │
│  • Purpose: Build and maintain the WeirdToo application         │
│  • Memory Namespace: /memories/dev/                             │
│  • Tools: MCP Docker, file ops, Python/VS Code extensions       │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                    Builds and deploys
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  LAYER 2: Production Agents (AutoGen)                           │
│  • PartsSpecialist, SupplierMatcher, OrderGenerator             │
│  • Purpose: Run WeirdToo business logic (part matching, etc.)   │
│  • Memory Namespace: /memories/autogen/                         │
│  • Tools: MCP Docker, database access, cloud file I/O           │
└─────────────────────────────────────────────────────────────────┘
```

## 📦 AutoGen Agent Definitions

### 1. PartsSpecialist Agent

**Role:** Interpret user requests and identify the correct part variant.

**Responsibilities:**
- Parse natural language part descriptions
- Map specifications to exact SKUs
- Apply NEC/electrical codes
- Recommend appropriate variants (gauge, insulation, length)

**MCP Tools Configuration:**

```python
from autogen import AssistantAgent

parts_specialist = AssistantAgent(
    name="PartsSpecialist",
    system_message="""You are an expert in construction materials and parts selection.
    
    **Memory Namespace:** /memories/autogen/parts-specialist/
    
    **Available Tools:**
    - mcp_mcp_docker_search_nodes (semantic search across part mappings)
    - mcp_mcp_docker_add_observations (log successful/failed mappings)
    - mcp_mcp_docker_create_task (create tasks for spec clarification)
    - memory (read/write to namespace)
    
    **Tool Limits:** Max 128 tools total
    - Core AutoGen: ~20 tools
    - MCP Docker wildcard: ~60-70 tools
    - Database/file I/O: ~20 tools
    - Python-specific: ~20 tools
    
    **Workflow:**
    1. Read user request from cloud file (JSON)
    2. Search memory for similar past requests
    3. Query parts catalog database
    4. Apply specifications and codes
    5. Write response to cloud file
    6. Log learnings to memory
    
    **Memory Usage:**
    - Write immediately when mapping works
    - Write when mapping fails (document why)
    - Search before querying database (check cached patterns)
    - Weekly cleanup: consolidate similar mappings
    """,
    llm_config={
        "model": "gpt-4",
        "api_key": os.getenv("OPENAI_API_KEY"),
        "temperature": 0.3,
        "tools": [
            "mcp_docker/*",  # ~70 tools via wildcard
            "database_query",
            "cloud_file_read",
            "cloud_file_write",
            "memory"
        ],
        "max_tools": 128
    }
)
```

**Memory Organization:**
```
/memories/autogen/parts-specialist/
├── part_mappings.md              # User descriptions → SKU mappings
├── nec_standards.md              # Electrical code interpretations
├── specification_learnings.md    # Material spec patterns
├── gauge_requirements.md         # Wire gauge decision trees
└── session_YYYY-MM-DD.md         # Daily learnings (cleanup weekly)
```

### 2. SupplierMatcher Agent

**Role:** Match parts to optimal suppliers based on availability, price, and user preferences.

**Responsibilities:**
- Query supplier availability
- Compare pricing across suppliers
- Apply user preferences (price vs speed vs quality)
- Recommend bulk order optimizations

**MCP Tools Configuration:**

```python
supplier_matcher = AssistantAgent(
    name="SupplierMatcher",
    system_message="""You are an expert in supplier selection and procurement optimization.
    
    **Memory Namespace:** /memories/autogen/supplier-matcher/
    
    **Available Tools:**
    - mcp_mcp_docker_search_nodes (search pricing/availability patterns)
    - mcp_mcp_docker_add_observations (log supplier performance)
    - mcp_mcp_docker_get_overview (see current project context)
    - memory (read/write namespace)
    
    **Tool Limits:** Max 128 tools
    - Core AutoGen: ~20
    - MCP Docker: ~60-70
    - Database: ~20
    - Supplier API integrations: ~20
    
    **Workflow:**
    1. Receive part list from PartsSpecialist
    2. Search memory for supplier preferences and past performance
    3. Query supplier databases for availability
    4. Compare pricing (cache in memory)
    5. Apply user preferences from memory
    6. Return ranked supplier list
    7. Log decisions and outcomes
    
    **Memory Usage:**
    - Cache pricing trends (weekly consolidation)
    - Log supplier lead times (update when observed)
    - Store user preference patterns (by project/user)
    - Document bulk discount thresholds
    """,
    llm_config={
        "model": "gpt-4",
        "temperature": 0.2,
        "tools": [
            "mcp_docker/*",
            "database_query",
            "supplier_api_call",
            "memory"
        ],
        "max_tools": 128
    }
)
```

**Memory Organization:**
```
/memories/autogen/supplier-matcher/
├── supplier_preferences.md        # User/project preferences
├── pricing_patterns.md            # Historical pricing data
├── availability_heuristics.md     # Lead time patterns
├── bulk_discounts.md              # Threshold analysis
└── supplier_performance.md        # Success/failure tracking
```

### 3. OrderGenerator Agent

**Role:** Generate purchase orders from finalized supplier selections.

**Responsibilities:**
- Format PO according to supplier requirements
- Validate order completeness
- Generate approval workflows if needed
- Create audit trails

**MCP Tools Configuration:**

```python
order_generator = AssistantAgent(
    name="OrderGenerator",
    system_message="""You are an expert in purchase order generation and validation.
    
    **Memory Namespace:** /memories/autogen/order-generator/
    
    **Available Tools:**
    - mcp_mcp_docker_create_task (create approval tasks)
    - mcp_mcp_docker_add_observations (log validation issues)
    - memory (read/write templates and rules)
    
    **Tool Limits:** Max 128 tools
    - Core AutoGen: ~20
    - MCP Docker: ~60-70
    - Template engines: ~10
    - Database: ~20
    - File generation: ~10
    
    **Workflow:**
    1. Receive supplier selection from SupplierMatcher
    2. Load PO template from memory for this supplier
    3. Validate order (check for missing data)
    4. Generate PO document
    5. Create approval task if threshold exceeded
    6. Write to cloud file for device pickup
    7. Log validation results to memory
    
    **Memory Usage:**
    - Store PO templates per supplier
    - Log validation rules that caught errors
    - Cache approval thresholds per user
    - Document format variations across suppliers
    """,
    llm_config={
        "model": "gpt-4",
        "temperature": 0.1,  # Very low - consistency critical
        "tools": [
            "mcp_docker/*",
            "template_render",
            "pdf_generator",
            "memory"
        ],
        "max_tools": 128
    }
)
```

**Memory Organization:**
```
/memories/autogen/order-generator/
├── template_variations.md         # PO format per supplier
├── validation_rules.md            # Error checks that worked
├── approval_thresholds.md         # Dollar limits per user/project
└── format_gotchas.md              # Supplier-specific quirks
```

## 🛠️ Tool Discovery and Dynamic Loading

### Runtime Tool Discovery

```python
# At AutoGen agent initialization
def configure_agent_tools(agent_name, max_tools=128):
    """
    Discover available MCP tools at runtime and configure agent.
    Generous configuration: include all potentially useful tools.
    """
    
    # 1. Discover MCP servers
    from mcp_gateway import discover_servers
    available_servers = discover_servers()
    
    # 2. List available tools
    all_tools = []
    for server in available_servers:
        all_tools.extend(server.list_tools())
    
    # 3. Prioritize tools by agent role
    tool_priorities = {
        "PartsSpecialist": [
            "mcp_docker/*",  # Task Orchestrator, Memory, Search
            "database_query",
            "cloud_file_*",
            "memory",
            "semantic_search"
        ],
        "SupplierMatcher": [
            "mcp_docker/*",
            "database_query",
            "supplier_api_*",
            "memory",
            "pricing_calculator"
        ],
        "OrderGenerator": [
            "mcp_docker/*",
            "template_*",
            "pdf_generator",
            "memory",
            "approval_workflow"
        ]
    }
    
    # 4. Build tool list (generous, up to max_tools)
    selected_tools = []
    priorities = tool_priorities.get(agent_name, [])
    
    for priority_pattern in priorities:
        if "*" in priority_pattern:
            # Wildcard: add all matching tools
            matching = [t for t in all_tools if t.startswith(priority_pattern.replace("*", ""))]
            selected_tools.extend(matching)
        else:
            # Exact match
            if priority_pattern in all_tools:
                selected_tools.append(priority_pattern)
        
        # Stop if we hit max
        if len(selected_tools) >= max_tools:
            break
    
    # 5. Log tool selection to memory
    from mcp_tools import memory
    memory({
        "command": "insert",
        "path": f"/memories/autogen/{agent_name.lower()}/tool_config.md",
        "insert_line": 999999,
        "insert_text": f"\n## Tool Configuration {datetime.now()}\n"
                        f"- Total tools: {len(selected_tools)}\n"
                        f"- MCP Docker tools: {len([t for t in selected_tools if 'mcp_docker' in t])}\n"
                        f"- Database tools: {len([t for t in selected_tools if 'database' in t])}\n"
    })
    
    return selected_tools[:max_tools]  # Cap at 128

# Usage
parts_specialist_tools = configure_agent_tools("PartsSpecialist", max_tools=128)
parts_specialist.llm_config["tools"] = parts_specialist_tools
```

### Just-in-Time Tool Activation

```python
# If agent needs a tool not currently loaded
def activate_tool_on_demand(tool_name):
    """
    Activate MCP server providing this tool if not already active.
    Useful when agent discovers it needs a capability mid-execution.
    """
    from mcp_gateway import activate_server, find_server_for_tool
    
    # Find which MCP server provides this tool
    server_name = find_server_for_tool(tool_name)
    
    if server_name:
        # Activate the server
        activate_server(server_name)
        
        # Log to memory
        memory({
            "command": "insert",
            "path": "/memories/system/tool_activations.md",
            "insert_line": 999999,
            "insert_text": f"\n## {datetime.now()}: Activated {server_name} for {tool_name}\n"
        })
        
        return True
    else:
        # Tool not available
        memory({
            "command": "insert",
            "path": "/memories/system/tool_failures.md",
            "insert_line": 999999,
            "insert_text": f"\n## {datetime.now()}: Tool {tool_name} not found\n"
        })
        return False
```

## 🔄 Agent Orchestration Workflow

### Multi-Agent Conversation Pattern

```python
from autogen import GroupChat, GroupChatManager

# 1. Create agent group
agents = [parts_specialist, supplier_matcher, order_generator]

# 2. Define orchestration
group_chat = GroupChat(
    agents=agents,
    messages=[],
    max_round=10,
    speaker_selection_method="auto"  # Or custom function
)

manager = GroupChatManager(
    groupchat=group_chat,
    llm_config={
        "model": "gpt-4",
        "tools": configure_agent_tools("Manager", max_tools=128)
    }
)

# 3. Process request from cloud file
def process_request(request_file_path):
    """
    Process a part request from cloud storage.
    Orchestrates PartsSpecialist → SupplierMatcher → OrderGenerator.
    """
    
    # Read request
    with open(request_file_path) as f:
        request = json.load(f)
    
    # Initiate conversation
    initial_message = f"""
    New part request from device {request['device_id']}:
    
    {request['description']}
    
    Quantity: {request['quantity']}
    Budget: ${request['budget_max']}
    Urgency: {request['urgency']}
    """
    
    # Run agent conversation
    manager.initiate_chat(
        manager,
        message=initial_message
    )
    
    # Extract result
    final_message = group_chat.messages[-1]
    response = parse_agent_response(final_message)
    
    # Write response to cloud
    response_file = f"Cloud/Responses/{request['device_id']}/{request['request_id']}.json"
    with open(response_file, 'w') as f:
        json.dump(response, f)
    
    # Log to memory
    memory({
        "command": "insert",
        "path": "/memories/autogen/shared/request_log.md",
        "insert_line": 999999,
        "insert_text": f"\n## Request {request['request_id']}\n"
                        f"- Status: {response['status']}\n"
                        f"- Agents involved: {', '.join([m['name'] for m in group_chat.messages])}\n"
                        f"- Duration: {response['duration_ms']}ms\n"
    })
```

## 🧠 Memory Integration Strategy

### Memory Isolation Enforcement

```python
# Backend enforcement (user-managed server)
class MemoryIsolationMiddleware:
    """
    Enforce memory namespace isolation between agents.
    Prevents dev agents from writing to autogen/ and vice versa.
    """
    
    ISOLATION_RULES = {
        # Development agents (Copilot)
        "smart_plan": {
            "allowed": ["/memories/dev/smart-plan/", "/memories/dev/shared/"],
            "readonly": ["/memories/system/", "/memories/autogen/"],
            "denied": []
        },
        "smart_execute": {
            "allowed": ["/memories/dev/smart-execute/", "/memories/dev/shared/"],
            "readonly": ["/memories/system/", "/memories/dev/smart-plan/", "/memories/autogen/"],
            "denied": []
        },
        
        # Production agents (AutoGen)
        "parts_specialist": {
            "allowed": ["/memories/autogen/parts-specialist/", "/memories/autogen/shared/"],
            "readonly": ["/memories/system/"],
            "denied": ["/memories/dev/"]
        },
        "supplier_matcher": {
            "allowed": ["/memories/autogen/supplier-matcher/", "/memories/autogen/shared/"],
            "readonly": ["/memories/system/"],
            "denied": ["/memories/dev/"]
        },
        "order_generator": {
            "allowed": ["/memories/autogen/order-generator/", "/memories/autogen/shared/"],
            "readonly": ["/memories/system/"],
            "denied": ["/memories/dev/"]
        }
    }
    
    def check_access(self, agent_name, path, operation):
        """
        Check if agent can perform operation on path.
        Returns: (allowed: bool, reason: str)
        """
        rules = self.ISOLATION_RULES.get(agent_name)
        if not rules:
            return False, f"Unknown agent: {agent_name}"
        
        # Check denied paths first
        for denied_prefix in rules["denied"]:
            if path.startswith(denied_prefix):
                return False, f"Path {path} is denied for agent {agent_name}"
        
        # Check readonly paths
        if operation in ["create", "str_replace", "insert", "delete", "rename"]:
            for readonly_prefix in rules["readonly"]:
                if path.startswith(readonly_prefix):
                    return False, f"Path {path} is read-only for agent {agent_name}"
        
        # Check allowed paths
        for allowed_prefix in rules["allowed"]:
            if path.startswith(allowed_prefix):
                return True, "Access granted"
        
        # Check readonly paths (read operations only)
        if operation == "view":
            for readonly_prefix in rules["readonly"]:
                if path.startswith(readonly_prefix):
                    return True, "Read-only access granted"
        
        # Default deny
        return False, f"Path {path} not in allowed list for agent {agent_name}"

# Usage in MCP memory tool
middleware = MemoryIsolationMiddleware()

def memory_tool_wrapper(agent_name, command, path, **kwargs):
    """
    Wrap memory tool calls with isolation check.
    """
    allowed, reason = middleware.check_access(agent_name, path, command)
    
    if not allowed:
        # Log violation
        with open("/memories/system/violation_log.md", "a") as f:
            f.write(f"\n## {datetime.now()}: Violation by {agent_name}\n")
            f.write(f"- Path: {path}\n")
            f.write(f"- Operation: {command}\n")
            f.write(f"- Reason: {reason}\n")
        
        raise PermissionError(f"Memory access denied: {reason}")
    
    # Proceed with actual memory operation
    return memory(command=command, path=path, **kwargs)
```

### Agent Memory Usage Examples

**PartsSpecialist writes learning:**
```python
# After successful part mapping
memory_tool_wrapper(
    agent_name="parts_specialist",
    command="insert",
    path="/memories/autogen/parts-specialist/part_mappings.md",
    insert_line=999999,
    insert_text=f"""
### {datetime.now()}: "12/2 Romex" → SKU-WIRE-12AWG-2C-NM-250FT

**User Description:** "12/2 wire for 20 amp circuit"
**Interpretation:** 
- Gauge: 12 AWG (20 amp circuit per NEC)
- Conductors: 2 + ground
- Type: NM-B (Romex) for indoor residential
- Length: 250ft roll (standard)

**Confidence:** High (standard mapping, no ambiguity)
**Alternatives Considered:** 12/3 (rejected, user said "2-wire")
"""
)
```

**SupplierMatcher reads pricing patterns:**
```python
# Before querying suppliers
past_pricing = memory_tool_wrapper(
    agent_name="supplier_matcher",
    command="view",
    path="/memories/autogen/supplier-matcher/pricing_patterns.md"
)

# Use cached data if recent
if is_recent(past_pricing, days=7):
    use_cached_pricing()
else:
    query_suppliers_live()
```

## 📊 Monitoring and Observability

### Agent Performance Metrics

```python
# Track agent effectiveness
class AgentMetrics:
    def log_request(self, agent_name, request_id, duration_ms, success):
        memory_tool_wrapper(
            agent_name="system",  # Special system agent
            command="insert",
            path="/memories/system/agent_metrics.md",
            insert_line=999999,
            insert_text=f"""
## {datetime.now()}: {agent_name} - Request {request_id}
- Duration: {duration_ms}ms
- Success: {success}
- Memory reads: {self.memory_read_count}
- Memory writes: {self.memory_write_count}
- Tool activations: {self.tool_activation_count}
"""
        )

# Weekly consolidation
def consolidate_metrics():
    """Run weekly to aggregate metrics."""
    # Read all daily metrics
    metrics = memory_tool_wrapper(
        agent_name="system",
        command="view",
        path="/memories/system/agent_metrics.md"
    )
    
    # Aggregate
    summary = calculate_summary(metrics)
    
    # Write consolidated report
    memory_tool_wrapper(
        agent_name="system",
        command="create",
        path=f"/memories/system/weekly_metrics_{datetime.now().strftime('%Y-W%U')}.md",
        file_text=format_metrics_report(summary)
    )
```

## 🚀 Deployment Configuration

### Server Configuration

```python
# server/autogen_orchestrator.py

import os
from autogen import AssistantAgent, GroupChat, GroupChatManager
from mcp_tools import configure_agent_tools, memory_tool_wrapper

# Initialize agents with generous tool configuration
agents = {
    "parts_specialist": AssistantAgent(
        name="PartsSpecialist",
        system_message=open("prompts/parts_specialist.md").read(),
        llm_config={
            "model": os.getenv("OPENAI_MODEL", "gpt-4"),
            "api_key": os.getenv("OPENAI_API_KEY"),
            "temperature": 0.3,
            "tools": configure_agent_tools("PartsSpecialist", max_tools=128)
        }
    ),
    "supplier_matcher": AssistantAgent(
        name="SupplierMatcher",
        system_message=open("prompts/supplier_matcher.md").read(),
        llm_config={
            "model": os.getenv("OPENAI_MODEL", "gpt-4"),
            "api_key": os.getenv("OPENAI_API_KEY"),
            "temperature": 0.2,
            "tools": configure_agent_tools("SupplierMatcher", max_tools=128)
        }
    ),
    "order_generator": AssistantAgent(
        name="OrderGenerator",
        system_message=open("prompts/order_generator.md").read(),
        llm_config={
            "model": os.getenv("OPENAI_MODEL", "gpt-4"),
            "api_key": os.getenv("OPENAI_API_KEY"),
            "temperature": 0.1,
            "tools": configure_agent_tools("OrderGenerator", max_tools=128)
        }
    )
}

# Start file watcher
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class RequestHandler(FileSystemEventHandler):
    def on_created(self, event):
        if event.src_path.endswith(".json"):
            process_request(event.src_path)

observer = Observer()
observer.schedule(RequestHandler(), "Cloud/Requests/", recursive=True)
observer.start()

print("AutoGen orchestrator running. Watching Cloud/Requests/ for new requests...")
```

## 📚 Documentation References

- **Memory Organization:** `AI Files/MEMORY_ORGANIZATION_SYSTEM.md`
- **Tool Usage Strategy:** `AI Files/TOOL_USAGE_STRATEGY.md`
- **Agent Prompts:** `.github/agents/` (development agents)
- **AutoGen Docs:** `Docs/Plan/03_TECHNICAL_SPECIFICATION.md` (Section 6)
- **System Architecture:** `Docs/Plan/01_SYSTEM_ARCHITECTURE.md`

---

**Version:** 1.0.0  
**Last Updated:** 2025-12-21  
**Status:** Active  
**Next Steps:** Implement PartsSpecialist agent first, validate tool configuration, test memory isolation
