# SDLC Custom Agents for VS Code Copilot

This directory contains custom agents for each phase of the Software Development Life Cycle (SDLC). These agents provide specialized AI assistance tailored to specific development tasks.

## Available Agents

### 1. 📋 Planning Agent
**File**: [planning.agent.md](planning.agent.md)

**Purpose**: Define project scope, objectives, timelines, resources, and budget.

**When to Use**:
- Starting a new project or feature
- Defining project requirements and scope
- Creating project roadmaps
- Assessing feasibility and risks
- Resource and budget planning

**Tools**: Read-only (search, usages, fetch, githubRepo)

**Handoff**: → Requirements Analysis

---

### 2. 📝 Requirements Analysis Agent
**File**: [requirements-analysis.agent.md](requirements-analysis.agent.md)

**Purpose**: Gather and analyze detailed functional and non-functional requirements.

**When to Use**:
- Gathering user requirements
- Creating use cases and user stories
- Documenting functional and non-functional requirements
- Validating requirements with stakeholders
- Creating acceptance criteria

**Tools**: Read-only plus documentation (search, usages, fetch, read_file)

**Handoff**: → Design

---

### 3. 🏗️ Design Agent
**File**: [design.agent.md](design.agent.md)

**Purpose**: Create system architecture, detailed design, and technical blueprints.

**When to Use**:
- Designing system architecture
- Creating database schemas
- Designing APIs and interfaces
- Creating UI/UX wireframes
- Security and scalability design
- Documenting design decisions

**Tools**: Read and limited write (search, usages, fetch, read_file, replace_string_in_file, create_file)

**Handoff**: → Implementation

---

### 4. 💻 Implementation Agent
**File**: [implementation.agent.md](implementation.agent.md)

**Purpose**: Write production-quality code based on design specifications.

**When to Use**:
- Writing new features
- Implementing designs
- Refactoring code
- Fixing bugs
- Integrating components
- Code optimization

**Tools**: Full development capabilities (all read/write tools, terminal, error checking)

**Handoff**: → Testing

---

### 5. ✅ Testing Agent
**File**: [testing.agent.md](testing.agent.md)

**Purpose**: Create and execute comprehensive tests to ensure software quality.

**When to Use**:
- Writing unit tests
- Creating integration tests
- Performance testing
- Security testing
- Test automation
- Debugging test failures
- Code coverage analysis

**Tools**: Testing tools (read/write, runTests, terminal, error checking)

**Handoff**: → Deployment

---

### 6. 🚀 Deployment Agent
**File**: [deployment.agent.md](deployment.agent.md)

**Purpose**: Deploy applications to production and manage release processes.

**When to Use**:
- Deploying to production
- Setting up CI/CD pipelines
- Managing environments
- Creating deployment scripts
- Infrastructure as code
- Rollback procedures
- Release management

**Tools**: Deployment tools (search, read_file, fetch, terminal)

**Handoff**: → Maintenance

---

### 7. 🔧 Maintenance Agent
**File**: [maintenance.agent.md](maintenance.agent.md)

**Purpose**: Monitor, maintain, and optimize deployed applications.

**When to Use**:
- Monitoring application health
- Investigating production issues
- Performance optimization
- Applying security patches
- Updating dependencies
- Incident response
- Continuous improvement

**Tools**: Monitoring and optimization tools (read/write, terminal, error checking)

**Handoff**: → Planning (for new enhancements)

---

## How to Use These Agents

### Switching to an Agent

1. Open the Chat view in VS Code
2. Click the agents dropdown (@ symbol)
3. Select the desired SDLC agent

Alternatively, type `@` followed by the agent name in chat:
```
@sdlc-planning
@requirements-analysis
@design
@implementation
@testing
@deployment
@maintenance
```

### Using Handoffs

Each agent has a handoff button that appears after completing a task. This allows you to seamlessly transition to the next phase:

