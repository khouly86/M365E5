#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Sets up Cloudativ Assessment under IIS.
.DESCRIPTION
    Installs ASP.NET Core Hosting Bundle (if needed), creates the IIS site,
    application pool, and configures the application for IIS hosting.
.PARAMETER SiteName
    IIS site name. Default: CloudativAssessment
.PARAMETER PhysicalPath
    Path to published application files. Default: C:\inetpub\CloudativAssessment
.PARAMETER DataPath
    Data directory (DB, logs). Default: C:\ProgramData\Cloudativ Assessment
.PARAMETER Port
    HTTPS port. Default: 443
.PARAMETER HostName
    Host header for the site binding. Default: empty (all hostnames)
.PARAMETER SourcePath
    Path to the published build output to copy from. Default: current directory
.EXAMPLE
    .\Setup-IIS.ps1
    .\Setup-IIS.ps1 -SiteName "Cloudativ" -Port 8443
#>
[CmdletBinding()]
param(
    [string]$SiteName = "CloudativAssessment",
    [string]$PhysicalPath = "C:\inetpub\CloudativAssessment",
    [string]$DataPath = "C:\ProgramData\Cloudativ Assessment",
    [int]$Port = 443,
    [string]$HostName = "",
    [string]$SourcePath = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Cloudativ Assessment - IIS Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
#  Step 1 - Check prerequisites
# ============================================================
Write-Host "[1/6] Checking prerequisites..." -ForegroundColor Cyan

# Check IIS is installed
$iisFeature = Get-WindowsFeature -Name Web-Server -ErrorAction SilentlyContinue
if (-not $iisFeature -or -not $iisFeature.Installed) {
    Write-Error @"
IIS is not installed. Install it first:

  Install-WindowsFeature -Name Web-Server -IncludeManagementTools

Then install the ASP.NET Core Hosting Bundle from:
  https://dotnet.microsoft.com/download/dotnet/8.0
"@
    exit 1
}

# Check ASP.NET Core module
$aspnetModule = Get-WebGlobalModule -Name "AspNetCoreModuleV2" -ErrorAction SilentlyContinue
if (-not $aspnetModule) {
    Write-Warning @"
ASP.NET Core Hosting Bundle not detected.
Download and install from: https://dotnet.microsoft.com/download/dotnet/8.0
Then re-run this script.
"@
    exit 1
}

Write-Host "  IIS: Installed" -ForegroundColor Gray
Write-Host "  ASP.NET Core Module V2: Installed" -ForegroundColor Gray

# ============================================================
#  Step 2 - Create directories
# ============================================================
Write-Host "[2/6] Creating directories..." -ForegroundColor Cyan

$directories = @(
    $PhysicalPath,
    (Join-Path $DataPath "data"),
    (Join-Path $DataPath "logs")
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# ============================================================
#  Step 3 - Copy application files
# ============================================================
Write-Host "[3/6] Copying application files..." -ForegroundColor Cyan

# Check for published exe
$exeSource = Join-Path $SourcePath "Cloudativ.Assessment.Web.exe"
if (-not (Test-Path $exeSource)) {
    # Try looking for DLL-based deployment
    $dllSource = Join-Path $SourcePath "Cloudativ.Assessment.Web.dll"
    if (-not (Test-Path $dllSource)) {
        Write-Error "Published application not found in: $SourcePath"
        exit 1
    }
}

$robocopyArgs = @($SourcePath, $PhysicalPath, "/MIR", "/XF", "Setup-IIS.ps1", "/NFL", "/NDL", "/NJH", "/NJS", "/NC", "/NS", "/NP")
& robocopy @robocopyArgs
if ($LASTEXITCODE -gt 7) {
    Write-Error "Failed to copy files. Robocopy exit code: $LASTEXITCODE"
    exit 1
}

# Copy web.config for IIS
$webConfigSource = Join-Path $PSScriptRoot "web.config"
if (Test-Path $webConfigSource) {
    Copy-Item $webConfigSource -Destination $PhysicalPath -Force
}

Write-Host "  Files synced to: $PhysicalPath" -ForegroundColor Gray

# ============================================================
#  Step 4 - Configure appsettings.Production.json
# ============================================================
Write-Host "[4/6] Writing production configuration..." -ForegroundColor Cyan

$dbPath = (Join-Path $DataPath "data/cloudativ_assessment.db") -replace '\\', '/'
$logPath = (Join-Path $DataPath "logs/cloudativ-.log") -replace '\\', '/'

$productionSettings = @{
    ConnectionStrings = @{
        DefaultConnection = "Data Source=$dbPath"
    }
    Database = @{
        Provider = "SQLite"
    }
    Serilog = @{
        LogPath = $logPath
    }
} | ConvertTo-Json -Depth 10

$settingsPath = Join-Path $PhysicalPath "appsettings.Production.json"
Set-Content -Path $settingsPath -Value $productionSettings -Encoding UTF8
Write-Host "  Written: appsettings.Production.json" -ForegroundColor Gray

# ============================================================
#  Step 5 - Create IIS Application Pool and Site
# ============================================================
Write-Host "[5/6] Configuring IIS..." -ForegroundColor Cyan

Import-Module WebAdministration

$poolName = $SiteName

# Create or update Application Pool
if (-not (Test-Path "IIS:\AppPools\$poolName")) {
    New-WebAppPool -Name $poolName | Out-Null
    Write-Host "  Created app pool: $poolName" -ForegroundColor Gray
}

# Configure pool for ASP.NET Core (No Managed Code)
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "startMode" -Value "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "processModel.idleTimeout" -Value ([TimeSpan]::FromMinutes(0))

# Grant app pool identity permissions to data directory
$poolIdentity = "IIS AppPool\$poolName"
$acl = Get-Acl $DataPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule($poolIdentity, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl -Path $DataPath -AclObject $acl
Write-Host "  Granted permissions on: $DataPath" -ForegroundColor Gray

# Create or update Website
$existingSite = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if ($existingSite) {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name "physicalPath" -Value $PhysicalPath
    Write-Host "  Updated site: $SiteName" -ForegroundColor Gray
} else {
    if ($HostName) {
        New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $poolName `
            -Port $Port -Ssl -HostHeader $HostName | Out-Null
    } else {
        New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $poolName `
            -Port $Port | Out-Null
    }
    Write-Host "  Created site: $SiteName on port $Port" -ForegroundColor Gray
}

# ============================================================
#  Step 6 - Start and verify
# ============================================================
Write-Host "[6/6] Starting site..." -ForegroundColor Cyan

Start-Website -Name $SiteName -ErrorAction SilentlyContinue
Start-WebAppPool -Name $poolName -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  IIS Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Site Name    : $SiteName" -ForegroundColor White
Write-Host "  App Pool     : $poolName" -ForegroundColor White
Write-Host "  Physical Path: $PhysicalPath" -ForegroundColor Gray
Write-Host "  Data Path    : $DataPath" -ForegroundColor Gray
Write-Host "  Port         : $Port" -ForegroundColor Gray
Write-Host ""
Write-Host "  Default Login  : admin@cloudativ.local" -ForegroundColor White
Write-Host "  Default Pass   : Admin@123!" -ForegroundColor White
Write-Host ""
Write-Host "  Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Bind an SSL certificate in IIS Manager" -ForegroundColor White
Write-Host "  2. Configure the host header if using a domain name" -ForegroundColor White
Write-Host "  3. Change the default admin password immediately" -ForegroundColor Yellow
Write-Host ""
