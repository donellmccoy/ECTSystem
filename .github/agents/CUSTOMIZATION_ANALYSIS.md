# ECTSystem-Specific Agent Customization Analysis

## Executive Summary

This document analyzes the potential for customizing the generic bug fix agents to be specifically tailored for the ECTSystem solution. The analysis examines current capabilities, available context, customization opportunities, and expected benefits.

## Current State vs. ECTSystem-Specific State

### Current Generic Agents

The existing agents are designed to work with **any** .NET/Azure DevOps project:

- Generic references to "your codebase", "the application", "relevant files"
- No pre-loaded knowledge of ECTSystem architecture
- Must discover project structure, patterns, and conventions during each session
- Requires explanations of ECTSystem-specific patterns (gRPC, Blazor, Aspire, etc.)

### Potential ECTSystem-Specific Agents

Customized agents would have built-in knowledge of:

- **Project Structure**: AF.ECT.Server, AF.ECT.WebClient, AF.ECT.WindowsClient, AF.ECT.Database, AF.ECT.AppHost, AF.ECT.Shared
- **Architecture Patterns**: gRPC services, Blazor WebAssembly, WinUI desktop, .NET Aspire orchestration
- **Key Technologies**: Entity Framework Core, Protobuf, SQL Server, Audit.NET
- **Conventions**: File-scoped namespaces, XML documentation, WorkflowClient/WorkflowServiceImpl patterns
- **Build System**: Tasks, solution structure, AppHost for orchestration
- **Domain Context**: ALOD (Army Lodging), Electronic Case Tracking, military workflows

## Available ECTSystem Context

From `.github/copilot-instructions.md` and workspace structure:

### Architecture Details
- **.NET Aspire Orchestration**: Service management via AF.ECT.AppHost/AppHost.cs
- **gRPC Communication**: Protobuf definitions in AF.ECT.Shared/Protos, services in AF.ECT.Server/Services
- **Client Technologies**: Blazor WASM (AF.ECT.WebClient), WinUI (AF.ECT.WindowsClient)
- **Data Layer**: EF Core with stored procedures (AF.ECT.Database), data access in AF.ECT.Data
- **Audit Logging**: Audit.NET for EF Core and gRPC operations with correlation IDs

### Key Conventions
- **Project Naming**: All prefixed `AF.ECT.*`
- **Async Patterns**: Methods use Async suffix, streaming methods end with Stream
- **Documentation**: XML comments required for all public APIs
- **Configuration**: Strongly-typed options classes with validation
- **Resilience**: Polly policies for retries/circuit breakers
- **Build Verification**: Always verify `dotnet build ECTSystem.sln` succeeds

### Critical Files
- `AF.ECT.AppHost/AppHost.cs` - Service orchestration
- `AF.ECT.Server/Program.cs` - Server setup
- `AF.ECT.Server/Extensions/ServiceCollectionExtensions.cs` - Audit.NET configuration
- `AF.ECT.Shared/Services/WorkflowClient.cs` - gRPC client wrapper
- `AF.ECT.Shared/Options/` - Configuration validation
- `AF.ECT.Database/dbo/Tables/` - SQL schemas

## Customization Opportunities by Agent

### 1. Bug Fix Planning Agent

**Current Generic Guidance:**
```markdown
Review the bug report and gather context:
- Read the bug description
- Examine error logs and stack traces
- Identify affected components
```

**ECTSystem-Specific Enhancement:**
```markdown
Review the Azure DevOps bug work item and gather context:
- Read the bug description and acceptance criteria
- Examine Application Insights logs (via AppLens) for correlation IDs
- Identify affected components:
  - gRPC service in AF.ECT.Server/Services/WorkflowServiceImpl.cs
  - Client calls in AF.ECT.Shared/Services/WorkflowClient.cs
  - Protobuf contracts in AF.ECT.Shared/Protos/workflow.proto
  - Database layer in AF.ECT.Data/ or stored procedures in AF.ECT.Database/
- Check .NET Aspire dashboard (http://localhost:15888) for service health
- Verify audit logs in AuditLogs table for operation traces
```

**Benefits:**
- Saves time explaining ECTSystem architecture
- Automatically checks the right places (AppHost dashboard, correlation IDs, specific paths)
- Understands gRPC client/server separation

### 2. Root Cause Analysis Agent

**Current Generic Guidance:**
```markdown
Analyze code paths and dependencies
Use search tools to find relevant code
```

