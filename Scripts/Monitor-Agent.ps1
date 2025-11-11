<#
.SYNOPSIS
    Monitors Azure DevOps self-hosted agent in real-time.

.DESCRIPTION
    Displays live status of the Azure DevOps agent service, processes, and build activity.
    Auto-refreshes every 3 seconds.

.NOTES
    Author: ECTSystem DevOps Team
    Press Ctrl+C to stop monitoring
#>

Write-Host "Monitoring Azure DevOps Agent..." -ForegroundColor Cyan
Write-Host ""

while ($true) {
    Clear-Host
    Write-Host "=== Azure DevOps Agent Monitor ===" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Service Status:" -ForegroundColor Yellow
    Get-Service "vstsagent.*" | Format-Table Name, Status, StartType -AutoSize
    
    Write-Host "Agent Processes:" -ForegroundColor Yellow
    Get-Process Agent.* -ErrorAction SilentlyContinue | Format-Table ProcessName, Id, @{N='CPU(s)';E={[math]::Round($_.CPU,2)}}, @{N='Memory(MB)';E={[math]::Round($_.WorkingSet/1MB,2)}} -AutoSize
    
    $worker = Get-Process Agent.Worker -ErrorAction SilentlyContinue
    if ($worker) {
        Write-Host "[ACTIVE] BUILD RUNNING!" -ForegroundColor Green
    } else {
        Write-Host "[IDLE] Waiting for jobs..." -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor DarkGray
    
    Start-Sleep -Seconds 3
}