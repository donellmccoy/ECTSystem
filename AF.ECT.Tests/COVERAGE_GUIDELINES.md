# Test Coverage Guidelines for ECTSystem

## Overview
This document defines test coverage targets and strategies for the ECTSystem project to ensure military-grade reliability and compliance with audit logging requirements.

## Coverage Targets by Module

| Module | Target Coverage | Priority | Strategy |
|--------|-----------------|----------|----------|
| **AF.ECT.Server** | 75%+ | High | gRPC service implementations, workflow logic |
| **AF.ECT.Shared** | 85%+ | Critical | WorkflowClient (widely used), audit logging |
| **AF.ECT.Data** | 70%+ | High | EF Core contexts, stored procedures |
| **AF.ECT.WebClient** | 65%+ | Medium | Blazor components, client-side logic |
| **AF.ECT.WindowsClient** | 60%+ | Medium | WinUI desktop features |
| **AF.ECT.MobileClient** | 60%+ | Medium | MAUI mobile features |

## Coverage Calculation

Coverage is calculated using **Coverlet** with the following formula:

```
Coverage = (Lines Covered / Total Lines) × 100
```

Excluded from coverage:
- Test projects (`*.Tests`)
- Auto-generated code (protobuf, gRPC)
- Compiler-generated attributes
- Obsolete code

## Test Pyramid Structure

Target distribution of tests:

```
Unit Tests:     70% (Fast, mocked dependencies)
Integration:    25% (Real components, mocked external)
E2E:            5%  (Real system, all dependencies)
```

### Unit Tests (70%)
- **Goal**: Fast execution, isolated component testing
- **Examples**: Service methods, utility functions, business logic
- **Setup**: Mocked dependencies, in-memory data
- **Target Time**: <100ms per test
- **Tools**: xUnit, Moq, FluentAssertions

### Integration Tests (25%)
- **Goal**: Verify component interaction, database operations
- **Examples**: gRPC endpoints with real channel, EF Core queries
- **Setup**: Real database (in-memory or Testcontainers), real gRPC channel
- **Target Time**: <1s per test
- **Tools**: xUnit, WebApplicationFactory, TestServer

### E2E Tests (5%)
- **Goal**: Verify complete workflows, audit trails, compliance
- **Examples**: User registration → workflow creation → completion
- **Setup**: Real system, real database, real services
- **Target Time**: <10s per test
- **Tools**: xUnit with real AppHost, Selenium/Playwright for Blazor

## Coverage Expectations by Feature Area

### Workflow Management
- **Unit**: All service methods (75%+)
- **Integration**: gRPC endpoints with mock data (65%+)
- **E2E**: Complete workflow scenarios (50%+)
- **Negative Tests**: Error handling, invalid inputs, state transitions

### User Management
- **Unit**: User validation, permission checks (80%+)
- **Integration**: User search, registration flows (70%+)
- **E2E**: Multi-step user operations (50%+)
- **Audit**: Verify user actions logged with correlation IDs

### Audit Logging
- **Unit**: Audit event creation (90%+)
- **Integration**: End-to-end audit trail capture (80%+)
- **E2E**: Verify compliance with military standards (70%+)
- **Coverage**: All data-modifying operations must be audited

### Resilience Policies
- **Unit**: Polly policy configuration (85%+)
- **Integration**: Retry/fallback behavior under failure (75%+)
- **E2E**: System recovery from transient failures (50%+)

## Running Coverage Reports

### Generate Coverage Report
```bash
dotnet test --collect:"XPlat Code Coverage;Format=opencover" /p:CollectCoverage=true
```

### View Coverage Report
```bash
reportgenerator -reports:"path/to/coverage.xml" -reporttypes:"Html" -targetdir:"path/to/report"
```

### CI/CD Integration
Coverage reports are automatically generated on each PR and compared against baseline targets.

## Best Practices

1. **Test Naming**: Use `[Method]_[Scenario]_[ExpectedResult]` convention
   ```csharp
   public void GetUserName_ValidInput_ReturnsUser() { }
   ```

2. **Test Isolation**: Use builders and fixtures to eliminate setup duplication
   ```csharp
   var request = RequestBuilder.CreateUserNameRequest("John", "Doe");
   var response = await client.GetUserNameAsync(request);
   ```

3. **Assertion Quality**: Use FluentAssertions for readable assertions
   ```csharp
   response.Items.Should().HaveCount(1)
       .And.ContainSingle(x => x.FirstName == "John");
   ```

4. **Arrange-Act-Assert**: Structure all tests for clarity
   ```csharp
   // Arrange
   var data = SetupTestData();
   
   // Act
   var result = await sut.MethodUnderTest(data);
   
   // Assert
   result.Should().Be(expected);
   ```

5. **Audit Verification**: Always verify audit logs for sensitive operations
   ```csharp
   var auditLog = await GetAuditLogAsync(correlationId);
   auditLog.Should().NotBeNull()
       .And.Subject.EventType.Should().Be("EF:User");
   ```

## Coverage Thresholds

Configured in `coverlet.runsettings`:

```xml
<Threshold>70</Threshold>  <!-- Minimum project coverage -->
```

CI/CD will fail if:
- Overall coverage drops below 70%
- Coverage for critical modules (Server, Shared) drops below targets
- Any test fails

## Monitoring and Reporting

### Dashboard Metrics
- Total line coverage (target: 75%+)
- Branch coverage (target: 70%+)
- Method coverage (target: 80%+)
- Coverage trend over time

### Regular Reviews
- Weekly: Review coverage reports
- Monthly: Analyze coverage gaps
- Quarterly: Adjust coverage targets based on project evolution

## Contributing

When adding new features:
1. Write tests first (TDD approach recommended)
2. Ensure coverage meets module target
3. Document audit requirements
4. Add integration test if component interacts with other services
5. Get code review approval before merge

## References

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/Moq/moq4)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [gRPC Testing Best Practices](https://grpc.io/docs/guides/performance-best-practices/)
