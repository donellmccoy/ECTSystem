<#
.SYNOPSIS
    Views Azure DevOps agent logs in real-time.

.DESCRIPTION
    Watches the latest agent diagnostic log file and streams new entries.
    Shows the last 50 lines and continues to display new log entries as they are written.

.NOTES
    Author: ECTSystem DevOps Team
    Press Ctrl+C to stop watching
#>

$latestLog = Get-ChildItem "C:\agents\agent1\_diag" -Filter "*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($latestLog) {
    Write-Host "Watching: $($latestLog.Name)" -ForegroundColor Cyan
    Write-Host "Location: $($latestLog.FullName)" -ForegroundColor Gray
    Write-Host ""
    
    Get-Content $latestLog.FullName -Wait -Tail 50
} else {
    Write-Host "No logs found in C:\agents\agent1\_diag" -ForegroundColor Red
    Write-Host "The agent may not have run any jobs yet." -ForegroundColor Yellow
}