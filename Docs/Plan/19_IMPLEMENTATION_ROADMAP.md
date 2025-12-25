# Implementation Roadmap - CloudWatcher Platform

**Task D6**: Create Implementation Guidance Document
**Status**: Active - Implementation guide for development teams
**Date**: December 24, 2025

---

## Executive Summary

This roadmap bridges **specification** (Tasks 4-6) and **implementation** (code development). Follow this guide to execute the 21 architecture documents and transform them into working code.

**Implementation Order**: Database → API → Testing → Integration
**Estimated Timeline**: 12-16 weeks (3-4 developers)
**Critical Dependencies**: .NET SDK 9.0, PostgreSQL 15+, Python 3.12+

---

## Phase 1: Database Implementation (Weeks 1-2)

### 1.1 Environment Setup

**Prerequisites**:
```powershell
# Install PostgreSQL 15+
winget install PostgreSQL.PostgreSQL

# Verify installation
psql --version  # Should show 15.x or higher

# Install Entity Framework Core tools
dotnet tool install --global dotnet-ef --version 9.0.0
```

**Database Creation**:
```sql
-- Connect to PostgreSQL as admin
psql -U postgres

-- Create database and user
CREATE DATABASE cloudwatcher_dev;
CREATE USER cloudwatcher_user WITH ENCRYPTED PASSWORD 'dev_password_here';
GRANT ALL PRIVILEGES ON DATABASE cloudwatcher_dev TO cloudwatcher_user;

-- Enable required extensions
\c cloudwatcher_dev
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";  -- For text search
```

### 1.2 Entity Framework Migration Commands

**Reference**: [16_DATABASE_SCHEMA.md](./16_DATABASE_SCHEMA.md) (910 lines, 20+ tables)

**Step 1: Create EF Core DbContext**
```bash
cd server/CloudWatcher

# Create DbContext with entities
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# Review generated migration in Data/Migrations/
# Verify it matches 16_DATABASE_SCHEMA.md specifications
```

**Step 2: Apply Migrations**
```bash
# Apply to development database
dotnet ef database update

# Verify tables created
psql -U cloudwatcher_user -d cloudwatcher_dev -c "\dt"
# Should show all tables from schema diagram
```

**Step 3: Seed Test Data**
```bash
# Run seeding script
dotnet run --seed-data

# Verify data
psql -U cloudwatcher_user -d cloudwatcher_dev -c "SELECT COUNT(*) FROM users;"
psql -U cloudwatcher_user -d cloudwatcher_dev -c "SELECT COUNT(*) FROM parts;"
```

**Troubleshooting**:
- **"dotnet: command not found"** → Install .NET SDK 9.0 (see setup.bat)
- **"Connection refused"** → Start PostgreSQL service: `net start postgresql-x64-15`
- **"Migration already applied"** → Rollback: `dotnet ef database update 0`, then reapply

---

## Phase 2: API Implementation (Weeks 3-6)

### 2.1 API Project Scaffolding

**Reference**: [17_API_ENDPOINTS.md](./17_API_ENDPOINTS.md) (1005 lines, 25+ endpoints)

**Step 1: Create API Controllers from OpenAPI Spec**
```bash
# Install OpenAPI code generator
dotnet tool install --global Microsoft.dotnet-openapi --version 9.0.0

# Generate controllers (if OpenAPI spec exists)
dotnet openapi add file Docs/api-spec.yaml

# Or create manually from 17_API_ENDPOINTS.md templates
# Example: UsersController, PartsController, RequestsController
```

**Step 2: Implement Core Controllers**

**Create `Controllers/UsersController.cs`**:
```csharp
using Microsoft.AspNetCore.Mvc;
using CloudWatcher.Data;
using CloudWatcher.Models;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly CloudWatcherContext _context;
    
    public UsersController(CloudWatcherContext context)
    {
        _context = context;
    }
    
    // GET /api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }
    
    // Additional endpoints from 17_API_ENDPOINTS.md
}
```

**Step 3: Implement Authentication Middleware**

**Add to `Program.cs`**:
```csharp
// OAuth2 authentication (Azure AD / Google)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => 
        policy.RequireRole("admin"));
});
```

**Configuration in `appsettings.json`**:
```json
{
  "Auth": {
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "Audience": "api://cloudwatcher-api",
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-secret-here"
  }
}
```

### 2.2 API Testing During Development

**Manual Testing with curl**:
```bash
# Test health endpoint
curl http://localhost:5000/health

# Test users endpoint (with auth token)
$token = "Bearer YOUR_JWT_TOKEN_HERE"
curl -H "Authorization: $token" http://localhost:5000/api/users

# Test request submission
curl -X POST http://localhost:5000/api/requests \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "truck-001",
    "requestType": "get_parts",
    "payload": {"partCode": "P-12345"}
  }'
```

**Automated API Tests** (see Phase 3):
```bash
# Run integration tests
dotnet test --filter Category=Integration
```

---

## Phase 3: Testing Framework Implementation (Weeks 7-9)

### 3.1 Test Project Initialization

