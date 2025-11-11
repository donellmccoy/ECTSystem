# Azure DevOps Agent Configuration Template
# 
# This file provides configuration examples and templates for Azure DevOps self-hosted agents.
# Copy and modify these templates for your specific needs.

#==============================================================================
# BASIC CONFIGURATION
#==============================================================================

# Interactive Configuration (Recommended for first-time setup)
# Run from: C:\agents\agent1
# Command: .\config.cmd
#
# Prompts will ask for:
# - Server URL: https://dev.azure.com/{your-organization}
# - Authentication: PAT (Personal Access Token)
# - Token: [Your PAT with Agent Pools (read, manage) scope]
# - Agent Pool: Default
# - Agent Name: ECTSystem-Agent-01
# - Work folder: _work
# - Run as service: Y
# - Service account: NT AUTHORITY\NETWORK SERVICE

#==============================================================================
# UNATTENDED CONFIGURATION EXAMPLES
#==============================================================================

# Example 1: Basic unattended configuration using PAT
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService `
  --windowsLogonAccount "NT AUTHORITY\NETWORK SERVICE"

# Example 2: Configuration with custom service account
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService `
  --windowsLogonAccount "DOMAIN\ServiceAccount" `
  --windowsLogonPassword "ServiceAccountPassword"

# Example 3: Replace existing agent configuration
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --replace `
  --runAsService `
  --windowsLogonAccount "NT AUTHORITY\NETWORK SERVICE"

# Example 4: Interactive mode (not as service)
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --pool "Default" `
  --agent "ECTSystem-Dev-Agent" `
  --work "_work"

# Example 5: Using environment variables for PAT (more secure)
# Set environment variable first:
# $env:AZURE_DEVOPS_PAT = "your-pat-token"
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token $env:AZURE_DEVOPS_PAT `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService

# Example 6: Deployment group agent
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --deploymentGroup `
  --deploymentGroupName "Production-Servers" `
  --projectName "ECTSystem" `
  --addDeploymentGroupTags `
  --deploymentGroupTags "web,production,windows" `
  --agent "ECTSystem-Prod-Web-01" `
  --work "_work" `
  --runAsService

#==============================================================================
# AGENT ENVIRONMENT FILE (.env)
#==============================================================================

# Create: C:\agents\agent1\.env
# This file sets environment variables for the agent

# .NET Configuration
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
DOTNET_MULTILEVEL_LOOKUP=0
DOTNET_NOLOGO=1

# NuGet Configuration
NUGET_PACKAGES=C:\agents\.nuget\packages
NUGET_HTTP_CACHE_PATH=C:\agents\.nuget\http-cache

# MSBuild Configuration
MSBuildSDKsPath=C:\Program Files\dotnet\sdk\9.0.100\Sdks
MSBUILD_EXE_PATH=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe

# Build Configuration
BuildConfiguration=Release
TreatWarningsAsErrors=false

# Git Configuration
GIT_REDIRECT_STDERR=2>&1

# Agent Configuration
AZP_AGENT_CLEANUP_PSMODULES_IN_POWERSHELL=true
AGENT_ALLOW_RUNASROOT=false

# SQL Server Configuration (if needed)
SQLCMD_PATH=C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn

# Custom paths
PATH=%PATH%;C:\agents\tools

#==============================================================================
# POWERSHELL CONFIGURATION SCRIPT
#==============================================================================

<#
# Complete PowerShell configuration script
# Save as: Configure-Agent.ps1
# Run as: .\Configure-Agent.ps1 -OrgUrl "https://dev.azure.com/yourorg" -PAT "your-token"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$OrgUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$PAT,
    
    [string]$AgentName = "$env:COMPUTERNAME-Agent-01",
    [string]$Pool = "Default",
    [string]$AgentDir = "C:\agents\agent1"
)

# Navigate to agent directory
Set-Location $AgentDir

# Configure agent
.\config.cmd --unattended `
  --url $OrgUrl `
  --auth pat `
  --token $PAT `
  --pool $Pool `
  --agent $AgentName `
  --work "_work" `
  --runAsService `
  --windowsLogonAccount "NT AUTHORITY\NETWORK SERVICE" `
  --acceptTeeEula

# Create .env file
$envContent = @"
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
NUGET_PACKAGES=C:\agents\.nuget\packages
AZP_AGENT_CLEANUP_PSMODULES_IN_POWERSHELL=true
"@

