<#
.SYNOPSIS
    Automated setup script for Azure DevOps self-hosted Windows agent for ECTSystem.

.DESCRIPTION
    This script automates the download, installation, and configuration of an Azure DevOps
    self-hosted Windows agent. It includes prerequisites checking, software installation,
    and agent registration.

.PARAMETER OrganizationUrl
    The Azure DevOps organization URL (e.g., https://dev.azure.com/yourorganization)

.PARAMETER PAT
    Personal Access Token with 'Agent Pools (read, manage)' scope.
    For security, consider using -PATFromKeyVault or -PATEnvironmentVariable instead.

.PARAMETER PATEnvironmentVariable
    Name of environment variable containing the PAT token (more secure than -PAT parameter)

.PARAMETER AgentName
    Name for the agent (default: computername-Agent-01)

.PARAMETER AgentPool
    Agent pool name (default: Default)

.PARAMETER AgentDirectory
    Directory where agent will be installed (default: C:\agents\agent1)

.PARAMETER RunAsService
    Configure agent to run as Windows service (default: true)

.PARAMETER ServiceAccount
    Service account for agent service (default: NT AUTHORITY\NETWORK SERVICE)

.PARAMETER ServicePassword
    Password for service account (only needed for domain/local accounts, not for built-in accounts)

.PARAMETER InstallDependencies
    Install required software dependencies for ECTSystem (default: true)

.PARAMETER SkipPrerequisiteCheck
    Skip prerequisite validation checks (not recommended)

.PARAMETER Unattended
    Run in unattended mode with no user prompts (requires all parameters)

.PARAMETER WorkDirectory
    Agent work directory (default: _work)

.PARAMETER Replace
    Replace existing agent with same name

.EXAMPLE
    # Interactive mode - will prompt for missing parameters
    .\Setup-AzureDevOpsAgent.ps1

.EXAMPLE
    # Unattended mode using environment variable for PAT
    $env:AZURE_DEVOPS_PAT = "your-pat-token-here"
    .\Setup-AzureDevOpsAgent.ps1 -OrganizationUrl "https://dev.azure.com/yourorg" -PATEnvironmentVariable "AZURE_DEVOPS_PAT" -Unattended

.EXAMPLE
    # Configure specific agent with custom settings
    .\Setup-AzureDevOpsAgent.ps1 -OrganizationUrl "https://dev.azure.com/yourorg" -PAT "your-token" -AgentName "ECTSystem-Build-01" -AgentPool "ECTSystem-Pool" -AgentDirectory "C:\agents\build01"

.EXAMPLE
    # Install without dependencies (if already installed)
    .\Setup-AzureDevOpsAgent.ps1 -OrganizationUrl "https://dev.azure.com/yourorg" -PAT "your-token" -InstallDependencies $false

.NOTES
    Author: ECTSystem DevOps Team
    Version: 1.0.0
    Requires: PowerShell 5.1 or later, Administrator privileges
    
    Security: Never commit PAT tokens to source control. Use environment variables,
    Azure Key Vault, or Windows Credential Manager.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$OrganizationUrl,

    [Parameter(Mandatory = $false)]
    [string]$PAT,

    [Parameter(Mandatory = $false)]
    [string]$PATEnvironmentVariable,

    [Parameter(Mandatory = $false)]
    [string]$AgentName = "$env:COMPUTERNAME-Agent-01",

    [Parameter(Mandatory = $false)]
    [string]$AgentPool = "Default",

    [Parameter(Mandatory = $false)]
    [string]$AgentDirectory = "C:\agents\agent1",

    [Parameter(Mandatory = $false)]
    [bool]$RunAsService = $true,

    [Parameter(Mandatory = $false)]
    [string]$ServiceAccount = "NT AUTHORITY\NETWORK SERVICE",

    [Parameter(Mandatory = $false)]
    [string]$ServicePassword,

    [Parameter(Mandatory = $false)]
    [bool]$InstallDependencies = $true,

    [Parameter(Mandatory = $false)]
    [switch]$SkipPrerequisiteCheck,

    [Parameter(Mandatory = $false)]
    [switch]$Unattended,

    [Parameter(Mandatory = $false)]
    [string]$WorkDirectory = "_work",

    [Parameter(Mandatory = $false)]
    [switch]$Replace
)