**Reference**: [18_UNIT_TESTING_FRAMEWORK.md](./18_UNIT_TESTING_FRAMEWORK.md) (941 lines)

**Step 1: Create Test Projects**
```bash
# .NET unit tests (xUnit)
dotnet new xunit -n CloudWatcher.Tests.Unit
dotnet sln add CloudWatcher.Tests.Unit/CloudWatcher.Tests.Unit.csproj

# .NET integration tests
dotnet new xunit -n CloudWatcher.Tests.Integration
dotnet sln add CloudWatcher.Tests.Integration/CloudWatcher.Tests.Integration.csproj

# Python tests (pytest)
cd device/python
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install pytest pytest-asyncio pytest-cov
```

**Step 2: Configure Test Dependencies**

**CloudWatcher.Tests.Unit.csproj**:
```xml
<ItemGroup>
  <PackageReference Include="xUnit" Version="2.9.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="FluentAssertions" Version="7.0.0" />
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
</ItemGroup>
```

**Python `requirements-test.txt`**:
```
pytest==8.3.4
pytest-asyncio==0.24.0
pytest-cov==6.0.0
pytest-mock==3.14.0
httpx==0.28.1  # For API client testing
```

### 3.2 Test Execution Workflow

**Unit Tests** (run frequently during development):
```bash
# .NET unit tests
dotnet test CloudWatcher.Tests.Unit --logger "console;verbosity=detailed"

# Python unit tests
cd device/python
pytest tests/unit/ -v --cov=.

# Watch mode for continuous testing (TDD)
dotnet watch test  # .NET
pytest-watch       # Python
```

**Integration Tests** (run before commits):
```bash
# .NET integration tests (require running database)
dotnet test CloudWatcher.Tests.Integration --filter Category=Integration

# Python integration tests
pytest tests/integration/ -v --tb=short
```

**End-to-End Tests** (run before releases):
```bash
# Install Playwright
npm install -D @playwright/test
npx playwright install

# Run E2E tests
npx playwright test

# With UI for debugging
npx playwright test --ui
```

### 3.3 Test Coverage Targets

**From 18_UNIT_TESTING_FRAMEWORK.md**:
- **Unit Tests**: 80%+ coverage on business logic
- **Integration Tests**: All API endpoints, critical database operations
- **E2E Tests**: Core user workflows (request submission, approval, order generation)

**Generate Coverage Report**:
```bash
# .NET coverage
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"

# Python coverage
pytest --cov=. --cov-report=html
# Open htmlcov/index.html
```

---

## Phase 4: Integration & Deployment (Weeks 10-12)

### 4.1 Cloud Storage Integration

**Reference**: Task D7 (Cloud Storage Integration feature)

**Setup SharePoint OAuth2**:
```bash
# Register app in Azure AD portal
# Get client_id, client_secret, tenant_id

# Configure in appsettings.json
{
  "CloudStorage": {
    "Provider": "SharePoint",
    "SharePoint": {
      "TenantId": "your-tenant-id",
      "ClientId": "your-client-id",
      "ClientSecret": "your-secret",
      "SiteUrl": "https://yourtenant.sharepoint.com/sites/CloudWatcher"
    }
  }
}
```

**Test Cloud Upload**:
```bash
# Test request file upload
dotnet run --cloud-test

# Verify in SharePoint: /Cloud/Requests/truck-001/
```

### 4.2 File Watcher Service

**Already Implemented**: See [server/CloudWatcher/Program.cs](../../server/CloudWatcher/Program.cs)

**Verify Functionality**:
```bash
# Start CloudWatcher service
cd server/CloudWatcher
dotnet run

# In another terminal, create test request
cd Cloud/Requests/truck-001
echo '{"request_type": "get_parts", "request_id": "test-001"}' > req-test-001.json

# Watch CloudWatcher console output:
# - Should detect new file
# - Process request
# - Create response in Cloud/Responses/truck-001/
```

### 4.3 CI/CD Pipeline

**GitHub Actions Workflow** (`.github/workflows/ci.yml`):
```yaml
name: CI Pipeline

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_USER: cloudwatcher_user
          POSTGRES_PASSWORD: test_password
          POSTGRES_DB: cloudwatcher_test
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Run unit tests
        run: dotnet test --filter Category=Unit --no-build
      - name: Run integration tests
        run: dotnet test --filter Category=Integration --no-build
```

---

## Phase 5: AI Orchestration (Weeks 13-16)

### 5.1 Microsoft Autogen Setup

**Reference**: Task D10 (AI Orchestration feature)

**Install Autogen Framework**:
```bash
# Python environment
cd server/ai-agents
python -m venv .venv
.venv\Scripts\Activate.ps1

pip install pyautogen==0.2.35
pip install openai==1.57.4
pip install azure-identity==1.19.0
```

**Configure AI Agents**:
```python
# config/autogen_config.json
{
  "llm_config": {
    "model": "gpt-4o",
    "api_key": "your-openai-api-key",
    "temperature": 0.0
  },
  "agents": {
    "RequestRouter": {
      "system_message": "Route requests to approvers based on business rules"
    },
    "PartsSpecialist": {
      "system_message": "Help users select correct part variants"
    }
  }
}
```

