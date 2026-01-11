---
name: executing-plans
description: Use when you have a written implementation plan to execute in a separate session with review checkpoints
---

# Executing Plans

## Overview

Load plan, review critically, execute tasks in batches, report for review between batches.

**Core principle:** Batch execution with checkpoints for architect review.

**Announce at start:** "I'm using the executing-plans skill to implement this plan."

## The Process

### Step 1: Load and Review Plan
1. Read plan file
2. Review critically - identify any questions or concerns about the plan
3. If concerns: Raise them with your human partner before starting
4. If no concerns: Create TodoWrite and proceed

### Step 2: Execute Batch
**Default: First 3 tasks**

For each task:
1. Mark as in_progress
2. Follow each step exactly (plan has bite-sized steps)
3. Run verifications as specified
4. Mark as completed

**For C# .NET Projects (ECTSystem):**
```bash
# After each task in plan, run verification
# Build the solution to catch compilation errors early
dotnet build ElectronicCaseTracking.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary

# Run tests to verify functionality
dotnet test AF.ECT.Tests --filter "YourTestNamePattern" --no-build

# Check for warnings that become errors
dotnet build ElectronicCaseTracking.sln /property:GenerateFullPaths=true /p:TreatWarningsAsErrors=true
```

### Step 3: Report
When batch complete:
- Show what was implemented
- Show verification output
- Say: "Ready for feedback."

**For C# .NET Projects (ECTSystem):**

Example report:
```
Completed Tasks 1-3:

Task 1: Added GetUserCount gRPC method
- Files modified: AF.ECT.Shared/Protos/workflow.proto, AF.ECT.Server/Services/WorkflowServiceImpl.cs
- Tests: 2/2 passing (GetUserCount_WithValidRequest_ReturnsCount, GetUserCount_WithInvalidStatus_ThrowsRpcException)
- Build: ✓ 0 errors, 0 warnings

Task 2: Implemented User audit logging
- Files modified: AF.ECT.Server/Interceptors/AuditInterceptor.cs, AF.ECT.Data/EctDbContext.cs
- Tests: 4/4 passing (AuditInterceptor_LogsMethodCall, AuditInterceptor_IncludesCorrelationId, etc.)
- Build: ✓ 0 errors, 0 warnings

Task 3: Added migrations for audit tables
- Files modified: AF.ECT.Database/dbo/Tables/AuditLogs.sql
- Tests: Migration tested via EF Core context (3 tables created successfully)
- Build: ✓ 0 errors, 0 warnings

Verification Results:
$ dotnet test AF.ECT.Tests --filter "Audit|UserCount" --no-build
  9 tests passed, 0 failed

$ dotnet build ElectronicCaseTracking.sln
  Build complete with 0 errors, 0 warnings

Ready for feedback.
```

### Step 4: Continue
Based on feedback:
- Apply changes if needed
- Execute next batch
- Repeat until complete

### Step 5: Complete Development

After all tasks complete and verified:
- Announce: "I'm using the finishing-a-development-branch skill to complete this work."
- **REQUIRED SUB-SKILL:** Use superpowers:finishing-a-development-branch
- Follow that skill to verify tests, present options, execute choice

## When to Stop and Ask for Help

**STOP executing immediately when:**
- Hit a blocker mid-batch (missing dependency, test fails, instruction unclear)
- Plan has critical gaps preventing starting
- You don't understand an instruction
- Verification fails repeatedly

**Ask for clarification rather than guessing.**

## When to Revisit Earlier Steps

**Return to Review (Step 1) when:**
- Partner updates the plan based on your feedback
- Fundamental approach needs rethinking

**Don't force through blockers** - stop and ask.

## Remember
- Review plan critically first
- Follow plan steps exactly
- Don't skip verifications
- Reference skills when plan says to
- Between batches: just report and wait
- Stop when blocked, don't guess
