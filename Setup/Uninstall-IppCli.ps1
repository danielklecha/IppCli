# Set error handling preference
$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Uninstalling IppCli Global .NET Tool     " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Uninstalling global tool 'IppCli'..." -ForegroundColor Yellow

    # Check if tool is currently installed
    $installedTools = dotnet tool list --global 2>$null
    $isInstalled = $installedTools | Where-Object { $_ -match "^\s*ippcli\b" }

    if (-not $isInstalled) {
        Write-Host "Tool 'IppCli' is not currently installed as a global tool." -ForegroundColor DarkGray
    }
    else {
        dotnet tool uninstall --global IppCli
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool uninstall failed with exit code $LASTEXITCODE"
        }
    }

    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host " IppCli tool uninstalled successfully!   " -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
}
finally {
    Write-Host ""
    Read-Host -Prompt "Press Enter to exit..."
}
