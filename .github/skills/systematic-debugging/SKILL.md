---
name: systematic-debugging
description: Use when encountering any bug, test failure, or unexpected behavior in C#/.NET code before proposing fixes; applies to gRPC services, EF Core queries, async operations, and build failures
---

# Systematic Debugging

## Overview

Random fixes waste time and create new bugs. Quick patches mask underlying issues.

**Core principle:** ALWAYS find root cause before attempting fixes. Symptom fixes are failure.

**Violating the letter of this process is violating the spirit of debugging.**

## The Iron Law

```
NO FIXES WITHOUT ROOT CAUSE INVESTIGATION FIRST
```

If you haven't completed Phase 1, you cannot propose fixes.

## When to Use

Use for ANY technical issue:
- Test failures
- Bugs in production
- Unexpected behavior
- Performance problems
- Build failures
- Integration issues

**Use this ESPECIALLY when:**
- Under time pressure (emergencies make guessing tempting)
- "Just one quick fix" seems obvious
- You've already tried multiple fixes
- Previous fix didn't work
- You don't fully understand the issue

**C# .NET Specific:**
- gRPC service throwing RpcException with wrong status code
- EF Core query returning unexpected results
- Async deadlocks or TaskScheduler issues
- Build failures with cryptic compiler errors
- NUnit/xUnit test failures with no clear output

## The Four Phases

You MUST complete each phase before proceeding to the next.

### Phase 1: Root Cause Investigation

**BEFORE attempting ANY fix:**

1. **Read Error Messages Carefully**
   - Don't skip past errors or warnings (warnings become errors in build)
   - Exception stack traces show exact line numbers
   - Read StackTrace property completely
   - Check InnerException for root cause
   - .NET error codes (CS, MSB, RPC Status codes) have specific meanings
   - For gRPC: Check StatusCode (InvalidArgument, NotFound, Internal, etc.)
   - For EF Core: Check DbUpdateException, SqlException inner exception
   
   **C# .NET Examples:**
   ```bash
   # Read full exception with inner exception chain
   $ dotnet test AF.ECT.Tests --filter "WorkflowTest" --no-build
   
   FAIL: GetWorkflow_WithNullId_ThrowsException
   Error: 
     System.ArgumentNullException: Value cannot be null. (Parameter 'workflowId')
       at AF.ECT.Server.Services.WorkflowServiceImpl.ValidateWorkflowId(String id)
       at AF.ECT.Server.Services.WorkflowServiceImpl.GetWorkflow(GetWorkflowRequest request, ServerCallContext context)
     InnerException: (check if significant)
   
   # Check EF Core query logs
   $ dotnet run --project AF.ECT.Server -- --debug
   # Look for: SqlException, DbUpdateException with inner message about FK constraints
   
   # Check gRPC response status codes
   # StatusCode.InvalidArgument = client sent bad request (your validation)
   # StatusCode.NotFound = resource doesn't exist (query returned null)
   # StatusCode.Internal = server error (unhandled exception)
   ```

2. **Reproduce Consistently**
   - Can you trigger it reliably with `dotnet test AF.ECT.Tests` or `dotnet run --project AF.ECT.AppHost`?
   - What are the exact steps or input?
   - Does it happen every test run?
   - If intermittent → add logging to gRPC interceptor, EF Core query logs
   - Don't guess on race conditions - instrument first

   **C# .NET Examples:**
   ```bash
   # Reproduce test failure consistently
   $ dotnet test AF.ECT.Tests --filter "GetWorkflow" --no-build -v detailed
   
   # For intermittent failures, enable EF Core SQL logging
   # In your DbContext OnConfiguring:
   optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
   
   # For gRPC issues, check ServerCallContext logging in AuditInterceptor
   # Look for: request/response timing, connection resets, timeouts
   ```

3. **Check Recent Changes**
   - What changed that could cause this?
   - `git diff main`, recent commits
   - New gRPC method, EF Core query change, config values
   - Environmental differences (LocalDB vs SQL Server, network)

