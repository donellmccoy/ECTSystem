---
name: verification-before-completion
description: Use when about to claim work is complete, fixed, or passing, before committing or creating PRs - requires running verification commands and confirming output before making any success claims; evidence before assertions always
---

# Verification Before Completion

## Overview

Claiming work is complete without verification is dishonesty, not efficiency.

**Core principle:** Evidence before claims, always.

**Violating the letter of this rule is violating the spirit of this rule.**

## The Iron Law

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

If you haven't run the verification command in this message, you cannot claim it passes.

## The Gate Function

```
BEFORE claiming any status or expressing satisfaction:

1. IDENTIFY: What command proves this claim?
2. RUN: Execute the FULL command (fresh, complete)
3. READ: Full output, check exit code, count failures
4. VERIFY: Does output confirm the claim?
   - If NO: State actual status with evidence
   - If YES: State claim WITH evidence
5. ONLY THEN: Make the claim

Skip any step = lying, not verifying
```

## Common Failures

| Claim | Requires | Not Sufficient |
|-------|----------|----------------|
| Tests pass | Test command output: 0 failures | Previous run, "should pass" |
| Linter clean | Linter output: 0 errors | Partial check, extrapolation |
| Build succeeds | Build command: exit 0 | Linter passing, logs look good |
| Bug fixed | Test original symptom: passes | Code changed, assumed fixed |
| Regression test works | Red-green cycle verified | Test passes once |
| Agent completed | VCS diff shows changes | Agent reports "success" |
| Requirements met | Line-by-line checklist | Tests passing |

**C# .NET Specific (ECTSystem):**

| Claim | Requires C# Verification | Not Sufficient |
|-------|-------------------------|----------------|
| Build succeeds | `dotnet build ElectronicCaseTracking.sln` exit 0, 0 warnings | Restore succeeded, IntelliSense green |
| Tests pass | `dotnet test AF.ECT.Tests --no-build` shows: X passed, 0 failed | Individual test passed once |
| gRPC service works | Test includes RpcException with correct StatusCode | Code compiles, proto file exists |
| EF Core query works | InMemoryDatabase test passes + SQL Server test passes | Compiles, LINQ looks right |
| No warnings | `dotnet build /p:TreatWarningsAsErrors=true` succeeds | CS warnings list is empty |
| Migration valid | `dotnet ef migrations list` + InMemoryDatabase test | Migration file created |

## Red Flags - STOP

- Using "should", "probably", "seems to"
- Expressing satisfaction before verification ("Great!", "Perfect!", "Done!", etc.)
- About to commit/push/PR without verification
- Trusting agent success reports
- Relying on partial verification
- Thinking "just this once"
- Tired and wanting work over
- **ANY wording implying success without having run verification**

## Rationalization Prevention

| Excuse | Reality |
|--------|---------|
| "Should work now" | RUN the verification |
| "I'm confident" | Confidence ≠ evidence |
| "Just this once" | No exceptions |
| "Linter passed" | Linter ≠ compiler |
| "Agent said success" | Verify independently |
| "I'm tired" | Exhaustion ≠ excuse |
| "Partial check is enough" | Partial proves nothing |
| "Different words so rule doesn't apply" | Spirit over letter |

## Key Patterns

**Tests:**
```
✅ [Run dotnet test] [See: X passed] "All tests pass"
$ dotnet test --no-build --logger "console;verbosity=minimal"
  34 test(s) passed
❌ "Linter passed" (linter ≠ tests)
```

**Regression tests (TDD Red-Green):**
```
✅ Write test → Run (FAIL) → Fix code → Run (PASS) → Revert fix → Run (FAIL) → Restore → Run (PASS)
$ dotnet test AF.ECT.Tests --filter "MethodName" --no-build
  FAIL: MethodName
[Fix code]
$ dotnet test AF.ECT.Tests --filter "MethodName" --no-build
  1 passed
❌ "I've written a test" (without watching it fail then pass)
```

**Build:**
```
✅ [Run build] [See: exit 0] "Build passes"
$ dotnet build ElectronicCaseTracking.sln --no-restore
Building solution configuration 'Release|Any CPU'.
  All projects are up to date for restoration.
  All projects are up to date for generation.
  All projects are up to date for compilation.

❌ "Restore passed" (restore ≠ build)
```

**C# .NET Specific (ECTSystem):**

**gRPC service verification:**
```bash
✅ Test written for both success AND exception paths
[Fact]
public async Task GetWorkflow_WithValidId_ReturnsWorkflow() { ... }  ✓ PASS

[Fact]
public async Task GetWorkflow_WithNullId_ThrowsRpcException() { ... }  ✓ PASS

❌ "Only tested success path" (missing exception test)
```

**EF Core verification:**
```bash
✅ Test uses InMemoryDatabase AND actual SQL Server connection tested
// Test with InMemory
var options = new DbContextOptionsBuilder<EctDbContext>()
    .UseInMemoryDatabase("test")
    .Options;
using var context = new EctDbContext(options);
// Verify query logic works

// Integration test with actual SQL Server connection
dotnet test AF.ECT.Tests/Integration --filter "GetWorkflow_Integration" --no-build

❌ "InMemory test passes" (without integration test)
```

**Build warning verification:**
```bash
✅ dotnet build ElectronicCaseTracking.sln /p:TreatWarningsAsErrors=true
Build succeeded. 0 errors, 0 warnings

❌ "Build succeeded" (with CS warnings present)
```

## Why This Matters

From 24 failure memories:
- your human partner said "I don't believe you" - trust broken
- Undefined functions shipped - would crash
- Missing requirements shipped - incomplete features
- Time wasted on false completion → redirect → rework
- Violates: "Honesty is a core value. If you lie, you'll be replaced."

## When To Apply

**ALWAYS before:**
- ANY variation of success/completion claims
- ANY expression of satisfaction
- ANY positive statement about work state
- Committing, PR creation, task completion
- Moving to next task
- Delegating to agents

**Rule applies to:**
- Exact phrases
- Paraphrases and synonyms
- Implications of success
- ANY communication suggesting completion/correctness

## The Bottom Line

**No shortcuts for verification.**

Run the command. Read the output. THEN claim the result.

This is non-negotiable.
