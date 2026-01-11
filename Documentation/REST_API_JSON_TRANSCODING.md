# REST API JSON Transcoding for gRPC Services

## Overview

JSON Transcoding converts gRPC services into REST/HTTP endpoints automatically, allowing clients to call gRPC services using standard HTTP GET/POST requests. This is essential for browser-based clients, legacy systems, and API consumers who don't support gRPC.

---

## Protobuf Configuration

### 1. Add google.api Annotations

Update your `.proto` files with HTTP rules:

```protobuf
// AF.ECT.Shared/Protos/workflow.proto

syntax = "proto3";

package af.ect.v1;

import "google/api/annotations.proto";
import "google/api/client.proto";
import "google/protobuf/empty.proto";
import "google/protobuf/timestamp.proto";

option csharp_namespace = "AF.ECT.Shared.Protos";
option java_package = "af.ect.v1";

service WorkflowService {
  option (google.api.default_host) = "localhost:5000";

  // Get a single workflow
  rpc GetWorkflow (GetWorkflowRequest) returns (GetWorkflowResponse) {
    option (google.api.http) = {
      get: "/v1/workflows/{id}"
    };
  }

  // Get all workflows with filters
  rpc ListWorkflows (ListWorkflowsRequest) returns (ListWorkflowsResponse) {
    option (google.api.http) = {
      get: "/v1/workflows"
    };
  }

  // Create a new workflow
  rpc CreateWorkflow (CreateWorkflowRequest) returns (GetWorkflowResponse) {
    option (google.api.http) = {
      post: "/v1/workflows"
      body: "*"
    };
  }

  // Update existing workflow
  rpc UpdateWorkflow (UpdateWorkflowRequest) returns (GetWorkflowResponse) {
    option (google.api.http) = {
      patch: "/v1/workflows/{workflow.id}"
      body: "workflow"
    };
  }

  // Delete a workflow
  rpc DeleteWorkflow (DeleteWorkflowRequest) returns (google.protobuf.Empty) {
    option (google.api.http) = {
      delete: "/v1/workflows/{id}"
    };
  }

  // List users (streaming)
  rpc GetUsersOnlineStream (google.protobuf.Empty) returns (stream UserOnlineItem) {
    option (google.api.http) = {
      get: "/v1/users/online"
    };
  }
}

// Request/Response Messages

message GetWorkflowRequest {
  string id = 1;
}

message GetWorkflowResponse {
  string id = 1;
  string title = 2;
  string status = 3;
  google.protobuf.Timestamp created_date = 4;
  google.protobuf.Timestamp modified_date = 5;
}

message ListWorkflowsRequest {
  string status_filter = 1;
  int32 page_size = 2;
  string page_token = 3;
}

message ListWorkflowsResponse {
  repeated GetWorkflowResponse workflows = 1;
  string next_page_token = 2;
  int32 total_count = 3;
}

message CreateWorkflowRequest {
  string title = 1;
  string description = 2;
}

message UpdateWorkflowRequest {
  GetWorkflowResponse workflow = 1;
}

message DeleteWorkflowRequest {
  string id = 1;
}

message UserOnlineItem {
  string user_id = 1;
  string user_name = 2;
  string status = 3;
  google.protobuf.Timestamp last_active = 4;
}
```

### 2. Configure Project to Generate API Annotations

Update `.csproj`:

```xml
<ItemGroup>
  <!-- Protobuf with google.api annotations support -->
  <PackageReference Include="Google.Api.Gapic.Grpc" Version="2.5.0" />
  <PackageReference Include="Google.Api.CommonProtos" Version="1.3.0" />
</ItemGroup>

<ItemGroup>
  <Protobuf Include="Protos\*.proto" GrpcServices="Server" />
</ItemGroup>
```

---

## Server-Side Configuration

### 1. Enable JSON Transcoding Middleware

```csharp
// AF.ECT.Server/Program.cs

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc();

// Add gRPC JSON transcoding
builder.Services.AddGrpcJsonTranscoding();

var app = builder.Build();

// Configure routing
app.UseRouting();

// Map gRPC services
app.MapGrpcService<WorkflowServiceImpl>();

// Enable JSON transcoding endpoints
app.MapGrpcReflectionService();

// Health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

### 2. Service Implementation with Route Mapping

```csharp
// AF.ECT.Server/Services/WorkflowServiceImpl.cs