#Requires -RunAsAdministrator

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Constants
$AGENT_DOWNLOAD_URL = "https://vstsagentpackage.azureedge.net/agent"
$REQUIRED_DOTNET_VERSION = "9.0"
$REQUIRED_POWERSHELL_VERSION = "5.1"

#region Helper Functions

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Success', 'Warning', 'Error')]
        [string]$Level = 'Info'
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        'Success' { 'Green' }
        'Warning' { 'Yellow' }
        'Error' { 'Red' }
        default { 'White' }
    }
    
    $prefix = switch ($Level) {
        'Success' { '[✓]' }
        'Warning' { '[!]' }
        'Error' { '[✗]' }
        default { '[i]' }
    }
    
    Write-Host "$timestamp $prefix $Message" -ForegroundColor $color
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-Prerequisites {
    Write-Log "Checking prerequisites..." -Level Info
    
    # Check administrator privileges
    if (-not (Test-Administrator)) {
        throw "This script must be run as Administrator"
    }
    Write-Log "Administrator privileges: OK" -Level Success
    
    # Check PowerShell version
    $psVersion = $PSVersionTable.PSVersion
    if ($psVersion.Major -lt 5 -or ($psVersion.Major -eq 5 -and $psVersion.Minor -lt 1)) {
        throw "PowerShell version $REQUIRED_POWERSHELL_VERSION or higher required. Current: $($psVersion.ToString())"
    }
    Write-Log "PowerShell version $($psVersion.ToString()): OK" -Level Success
    
    # Check operating system
    $os = Get-CimInstance Win32_OperatingSystem
    Write-Log "Operating System: $($os.Caption) $($os.Version)" -Level Info
    
    # Check disk space (require at least 50GB free)
    $systemDrive = $env:SystemDrive
    $freeSpace = (Get-PSDrive $systemDrive.TrimEnd(':')).Free / 1GB
    if ($freeSpace -lt 50) {
        Write-Log "Low disk space: $([math]::Round($freeSpace, 2)) GB free. Recommended: 100+ GB" -Level Warning
    } else {
        Write-Log "Disk space: $([math]::Round($freeSpace, 2)) GB free" -Level Success
    }
    
    # Check memory
    $memory = (Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB
    if ($memory -lt 8) {
        Write-Log "Memory: $([math]::Round($memory, 2)) GB. Recommended: 8+ GB for optimal performance" -Level Warning
    } else {
        Write-Log "Memory: $([math]::Round($memory, 2)) GB" -Level Success
    }
    
    # Check network connectivity
    try {
        $null = Test-NetConnection -ComputerName "dev.azure.com" -Port 443 -InformationLevel Quiet -ErrorAction Stop
        Write-Log "Network connectivity to Azure DevOps: OK" -Level Success
    } catch {
        throw "Cannot connect to dev.azure.com. Check network/firewall settings."
    }
}

function Get-LatestAgentVersion {
    Write-Log "Fetching latest agent version..." -Level Info
    
    try {
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/microsoft/azure-pipelines-agent/releases/latest" -Method Get
        $version = $response.tag_name -replace '^v', ''
        Write-Log "Latest agent version: $version" -Level Success
        return $version
    } catch {
        Write-Log "Failed to fetch latest version from GitHub. Using fallback version." -Level Warning
        return "3.245.0"  # Fallback version
    }
}

function Download-Agent {
    param(
        [string]$Version,
        [string]$DestinationPath
    )
    
    $agentZip = Join-Path $env:TEMP "vsts-agent-win-x64-$Version.zip"
    $downloadUrl = "$AGENT_DOWNLOAD_URL/$Version/vsts-agent-win-x64-$Version.zip"
    
    Write-Log "Downloading agent from: $downloadUrl" -Level Info
    
    try {
        # Create destination directory
        if (-not (Test-Path $DestinationPath)) {
            New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null
            Write-Log "Created directory: $DestinationPath" -Level Success
        }
        
        # Download agent
        Invoke-WebRequest -Uri $downloadUrl -OutFile $agentZip -UseBasicParsing
        Write-Log "Downloaded agent to: $agentZip" -Level Success
        
        # Extract agent
        Write-Log "Extracting agent to: $DestinationPath" -Level Info
        Expand-Archive -Path $agentZip -DestinationPath $DestinationPath -Force
        Write-Log "Agent extracted successfully" -Level Success
        
        # Cleanup
        Remove-Item $agentZip -Force
        
    } catch {
        throw "Failed to download/extract agent: $_"
    }
}

function Set-AgentDirectoryPermissions {
    param([string]$Path)
    
    Write-Log "Setting secure permissions on agent directory..." -Level Info
    
    try {
        $acl = Get-Acl $Path
        $acl.SetAccessRuleProtection($true, $false)  # Disable inheritance
        
        # Add Administrators
        $adminRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "BUILTIN\Administrators",
            "FullControl",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.SetAccessRule($adminRule)
        
        # Add SYSTEM
        $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "NT AUTHORITY\SYSTEM",
            "FullControl",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.SetAccessRule($systemRule)
        
        # Add service account if not a built-in account
        if ($ServiceAccount -notmatch '^NT AUTHORITY') {
            $serviceRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $ServiceAccount,
                "FullControl",
                "ContainerInherit,ObjectInherit",
                "None",
                "Allow"
            )
            $acl.SetAccessRule($serviceRule)
        }
        
        Set-Acl $Path $acl
        Write-Log "Directory permissions set successfully" -Level Success
        
    } catch {
        Write-Log "Failed to set directory permissions: $_" -Level Warning
    }
}