4. **Gather Evidence in Multi-Component Systems**

   **WHEN system has multiple components (Blazor → gRPC → Server → EF Core → SQL Server):**

   **BEFORE proposing fixes, add diagnostic instrumentation:**
   ```
   For EACH component boundary:
     - Log what data enters component
     - Log what data exits component
     - Verify environment/config propagation
     - Check state at each layer
   ```

   **Example (.NET multi-layer system in ECTSystem):**
   ```bash
   # Layer 1: Configuration & Dependency Injection
   echo "=== appsettings.Development.json values: ==="
   cat AF.ECT.Server/appsettings.Development.json | jq '.ConnectionStrings, .Logging'

   # Layer 2: gRPC Channel & Client Configuration
   echo "=== WorkflowClient initialization: ==="
   # Add temporary logging in AF.ECT.Shared/Services/WorkflowClient.cs constructor
   // Log: ServiceUrl, ChannelCredentials, etc.

   # Layer 3: gRPC Request Interceptor
   echo "=== gRPC call audit: ==="
   # Check AF.ECT.Server/Interceptors/AuditInterceptor.cs logs
   # Look for: Request details, correlation ID, response status

   # Layer 4: Server Endpoint Processing
   echo "=== gRPC service method execution: ==="
   # Add logging in AF.ECT.Server/Services/WorkflowServiceImpl.cs
   // Log: Method entry, parameters, database calls

   # Layer 5: EF Core Data Access
   echo "=== Database query execution: ==="
   # Enable in AF.ECT.Data/EctDbContext.cs OnConfiguring:
   optionsBuilder.LogTo(Console.WriteLine, LogLevel.Debug);

   # Layer 6: SQL Server Execution
   echo "=== SQL tracing: ==="
   # Use SQL Server Profiler or Extended Events to trace actual queries
   ```

   **Run once to gather evidence showing WHERE it breaks, then investigate that specific component**

