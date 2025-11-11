# Azure DevOps Self-Hosted Windows Agent Setup Guide

## Overview
This guide provides instructions for setting up a self-hosted Windows agent for the ECTSystem Azure DevOps pipeline. A self-hosted agent gives you more control over the environment, allowing you to install specific software, maintain consistent builds, and potentially reduce costs.

## Prerequisites

### System Requirements
- **Operating System**: 
  - Windows Server 2012 or higher (recommended: Windows Server 2022)
  - OR Windows 10/11 for development/testing
- **PowerShell**: Version 3.0 or higher (PowerShell 7+ recommended)
- **Disk Space**: Minimum 50 GB free space (recommended: 100+ GB for build artifacts and caching)
- **Memory**: Minimum 4 GB RAM (recommended: 8+ GB for .NET builds)
- **Network**: Stable internet connection with access to Azure DevOps Services

### Software Dependencies for ECTSystem
Based on the ECTSystem solution requirements:
- **.NET SDK**: .NET 9.0 SDK or later (as specified in global.json)
- **SQL Server**: SQL Server 2019 or later (for database projects)
- **Visual Studio Build Tools**: 2022 or later (recommended for full .NET 9.0 support)
- **Git**: Latest version
- **Node.js**: Latest LTS version (for Blazor WebAssembly tooling)

### Azure DevOps Permissions
You need one of the following:
- Azure DevOps organization owner
- Azure DevOps project administrator
- Agent pool administrator permissions

## Security Considerations

**CRITICAL**: Follow Microsoft security best practices:

1. **Least Privilege**: Run the agent service with minimal required permissions
2. **Folder Security**: Restrict agent folder access to administrators and the agent service account only
3. **Network Security**: Configure firewall rules to allow only required Azure DevOps endpoints
4. **Isolation**: Consider using dedicated machines for production agents
5. **Credentials**: Never store credentials in plain text; use Azure Key Vault or Windows Credential Manager

## Installation Steps

### Step 1: Prepare the Machine

1. **Create dedicated agent directory** (avoid spaces in path):
   ```powershell
   New-Item -Path "C:\agents" -ItemType Directory -Force
   ```

2. **Set proper folder permissions** (administrators only):
   ```powershell
   $acl = Get-Acl "C:\agents"
   $acl.SetAccessRuleProtection($true, $false)
   $adminRule = New-Object System.Security.AccessControl.FileSystemAccessRule("BUILTIN\Administrators","FullControl","ContainerInherit,ObjectInherit","None","Allow")
   $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT AUTHORITY\SYSTEM","FullControl","ContainerInherit,ObjectInherit","None","Allow")
   $acl.SetAccessRule($adminRule)
   $acl.SetAccessRule($systemRule)
   Set-Acl "C:\agents" $acl
   ```

### Step 2: Download the Agent

**Option A: Using Azure DevOps Portal**
1. Sign in to Azure DevOps: `https://dev.azure.com/{your-organization}`
2. Navigate to **Organization Settings** > **Agent pools**
3. Select **Default** pool (or create a new pool)
4. Click **New agent** button
5. Select **Windows** tab
6. Choose **x64** for 64-bit Windows
7. Click **Download** button

**Option B: Using PowerShell Script**
Use the automated setup script provided in this repository (see `Scripts/Setup-AzureDevOpsAgent.ps1`).

### Step 3: Extract the Agent

Extract the downloaded ZIP file to `C:\agents\agent1`:

```powershell
# Create agent directory
New-Item -Path "C:\agents\agent1" -ItemType Directory -Force

# Extract agent (update path to your downloaded file)
Expand-Archive -Path "$env:USERPROFILE\Downloads\vsts-agent-win-x64-*.zip" -DestinationPath "C:\agents\agent1" -Force
```

### Step 4: Configure the Agent

**IMPORTANT**: Run configuration from an **elevated PowerShell window** (Run as Administrator).

1. Navigate to agent directory:
   ```powershell
   cd C:\agents\agent1
   ```

2. Run configuration:
   ```powershell
   .\config.cmd
   ```

3. Answer the prompts:
   - **Server URL**: `https://dev.azure.com/{your-organization}`
   - **Authentication type**: PAT (Personal Access Token) - recommended
   - **Personal Access Token**: Enter a PAT with "Agent Pools (read, manage)" scope
   - **Agent pool**: `Default` (or your custom pool name)
   - **Agent name**: `ECTSystem-Agent-01` (or a unique descriptive name)
   - **Work folder**: Press Enter to accept default `_work`
   - **Run agent as service**: `Y` (recommended for production)
   - **User account**: Use a dedicated service account or press Enter for `NT AUTHORITY\NETWORK SERVICE`
   - **Enable SERVICE_SID_TYPE_UNRESTRICTED**: `N` (unless you have specific requirements)

### Step 5: Install ECTSystem Dependencies

After agent configuration, install required software:

```powershell
# Install .NET 9.0 SDK (check latest version)
winget install Microsoft.DotNet.SDK.9

# Install SQL Server (if not already installed)
# Download from: https://www.microsoft.com/sql-server/sql-server-downloads

# Install Visual Studio Build Tools 2022
winget install Microsoft.VisualStudio.2022.BuildTools

# Install Git
winget install Git.Git

# Install Node.js LTS
winget install OpenJS.NodeJS.LTS

# Verify installations
dotnet --list-sdks
git --version
node --version
```

### Step 6: Configure Firewall (if applicable)

If running behind a firewall, allow outbound HTTPS access to:

```
https://dev.azure.com
https://*.dev.azure.com
https://login.microsoftonline.com
https://management.core.windows.net
https://download.agent.dev.azure.com
https://vssps.dev.azure.com
https://*.blob.core.windows.net
https://*.vsassets.io
```