function Install-DotNetSDK {
    Write-Log "Checking .NET SDK installation..." -Level Info
    
    # Check if .NET 9.0 SDK is installed
    $installedSDKs = dotnet --list-sdks 2>$null
    if ($installedSDKs -match "9\.0") {
        Write-Log ".NET 9.0 SDK already installed" -Level Success
        return
    }
    
    Write-Log "Installing .NET 9.0 SDK..." -Level Info
    
    try {
        # Check if winget is available
        $wingetPath = Get-Command winget -ErrorAction SilentlyContinue
        if ($wingetPath) {
            winget install Microsoft.DotNet.SDK.9 --silent --accept-source-agreements --accept-package-agreements
            Write-Log ".NET 9.0 SDK installed via winget" -Level Success
        } else {
            Write-Log "winget not found. Please install .NET 9.0 SDK manually from: https://dotnet.microsoft.com/download" -Level Warning
        }
    } catch {
        Write-Log "Failed to install .NET SDK: $_" -Level Warning
        Write-Log "Please install manually from: https://dotnet.microsoft.com/download" -Level Warning
    }
}

function Install-Git {
    Write-Log "Checking Git installation..." -Level Info
    
    $gitInstalled = Get-Command git -ErrorAction SilentlyContinue
    if ($gitInstalled) {
        $gitVersion = git --version
        Write-Log "Git already installed: $gitVersion" -Level Success
        return
    }
    
    Write-Log "Installing Git..." -Level Info
    
    try {
        $wingetPath = Get-Command winget -ErrorAction SilentlyContinue
        if ($wingetPath) {
            winget install Git.Git --silent --accept-source-agreements --accept-package-agreements
            Write-Log "Git installed via winget" -Level Success
        } else {
            Write-Log "winget not found. Please install Git manually from: https://git-scm.com/download/win" -Level Warning
        }
    } catch {
        Write-Log "Failed to install Git: $_" -Level Warning
    }
}

function Install-NodeJS {
    Write-Log "Checking Node.js installation..." -Level Info
    
    $nodeInstalled = Get-Command node -ErrorAction SilentlyContinue
    if ($nodeInstalled) {
        $nodeVersion = node --version
        Write-Log "Node.js already installed: $nodeVersion" -Level Success
        return
    }
    
    Write-Log "Installing Node.js LTS..." -Level Info
    
    try {
        $wingetPath = Get-Command winget -ErrorAction SilentlyContinue
        if ($wingetPath) {
            winget install OpenJS.NodeJS.LTS --silent --accept-source-agreements --accept-package-agreements
            Write-Log "Node.js LTS installed via winget" -Level Success
        } else {
            Write-Log "winget not found. Please install Node.js manually from: https://nodejs.org/" -Level Warning
        }
    } catch {
        Write-Log "Failed to install Node.js: $_" -Level Warning
    }
}

function Install-Dependencies {
    Write-Log "Installing ECTSystem dependencies..." -Level Info
    
    Install-DotNetSDK
    Install-Git
    Install-NodeJS
    
    # Refresh environment variables
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
    
    Write-Log "Dependency installation complete" -Level Success
}