using Grpc.Core;
using Google.Protobuf.WellKnownTypes;

/// <summary>
/// Workflow service implementation with automatic JSON transcoding.
/// All methods can be called via HTTP/REST or gRPC.
/// </summary>
public class WorkflowServiceImpl : WorkflowService.WorkflowServiceBase
{
    private readonly ECTContext _context;
    private readonly ILogger<WorkflowServiceImpl> _logger;

    public WorkflowServiceImpl(
        ECTContext context,
        ILogger<WorkflowServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /v1/workflows/{id}
    /// </summary>
    public override async Task<GetWorkflowResponse> GetWorkflow(
        GetWorkflowRequest request,
        ServerCallContext context)
    {
        var workflow = await _context.Workflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.Id);

        if (workflow == null)
        {
            throw new RpcException(
                new Status(StatusCode.NotFound, 
                    $"Workflow {request.Id} not found"));
        }

        return new GetWorkflowResponse
        {
            Id = workflow.Id,
            Title = workflow.Title,
            Status = workflow.Status,
            CreatedDate = Timestamp.FromDateTime(workflow.CreatedDate),
            ModifiedDate = Timestamp.FromDateTime(workflow.ModifiedDate)
        };
    }

    /// <summary>
    /// GET /v1/workflows?status_filter=Active&page_size=10
    /// </summary>
    public override async Task<ListWorkflowsResponse> ListWorkflows(
        ListWorkflowsRequest request,
        ServerCallContext context)
    {
        var query = _context.Workflows.AsQueryable();

        if (!string.IsNullOrEmpty(request.StatusFilter))
        {
            query = query.Where(w => w.Status == request.StatusFilter);
        }

        var totalCount = await query.CountAsync();

        var workflows = await query
            .AsNoTracking()
            .Skip((request.PageSize > 0 ? request.PageSize : 10) * 
                  (string.IsNullOrEmpty(request.PageToken) ? 0 : int.Parse(request.PageToken)))
            .Take(request.PageSize > 0 ? request.PageSize : 10)
            .ToListAsync();

        var response = new ListWorkflowsResponse
        {
            TotalCount = totalCount
        };

        response.Workflows.AddRange(workflows.Select(w =>
            new GetWorkflowResponse
            {
                Id = w.Id,
                Title = w.Title,
                Status = w.Status,
                CreatedDate = Timestamp.FromDateTime(w.CreatedDate),
                ModifiedDate = Timestamp.FromDateTime(w.ModifiedDate)
            }));

        return response;
    }

    /// <summary>
    /// POST /v1/workflows
    /// Body: { "title": "...", "description": "..." }
    /// </summary>
    public override async Task<GetWorkflowResponse> CreateWorkflow(
        CreateWorkflowRequest request,
        ServerCallContext context)
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Status = "New",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        return new GetWorkflowResponse
        {
            Id = workflow.Id,
            Title = workflow.Title,
            Status = workflow.Status,
            CreatedDate = Timestamp.FromDateTime(workflow.CreatedDate),
            ModifiedDate = Timestamp.FromDateTime(workflow.ModifiedDate)
        };
    }

    /// <summary>
    /// PATCH /v1/workflows/{workflow.id}
    /// Body: { "workflow": { "id": "...", "status": "..." } }
    /// </summary>
    public override async Task<GetWorkflowResponse> UpdateWorkflow(
        UpdateWorkflowRequest request,
        ServerCallContext context)
    {
        var workflow = await _context.Workflows
            .FirstOrDefaultAsync(w => w.Id == request.Workflow.Id);

        if (workflow == null)
        {
            throw new RpcException(
                new Status(StatusCode.NotFound, 
                    $"Workflow {request.Workflow.Id} not found"));
        }

        workflow.Status = request.Workflow.Status;
        workflow.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new GetWorkflowResponse
        {
            Id = workflow.Id,
            Title = workflow.Title,
            Status = workflow.Status,
            CreatedDate = Timestamp.FromDateTime(workflow.CreatedDate),
            ModifiedDate = Timestamp.FromDateTime(workflow.ModifiedDate)
        };
    }

    /// <summary>
    /// DELETE /v1/workflows/{id}
    /// </summary>
    public override async Task<Empty> DeleteWorkflow(
        DeleteWorkflowRequest request,
        ServerCallContext context)
    {
        var workflow = await _context.Workflows
            .FirstOrDefaultAsync(w => w.Id == request.Id);

        if (workflow == null)
        {
            throw new RpcException(
                new Status(StatusCode.NotFound, 
                    $"Workflow {request.Id} not found"));
        }

        _context.Workflows.Remove(workflow);
        await _context.SaveChangesAsync();

        return new Empty();
    }

    /// <summary>
    /// GET /v1/users/online (Server Streaming)
    /// Streams users in JSON format
    /// </summary>
    public override async Task GetUsersOnlineStream(
        Empty request,
        IAsyncStreamWriter<UserOnlineItem> responseStream,
        ServerCallContext context)
    {
        var users = _context.Users
            .Where(u => u.IsOnline)
            .AsAsyncEnumerable();

        await foreach (var user in users)
        {
            await responseStream.WriteAsync(new UserOnlineItem
            {
                UserId = user.Id.ToString(),
                UserName = user.Name,
                Status = user.Status,
                LastActive = Timestamp.FromDateTime(user.LastActive)
            });
        }
    }
}
```

---

## REST API Usage Examples

### 1. Using curl

```bash
# GET: Retrieve single workflow
curl -X GET http://localhost:5000/v1/workflows/workflow-123 \
  -H "Content-Type: application/json"

