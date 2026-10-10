# Set error handling preference
$ErrorActionPreference = "Stop"

# Determine repository root relative to this script's directory
$RepoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$PackageDir = Join-Path $RepoRoot "IppCli\bin\Release"
$ProjectPath = Join-Path $RepoRoot "IppCli\IppCli.csproj"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Installing IppCli as a Global .NET Tool " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

Push-Location $RepoRoot

try {
    # 1. Clean previous nupkg output
    if (Test-Path $PackageDir) {
        Write-Host "Cleaning existing packages in: $PackageDir" -ForegroundColor DarkGray
        Remove-Item -Path "$PackageDir\*.nupkg", "$PackageDir\*.snupkg" -Force -ErrorAction SilentlyContinue
    }

    # 2. Pack the IppCli project (automatically builds dependencies and runs tool publish)
    Write-Host "[1/3] Creating NuGet package..." -ForegroundColor Yellow
    dotnet pack --configuration Release "$ProjectPath"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE"
    }

    # Find the newly packed package and determine its version
    $nupkg = Get-ChildItem -Path $PackageDir -Filter "IppCli.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $nupkg) {
        throw "No .nupkg file found in $PackageDir"
    }
    $packageVersion = if ($nupkg.Name -match '^IppCli\.(.+)\.nupkg$') { $matches[1] } else { $null }

    # 3. Uninstall existing package if present
    Write-Host "`n[2/3] Uninstalling existing global tool 'IppCli' (if installed)..." -ForegroundColor Yellow
    dotnet tool uninstall --global IppCli 2>$null | Out-Null
    $global:LASTEXITCODE = 0

    # 4. Install the freshly packed global tool from the local package directory only
    $versionInfo = if ($packageVersion) { " v$packageVersion" } else { "" }
    Write-Host "`n[3/3] Installing global tool 'IppCli'$versionInfo from local source..." -ForegroundColor Yellow
    
    $installArgs = @("tool", "install", "--global", "--source", $PackageDir, "--no-cache")
    if ($packageVersion) {
        $installArgs += @("--version", $packageVersion)
    }
    $installArgs += "IppCli"

    dotnet @installArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool install failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host " IppCli tool$versionInfo installed successfully! " -ForegroundColor Green
    Write-Host " Command: ipp-cli --help" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
}
finally {
    Pop-Location
    Write-Host ""
    Read-Host -Prompt "Press Enter to exit..."
}