### 5.2 Agent Integration Testing

```bash
# Test RequestRouter agent
python -m ai_agents.test_request_router

# Test full workflow
python -m ai_agents.test_approval_workflow
```

---

## Dependency Installation Checklist

### Server (.NET)
- [ ] .NET SDK 9.0 installed (`dotnet --version`)
- [ ] PostgreSQL 15+ installed and running
- [ ] Entity Framework Core tools installed (`dotnet ef`)
- [ ] CloudWatcher project restored (`dotnet restore`)

### Mobile/Device (Python)
- [ ] Python 3.12+ installed (`python --version`)
- [ ] Virtual environment created (`.venv`)
- [ ] Dependencies installed (`pip install -r requirements.txt`)

### Testing
- [ ] xUnit packages installed (.NET)
- [ ] pytest installed (Python)
- [ ] Playwright installed (E2E)

### Cloud Integration
- [ ] Azure AD app registered (OAuth2)
- [ ] SharePoint site created and accessible
- [ ] Google Drive API credentials (optional)

---

## Common Setup Issues & Solutions

### Issue 1: "dotnet: command not found"
**Solution**:
```powershell
# Run setup script
.\setup.bat

# Or manually install
winget install Microsoft.DotNet.SDK.9

# Verify PATH
$env:PATH -split ';' | Select-String 'dotnet'
```

### Issue 2: PostgreSQL Connection Failed
**Solution**:
```powershell
# Check PostgreSQL service
Get-Service postgresql*

# Start if stopped
net start postgresql-x64-15

# Test connection
psql -U postgres -c "SELECT version();"
```

### Issue 3: EF Migrations Not Applying
**Solution**:
```bash
# Check migration status
dotnet ef migrations list

# Remove failed migration
dotnet ef migrations remove

# Recreate and apply
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Issue 4: Python venv Activation Issues
**Solution**:
```powershell
# Allow script execution
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned

# Activate venv
.venv\Scripts\Activate.ps1

# Verify
python -c "import sys; print(sys.prefix)"
```

---

## Implementation Progress Tracking

### Week 1-2: Database ✅
- [ ] PostgreSQL installed and configured
- [ ] EF Core migrations created and applied
- [ ] All 20+ tables created successfully
- [ ] Test data seeded
- [ ] Database indexes verified

### Week 3-6: API ⏳
- [ ] API project structure created
- [ ] Core controllers implemented (Users, Parts, Requests)
- [ ] OAuth2 authentication configured
- [ ] Validation and error handling added
- [ ] API documentation (Swagger) generated

### Week 7-9: Testing ⏳
- [ ] Unit test projects created
- [ ] Integration test projects created
- [ ] Test coverage >80% achieved
- [ ] E2E tests implemented with Playwright
- [ ] CI pipeline configured

### Week 10-12: Integration ⏳
- [ ] Cloud storage integration complete
- [ ] File watcher service validated
- [ ] Request/response protocol tested end-to-end
- [ ] Mobile app connected and tested

### Week 13-16: AI Orchestration ⏳
- [ ] Autogen framework installed
- [ ] AI agents implemented (RequestRouter, PartsSpecialist, etc.)
- [ ] Agent testing completed
- [ ] Full workflow validated

---

## Next Steps

1. **Review Architecture Docs**: Read [16_DATABASE_SCHEMA.md](./16_DATABASE_SCHEMA.md), [17_API_ENDPOINTS.md](./17_API_ENDPOINTS.md), [18_UNIT_TESTING_FRAMEWORK.md](./18_UNIT_TESTING_FRAMEWORK.md)

2. **Set Up Development Environment**: Follow Phase 1 instructions above

3. **Start Database Implementation**: Execute EF migrations from 16_DATABASE_SCHEMA.md

4. **Build API Layer**: Implement controllers from 17_API_ENDPOINTS.md specifications

5. **Write Tests**: Follow testing pyramid from 18_UNIT_TESTING_FRAMEWORK.md

6. **Integrate Components**: Connect database → API → file watcher → cloud storage

7. **Deploy MVP**: Follow CI/CD pipeline for staging deployment

---

## References

- [16_DATABASE_SCHEMA.md](./16_DATABASE_SCHEMA.md) - 910 lines, 20+ tables, 42+ indexes
- [17_API_ENDPOINTS.md](./17_API_ENDPOINTS.md) - 1005 lines, 25+ endpoints
- [18_UNIT_TESTING_FRAMEWORK.md](./18_UNIT_TESTING_FRAMEWORK.md) - 941 lines, testing pyramid
- [BACKLOG.md](../TODO/BACKLOG.md) - 31 implementation tasks across 5 features
- [02_DESIGN_DOCUMENT.md](./02_DESIGN_DOCUMENT.md) - System design and constraints
- [03_TECHNICAL_SPECIFICATION.md](./03_TECHNICAL_SPECIFICATION.md) - Technology choices

---

**Document Status**: ACTIVE - Use this as primary implementation guide
**Last Updated**: December 24, 2025
**Maintained By**: Development Team
