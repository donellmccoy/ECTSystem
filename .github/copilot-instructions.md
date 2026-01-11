# AI Coding Assistant Instructions for ECTSystem

## CRITICAL: Microsoft Best Patterns and Practices
**ALWAYS use Microsoft Best Patterns and Practices as the basis for all recommendations, architectural decisions, and code suggestions.**

- Follow official Microsoft guidance from Microsoft Learn, Azure Architecture Center, and .NET documentation
- Adhere to established patterns for .NET, ASP.NET Core, Entity Framework Core, and Azure services
- Apply Microsoft's recommended practices for security, performance, scalability, and maintainability
- Reference Microsoft's best practices for cloud-native applications, microservices, and distributed systems
- Consult Microsoft's guidance for specific technologies used in this project (gRPC, Blazor, .NET Aspire, etc.)

## CRITICAL: Build Verification Requirement
**ALWAYS verify the solution builds successfully before responding to any code changes. Do not rebuild solution if markdown are changed**

- After making ANY code modifications, run `dotnet build ECTSystem.sln` and wait for completion
- Only respond after confirming the build succeeds (exit code 0)
- If build fails, fix errors before providing any analysis or next steps
- Never assume builds will succeed - always verify explicitly

## Overview
ECTSystem is an Electronic Case Tracking application for ALOD (Army Lodging) built with .NET 9.0, ASP.NET Core, Blazor WebAssembly, Win UI (desktop), .NET Aspire orchestration, gRPC services, Entity Framework Core, and SQL Server. It manages case workflows, user management, and reporting in a distributed microservices architecture.

## Architecture
- **Orchestration**: .NET Aspire manages service discovery, health checks, and observability via `AF.ECT.AppHost/AppHost.cs`.
- **Client**: Blazor WASM in `AF.ECT.WebClient` and Win UI desktop in `AF.ECT.WindowsClient` communicate with server via gRPC-Web.
- **Server**: ASP.NET Core API in `AF.ECT.Server` exposes gRPC services with JSON transcoding for REST-like endpoints.
- **Data**: EF Core with stored procedures in `AF.ECT.Database` connects to SQL Server; data access layer in `AF.ECT.Data`.
- **Shared**: Protobuf definitions in `AF.ECT.Shared/Protos` for gRPC contracts.
- **Communication**: Client uses `WorkflowClient` for gRPC calls; server implements `WorkflowServiceImpl`.

## Development Workflow
- **Build**: `dotnet build ECTSystem.sln` (uses tasks.json for automation).
- **Run**: `dotnet run` in `AF.ECT.AppHost` launches Aspire dashboard at http://localhost:15888.
- **Debug**: Use launchSettings.json profiles; attach debugger to processes.
- **Test**: `dotnet test` runs xUnit tests in `AF.ECT.Tests`.
- **Package Updates**: `dotnet list ECTSystem.sln package --outdated` to check for outdated NuGet packages.
- **Database**: Migrations via EF Core; stored procedures for complex queries.


## Conventions and Patterns
- **Naming**: Projects prefixed `AF.ECT.*`; methods use Async suffix; streaming methods end with `Stream`.
- **gRPC**: Unary for single responses, streaming for large datasets (e.g., `GetUsersOnlineStreamAsync`).
- **Configuration Validation**: Use strongly-typed options classes with data annotations for all configuration settings. Validate on startup using `ValidateDataAnnotations().ValidateOnStart()`. Options classes in `AF.ECT.Shared/Options` with `[Required]`, `[Range]`, `[Url]`, etc.
- **Audit Logging**: Client-side audit logging with correlation IDs for end-to-end traceability; implemented in `WorkflowClient` methods for military-grade compliance.
- **Resilience**: Polly policies for retries/circuit breakers in `ServiceDefaults`.
- **Security**: Antiforgery tokens in Blazor forms; CORS restricted to trusted origins.
- **Observability**: OpenTelemetry integrated; logs via Serilog; health checks at `/healthz`; structured audit events with performance metrics.
- **Injection**: Dependency injection everywhere; configure in `Program.cs`.
- **Code Refactoring**: Follow SOLID principles; use partial classes and regions for organization. Use static methods and extension methods to keep code clean.
- **Styling**: Inline temporary variables wherever possible; use block body for lambda expressions.
- **File-Scoped Namespaces**: Use file-scoped namespaces for all C# files throughout the solution to improve readability and reduce indentation.
- **Usings Cleanup**: Remove unused and duplicate `using` statements in all files to keep code clean and maintainable.
- **Project Versioning**: All projects added to the solution must target at least .NET 9.0 to maintain compatibility with the global.json specification.

## Audit Logging Implementation
- **Automated Auditing**: Implemented Audit.NET for comprehensive auditing of Entity Framework changes and gRPC operations.
- **EF Core Auditing**: Automatic audit logging for all database operations using Audit.EntityFramework.Core with SQL Server storage.
- **gRPC Auditing**: Client-side gRPC calls audited via AuditScope.Create() with correlation IDs, performance metrics, and structured events.
- **Correlation IDs**: Generated per operation for linking client and server audit trails using `GenerateCorrelationId()`.
- **Audit Events**: Stored in AuditLogs table with EventType ("EF:{entity}" or "gRPC:{method}"), timestamps, duration, success/failure status, and parameter data.
- **Performance Metrics**: Automatic timing for all operations.
- **Structured Logging**: Audit events include timestamp, correlation ID, method name, duration, success status, error messages, and additional context.
- **Coverage**: Applied to all EF operations and unary gRPC methods; streaming methods excluded as they are not single operations.
- **Compliance**: Supports military-grade observability and end-to-end traceability requirements with automated audit trails.

