# Bisection script to find which test creates unwanted files/state
# Usage: .\FindPolluter.ps1 -FileToCheck <file_or_dir_to_check> -TestPattern <test_pattern> [-WorkingDir <path>]
# Example: .\FindPolluter.ps1 -FileToCheck '.git' -TestPattern 'AF.ECT.Tests'

param(
    [Parameter(Mandatory = $true)]
    [string]$FileToCheck,

    [Parameter(Mandatory = $true)]
    [string]$TestPattern,

    [string]$WorkingDir = (Get-Location).Path
)

Write-Host "🔍 Searching for test that creates: $FileToCheck" -ForegroundColor Cyan
Write-Host "Test pattern: $TestPattern" -ForegroundColor Cyan
Write-Host ""

# Get list of test files matching pattern
$testFiles = @()
if (Test-Path $TestPattern) {
    $testFiles = @(Get-ChildItem -Path $TestPattern -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
}
else {
    $testFiles = @(Get-ChildItem -Path "$WorkingDir\$TestPattern" -Filter "*Tests.cs" -Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
}

if ($testFiles.Count -eq 0) {
    Write-Host "❌ No test files found matching pattern" -ForegroundColor Red
    exit 1
}

Write-Host "Found $($testFiles.Count) test files" -ForegroundColor Green
Write-Host ""

# Function to run tests and check for pollution
function Test-Pollution {
    param(
        [string[]]$TestFiles,
        [string]$PollutionCheck,
        [string]$WorkDir
    )

    try {
        # Extract test class names from files
        $testClasses = @()
        foreach ($file in $TestFiles) {
            $content = Get-Content -Path $file -Raw
            $matches = [regex]::Matches($content, 'public\s+class\s+(\w+)\s*(?:\:|where|{)')
            foreach ($match in $matches) {
                $testClasses += $match.Groups[1].Value
            }
        }

        if ($testClasses.Count -eq 0) {
            return $false
        }

        $filter = $testClasses -join "|" | ForEach-Object { "FullyQualifiedName~$_" }
        
        Push-Location $WorkDir
        try {
            # Run xUnit tests
            & dotnet test --filter "$filter" --no-build --logger "console;verbosity=quiet" *> $null
        }
        finally {
            Pop-Location
        }

        # Check if pollution exists
        $pollutionPath = Join-Path -Path $WorkDir -ChildPath $PollutionCheck
        return (Test-Path -Path $pollutionPath)
    }
    catch {
        Write-Host "Error running tests: $_" -ForegroundColor Red
        return $false
    }
}

# Bisection search
Write-Host "Running bisection search..." -ForegroundColor Yellow
Write-Host ""

$left = 0
$right = $testFiles.Count - 1

while ($left -lt $right) {
    $mid = [int](($left + $right) / 2)
    $subset = $testFiles[$left..$mid]
    
    Write-Host "Testing subset [$left-$mid] ($($subset.Count) files)..." -ForegroundColor Cyan
    $polluted = Test-Pollution -TestFiles $subset -PollutionCheck $FileToCheck -WorkDir $WorkingDir
    
    if ($polluted) {
        $right = $mid
        Write-Host "✓ Pollution found in this subset, narrowing..." -ForegroundColor Green
    }
    else {
        $left = $mid + 1
        Write-Host "✓ No pollution in this subset, searching right half..." -ForegroundColor Green
    }
    
    Write-Host ""
}

if ($left -eq $right) {
    Write-Host ""
    Write-Host "✅ FOUND POLLUTER: $($testFiles[$left])" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Open: $($testFiles[$left])" -ForegroundColor White
    Write-Host "2. Look for setup/teardown that doesn't clean up" -ForegroundColor White
    Write-Host "3. Add proper cleanup in test fixture or [TearDown] method" -ForegroundColor White
}
else {
    Write-Host "❌ Could not identify single polluter" -ForegroundColor Red
    Write-Host "Pollution may be cumulative from multiple tests" -ForegroundColor Yellow
}
