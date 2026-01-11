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

# C# Compilation Dependency Tree Analysis Skill

## Prerequisites
- .NET CLI installed (dotnet command access).  
- Project uses SDK-style .csproj.  
- Tools: IDE (Visual Studio/VS Code), NuGet Package Manager.  
- Optional: Analyzers like NetArchTest, NDepend; Graphviz for visualization.  

## Instructions for AI Agent
You are an AI agent specializing in C# dependency management. Follow these steps to analyze and fix compilation issues via dependency trees. Respond with analysis results, fixes, and validation status.

### Step 1: Restore Dependencies
- Execute: `dotnet restore`  
- Purpose: Builds the dependency tree and generates packages.lock.json for deterministic restores.  
- Check: Verify all packages are fetched without errors.

### Step 2: Generate Dependency Tree
- CLI Command: `dotnet list package --include-transitive` (lists top-level and transitive dependencies).  
- Visual Graph: Install dotnet-graph via NuGet, then `dotnet graph --project Proj.csproj --output graph.dot`. Convert dot file to image using Graphviz.  
- IDE Alternative: Use Visual Studio's Solution Explorer > Dependencies node for tree view.

### Step 3: Analyze Tree
- **Missing Types**:  
  - Traverse tree nodes.  
  - Validate referenced types via IDE "Go to Definition" or `dotnet build --no-restore`.  
  - Log errors like CS0246 (type/namespace not found).  
  - Fix: Add missing `<PackageReference>` or `<ProjectReference>` in .csproj.  
    - Example .csproj fix for missing Newtonsoft.Json:  
      ```xml  
      <ItemGroup>  
        <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />  
      </ItemGroup>  
      ```  
- **Version Conflicts**:  
  - Identify duplicate packages with differing versions.  
  - Fix: Add binding redirects in app.config or explicit versions in .csproj. Consolidate via package updates.  
    - Example .csproj fix for explicit version pinning:  
      ```xml  
      <ItemGroup>  
        <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />  
      </ItemGroup>  
      ```  
    - Example app.config binding redirect:  
      ```xml  
      <runtime>  
        <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">  
          <dependentAssembly>  
            <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />  
            <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />  
          </dependentAssembly>  
        </assemblyBinding>  
      </runtime>  
      ```  
- **Circular Dependencies**:  
  - Detect cycles (e.g., A refs B, B refs A) using NDepend or manual graph inspection.  
  - Fix: Refactor code (e.g., extract interfaces to break cycles).  
    - Example .csproj fix: Remove circular `<ProjectReference>` and introduce a shared interface project.  
      ```xml  
      <!-- In ProjectA.csproj: Remove reference to ProjectB -->  
      <ItemGroup>  
        <ProjectReference Include="..\SharedInterfaces\SharedInterfaces.csproj" />  
      </ItemGroup>  
      ```  
- **Framework Incompatibilities**:  
  - Ensure all nodes match `<TargetFramework>` (e.g., net8.0).  
  - Fix: Update .csproj or downgrade/upgrade packages.  
    - Example .csproj fix for framework alignment:  
      ```xml  
      <PropertyGroup>  
        <TargetFramework>net8.0</TargetFramework>  
      </PropertyGroup>  
      ```  
- **Outdated/Vulnerable Packages**:  
  - Command: `dotnet list package --outdated --vulnerable`.  
  - Fix: Update via `dotnet add package PackageName --version X.Y.Z`.  
    - Example .csproj after update:  
      ```xml  
      <ItemGroup>  
        <PackageReference Include="System.Text.Json" Version="8.0.1" />  
      </ItemGroup>  
      ```  

### Step 4: Validate Compilation
- Build Command: `dotnet build /p:TreatWarningsAsErrors=true`.  
- Enable Analyzers: Add `<EnableNETAnalyzers>true</EnableNETAnalyzers>` to .csproj.  
  - Example .csproj addition:  
    ```xml  
    <PropertyGroup>  
      <EnableNETAnalyzers>true</EnableNETAnalyzers>  
    </PropertyGroup>  
    ```  
- If errors persist, map to tree and iterate fixes.  
- Post-Fix: Re-generate and re-analyze tree.

### Step 5: Automate and Report
- **CI Integration**: Add to pipelines (e.g., GitHub Actions YAML):  
  ```yaml  
  steps:  
    - run: dotnet restore  
    - run: dotnet list package --include-transitive > deps.txt  
    - run: dotnet build /p:TreatWarningsAsErrors=true  
  ```  
- **Report Format**: Use a markdown table for summary:  
  | Package | Version | Issue | Fix |  
  |---------|---------|-------|-----|  
  | ExamplePkg | 1.0.0 | Conflict | Update to 2.0.0 |  
- Output: Tree summary, identified issues, applied fixes, and final build status.

## Error Handling
- If restore/build fails: Inspect .csproj for syntax errors; fallback to manual dependency listing.  
- Complex graphs: Recommend human review or advanced tools like NDepend.  
- No internet: Rely on local CLI and IDE.

## Best Practices
- Minimize tree: Remove unused references with analyzers or `dotnet sln remove`.  
- Use semantic versioning; pin critical dependencies.  
- Enable nullable types: `<Nullable>enable</Nullable>`.  
  - Example .csproj:  
    ```xml  
    <PropertyGroup>  
      <Nullable>enable</Nullable>  
    </PropertyGroup>  
    ```  
- Re-validate after any changes to maintain integrity.

## Usage Example
**Input**: "Analyze dependencies in MyProject.csproj for compilation errors."  
**Agent Actions**: Restore → Generate tree → Analyze → Validate → Report fixes.  

This skill ensures proactive dependency resolution, reducing C# compilation failures.