## Examples
- Add gRPC method: Define in `workflow.proto`, implement in `WorkflowServiceImpl.cs`, call via `WorkflowClient`.
- Database query: Use EF Core context with stored procedures like `GetWorkflowById`.
- Client component: In `AF.ECT.WebClient/Pages`, inject `IWorkflowClient` for data.

## Key Files
- `AF.ECT.AppHost/AppHost.cs`: Service orchestration.
- `AF.ECT.Server/Program.cs`: Server setup with gRPC.
- `AF.ECT.Server/Extensions/ServiceCollectionExtensions.cs`: Audit.NET configuration for EF Core and SQL Server storage.
- `AF.ECT.Shared/Services/WorkflowClient.cs`: Shared gRPC client wrapper with Audit.NET audit logging and correlation IDs.
- `AF.ECT.Shared/Options/`: Strongly-typed configuration options with validation (DatabaseOptions, CorsOptions, ServerOptions, WorkflowClientOptions).
- `AF.ECT.WindowsClient/`: Win UI desktop application.
- `AF.ECT.Database/dbo/Tables/`: SQL schemas.
- `AF.ECT.Data/`: Data access layer.
- `Documentation/`: Architectural and REST guidelines.

## Documentation
- Use XML documentation comments for all methods, classes, properties and fields to enable IntelliSense and API documentation.
- Format: `/// <summary>Description</summary>` for summaries, `<param name="param">Description</param>` for parameters, `<returns>Description</returns>` for return values, `<exception cref="ExceptionType">Description</exception>` for exceptions.
- Enable XML documentation generation in project files: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.
- Configuration: Use strongly-typed options classes with data annotations; validate on startup with `ValidateDataAnnotations().ValidateOnStart()`.
- Example: See `AF.ECT.WebClient/Services/WorkflowClient.cs` for extensive XML comments on gRPC client methods.

Focus on gRPC-first design, Aspire for cloud readiness, and military-specific workflows. Avoid generic patterns; follow existing protobuf and EF conventions.

### AI Agent Instructions: Ensuring C# Compilation via Dependency Tree Analysis

**Objective**: Analyze and resolve type dependency issues in a C# project to ensure error-free compilation. Focus on dependency trees (graphs of project references, NuGet packages, and transitive dependencies) to detect missing types, version conflicts, circular references, or incompatibilities.

**Prerequisites**:
- Access to .NET CLI (e.g., `dotnet` commands).
- Project in SDK-style .csproj format.
- Tools: IDE (Visual Studio/VS Code), NuGet Package Manager, optional analyzers (e.g., NetArchTest).

**Step-by-Step Process**:

1. **Restore Dependencies**:
   - Run `dotnet restore` to fetch all packages and build the initial dependency tree.
   - Output: Locks file (packages.lock.json) if enabled; enables deterministic restores.

2. **Generate Dependency Tree**:
   - Use `dotnet list package --include-transitive` to output the full dependency tree (top-level and transitive packages).
   - For visual graph: Install dotnet-graph (via NuGet) and run `dotnet graph --project YourProject.csproj --output graph.dot`; convert to image (e.g., via Graphviz).
   - Alternative: In Visual Studio, view Dependencies node in Solution Explorer for tree visualization.

3. **Analyze Tree for Issues**:
   - **Missing Types**: Traverse tree nodes; check if referenced types (from code) exist in listed assemblies. Use IDE's "Go to Definition" or `dotnet build --no-restore` to simulate and log unresolved references (e.g., CS0246 errors).
   - **Version Conflicts**: Identify duplicates (e.g., multiple versions of same package). Resolve via binding redirects or package consolidation (update .csproj with explicit versions).
   - **Circular Dependencies**: Detect cycles (e.g., Project A refs B, B refs A). Use tools like NDepend or manual graph traversal; break by refactoring interfaces.
   - **Incompatibilities**: Check target frameworks (e.g., .NET 8 vs .NET Framework); ensure all nodes align via `<TargetFramework>` in .csproj.
   - **Vulnerabilities/Outdated**: Run `dotnet list package --outdated --vulnerable` on tree; update risky nodes.

4. **Validate Compilation**:
   - Build with tree insights: `dotnet build /p:TreatWarningsAsErrors=true`.
   - If errors, map to tree (e.g., missing transitive dep → add explicit reference).
   - Enable analyzers: Add `<EnableNETAnalyzers>true</EnableNETAnalyzers>` to .csproj; run to flag tree-related warnings.

5. **Automate & Report**:
   - Integrate into CI: Script tree generation/analysis in pipelines (e.g., GitHub Actions YAML with `dotnet` steps).
   - Report: Output tree summary, issues, and fixes (e.g., JSON or markdown table: | Package | Version | Issue | Resolution |).

**Error Handling**:
- If tree generation fails: Check .csproj validity; fallback to manual inspection.
- Escalate unresolved issues: Suggest human review for complex graphs.

**Best Practices**:
- Keep tree minimal: Remove unused refs with `dotnet sln remove` or analyzers.
- Use semantic versioning; pin critical deps.
- Re-run process after changes for iterative validation.

These instructions ensure proactive dependency management, reducing compilation failures to near-zero.