# Azure DevOps Self-Hosted Agent Troubleshooting Guide

## Overview
This guide helps resolve common issues when setting up and running Azure DevOps self-hosted Windows agents for the ECTSystem project.

## Quick Diagnostics Checklist

Before troubleshooting specific issues, run these quick checks:

```powershell
# 1. Check if agent service is running
Get-Service "vstsagent.*" | Format-Table Name, Status, StartType

# 2. Check agent directory exists and has proper permissions
Get-Acl "C:\agents\agent1" | Format-List

# 3. Test Azure DevOps connectivity
Test-NetConnection -ComputerName "dev.azure.com" -Port 443

# 4. Run agent diagnostics
cd C:\agents\agent1
.\run.cmd --diagnostics

# 5. Check latest diagnostic log
Get-Content (Get-ChildItem "C:\agents\agent1\_diag" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -Tail 100
```

---

## Common Issues and Solutions

### 1. Agent Configuration Fails

#### Symptom
`.\config.cmd` fails with authentication or connection errors.

#### Possible Causes & Solutions

**A. Invalid or Expired PAT Token**
```powershell
# Error: "Failed to authenticate using the supplied token"
# Solution: Generate new PAT with correct scopes

# 1. Go to Azure DevOps
# 2. User Settings > Personal Access Tokens
# 3. Create new token with:
#    - Scope: Agent Pools (read, manage)
#    - Expiration: 90 days or custom
# 4. Copy token immediately (shown only once)
# 5. Retry configuration
```

**B. Incorrect Organization URL**
```powershell
# Error: "Cannot connect to server"
# Solution: Verify URL format

# Correct formats:
https://dev.azure.com/yourorganization
# NOT:
https://dev.azure.com/yourorganization/
https://yourorganization.visualstudio.com  # Old format, but may still work
```

**C. Network/Firewall Blocking**
```powershell
# Test connectivity to required endpoints
$endpoints = @(
    "dev.azure.com",
    "login.microsoftonline.com",
    "vssps.dev.azure.com"
)

foreach ($endpoint in $endpoints) {
    $result = Test-NetConnection -ComputerName $endpoint -Port 443
    Write-Host "$endpoint : $($result.TcpTestSucceeded)" -ForegroundColor $(if($result.TcpTestSucceeded){'Green'}else{'Red'})
}

# If tests fail, configure firewall/proxy
# See "Network and Firewall Issues" section below
```

**D. Insufficient Permissions**
```powershell
# Error: "Access denied" or "Insufficient permissions"
# Solution: Verify you have Agent Pool Administrator role

# Check in Azure DevOps:
# 1. Organization Settings > Agent pools > Default > Security
# 2. Add your user with "Administrator" role
# 3. Retry configuration
```

**E. PowerShell Version Too Old**
```powershell
# Check PowerShell version
$PSVersionTable.PSVersion

# Required: 5.1 or higher
# Update if needed:
# - Windows PowerShell 5.1: Part of Windows Management Framework 5.1
# - PowerShell 7+: Download from https://github.com/PowerShell/PowerShell
```

---

### 2. Agent Service Won't Start

#### Symptom
Agent configures successfully but service fails to start or shows "Stopped" status.

#### Possible Causes & Solutions

**A. Service Account Permissions**
```powershell
# Check service configuration
Get-WmiObject Win32_Service | Where-Object {$_.Name -like "vstsagent.*"} | Format-List Name, State, StartName

# If using domain/local account, verify:
# 1. Account has "Log on as a service" right
# 2. Account has permissions to agent directory

# Grant "Log on as a service":
# Run: secpol.msc
# Navigate: Local Policies > User Rights Assignment > Log on as a service
# Add your service account

# Fix directory permissions:
$agentPath = "C:\agents\agent1"
$serviceAccount = "DOMAIN\ServiceAccount"  # Replace with your account
$acl = Get-Acl $agentPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $serviceAccount,
    "FullControl",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $agentPath $acl
```

