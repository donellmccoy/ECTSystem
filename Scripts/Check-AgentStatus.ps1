<#
.SYNOPSIS
    Displays current status of the Azure DevOps self-hosted agent.

.DESCRIPTION
    Shows a snapshot of the agent service, processes, and work directory.
    Provides a quick overview of the agent's current state.

.NOTES
    Author: ECTSystem DevOps Team
#>

Write-Host "=== Azure DevOps Agent Status ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Service:" -ForegroundColor Yellow
Get-Service "vstsagent.*" | Format-Table Name, Status, StartType -AutoSize

Write-Host "Listener Process:" -ForegroundColor Yellow
$listener = Get-Process Agent.Listener -ErrorAction SilentlyContinue
if ($listener) {
    $listener | Format-Table ProcessName, Id, StartTime -AutoSize
} else {
    Write-Host "Not running" -ForegroundColor Red
}

Write-Host "Worker Process:" -ForegroundColor Yellow
$worker = Get-Process Agent.Worker -ErrorAction SilentlyContinue
if ($worker) {
    $worker | Format-Table ProcessName, Id, StartTime -AutoSize
    Write-Host "✓ Agent is currently processing a build job" -ForegroundColor Green
} else {
    Write-Host "No active build jobs" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Work Directory:" -ForegroundColor Yellow
if (Test-Path "C:\agents\agent1\_work") {
    $items = Get-ChildItem "C:\agents\agent1\_work" -ErrorAction SilentlyContinue
    if ($items) {
        $items | Format-Table Name, LastWriteTime -AutoSize
    } else {
        Write-Host "Empty (no jobs processed yet)" -ForegroundColor Gray
    }
} else {
    Write-Host "No work directory (no jobs processed yet)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "View in Azure DevOps:" -ForegroundColor Yellow
Write-Host "https://dev.azure.com/donellmccoy/_settings/agentpools?poolId=1&view=agents" -ForegroundColor Cyan
Write-Host ""

# Pause so user can read the output
Write-Host "Press any key to continue..." -ForegroundColor DarkGray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")