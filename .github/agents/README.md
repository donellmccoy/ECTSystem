# Bug Fix Workflow Custom Agents for VS Code Copilot

This directory contains custom agents for each phase of the bug fix lifecycle. These agents provide specialized AI assistance tailored to fixing software defects in Azure DevOps environments.

## Available Agents

### 1. 🐛 Bug Fix Planning
**File**: [planning.agent.md](planning.agent.md)

**Purpose**: Analyze and plan bug fixes with impact assessment and remediation strategy.

**When to Use**:
- Triaging new bugs
- Assessing bug severity and priority
- Planning hotfix vs regular fix approach
- Creating bug fix timelines
- Risk assessment for fixes

**Tools**: Read-only plus Azure DevOps (search, usages, fetch, read_file, get_errors, mcp_microsoft_azu/*)

**Handoff**: → Root Cause Analysis

---

### 2. 🔍 Root Cause Analysis
**File**: [requirements-analysis.agent.md](requirements-analysis.agent.md)

**Purpose**: Perform deep-dive root cause analysis and investigation for bugs.

**When to Use**:
- Investigating why bugs occur
- Analyzing logs and stack traces
- Tracing code execution paths
- Finding when bugs were introduced
- Identifying contributing factors
- Documenting reproduction steps

**Tools**: Investigation tools (search, usages, fetch, read_file, get_errors, grep_search, mcp_microsoft_azu/*)

**Handoff**: → Bug Fix Design

---

### 3. 🎯 Bug Fix Design
**File**: [design.agent.md](design.agent.md)

**Purpose**: Design solutions and remediation strategies for bug fixes.

**When to Use**:
- Designing the fix approach
- Evaluating fix alternatives
- Assessing regression risk
- Planning test strategies
- Determining hotfix vs proper fix
- Designing deployment strategy

**Tools**: Design tools (search, usages, fetch, read_file, list_code_usages, grep_search, mcp_microsoft_azu/*)

**Handoff**: → Bug Fix Implementation

---

### 4. ⚡ Bug Fix Implementation
**File**: [implementation.agent.md](implementation.agent.md)

**Purpose**: Implement bug fixes with precision and minimal risk.

**When to Use**:
- Writing the actual fix code
- Making surgical code changes
- Adding defensive checks
- Implementing error handling
- Creating bug fix branches
- Linking commits to work items

**Tools**: Full development capabilities (all read/write tools, terminal, error checking, mcp_microsoft_azu/*)

**Handoff**: → Regression Testing

---

### 5. ✅ Regression Testing
**File**: [testing.agent.md](testing.agent.md)

**Purpose**: Create regression tests to verify bug fixes and prevent recurrence.

**When to Use**:
- Writing tests for the bug fix
- Creating regression test suites
- Verifying fix doesn't break other features
- Testing edge cases
- Running test suites in Azure Pipelines
- Validating fix effectiveness

**Tools**: Testing tools (read/write, runTests, terminal, error checking, mcp_microsoft_azu/*)

**Handoff**: → Hotfix Deployment

---

### 6. 🚀 Hotfix Deployment
**File**: [deployment.agent.md](deployment.agent.md)

**Purpose**: Deploy bug fixes and hotfixes to production with minimal risk.

**When to Use**:
- Deploying critical hotfixes
- Planning phased rollouts
- Setting up hotfix pipelines
- Executing emergency deployments
- Managing rollback procedures
- Monitoring deployment progress

**Tools**: Deployment tools (search, read_file, fetch, terminal, mcp_microsoft_azu/*)

**Handoff**: → Post-Fix Monitoring

---

### 7. 📊 Post-Fix Monitoring
**File**: [maintenance.agent.md](maintenance.agent.md)

**Purpose**: Monitor deployed bug fixes and verify effectiveness.

**When to Use**:
- Verifying bug is resolved in production
- Monitoring error rates post-deployment
- Checking Application Insights for new issues
- Validating no regressions introduced
- Gathering effectiveness metrics
- Planning follow-up fixes if needed

**Tools**: Monitoring tools (search, read_file, fetch, terminal, mcp_microsoft_azu/*)

**Handoff**: → Plan Next Bug Fix (back to planning)

---

## How to Use These Agents

### Switching to an Agent

1. Open the Chat view in VS Code
2. Click the agents dropdown (@ symbol)
3. Select the desired bug fix agent

Alternatively, type `@` followed by the agent name in chat:
```
@bug-fix-planning
@root-cause-analysis
@bug-fix-design
@bug-fix-implementation
@regression-testing
@hotfix-deployment
@post-fix-monitoring
```

### Using Handoffs

Each agent has a handoff button that appears after completing a task. This creates a seamless bug fix workflow:

1. Complete your current task (e.g., identify root cause)
2. Click the handoff button (e.g., "Design Bug Fix")
3. The agent switches and pre-fills a relevant prompt
4. Review the prompt and send or modify as needed

### Example Workflow

**Complete Bug Fix Lifecycle**:
1. `@bug-fix-planning` - Triage and plan bug fix for work item #12345
2. Click "Start Root Cause Analysis"
3. `@root-cause-analysis` - Investigate and find root cause
4. Click "Design Bug Fix"
5. `@bug-fix-design` - Design minimal-risk fix approach
6. Click "Implement Bug Fix"
7. `@bug-fix-implementation` - Write the fix code
8. Click "Test Bug Fix"
9. `@regression-testing` - Create and run regression tests
10. Click "Deploy Hotfix"
11. `@hotfix-deployment` - Deploy to production
12. Click "Monitor Post-Fix"
13. `@post-fix-monitoring` - Verify fix effectiveness

**Or jump to specific phases as needed**:
```
@root-cause-analysis Investigate why users can't login after deployment
@bug-fix-implementation Fix the null reference exception in WorkflowService
@regression-testing Create tests for work item #12345 fix
@hotfix-deployment Deploy critical fix for P0 bug
```

## Agent Characteristics

| Agent | Read-Only | Can Edit Code | Can Run Commands | Primary Focus |
|-------|-----------|---------------|------------------|---------------|
| Bug Fix Planning | ✅ | ❌ | ❌ | Triage & Strategy |
| Root Cause Analysis | ✅ | ❌ | ❌ | Investigation & Analysis |
| Bug Fix Design | ⚠️ | Limited | ❌ | Solution Design & Risk Assessment |
| Bug Fix Implementation | ❌ | ✅ | ✅ | Coding Fixes |
| Regression Testing | ❌ | ✅ | ✅ | Test Creation & Execution |
| Hotfix Deployment | ⚠️ | Limited | ✅ | Emergency Releases |
| Post-Fix Monitoring | ⚠️ | Limited | ✅ | Verification & Metrics |

## Best Practices

### 1. Use the Right Agent for the Task
- Don't use Implementation agent for planning
- Don't use Planning agent for code changes
- Match the agent to the bug fix lifecycle phase

### 2. Follow the Handoff Workflow
- Complete one phase before moving to the next
- Use handoff buttons for smooth transitions
- Review and validate outputs at each stage

### 3. Provide Clear Context
- Reference Azure DevOps work item numbers
- Include error messages and stack traces
- Mention affected users and business impact
- Link to Application Insights data when available

### 4. Leverage Azure DevOps Integration
- All agents can access Azure DevOps tools
- Link work items, commits, and PRs
- Use Azure Pipelines for deployments
- Track progress in Azure Boards

### 5. Document as You Go
- Use Bug Fix Planning agent for triage documentation
- Root Cause Analysis agent for investigation reports
- Bug Fix Design agent for solution documentation
- Update Azure DevOps work items with findings

## Bug Fix Workflow

```
┌─────────────────────────────────────────────────────────────┐
│                    Bug Fixed Lifecycle                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
               ┌──────────────────────────┐
               │  1. Bug Fix Planning     │
               │  - Triage & prioritize   │
               │  - Assess impact         │
               │  - Create fix timeline   │
               └──────────────────────────┘
                              │
                              ▼
               ┌──────────────────────────┐
               │  2. Root Cause Analysis  │
               │  - Investigate logs      │
               │  - Trace code paths      │
               │  - Find when introduced  │
               └──────────────────────────┘
                              │
                              ▼
               ┌──────────────────────────┐
               │  3. Bug Fix Design       │
               │  - Design solution       │
               │  - Assess risks          │
               │  - Choose approach       │
               └──────────────────────────┘
                              │
                              ▼
               ┌──────────────────────────┐
               │  4. Bug Fix Impl         │
               │  - Write fix code        │
               │  - Create PR             │
               │  - Link to work item     │
               └──────────────────────────┘
                              │
                              ▼
               ┌──────────────────────────┐
               │  5. Regression Testing   │
               │  - Test fix works        │
               │  - No new regressions    │
               │  - Run in pipeline       │
               └──────────────────────────┘
                              │
                              ▼
               ┌──────────────────────────┐
               │  6. Hotfix Deployment    │
               │  - Deploy to prod        │
               │  - Monitor deployment    │
               │  - Verify health         │
               └──────────────────────────┘
                              │
                              ▼
               ┌──────────────────────────┐
               │  7. Post-Fix Monitoring  │
               │  - Verify effectiveness  │
               │  - Check metrics         │
               │  - Close work item       │
               └──────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │  Bug Resolved ✓  │
                    └──────────────────┘
```

## Customization

You can customize these agents by editing the `.agent.md` files:

- **Add Tools**: Include additional tools in the `tools` array
- **Modify Instructions**: Edit the agent body text
- **Change Handoffs**: Update handoff destinations and prompts
- **Adjust Model**: Change the AI model used

## Integration with ECTSystem

These agents are specifically configured for bug fixing in the ECTSystem project:

- Follow Microsoft Best Practices for bug fixes
- Use .NET 9.0 conventions and patterns
- Support gRPC, Blazor, and .NET Aspire architectures
- Include SQL Server and EF Core best practices
- Emphasize security and audit logging
- Build verification for all code changes
- Azure DevOps integration for work item tracking

## Azure DevOps Integration

All bug fix agents have access to Azure DevOps MCP tools:

### Work Items
- Retrieve bug details and history
- Update work item status and comments
- Link related work items
- Create child tasks for implementation

### Repositories
- Review recent commits and PRs
- Find when bugs were introduced
- Create bug fix branches
- Link commits to work items

### Pipelines
- Review build failures
- Check test results
- Deploy hotfixes
- Monitor deployment status

### Monitoring
- Query Application Insights
- Check Azure Monitor logs
- Review error rates and telemetry
- Verify fix effectiveness

## Troubleshooting

**Agent not appearing?**
- Ensure the `.agent.md` file is in `.github/agents/` directory
- Check that the file has a valid YAML frontmatter
- Reload VS Code window

**Handoff not working?**
- Verify target agent name matches file name (case-sensitive)
- Check YAML syntax in handoffs section
- Ensure target agent file exists

**Azure DevOps tools not working?**
- Verify Azure DevOps MCP extension is installed
- Check authentication to Azure DevOps
- Ensure project/organization access is configured

**Wrong tools available?**
- Review `tools` array in agent frontmatter
- Ensure tool names are spelled correctly
- Check that required extensions are installed

## Common Bug Fix Scenarios

### Critical Production Bug (P0)
```
@bug-fix-planning Critical: Login failures affecting all users
→ Triage, assess impact, create hotfix plan
→ @root-cause-analysis Find root cause in logs
→ @bug-fix-design Design minimal-risk fix
→ @bug-fix-implementation Implement fix
→ @regression-testing Verify fix works
→ @hotfix-deployment Deploy immediately
→ @post-fix-monitoring Verify effectiveness
```

### Regression After Deployment
```
@root-cause-analysis Feature broke after v2.1 deployment
→ Find recent commits that changed affected code
→ @bug-fix-design Design fix with regression tests
→ @bug-fix-implementation Implement and test
→ @hotfix-deployment Deploy with rollback plan
```

### Intermittent Bug Investigation
```
@root-cause-analysis Intermittent timeout errors in API
→ Analyze patterns, load conditions, timing
→ @bug-fix-design Design fix for race condition
→ @regression-testing Create tests to reproduce
→ @bug-fix-implementation Fix with proper locking
```

## Additional Resources

- [VS Code Custom Agents Documentation](https://code.visualstudio.com/docs/copilot/customization/custom-agents)
- [ECTSystem Coding Instructions](.github/copilot-instructions.md)
- [VS Code Copilot Skills](.github/skills/)
- [Azure DevOps MCP Documentation](https://github.com/microsoft/azure-devops-mcp)

---

**Created**: January 25, 2026
**Purpose**: Structured bug fix workflow with specialized AI agents
**Target**: Azure DevOps bug tracking and resolution
**Maintained by**: ECTSystem Development Team