# GET: List workflows with filtering
curl -X GET "http://localhost:5000/v1/workflows?status_filter=Active&page_size=10" \
  -H "Content-Type: application/json"

# POST: Create workflow
curl -X POST http://localhost:5000/v1/workflows \
  -H "Content-Type: application/json" \
  -d '{
    "title": "New Workflow",
    "description": "Example workflow"
  }'

# PATCH: Update workflow
curl -X PATCH http://localhost:5000/v1/workflows/workflow-123 \
  -H "Content-Type: application/json" \
  -d '{
    "workflow": {
      "id": "workflow-123",
      "status": "In Progress"
    }
  }'

# DELETE: Remove workflow
curl -X DELETE http://localhost:5000/v1/workflows/workflow-123 \
  -H "Content-Type: application/json"

# GET: Stream users (server-sent events)
curl -X GET http://localhost:5000/v1/users/online \
  -H "Accept: application/json"
```

### 2. Using HttpClient (C#)

```csharp
// AF.ECT.WebClient/Services/WorkflowHttpClient.cs

/// <summary>
/// REST client for Workflow service (alternative to gRPC client).
/// Uses JSON transcoding endpoints.
/// </summary>
public class WorkflowHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkflowHttpClient> _logger;

    public WorkflowHttpClient(
        HttpClient httpClient,
        ILogger<WorkflowHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GetWorkflowResponse> GetWorkflowAsync(string workflowId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/v1/workflows/{workflowId}");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GetWorkflowResponse>(content)
                ?? throw new InvalidOperationException("Invalid response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error retrieving workflow");
            throw;
        }
    }

    public async Task<ListWorkflowsResponse> ListWorkflowsAsync(
        string? statusFilter = null,
        int pageSize = 10)
    {
        var query = new StringBuilder("/v1/workflows?");
        
        if (!string.IsNullOrEmpty(statusFilter))
            query.Append($"status_filter={Uri.EscapeDataString(statusFilter)}&");
        
        query.Append($"page_size={pageSize}");

        try
        {
            var response = await _httpClient.GetAsync(query.ToString());
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ListWorkflowsResponse>(content)
                ?? throw new InvalidOperationException("Invalid response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error listing workflows");
            throw;
        }
    }

    public async Task<GetWorkflowResponse> CreateWorkflowAsync(
        CreateWorkflowRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/workflows", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GetWorkflowResponse>(responseContent)
                ?? throw new InvalidOperationException("Invalid response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            throw;
        }
    }

    public async IAsyncEnumerable<UserOnlineItem> GetUsersOnlineStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "/v1/users/online",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    break;

                var item = JsonSerializer.Deserialize<UserOnlineItem>(line);
                if (item != null)
                    yield return item;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error streaming users");
            throw;
        }
    }
}