1. Complete your current task (e.g., create a plan)
2. Click the handoff button (e.g., "Start Requirements Analysis")
3. The agent switches and pre-fills a relevant prompt
4. Review the prompt and send or modify as needed

### Example Workflow

**Complete SDLC Cycle**:
1. `@sdlc-planning` - Create project plan
2. Click "Start Requirements Analysis"
3. `@requirements-analysis` - Document requirements
4. Click "Start Design Phase"
5. `@design` - Create architecture and design
6. Click "Start Implementation"
7. `@implementation` - Write code
8. Click "Start Testing"
9. `@testing` - Create and run tests
10. Click "Proceed to Deployment"
11. `@deployment` - Deploy to production
12. Click "Start Maintenance"
13. `@maintenance` - Monitor and maintain

**Or jump to specific phases as needed**:
```
@implementation Fix the authentication bug in WorkflowService
@testing Create integration tests for the gRPC workflow service
@deployment Set up Azure deployment for the ECT System
```

## Agent Characteristics

| Agent | Read-Only | Can Edit Code | Can Run Commands | Primary Focus |
|-------|-----------|---------------|------------------|---------------|
| Planning | ✅ | ❌ | ❌ | Strategy & Roadmap |
| Requirements Analysis | ✅ | ❌ | ❌ | Requirements & Specs |
| Design | ⚠️ | Limited | ❌ | Architecture & Design |
| Implementation | ❌ | ✅ | ✅ | Coding & Development |
| Testing | ❌ | ✅ | ✅ | Quality Assurance |
| Deployment | ⚠️ | Limited | ✅ | Release Management |
| Maintenance | ❌ | ✅ | ✅ | Operations & Support |

## Best Practices

### 1. Use the Right Agent for the Task
- Don't use Implementation agent for planning
- Don't use Planning agent for code changes
- Match the agent to the SDLC phase

### 2. Follow the Handoff Workflow
- Complete one phase before moving to the next
- Use handoff buttons for smooth transitions
- Review and validate outputs at each stage

### 3. Provide Clear Context
- Give agents specific details about what you need
- Reference requirements when implementing
- Link design specs when coding
- Mention test coverage goals when testing

### 4. Leverage Agent Expertise
- Each agent has specialized knowledge
- Trust agent recommendations for their domain
- Ask agents to explain their rationale

### 5. Document as You Go
- Use Planning agent for documentation
- Requirements agent for specifications
- Design agent for architecture docs
- Implementation agent for code comments

## Customization

You can customize these agents by editing the `.agent.md` files:

- **Add Tools**: Include additional tools in the `tools` array
- **Modify Instructions**: Edit the agent body text
- **Change Handoffs**: Update handoff destinations and prompts
- **Adjust Model**: Change the AI model used

## Integration with ECTSystem

These agents are specifically configured for the ECTSystem project:

- Follow Microsoft Best Practices
- Use .NET 9.0 conventions
- Support gRPC, Blazor, and .NET Aspire
- Include SQL Server and EF Core patterns
- Emphasize security and audit logging
- Build verification for code changes

## Troubleshooting

**Agent not appearing?**
- Ensure the `.agent.md` file is in `.github/agents/` directory
- Check that the file has a valid YAML frontmatter
- Reload VS Code window

**Handoff not working?**
- Verify target agent name matches file name
- Check YAML syntax in handoffs section
- Ensure target agent file exists

**Wrong tools available?**
- Review `tools` array in agent frontmatter
- Ensure tool names are spelled correctly
- Check that required extensions are installed

## Additional Resources

- [VS Code Custom Agents Documentation](https://code.visualstudio.com/docs/copilot/customization/custom-agents)
- [ECTSystem Coding Instructions](.github/copilot-instructions.md)
- [VS Code Copilot Skills](.github/skills/)

---

**Created**: January 25, 2026
**Purpose**: Structured SDLC workflow with specialized AI agents
**Maintained by**: ECTSystem Development Team