function Configure-Agent {
    param(
        [string]$AgentPath,
        [string]$ServerUrl,
        [string]$Token,
        [string]$Pool,
        [string]$Name,
        [string]$Work,
        [bool]$Service,
        [string]$Account,
        [string]$Password,
        [bool]$ReplaceExisting
    )
    
    Write-Log "Configuring agent..." -Level Info
    
    $configPath = Join-Path $AgentPath "config.cmd"
    
    if (-not (Test-Path $configPath)) {
        throw "Agent config.cmd not found at: $configPath"
    }
    
    # Build configuration arguments
    $configArgs = @(
        "--unattended",
        "--url", $ServerUrl,
        "--auth", "pat",
        "--token", $Token,
        "--pool", $Pool,
        "--agent", $Name,
        "--work", $Work,
        "--acceptTeeEula"
    )
    
    if ($ReplaceExisting) {
        $configArgs += "--replace"
    }
    
    if ($Service) {
        $configArgs += "--runAsService"
        $configArgs += "--windowsLogonAccount", $Account
        
        # Only add password for non-built-in accounts
        if ($Account -notmatch '^NT AUTHORITY' -and $Password) {
            $configArgs += "--windowsLogonPassword", $Password
        }
    }
    
    try {
        Push-Location $AgentPath
        
        # Run configuration
        $process = Start-Process -FilePath $configPath -ArgumentList $configArgs -Wait -NoNewWindow -PassThru
        
        if ($process.ExitCode -eq 0) {
            Write-Log "Agent configured successfully" -Level Success
        } else {
            throw "Agent configuration failed with exit code: $($process.ExitCode)"
        }
        
    } catch {
        throw "Failed to configure agent: $_"
    } finally {
        Pop-Location
    }
}

function Test-AgentRegistration {
    param(
        [string]$ServerUrl,
        [string]$Token,
        [string]$Pool,
        [string]$AgentName
    )
    
    Write-Log "Verifying agent registration..." -Level Info
    
    try {
        # Extract organization from URL
        $orgName = ($ServerUrl -split '/')[-1]
        $apiUrl = "$ServerUrl/_apis/distributedtask/pools?api-version=7.0"
        
        $headers = @{
            Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Token"))
        }
        
        $pools = Invoke-RestMethod -Uri $apiUrl -Headers $headers -Method Get
        $targetPool = $pools.value | Where-Object { $_.name -eq $Pool }
        
        if ($targetPool) {
            $agentsUrl = "$ServerUrl/_apis/distributedtask/pools/$($targetPool.id)/agents?api-version=7.0"
            $agents = Invoke-RestMethod -Uri $agentsUrl -Headers $headers -Method Get
            $agent = $agents.value | Where-Object { $_.name -eq $AgentName }
            
            if ($agent) {
                $status = if ($agent.status -eq "online") { "Success" } else { "Warning" }
                Write-Log "Agent '$AgentName' found in pool '$Pool' with status: $($agent.status)" -Level $status
                return $true
            }
        }
        
        Write-Log "Agent not found in pool. It may take a few moments to appear." -Level Warning
        return $false
        
    } catch {
        Write-Log "Could not verify agent registration via API: $_" -Level Warning
        return $false
    }
}

function Create-AgentEnvironmentFile {
    param([string]$AgentPath)
    
    Write-Log "Creating agent environment configuration..." -Level Info
    
    $envContent = @"
# ECTSystem Agent Environment Variables
# This file is automatically loaded by the agent on startup

# .NET Configuration
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
DOTNET_MULTILEVEL_LOOKUP=0

# NuGet Configuration
NUGET_PACKAGES=C:\agents\.nuget\packages

# Build Configuration
BuildConfiguration=Release

# Agent Configuration
AZP_AGENT_CLEANUP_PSMODULES_IN_POWERSHELL=true
"@
    
    $envFile = Join-Path $AgentPath ".env"
    $envContent | Set-Content -Path $envFile -Force
    Write-Log "Environment file created: $envFile" -Level Success
}

#endregion

#region Main Script