$envContent | Set-Content -Path ".env" -Force

Write-Host "Agent configured successfully!" -ForegroundColor Green

#==============================================================================
# SERVICE CONFIGURATION
#==============================================================================

# Start agent service
Start-Service -Name "vstsagent.yourorganization.Default.ECTSystem-Agent-01"

# Stop agent service
Stop-Service -Name "vstsagent.yourorganization.Default.ECTSystem-Agent-01"

# Restart agent service
Restart-Service -Name "vstsagent.yourorganization.Default.ECTSystem-Agent-01"

# Check service status
Get-Service -Name "vstsagent.*" | Select-Object Name, Status, StartType

# Set service to automatic start
Set-Service -Name "vstsagent.yourorganization.Default.ECTSystem-Agent-01" -StartupType Automatic

#==============================================================================
# AGENT REMOVAL/RECONFIGURATION
#==============================================================================

# Remove agent configuration
.\config.cmd remove --auth pat --token "your-pat-token-here"

# Remove and reconfigure
.\config.cmd remove --auth pat --token "your-pat-token-here"
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService

#==============================================================================
# MULTIPLE AGENTS CONFIGURATION
#==============================================================================

# Configure multiple agents on same machine
# Agent 1
New-Item -Path "C:\agents\agent1" -ItemType Directory -Force
Expand-Archive -Path "vsts-agent-win-x64.zip" -DestinationPath "C:\agents\agent1"
Set-Location "C:\agents\agent1"
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService

# Agent 2
New-Item -Path "C:\agents\agent2" -ItemType Directory -Force
Expand-Archive -Path "vsts-agent-win-x64.zip" -DestinationPath "C:\agents\agent2"
Set-Location "C:\agents\agent2"
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token" `
  --pool "Default" `
  --agent "ECTSystem-Agent-02" `
  --work "_work" `
  --runAsService

#==============================================================================
# DIAGNOSTICS AND TROUBLESHOOTING
#==============================================================================

# Run diagnostics
.\run.cmd --diagnostics

# Run agent once (for testing)
.\run.cmd --once

# Run interactively for debugging
.\run.cmd

# Check agent capabilities
# Navigate to: https://dev.azure.com/{org}/_settings/agentpools
# Select pool > Agents > Click agent > Capabilities tab

# View diagnostic logs
Get-ChildItem "C:\agents\agent1\_diag" | Sort-Object LastWriteTime -Descending | Select-Object -First 5

# View latest log
Get-Content (Get-ChildItem "C:\agents\agent1\_diag" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -Tail 50

#==============================================================================
# FIREWALL CONFIGURATION
#==============================================================================

# Allow agent to communicate with Azure DevOps
# Add outbound rules for these endpoints:

$endpoints = @(
    "https://dev.azure.com",
    "https://*.dev.azure.com",
    "https://login.microsoftonline.com",
    "https://management.core.windows.net",
    "https://download.agent.dev.azure.com",
    "https://vssps.dev.azure.com",
    "https://*.blob.core.windows.net",
    "https://*.vsassets.io"
)

# Test connectivity
foreach ($endpoint in $endpoints) {
    $domain = ($endpoint -replace 'https://|\*\.', '').Split('/')[0]
    if ($domain -notmatch '\*') {
        try {
            $result = Test-NetConnection -ComputerName $domain -Port 443 -InformationLevel Quiet
            Write-Host "$endpoint : $(if($result){'OK'}else{'FAILED'})" -ForegroundColor $(if($result){'Green'}else{'Red'})
        } catch {
            Write-Host "$endpoint : ERROR" -ForegroundColor Red
        }
    }
}

#==============================================================================
# SECURITY BEST PRACTICES
#==============================================================================

# 1. Use Group Managed Service Account (gMSA) for service account
# Create gMSA first in Active Directory, then:
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService `
  --windowsLogonAccount "DOMAIN\gmsa-account$"
  # Note: No password needed for gMSA