**B. Missing Dependencies**
```powershell
# Check for .NET runtime errors in Event Viewer
Get-EventLog -LogName Application -Source "VSTSAgent*" -Newest 10 | Format-Table TimeGenerated, Message -AutoSize

# Install missing dependencies:
# - .NET SDK 9.0
winget install Microsoft.DotNet.SDK.9

# Restart service
Restart-Service -Name "vstsagent.*"
```

**C. Port Already in Use**
```powershell
# Check if agent listener port is in use
netstat -ano | findstr "LISTENING"

# Agent doesn't typically listen on ports, but check logs:
Get-Content "C:\agents\agent1\_diag\*.log" | Select-String "port" -Context 2
```

**D. Corrupted Configuration**
```powershell
# Remove and reconfigure agent
cd C:\agents\agent1
.\config.cmd remove --auth pat --token "your-pat-token"

# Reconfigure
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService `
  --windowsLogonAccount "NT AUTHORITY\NETWORK SERVICE"
```

---

### 3. Agent Shows Offline in Azure DevOps

#### Symptom
Agent is configured and service is running, but shows "Offline" in portal.

#### Possible Causes & Solutions

**A. Agent Can't Connect to Azure DevOps**
```powershell
# Check agent logs for connection errors
Get-Content (Get-ChildItem "C:\agents\agent1\_diag" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName | Select-String "error|exception|failed" -Context 2

# Common connection errors:
# - Proxy configuration needed
# - Firewall blocking
# - Network policy restrictions

# Test direct connectivity
Test-NetConnection -ComputerName "dev.azure.com" -Port 443
```

**B. Proxy Configuration Required**
```powershell
# If behind corporate proxy, configure agent proxy settings
cd C:\agents\agent1

# Create/edit .proxyurl file
"http://proxy.company.com:8080" | Set-Content ".proxyurl"

# Create/edit .proxycredentials file (if authentication required)
"username:password" | Set-Content ".proxycredentials"

# Restart agent
Restart-Service -Name "vstsagent.*"
```

**C. Certificate/SSL Issues**
```powershell
# If using corporate SSL inspection
# Export corporate root CA certificate and install

# Then configure agent to trust custom CA:
# Set environment variable
[Environment]::SetEnvironmentVariable("NODE_EXTRA_CA_CERTS", "C:\path\to\corporate-ca.pem", "Machine")

# Restart agent service
Restart-Service -Name "vstsagent.*"
```

**D. Agent Listener Crashed**
```powershell
# Check if Agent.Listener.exe is running
Get-Process Agent.Listener -ErrorAction SilentlyContinue

# If not running, check Windows Event Viewer for crash details
Get-EventLog -LogName Application -Source "Application Error" -Newest 10 | Where-Object {$_.Message -like "*Agent.Listener*"}

# Restart service
Restart-Service -Name "vstsagent.*"
```

---

### 4. Build/Pipeline Failures

#### Symptom
Agent is online but builds fail with various errors.

#### Possible Causes & Solutions

**A. Missing Capabilities/Software**
```powershell
# Check what software is available
# In Azure DevOps: Agent pools > Default > Agents > [Your Agent] > Capabilities

# Install missing software for ECTSystem:

# .NET SDK
winget install Microsoft.DotNet.SDK.9

# Git
winget install Git.Git

# Node.js
winget install OpenJS.NodeJS.LTS

# Visual Studio Build Tools
winget install Microsoft.VisualStudio.2022.BuildTools

# SQL Server tools (if needed)
# Download from: https://www.microsoft.com/sql-server/sql-server-downloads

# Restart agent to refresh capabilities
Restart-Service -Name "vstsagent.*"
```

**B. Insufficient Disk Space**
```powershell
# Check available disk space
Get-PSDrive C | Select-Object @{N="Free(GB)";E={[math]::Round($_.Free/1GB,2)}}, @{N="Used(GB)";E={[math]::Round($_.Used/1GB,2)}}

# Clean up if needed:
# 1. Clean agent work directory (careful!)
Remove-Item "C:\agents\agent1\_work\*" -Recurse -Force -Confirm

# 2. Clean NuGet cache
dotnet nuget locals all --clear

# 3. Clean temp files
Remove-Item $env:TEMP\* -Recurse -Force -ErrorAction SilentlyContinue

# 4. Run Windows Disk Cleanup
cleanmgr /d C:
```

**C. Permissions Issues in Build**
```powershell
# If builds fail with "Access Denied" errors
# Check service account has access to required resources

# Example: SQL Server access
# Add service account to SQL Server with appropriate permissions
# Or use Windows Authentication in connection strings

# Example: File system access
# Grant service account permissions to specific folders
$path = "C:\SomeFolder"
$serviceAccount = "NT AUTHORITY\NETWORK SERVICE"
$acl = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $serviceAccount,
    "ReadAndExecute",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $path $acl
```

**D. Environment Variables Not Set**
```powershell
# Create/update .env file in agent directory
$envContent = @"
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
NUGET_PACKAGES=C:\agents\.nuget\packages
AZP_AGENT_CLEANUP_PSMODULES_IN_POWERSHELL=true
"@

$envContent | Set-Content "C:\agents\agent1\.env" -Force

# Restart agent
Restart-Service -Name "vstsagent.*"
```

**E. Git Authentication Issues**
```powershell
# If builds fail during Git operations
# Configure Git credentials

# For Azure Repos, use Git Credential Manager
git config --global credential.helper manager

# Or configure PAT for Git
git config --global credential.https://dev.azure.com.username "YourUsername"
# It will prompt for password (use PAT)
```

---

### 5. Agent Updates Failing

#### Symptom
Agent fails to auto-update or manual update fails.

#### Solutions

**A. Insufficient Permissions for Auto-Update**
```powershell
# Service account needs write access to agent directory
$agentPath = "C:\agents\agent1"
$serviceAccount = "NT AUTHORITY\NETWORK SERVICE"

$acl = Get-Acl $agentPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $serviceAccount,
    "FullControl",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $agentPath $acl
```

**B. Manual Update Process**
```powershell
# 1. Download latest agent
$version = "3.245.0"  # Check latest from GitHub
$url = "https://vstsagentpackage.azureedge.net/agent/$version/vsts-agent-win-x64-$version.zip"
$zipPath = "$env:TEMP\vsts-agent-win-x64-$version.zip"
Invoke-WebRequest -Uri $url -OutFile $zipPath

# 2. Stop agent service
Stop-Service -Name "vstsagent.*"

# 3. Backup current config
$backupPath = "C:\agents\backups\agent1-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -Path $backupPath -ItemType Directory -Force
Copy-Item "C:\agents\agent1\.agent" -Destination $backupPath
Copy-Item "C:\agents\agent1\.credentials" -Destination $backupPath

# 4. Extract new version (keeping config files)
Expand-Archive -Path $zipPath -DestinationPath "C:\agents\agent1" -Force

# 5. Start agent service
Start-Service -Name "vstsagent.*"

# 6. Verify version
Get-Content "C:\agents\agent1\.agent" | ConvertFrom-Json | Select-Object AgentVersion
```

---

### 6. Network and Firewall Issues

#### Symptom
Connection timeouts, SSL errors, or intermittent connectivity.

#### Solutions

**A. Whitelist Required URLs**
```powershell
# Add these URLs to firewall/proxy allowlist:
$requiredUrls = @(
    "https://dev.azure.com",
    "https://*.dev.azure.com",
    "https://login.microsoftonline.com",
    "https://management.core.windows.net",
    "https://download.agent.dev.azure.com",
    "https://vssps.dev.azure.com",
    "https://*.blob.core.windows.net",
    "https://*.vsassets.io"
)

# Test each URL
foreach ($url in $requiredUrls) {
    $domain = ($url -replace 'https://|\*\.', '').Split('/')[0]
    if ($domain -notmatch '\*') {
        try {
            $result = Test-NetConnection -ComputerName $domain -Port 443 -InformationLevel Quiet
            Write-Host "$url : $(if($result){'OK'}else{'FAILED'})" -ForegroundColor $(if($result){'Green'}else{'Red'})
        } catch {
            Write-Host "$url : ERROR - $_" -ForegroundColor Red
        }
    }
}
```

**B. Configure IP Whitelist (if required)**
```
# IPv4 ranges to whitelist:
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

**C. Configure Proxy with Authentication**
```powershell
# Set proxy URL
"http://proxy.company.com:8080" | Set-Content "C:\agents\agent1\.proxyurl"

# Set proxy credentials (if required)
# Format: username:password
"domain\username:password" | Set-Content "C:\agents\agent1\.proxycredentials"

# Set proxy bypass list
".local,.internal,.company.com" | Set-Content "C:\agents\agent1\.proxybypass"

# Restart agent
Restart-Service -Name "vstsagent.*"
```

---

### 7. Performance Issues

#### Symptom
Builds are slow or agent becomes unresponsive.

#### Solutions

**A. Resource Monitoring**
```powershell
# Monitor agent resource usage
Get-Process Agent.* | Select-Object ProcessName, CPU, WorkingSet, Handles | Format-Table

# Check overall system resources
Get-Counter '\Processor(_Total)\% Processor Time', '\Memory\Available MBytes'

# Check disk I/O
Get-Counter '\PhysicalDisk(_Total)\Avg. Disk Queue Length'
```

**B. Optimize Build Configuration**
```yaml
# In your pipeline YAML, optimize parallelism
pool:
  name: 'Default'
  demands:
  - Agent.OS -equals Windows_NT

# Use parallel jobs sparingly based on agent capacity
strategy:
  maxParallel: 2  # Adjust based on agent resources
```

**C. Configure Workspace Cleanup**
```yaml
# Add workspace cleanup to pipeline
jobs:
- job: Build
  workspace:
    clean: all  # or 'outputs', 'resources', 'all'
  steps:
  - checkout: self
    clean: true
```

**D. Increase Agent Resources**
```powershell
# If VM/server, increase:
# - CPU cores (4-8 recommended)
# - RAM (8-16 GB recommended)
# - Disk speed (SSD recommended)

# Check current resources
Get-ComputerInfo | Select-Object CsProcessors, CsTotalPhysicalMemory, OsArchitecture
```

---

### 8. ECTSystem-Specific Issues

#### Symptom
Builds fail on ECTSystem-specific steps.

#### Solutions

**A. .NET 9.0 SDK Not Found**
```powershell
# Verify .NET 9.0 is installed
dotnet --list-sdks

# If missing, install
winget install Microsoft.DotNet.SDK.9

# Verify global.json is respected
Get-Content "D:\source\repos\donellmccoy\ECTSystem\global.json"

# Restart agent to pick up new SDK
Restart-Service -Name "vstsagent.*"
```

**B. SQL Server Database Project Build Failures**
```powershell
# Install SQL Server Data Tools
# Or use MSBuild with SQL Server targets

# Check if SqlPackage.exe is available
Get-Command SqlPackage.exe -ErrorAction SilentlyContinue

# If not found, install SQL Server Data Tools
# Download from: https://docs.microsoft.com/sql/ssdt/download-sql-server-data-tools-ssdt
```

**C. gRPC Build Errors**
```powershell
# Ensure Grpc.Tools package is restored
cd "D:\source\repos\donellmccoy\ECTSystem"
dotnet restore ECTSystem.sln

# Clean and rebuild
dotnet clean ECTSystem.sln
dotnet build ECTSystem.sln
```

**D. Blazor WebAssembly Build Issues**
```powershell
# Install Node.js (required for some Blazor tooling)
winget install OpenJS.NodeJS.LTS

# Clear Blazor cache
Remove-Item "$env:LOCALAPPDATA\Microsoft\VisualStudio\*\ComponentModelCache" -Recurse -Force -ErrorAction SilentlyContinue

# Rebuild
dotnet build AF.ECT.WebClient\AF.ECT.WebClient.csproj
```

---

## Advanced Troubleshooting

### Enable Verbose Logging
```powershell
# Stop agent service
Stop-Service -Name "vstsagent.*"

# Run agent interactively with diagnostic output
cd C:\agents\agent1
$env:VSTS_AGENT_DIAG = "true"
.\run.cmd

# Review detailed logs in _diag folder
Get-Content "C:\agents\agent1\_diag\Worker_*.log" | Select-String "error|exception|warning" -Context 3
```

### Collect Diagnostic Package
```powershell
# Create diagnostic package for support
$diagPath = "C:\agents\diagnostics\$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -Path $diagPath -ItemType Directory -Force

# Copy logs
Copy-Item "C:\agents\agent1\_diag\*" -Destination $diagPath -Recurse

# Export agent configuration (redact sensitive info)
Get-Content "C:\agents\agent1\.agent" | Set-Content "$diagPath\agent-config.json"

# Export service configuration
Get-Service "vstsagent.*" | Export-Clixml "$diagPath\service-info.xml"

# Export system info
Get-ComputerInfo | Export-Clixml "$diagPath\system-info.xml"

# Compress package
Compress-Archive -Path $diagPath -DestinationPath "$diagPath.zip"

Write-Host "Diagnostic package created: $diagPath.zip"
```

### Reset Agent Completely
```powershell
# Nuclear option: Complete reset

# 1. Stop and remove service
Stop-Service -Name "vstsagent.*"
cd C:\agents\agent1
.\config.cmd remove --auth pat --token "your-pat-token"

# 2. Delete agent directory
Remove-Item "C:\agents\agent1" -Recurse -Force

# 3. Recreate and reconfigure
New-Item -Path "C:\agents\agent1" -ItemType Directory -Force
# Download and extract agent again
# Reconfigure from scratch
```

---

## Getting Help

### Internal Resources
1. Review main setup guide: `Documentation\AzureDevOps-SelfHostedAgent-Setup.md`
2. Check configuration examples: `Documentation\AzureDevOps-Agent-Configuration-Templates.md`
3. Contact DevOps team

### External Resources
1. [Azure Pipelines Agent GitHub Issues](https://github.com/microsoft/azure-pipelines-agent/issues)
2. [Microsoft Learn Documentation](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/windows-agent)
3. [Azure DevOps Developer Community](https://developercommunity.visualstudio.com/azuredevops)

### Log Locations
```
Agent diagnostic logs:     C:\agents\agent1\_diag\
Agent configuration:       C:\agents\agent1\.agent
Agent credentials:         C:\agents\agent1\.credentials
Windows Event Log:         Application (Source: VSTSAgent*)
Service logs:              Check Windows Event Viewer
```

---

## Prevention Best Practices

1. **Regular Maintenance**
   - Update agent monthly
   - Clean work directory weekly
   - Monitor disk space daily
   - Review logs weekly

2. **Monitoring**
   - Set up alerts for agent offline status
   - Monitor build queue times
   - Track build success rates

3. **Documentation**
   - Document custom configuration
   - Keep PAT renewal dates tracked
   - Maintain runbook for common issues

4. **Testing**
   - Test agent updates in dev environment first
   - Validate pipelines after agent changes
   - Keep spare agents for failover

5. **Security**
   - Rotate PAT tokens regularly (90 days)
   - Audit agent folder permissions monthly
   - Review service account permissions quarterly
   - Keep audit logs of agent changes