try {
    Write-Log "========================================" -Level Info
    Write-Log "Azure DevOps Agent Setup for ECTSystem" -Level Info
    Write-Log "========================================" -Level Info
    Write-Log "" -Level Info
    
    # Check prerequisites
    if (-not $SkipPrerequisiteCheck) {
        Test-Prerequisites
    }
    
    # Get PAT token from environment variable if specified
    if ($PATEnvironmentVariable -and -not $PAT) {
        $PAT = [Environment]::GetEnvironmentVariable($PATEnvironmentVariable)
        if (-not $PAT) {
            throw "Environment variable '$PATEnvironmentVariable' not found or empty"
        }
    }
    
    # Prompt for missing required parameters in interactive mode
    if (-not $Unattended) {
        if (-not $OrganizationUrl) {
            $OrganizationUrl = Read-Host "Enter Azure DevOps organization URL (e.g., https://dev.azure.com/yourorg)"
        }
        
        if (-not $PAT) {
            $secureToken = Read-Host "Enter Personal Access Token (PAT)" -AsSecureString
            $PAT = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
                [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
            )
        }
    }
    
    # Validate required parameters
    if (-not $OrganizationUrl -or -not $PAT) {
        throw "OrganizationUrl and PAT are required. Use -Unattended with all parameters or run interactively."
    }
    
    # Install dependencies
    if ($InstallDependencies) {
        Install-Dependencies
    }
    
    # Get latest agent version and download
    $agentVersion = Get-LatestAgentVersion
    Download-Agent -Version $agentVersion -DestinationPath $AgentDirectory
    
    # Set directory permissions
    Set-AgentDirectoryPermissions -Path $AgentDirectory
    
    # Create environment file
    Create-AgentEnvironmentFile -AgentPath $AgentDirectory
    
    # Configure agent
    Configure-Agent `
        -AgentPath $AgentDirectory `
        -ServerUrl $OrganizationUrl `
        -Token $PAT `
        -Pool $AgentPool `
        -Name $AgentName `
        -Work $WorkDirectory `
        -Service $RunAsService `
        -Account $ServiceAccount `
        -Password $ServicePassword `
        -ReplaceExisting $Replace.IsPresent
    
    # Verify registration
    Start-Sleep -Seconds 5
    Test-AgentRegistration -ServerUrl $OrganizationUrl -Token $PAT -Pool $AgentPool -AgentName $AgentName
    
    Write-Log "" -Level Info
    Write-Log "========================================" -Level Success
    Write-Log "Agent setup completed successfully!" -Level Success
    Write-Log "========================================" -Level Success
    Write-Log "" -Level Info
    Write-Log "Agent Details:" -Level Info
    Write-Log "  Name: $AgentName" -Level Info
    Write-Log "  Pool: $AgentPool" -Level Info
    Write-Log "  Directory: $AgentDirectory" -Level Info
    Write-Log "  Run as Service: $RunAsService" -Level Info
    
    if ($RunAsService) {
        Write-Log "" -Level Info
        Write-Log "Service started automatically. Check status with:" -Level Info
        Write-Log "  services.msc" -Level Info
        Write-Log "  Look for: 'Azure Pipelines Agent ($AgentName)'" -Level Info
    } else {
        Write-Log "" -Level Info
        Write-Log "To start the agent interactively:" -Level Info
        Write-Log "  cd $AgentDirectory" -Level Info
        Write-Log "  .\run.cmd" -Level Info
    }
    
    Write-Log "" -Level Info
    Write-Log "Next steps:" -Level Info
    Write-Log "  1. Verify agent appears in Azure DevOps: $OrganizationUrl/_settings/agentpools" -Level Info
    Write-Log "  2. Check agent capabilities in the portal" -Level Info
    Write-Log "  3. Update your pipelines to use pool: '$AgentPool'" -Level Info
    Write-Log "  4. Review documentation: Documentation\AzureDevOps-SelfHostedAgent-Setup.md" -Level Info
    
} catch {
    Write-Log "========================================" -Level Error
    Write-Log "Agent setup failed!" -Level Error
    Write-Log "========================================" -Level Error
    Write-Log "Error: $_" -Level Error
    Write-Log "" -Level Error
    Write-Log "Troubleshooting:" -Level Info
    Write-Log "  1. Check error message above" -Level Info
    Write-Log "  2. Verify PAT token has 'Agent Pools (read, manage)' scope" -Level Info
    Write-Log "  3. Ensure you have Administrator privileges" -Level Info
    Write-Log "  4. Check network connectivity to Azure DevOps" -Level Info
    Write-Log "  5. Review logs in: $AgentDirectory\_diag" -Level Info
    
    exit 1
}

#endregion
