# Defense-in-Depth Validation

## Overview

When you fix a bug caused by invalid data, adding validation at one place feels sufficient. But that single check can be bypassed by different code paths, refactoring, or mocks.

**Core principle:** Validate at EVERY layer data passes through. Make the bug structurally impossible.

## Why Multiple Layers

Single validation: "We fixed the bug"
Multiple layers: "We made the bug impossible"

Different layers catch different cases:
- UI validation catches most user errors
- gRPC client validation prevents network calls
- Service layer validation ensures business logic integrity
- Debug logging helps when other layers fail

## The Four Layers

### Layer 1: UI Entry Point Validation
**Purpose:** Reject obviously invalid input at Blazor component boundary

```csharp
private async Task HandleWorkflowSelection(string workflowId)
{
    if (string.IsNullOrWhiteSpace(workflowId))
    {
        await _notificationService.ShowError("Workflow ID is required");
        return;
    }
    if (workflowId.Length > 50)
    {
        await _notificationService.ShowError("Workflow ID too long");
        return;
    }
    await LoadWorkflow(workflowId);
}
```

### Layer 2: gRPC Client Validation
**Purpose:** Ensure data makes sense before network call

```csharp
public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId)
{
    if (string.IsNullOrEmpty(workflowId))
        throw new ArgumentException("workflowId cannot be null or empty", nameof(workflowId));
    
    var request = new GetWorkflowRequest { WorkflowId = workflowId };
    return await _client.GetWorkflowAsync(request, _callOptions);
}
```

### Layer 3: Service Layer Validation
**Purpose:** Prevent dangerous operations with invalid business data

```csharp
public async Task<WorkflowResponse> GetWorkflow(GetWorkflowRequest request, ServerCallContext context)
{
    if (string.IsNullOrEmpty(request.WorkflowId))
        throw new RpcException(
            new Status(StatusCode.InvalidArgument, "workflowId is required"));
    
    var workflow = await _dataService.GetWorkflowByIdAsync(request.WorkflowId);
    return MapToResponse(workflow);
}
```

### Layer 4: Debug Instrumentation
**Purpose:** Capture context for forensics

```csharp
public async Task<WorkflowResponse> GetWorkflow(GetWorkflowRequest request, ServerCallContext context)
{
    var correlationId = context.GetHttpContext().Request.Headers["X-Correlation-ID"];
    _logger.LogInformation(
        "GetWorkflow called: workflowId={WorkflowId}, userId={UserId}, correlationId={CorrelationId}, stack={Stack}",
        request.WorkflowId,
        context.GetHttpContext().User?.Identity?.Name,
        correlationId,
        Environment.StackTrace);
    
    // ... proceed
}
```

## Applying the Pattern

When you find a bug:

1. **Trace the data flow** - Where does bad value originate? Where used?
2. **Map all checkpoints** - List every point data passes through
3. **Add validation at each layer** - Entry, business, environment, debug
4. **Test each layer** - Try to bypass layer 1, verify layer 2 catches it

## Example from ECTSystem

Bug: Null/empty `workflowId` caused unhandled exception instead of RpcException with proper StatusCode

**Data flow:**
1. Blazor component → passes empty string
2. `WorkflowClient.GetWorkflowAsync("")`
3. `WorkflowServiceImpl.GetWorkflow(request)`
4. `WorkflowDataService.GetWorkflowByIdAsync("")` returns null
5. NullReferenceException thrown instead of RpcException

**Four layers added:**
- Layer 1: BlazorComponent validates workflowId in OnInitializedAsync
- Layer 2: WorkflowClient validates not null/empty before gRPC call
- Layer 3: WorkflowServiceImpl validates with RpcException(StatusCode.InvalidArgument)
- Layer 4: Audit logging with correlation ID and user context

**Result:** All tests in AF.ECT.Tests passed, proper exception handling at all layers

## Key Insight

All four layers were necessary. During testing, each layer caught bugs the others missed:
- Different UI flows bypassed component validation
- Mocks bypassed gRPC client checks
- Edge cases in data service needed service layer validation
- Audit logging identified structural misuse patterns

**Don't stop at one validation point.** Add checks at UI, client, service, and logging layers.