5. **Trace Data Flow**

   **WHEN error is deep in call stack (gRPC → WorkflowClient → DataService → EF Core → DbContext):**

   See `root-cause-tracing.md` in this directory for the complete backward tracing technique.

   **Quick version for ECTSystem:**
   ```
   Error location: WorkflowServiceImpl.GetWorkflow() returns null
   
   1. Where does null come from? 
      Answer: EF Core query returned no results (_context.Workflows.FirstOrDefaultAsync())
   
   2. Why is query returning nothing?
      Answer: Check query logic, parameters passed to query
      Look at: WorkflowDataService.GetWorkflowByIdAsync(id)
      What is 'id' value? How is it passed from client?
   
   3. Where does bad ID originate?
      Answer: Client sent invalid/empty workflow ID in gRPC request
      Look at: WorkflowClient.GetWorkflowAsync(workflowId)
      Who calls this? What ID do they pass?
   
   4. Keep tracing up until you find source:
      Answer: Component passes hardcoded ID, or user input not validated
   
   5. Fix at source:
      - If client: Add validation in WorkflowClient
      - If user input: Validate in Blazor component before calling client
      - NOT: Add defensive checks at every layer
   ```
   
   - Where does bad value originate? (null object from gRPC request, invalid Id)
   - What called this with bad value? (WorkflowClient passed wrong Id)
   - Keep tracing up call stack until you find the source
   - Fix at source, not at symptom (don't add validation everywhere, fix the caller)

### Phase 2: Pattern Analysis

**Find the pattern before fixing:**

1. **Find Working Examples**
   - Locate similar working code in same codebase
   - What works that's similar to what's broken?

2. **Compare Against References**
   - If implementing pattern, read reference implementation COMPLETELY
   - Don't skim - read every line
   - Understand the pattern fully before applying

3. **Identify Differences**
   - What's different between working and broken?
   - List every difference, however small
   - Don't assume "that can't matter"

4. **Understand Dependencies**
   - What other components does this need?
   - What settings, config, environment?
   - What assumptions does it make?

### Phase 3: Hypothesis and Testing

**Scientific method:**

1. **Form Single Hypothesis**
   - State clearly: "I think X is the root cause because Y"
   - Write it down
   - Be specific, not vague

2. **Test Minimally**
   - Make the SMALLEST possible change to test hypothesis
   - One variable at a time
   - Don't fix multiple things at once

3. **Verify Before Continuing**
   - Did it work? Yes → Phase 4
   - Didn't work? Form NEW hypothesis
   - DON'T add more fixes on top

4. **When You Don't Know**
   - Say "I don't understand X"
   - Don't pretend to know
   - Ask for help
   - Research more

### Phase 4: Implementation

**Fix the root cause, not the symptom:**

1. **Create Failing Test Case**
   - Simplest possible reproduction
   - xUnit `[Fact]` test (or `[Theory]` with data)
   - MUST have before fixing
   - **REQUIRED SUB-SKILL:** Use superpowers:test-driven-development for writing proper failing tests

2. **Implement Single Fix**
   - Address the root cause identified
   - ONE change at a time
   - No "while I'm here" improvements
   - No bundled refactoring

3. **Verify Fix**
   - Test passes now?
   - No other tests broken?
   - Issue actually resolved?

4. **If Fix Doesn't Work**
   - STOP
   - Count: How many fixes have you tried?
   - If < 3: Return to Phase 1, re-analyze with new information
   - **If ≥ 3: STOP and question the architecture (step 5 below)**
   - DON'T attempt Fix #4 without architectural discussion

5. **If 3+ Fixes Failed: Question Architecture**

   **Pattern indicating architectural problem:**
   - Each fix reveals new shared state/coupling/problem in different place
   - Fixes require "massive refactoring" to implement
   - Each fix creates new symptoms elsewhere

   **STOP and question fundamentals:**
   - Is this pattern fundamentally sound?
   - Are we "sticking with it through sheer inertia"?
   - Should we refactor architecture vs. continue fixing symptoms?

   **Discuss with your human partner before attempting more fixes**

   This is NOT a failed hypothesis - this is a wrong architecture.

## Red Flags - STOP and Follow Process

If you catch yourself thinking:
- "Quick fix for now, investigate later"
- "Just try changing X and see if it works"
- "Add multiple changes, run tests"
- "Skip the test, I'll manually verify"
- "It's probably X, let me fix that"
- "I don't fully understand but this might work"
- "Pattern says X but I'll adapt it differently"
- "Here are the main problems: [lists fixes without investigation]"
- Proposing solutions before tracing data flow
- **"One more fix attempt" (when already tried 2+)**
- **Each fix reveals new problem in different place**

**ALL of these mean: STOP. Return to Phase 1.**

**If 3+ fixes failed:** Question the architecture (see Phase 4.5)

## your human partner's Signals You're Doing It Wrong

**Watch for these redirections:**
- "Is that not happening?" - You assumed without verifying
- "Will it show us...?" - You should have added evidence gathering
- "Stop guessing" - You're proposing fixes without understanding
- "Ultrathink this" - Question fundamentals, not just symptoms
- "We're stuck?" (frustrated) - Your approach isn't working

**When you see these:** STOP. Return to Phase 1.

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Issue is simple, don't need process" | Simple issues have root causes too. Process is fast for simple bugs. |
| "Emergency, no time for process" | Systematic debugging is FASTER than guess-and-check thrashing. |
| "Just try this first, then investigate" | First fix sets the pattern. Do it right from the start. |
| "I'll write test after confirming fix works" | Untested fixes don't stick. Test first proves it. |
| "Multiple fixes at once saves time" | Can't isolate what worked. Causes new bugs. |
| "Reference too long, I'll adapt the pattern" | Partial understanding guarantees bugs. Read it completely. |
| "I see the problem, let me fix it" | Seeing symptoms ≠ understanding root cause. |
| "One more fix attempt" (after 2+ failures) | 3+ failures = architectural problem. Question pattern, don't fix again. |

## Quick Reference

| Phase | Key Activities | Success Criteria |
|-------|---------------|------------------|
| **1. Root Cause** | Read errors, reproduce, check changes, gather evidence | Understand WHAT and WHY |
| **2. Pattern** | Find working examples, compare | Identify differences |
| **3. Hypothesis** | Form theory, test minimally | Confirmed or new hypothesis |
| **4. Implementation** | Create test, fix, verify | Bug resolved, tests pass |

## When Process Reveals "No Root Cause"

If systematic investigation reveals issue is truly environmental, timing-dependent, or external:

1. You've completed the process
2. Document what you investigated
3. Implement appropriate handling (retry, timeout, error message)
4. Add monitoring/logging for future investigation

**But:** 95% of "no root cause" cases are incomplete investigation.

## Supporting Techniques

These techniques are part of systematic debugging and available in this directory:

- **`root-cause-tracing.md`** - Trace bugs backward through call stack to find original trigger
- **`defense-in-depth.md`** - Add validation at multiple layers after finding root cause
- **`condition-based-waiting.md`** - Replace arbitrary timeouts with condition polling

**Related skills:**
- **superpowers:test-driven-development** - For creating failing test case (Phase 4, Step 1)
- **superpowers:verification-before-completion** - Verify fix worked before claiming success

## Real-World Impact

From debugging sessions:
- Systematic approach: 15-30 minutes to fix
- Random fixes approach: 2-3 hours of thrashing
- First-time fix rate: 95% vs 40%
- New bugs introduced: Near zero vs common
