#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the Cloudativ Assessment application for Windows deployment.
.DESCRIPTION
    Publishes a self-contained win-x64 release build ready for the installer.
    The output folder contains everything needed to deploy on a target server.
.PARAMETER OutputPath
    Output directory for published files. Default: .\publish
.PARAMETER Configuration
    Build configuration. Default: Release
.EXAMPLE
    .\Build-Release.ps1
    .\Build-Release.ps1 -OutputPath "C:\builds\cloudativ"
#>
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "publish"),
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Cloudativ Assessment Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Resolve solution root (two levels up from deploy/windows-service/)
$SolutionRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$WebProject = Join-Path $SolutionRoot "src\Cloudativ.Assessment.Web\Cloudativ.Assessment.Web.csproj"

# Verify project file exists
if (-not (Test-Path $WebProject)) {
    Write-Error "Project file not found: $WebProject"
    exit 1
}

# Clean output directory
if (Test-Path $OutputPath) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

Write-Host "Configuration : $Configuration" -ForegroundColor Gray
Write-Host "Runtime       : win-x64 (self-contained)" -ForegroundColor Gray
Write-Host "Output        : $OutputPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Publishing..." -ForegroundColor Cyan

dotnet publish $WebProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputPath `
    /p:PublishSingleFile=false `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Copy installer scripts into publish output
Copy-Item (Join-Path $PSScriptRoot "Install-CloudativAssessment.ps1") -Destination $OutputPath -Force
Copy-Item (Join-Path $PSScriptRoot "Uninstall-CloudativAssessment.ps1") -Destination $OutputPath -Force

# Summary
$totalFiles = (Get-ChildItem -Path $OutputPath -Recurse -File).Count
$totalSize = [math]::Round((Get-ChildItem -Path $OutputPath -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 1)

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Output : $OutputPath" -ForegroundColor White
Write-Host "  Files  : $totalFiles" -ForegroundColor Gray
Write-Host "  Size   : ${totalSize} MB" -ForegroundColor Gray
Write-Host ""
Write-Host "  Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Copy the '$OutputPath' folder to the target server" -ForegroundColor White
Write-Host "  2. Run Install-CloudativAssessment.ps1 as Administrator" -ForegroundColor White
Write-Host ""