**ECTSystem-Specific Enhancement:**
```markdown
Analyze ECTSystem-specific code paths:
- **gRPC Flow**: WorkflowClient → gRPC-Web → WorkflowServiceImpl
- **Data Flow**: WorkflowServiceImpl → EF Core DbContext → Stored Procedures
- **Audit Trail**: Check AuditLogs table for operation with correlation ID
- **Key Files to Examine**:
  - AF.ECT.Shared/Protos/*.proto (contract definitions)
  - AF.ECT.Server/Services/*ServiceImpl.cs (server implementation)
  - AF.ECT.Shared/Services/*Client.cs (client wrapper)
  - AF.ECT.Data/*Repository.cs (data access)
  - AF.ECT.Database/usp/*.sql (stored procedures)
- **5 Whys Template** pre-filled with ECTSystem context:
  - Why did the gRPC call fail?
  - Why was the protobuf message malformed?
  - Why did the EF Core query timeout?
```

**Benefits:**
- Knows to check both client and server sides of gRPC calls
- Understands the audit trail via correlation IDs
- Familiar with stored procedure usage patterns

### 3. Bug Fix Design Agent

**Current Generic Guidance:**
```markdown
Design a solution that minimizes risk
Consider hotfix vs. proper fix
```

**ECTSystem-Specific Enhancement:**
```markdown
Design a solution following ECTSystem patterns:
- **For gRPC Issues**:
  - Update protobuf definition in AF.ECT.Shared/Protos/
  - Regenerate code via build (automatic)
  - Update both WorkflowServiceImpl and WorkflowClient
  - Add XML documentation comments
- **For Database Issues**:
  - Modify stored procedure in AF.ECT.Database/usp/
  - Update EF Core migration if schema changes
  - Test via SQL Server connection string in appsettings
- **Deployment Strategy**:
  - Hotfix: Update AF.ECT.Server only (faster)
  - Proper fix: Update protobuf + regenerate + full deployment
  - Test via .NET Aspire: `dotnet run` in AF.ECT.AppHost
- **Audit Requirements**:
  - Ensure correlation IDs flow through fix
  - Verify Audit.NET captures operation
```

**Benefits:**
- Knows gRPC contract changes require client/server coordination
- Understands stored procedure deployment process
- Aware of Aspire-based testing workflow

### 4. Bug Fix Implementation Agent

**Current Generic Guidance:**
```markdown
Implement the fix with precision
Write clean, maintainable code
```

**ECTSystem-Specific Enhancement:**
```markdown
Implement the fix following ECTSystem conventions:
- **Code Style**:
  - Use file-scoped namespaces
  - Add XML documentation comments (required)
  - Async methods end with Async suffix
  - Streaming methods end with Stream
- **gRPC Implementation**:
  - Update AF.ECT.Shared/Protos/*.proto first
  - Implement in AF.ECT.Server/Services/*ServiceImpl.cs
  - Update AF.ECT.Shared/Services/*Client.cs wrapper
  - Add AuditScope.Create() for new operations
- **Configuration Changes**:
  - Use strongly-typed options in AF.ECT.Shared/Options/
  - Add DataAnnotations validation
  - Update appsettings.json/Development.json/Production.json
- **Build Verification** (CRITICAL):
  - Run `dotnet build ECTSystem.sln` before responding
  - Fix errors immediately if build fails
```

**Benefits:**
- Automatically follows project conventions
- Knows to update both proto and implementation
- Understands audit logging requirements

### 5. Regression Testing Agent

**Current Generic Guidance:**
```markdown
Create tests to verify the fix
Ensure the bug doesn't recur
```

**ECTSystem-Specific Enhancement:**
```markdown
Create regression tests following ECTSystem test patterns:
- **Test Location**: AF.ECT.Tests/ (xUnit framework)
- **Test Patterns**:
  - Use builders in AF.ECT.Tests/Builders/
  - Use fixtures in AF.ECT.Tests/Fixtures/
  - Follow guidelines in COVERAGE_GUIDELINES.md
- **gRPC Testing**:
  - Mock WorkflowServiceImpl for server tests
  - Test WorkflowClient for client tests
  - Verify protobuf serialization/deserialization
- **Database Testing**:
  - Use tSQLt framework (AF.ECT.Database/tsqlt/)
  - Test stored procedures directly
  - Verify EF Core mappings
- **Run Tests**: `dotnet test` from solution root
- **Coverage**: Check coverlet.runsettings for thresholds
```

**Benefits:**
- Knows existing test infrastructure (builders, fixtures, tSQLt)
- Understands gRPC testing requires mocking
- Aware of coverage requirements

### 6. Hotfix Deployment Agent

**Current Generic Guidance:**
```markdown
Deploy the fix to production
Monitor for issues
```