# 2. Set folder permissions (administrators only)
$agentPath = "C:\agents\agent1"
$acl = Get-Acl $agentPath
$acl.SetAccessRuleProtection($true, $false)
$adminRule = New-Object System.Security.AccessControl.FileSystemAccessRule("BUILTIN\Administrators","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT AUTHORITY\SYSTEM","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($adminRule)
$acl.SetAccessRule($systemRule)
Set-Acl $agentPath $acl

# 3. Store PAT in Azure Key Vault or Windows Credential Manager
# Using Windows Credential Manager:
$credential = Get-Credential -Message "Enter Azure DevOps PAT"
cmdkey /generic:"AzureDevOpsPAT" /user:"PAT" /pass:($credential.GetNetworkCredential().Password)

# Retrieve PAT from Credential Manager:
$target = "AzureDevOpsPAT"
$cred = cmdkey /list:$target

# 4. Enable SERVICE_SID_TYPE_UNRESTRICTED (if needed)
.\config.cmd --unattended `
  --url "https://dev.azure.com/yourorganization" `
  --auth pat `
  --token "your-pat-token-here" `
  --pool "Default" `
  --agent "ECTSystem-Agent-01" `
  --work "_work" `
  --runAsService `
  --windowsLogonAccount "NT AUTHORITY\NETWORK SERVICE" `
  --enableservicesidtypeunrestricted

#==============================================================================
# MONITORING AND MAINTENANCE
#==============================================================================

# Monitor agent service
Get-EventLog -LogName Application -Source "VSTSAgent*" -Newest 20

# Check agent disk space
Get-PSDrive C | Select-Object Used,Free

# Clean agent work directory (be careful!)
Remove-Item "C:\agents\agent1\_work\*" -Recurse -Force -ErrorAction SilentlyContinue

# Update agent (manual)
# 1. Download latest agent from GitHub
# 2. Stop service
# 3. Extract new files over existing (keep config files)
# 4. Start service

# Automated update check
$currentVersion = Get-Content "C:\agents\agent1\.agent" | ConvertFrom-Json | Select-Object -ExpandProperty AgentVersion
Write-Host "Current agent version: $currentVersion"
# Compare with latest version from GitHub

#==============================================================================
# BACKUP AND RECOVERY
#==============================================================================

# Backup agent configuration
$backupPath = "C:\agents\backups\agent1-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -Path $backupPath -ItemType Directory -Force
Copy-Item "C:\agents\agent1\.agent" -Destination $backupPath
Copy-Item "C:\agents\agent1\.credentials" -Destination $backupPath
Copy-Item "C:\agents\agent1\.env" -Destination $backupPath -ErrorAction SilentlyContinue

# Restore agent configuration
# 1. Install agent in new location
# 2. Stop service
# 3. Copy backed-up files to new location
# 4. Start service

#==============================================================================
# ECTSystem-SPECIFIC CONFIGURATION
#==============================================================================

# Environment variables specific to ECTSystem builds
$envContent = @"
# ECTSystem Build Configuration
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
NUGET_PACKAGES=C:\agents\.nuget\packages

# Solution paths
SOLUTION_PATH=ECTSystem.sln
DATABASE_SOLUTION_PATH=ECTSystem.Database.sln

# Build configuration
BuildConfiguration=Release
Platform=Any CPU

# Test configuration
CollectCoverage=true
CoverletOutputFormat=cobertura

# .NET Aspire
ASPIRE_ALLOW_UNSECURED_TRANSPORT=false

# SQL Server
SQLSERVER_CONNECTION_STRING=Server=localhost;Database=ALOD;Integrated Security=true;TrustServerCertificate=true

# Agent cleanup
AZP_AGENT_CLEANUP_PSMODULES_IN_POWERSHELL=true
"@

$envContent | Set-Content -Path "C:\agents\agent1\.env" -Force

#==============================================================================
# NOTES
#==============================================================================

# PAT Requirements:
# - Scope: Agent Pools (read, manage)
# - Expiration: Set appropriate expiration and plan for renewal
# - Store securely: Never commit to source control

# Service Account Recommendations:
# - NT AUTHORITY\NETWORK SERVICE (default, no password)
# - NT AUTHORITY\LOCAL SERVICE (limited permissions)
# - Group Managed Service Account (best for domain environments)
# - Custom domain/local account (requires password management)

# Agent Naming Convention:
# - {Project}-{Environment}-{Number}: ECTSystem-Prod-01
# - {Machine}-Agent-{Number}: SERVER01-Agent-01
# - {Purpose}-{OS}-{Number}: Build-Win-01

# Multiple Agents:
# - Use when you need parallel builds
# - Each agent needs separate directory
# - Each agent needs unique name
# - All can use same pool or different pools