// Program.cs - Register HTTP client
services.AddHttpClient<WorkflowHttpClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5000");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
```

### 3. Using JavaScript/Fetch

```javascript
// AF.ECT.WebClient/wwwroot/api/workflowClient.js

/**
 * REST client for Workflow service using JSON transcoding.
 */
class WorkflowHttpClient {
  constructor(baseUrl = '') {
    this.baseUrl = baseUrl || '';
  }

  async getWorkflow(workflowId) {
    const response = await fetch(`${this.baseUrl}/v1/workflows/${workflowId}`);
    if (!response.ok) {
      throw new Error(`Failed to get workflow: ${response.statusText}`);
    }
    return await response.json();
  }

  async listWorkflows(statusFilter = null, pageSize = 10) {
    let url = `${this.baseUrl}/v1/workflows?page_size=${pageSize}`;
    if (statusFilter) {
      url += `&status_filter=${encodeURIComponent(statusFilter)}`;
    }

    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Failed to list workflows: ${response.statusText}`);
    }
    return await response.json();
  }

  async createWorkflow(title, description) {
    const response = await fetch(`${this.baseUrl}/v1/workflows`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title, description })
    });

    if (!response.ok) {
      throw new Error(`Failed to create workflow: ${response.statusText}`);
    }
    return await response.json();
  }

  async updateWorkflow(workflowId, status) {
    const response = await fetch(
      `${this.baseUrl}/v1/workflows/${workflowId}`,
      {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          workflow: { id: workflowId, status }
        })
      }
    );

    if (!response.ok) {
      throw new Error(`Failed to update workflow: ${response.statusText}`);
    }
    return await response.json();
  }

  async deleteWorkflow(workflowId) {
    const response = await fetch(
      `${this.baseUrl}/v1/workflows/${workflowId}`,
      { method: 'DELETE' }
    );

    if (!response.ok) {
      throw new Error(`Failed to delete workflow: ${response.statusText}`);
    }
  }

  async *streamUsersOnline() {
    const response = await fetch(`${this.baseUrl}/v1/users/online`);
    if (!response.ok) {
      throw new Error(`Failed to stream users: ${response.statusText}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const text = decoder.decode(value, { stream: true });
        const lines = text.split('\n');

        for (const line of lines) {
          if (line.trim()) {
            yield JSON.parse(line);
          }
        }
      }
    } finally {
      reader.releaseLock();
    }
  }
}

// Usage
const client = new WorkflowHttpClient('/api');
const workflow = await client.getWorkflow('workflow-123');
const workflows = await client.listWorkflows('Active');
```

---

## OpenAPI/Swagger Integration

### 1. Add Swagger Documentation

```csharp
// AF.ECT.Server/Program.cs

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ECTSystem Workflow API",
        Version = "v1",
        Description = "REST API for Workflow management (via gRPC JSON transcoding)",
        Contact = new OpenApiContact
        {
            Name = "ALOD ECT Team",
            Email = "support@example.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Workflow API v1");
        options.RoutePrefix = "swagger";
    });
}

app.Run();
```

### 2. Swagger UI will show:

```
GET  /v1/workflows/{id}             - Get single workflow
GET  /v1/workflows                  - List workflows with filters
POST /v1/workflows                  - Create new workflow
PATCH /v1/workflows/{workflow.id}   - Update workflow
DELETE /v1/workflows/{id}           - Delete workflow
GET  /v1/users/online               - Stream online users
```

---

## Best Practices

1. **Consistent Naming**: Use snake_case in protobuf, automatically converted to camelCase in JSON
2. **Error Handling**: Proper HTTP status codes (400, 404, 500) mapped from gRPC StatusCodes
3. **Documentation**: Include comments in .proto files for auto-generated Swagger docs
4. **Versioning**: Include API version in routes (`/v1/`, `/v2/`)
5. **Security**: Enable authentication/authorization on endpoints
6. **CORS**: Configure appropriately for browser clients
7. **Content Negotiation**: Support both application/json and gRPC Content-Type headers

---

## Troubleshooting

### Issue: "400 Bad Request - Could not find route"
**Solution**: Ensure proto file has correct google.api.http annotations

### Issue: "Streaming not working over HTTP"
**Solution**: Use Server-Sent Events (SSE) for HTTP streaming instead of raw streaming

### Issue: "Content-Type mismatch"
**Solution**: Always set `Content-Type: application/json` for requests