**ECTSystem-Specific Enhancement:**
```markdown
Deploy the fix following ECTSystem deployment procedures:
- **Pre-Deployment**:
  - Verify `dotnet build ECTSystem.sln` succeeds
  - Run `dotnet test` (all tests pass)
  - Check .NET Aspire dashboard for service health
- **Deployment Targets**:
  - AF.ECT.Server (primary deployment)
  - AF.ECT.WebClient (if Blazor changes)
  - AF.ECT.WindowsClient (if WinUI changes)
  - AF.ECT.Database (if schema/stored procedure changes)
- **Azure DevOps Pipeline**:
  - Use existing YAML pipelines
  - Link deployment to bug work item
  - Tag release appropriately
- **Rollback Plan**:
  - .NET Aspire allows per-service rollback
  - Database changes require migration rollback
```

**Benefits:**
- Knows which projects need deployment for different fix types
- Understands Aspire-based deployment model
- Aware of database migration considerations

### 7. Post-Fix Monitoring Agent

**Current Generic Guidance:**
```markdown
Monitor the deployed fix
Verify effectiveness
```

**ECTSystem-Specific Enhancement:**
```markdown
Monitor the fix using ECTSystem observability tools:
- **Application Insights** (via AppLens):
  - Search for correlation IDs from bug report
  - Check exception counts decreased
  - Verify operation duration improved
- **Audit Logs**:
  - Query AuditLogs table for operation success rate
  - Verify correlation IDs flow correctly
  - Check for new error patterns
- **.NET Aspire Dashboard** (http://localhost:15888):
  - Monitor service health checks
  - Check OpenTelemetry traces
  - Verify no new errors in logs
- **SQL Server**:
  - Check for slow queries (if database fix)
  - Verify stored procedure performance
- **Metrics to Track**:
  - gRPC call success rate
  - Operation duration (P50, P95, P99)
  - Database query performance
  - Exception rate
```

**Benefits:**
- Knows exactly where to look (AppLens, Aspire dashboard, AuditLogs)
- Understands correlation ID tracing
- Familiar with OpenTelemetry integration

## Quantified Benefits of ECTSystem-Specific Customization

### Time Savings
- **30-50% reduction** in context switching between agent and copilot-instructions.md
- **Faster bug fixes** due to pre-loaded knowledge of architecture
- **Fewer clarifying questions** about project structure

### Quality Improvements
- **Better suggestions** that follow ECTSystem conventions automatically
- **Reduced errors** from incorrect assumptions about architecture
- **Consistent patterns** across all bug fixes

### Developer Experience
- **Lower cognitive load** - agent knows the system
- **More relevant examples** - uses actual ECTSystem file paths
- **Domain-aware language** - understands ALOD, ECT, WorkflowClient, etc.

## Implementation Effort

### Low-Medium Effort
Each agent would need:
1. **Updated description** with ECTSystem context (~5 minutes per agent)
2. **Enhanced instructions** using specific file paths and patterns (~10-15 minutes per agent)
3. **ECTSystem-specific examples** in templates (~5 minutes per agent)

**Total Estimated Time**: 2-3 hours for all 7 agents

### Maintenance Considerations
- Agents need updates when ECTSystem architecture changes significantly
- Should reference `.github/copilot-instructions.md` for source of truth
- Consider adding version numbers to track updates

## Recommendation

### Option 1: Full ECTSystem-Specific Customization
**Pros:**
- Maximum efficiency for ECTSystem bug fixes
- Agents become true ECTSystem experts
- Significant time savings over time

**Cons:**
- Agents only work for ECTSystem (not reusable)
- Requires maintenance when architecture changes
- Initial investment of 2-3 hours

### Option 2: Hybrid Approach
**Pros:**
- Keep generic core, add ECTSystem examples
- More maintainable
- Easier to adapt for other projects

**Cons:**
- Less efficiency than full customization
- Still requires some context switching

### Option 3: No Customization (Current State)
**Pros:**
- Works now without changes
- Reusable for other projects
- Zero maintenance overhead

**Cons:**
- Requires more manual context in each session
- Slower bug fixes
- More potential for convention violations

## Conclusion

**Yes, these agents can be significantly more focused and customized for ECTSystem.**

The customization would provide substantial benefits in:
- **Developer efficiency** (30-50% time savings on context)
- **Fix quality** (automatic adherence to conventions)
- **Consistency** (all fixes follow same patterns)

The implementation effort is relatively low (2-3 hours) compared to the ongoing benefits, especially for a project in active development with frequent bug fixes.

**Recommended Next Step**: Implement Option 1 (Full ECTSystem-Specific Customization) for all 7 agents, starting with the most commonly used agents (Bug Fix Planning, Root Cause Analysis, Bug Fix Implementation).