IP ranges to whitelist (IPv4):
```
13.107.6.0/24
13.107.9.0/24
13.107.42.0/24
13.107.43.0/24
150.171.22.0/24
150.171.23.0/24
150.171.73.0/24
150.171.74.0/24
150.171.75.0/24
150.171.76.0/24
```

### Step 7: Start the Agent

**If configured as a service** (recommended):
- The agent starts automatically
- View status: `services.msc` and look for "Azure Pipelines Agent (ECTSystem-Agent-01)"
- Restart: Right-click service > Restart

**If configured for interactive mode**:
```powershell
cd C:\agents\agent1
.\run.cmd
```

### Step 8: Verify Agent Registration

1. Go to Azure DevOps: `https://dev.azure.com/{your-organization}`
2. Navigate to **Organization Settings** > **Agent pools** > **Default**
3. Click **Agents** tab
4. Verify your agent appears with status "Online"
5. Click agent name to view capabilities

## Unattended Configuration (Advanced)

For automated deployments, use unattended configuration:

```powershell
# Example: Configure agent without prompts
.\config.cmd --unattended `
  --url "https://dev.azure.com/{your-organization}" `
  --auth pat `
  --token "{your-PAT-token}" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --runAsService `
  --windowsLogonAccount "NT AUTHORITY\NETWORK SERVICE" `
  --work "_work" `
  --replace
```

**Security Note**: Never commit PAT tokens to source control. Use Azure Key Vault or environment variables.

## Multiple Agents on One Machine

To run multiple agents:

1. Extract to separate directories:
   ```
   C:\agents\agent1
   C:\agents\agent2
   C:\agents\agent3
   ```

2. Configure each with unique names:
   - `ECTSystem-Agent-01`
   - `ECTSystem-Agent-02`
   - `ECTSystem-Agent-03`

3. Each will run as a separate service

## Updating the Agent

Agents auto-update when running tasks requiring newer versions. To manually update:

1. In Azure DevOps, go to **Agent pools** > **Default**
2. Right-click pool > **Update all agents**

## Removing an Agent

To remove/reconfigure:

```powershell
cd C:\agents\agent1
.\config.cmd remove --auth pat --token {your-PAT-token}
```

## Troubleshooting

### Agent Won't Start
1. Check Windows Event Viewer for errors
2. Verify service account has correct permissions
3. Check `C:\agents\agent1\_diag` folder for diagnostic logs

### Build Failures
1. Verify all required software is installed (check capabilities)
2. Ensure agent has network access to required resources
3. Check agent capabilities match pipeline demands

### Connection Issues
1. Verify firewall allows required URLs/IPs
2. Test connectivity: `Test-NetConnection dev.azure.com -Port 443`
3. Check proxy settings if behind corporate proxy

### Run Diagnostics
```powershell
cd C:\agents\agent1
.\run.cmd --diagnostics
```

## Agent Capabilities

After installation, verify the agent has required capabilities:

1. Go to **Agent pools** > **Default** > **Agents** > {your-agent}
2. Click **Capabilities** tab
3. Verify:
   - `Agent.Version` (should be latest)
   - `dotnet` (should show .NET 9.0)
   - `Git` (should be present)
   - `MSBuild` (if Visual Studio Build Tools installed)
   - `SqlCmd` (if SQL Server tools installed)

## Environment Variables

Create `.env` file in agent root directory for custom environment variables:

```
# C:\agents\agent1\.env
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
NUGET_PACKAGES=C:\agents\.nuget\packages
```

Restart agent after creating `.env` file.

## Best Practices for ECTSystem

1. **Dedicated Build Agent**: Use dedicated machines for production builds
2. **Resource Allocation**: Allocate 2-4 CPUs and 8-16 GB RAM per agent
3. **Workspace Cleanup**: Enable automatic workspace cleanup in pipelines
4. **Agent Pools**: Create separate pools for development, staging, and production
5. **Monitoring**: Set up alerts for agent offline status
6. **Maintenance Window**: Schedule regular maintenance for Windows updates
7. **Backup**: Document agent configuration for disaster recovery

## Integration with ECTSystem Pipelines

To use the self-hosted agent in your pipelines, specify the pool in your `azure-pipelines.yml`:

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  name: 'Default'  # Or your custom pool name
  demands:
  - agent.name -equals ECTSystem-Agent-01  # Optional: target specific agent

variables:
  buildConfiguration: 'Release'
  solution: 'ECTSystem.sln'

steps:
- task: UseDotNet@2
  displayName: 'Use .NET 9.0'
  inputs:
    version: '9.0.x'
    packageType: sdk

- task: DotNetCoreCLI@2
  displayName: 'Restore NuGet packages'
  inputs:
    command: 'restore'
    projects: '$(solution)'

- task: DotNetCoreCLI@2
  displayName: 'Build solution'
  inputs:
    command: 'build'
    projects: '$(solution)'
    arguments: '--configuration $(buildConfiguration) --no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Run tests'
  inputs:
    command: 'test'
    projects: 'AF.ECT.Tests/*.csproj'
    arguments: '--configuration $(buildConfiguration) --no-build'
```

## Additional Resources

- [Azure Pipelines Agent GitHub](https://github.com/microsoft/azure-pipelines-agent)
- [Microsoft Learn: Self-hosted Windows agents](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/windows-agent)
- [Agent authentication options](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/agent-authentication-options)
- [Azure DevOps Service endpoints](https://learn.microsoft.com/en-us/azure/devops/organizations/security/allow-list-ip-url)

## Support

For issues specific to ECTSystem agent setup:
1. Check this documentation and troubleshooting section
2. Review agent diagnostic logs in `C:\agents\agent1\_diag`
3. Contact the DevOps team or create an issue in the repository
