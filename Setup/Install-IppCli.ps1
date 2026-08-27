# Set error handling preference
$ErrorActionPreference = "Stop"

# Determine repository root relative to this script's directory
$RepoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$PackageDir = Join-Path $RepoRoot "IppCli\bin\Release"
$SolutionPath = Join-Path $RepoRoot "IppCli.slnx"
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
        Remove-Item -Path "$PackageDir\*.nupkg" -Force -ErrorAction SilentlyContinue
    }

    # 2. Build the solution in Release mode
    Write-Host "[1/4] Building solution in Release configuration..." -ForegroundColor Yellow
    dotnet build --configuration Release "$SolutionPath"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    # 3. Pack the IppCli project
    Write-Host "`n[2/4] Creating NuGet package..." -ForegroundColor Yellow
    dotnet pack --no-build --configuration Release "$ProjectPath"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE"
    }

    # 4. Uninstall existing package if present
    Write-Host "`n[3/4] Uninstalling existing global tool 'IppCli' (if installed)..." -ForegroundColor Yellow
    dotnet tool uninstall --global IppCli 2>$null | Out-Null
    $global:LASTEXITCODE = 0

    # 5. Install the freshly packed global tool
    Write-Host "`n[4/4] Installing global tool 'IppCli'..." -ForegroundColor Yellow
    dotnet tool install --global --add-source "$PackageDir" --no-cache IppCli
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool install failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host " IppCli tool installed successfully! " -ForegroundColor Green
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
