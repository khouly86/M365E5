#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs Cloudativ Assessment as a Windows Service with HTTPS.
.DESCRIPTION
    Performs a complete installation on Windows Server:
    - Creates directory structure (binaries separate from data)
    - Copies application files
    - Generates self-signed HTTPS certificate
    - Configures application settings for production
    - Registers and starts Windows Service with auto-recovery
    - Configures firewall rule

    Idempotent: safe to re-run for upgrades or repairs.
.PARAMETER InstallPath
    Application binary directory. Default: C:\Program Files\Cloudativ Assessment
.PARAMETER DataPath
    Data directory (DB, logs, certs). Default: C:\ProgramData\Cloudativ Assessment
.PARAMETER HttpsPort
    HTTPS port for the application. Default: 5443
.PARAMETER CertificateSubject
    CN for the self-signed certificate. Default: machine hostname
.PARAMETER ServiceName
    Windows Service name. Default: CloudativAssessment
.PARAMETER ServiceDisplayName
    Windows Service display name. Default: Cloudativ Assessment
.PARAMETER SkipFirewall
    Skip firewall rule creation.
.PARAMETER Force
    Overwrite existing installation without prompting.
.EXAMPLE
    .\Install-CloudativAssessment.ps1
    .\Install-CloudativAssessment.ps1 -HttpsPort 8443 -Force
    .\Install-CloudativAssessment.ps1 -InstallPath "D:\Apps\Cloudativ" -DataPath "D:\Data\Cloudativ"
#>
[CmdletBinding()]
param(
    [string]$InstallPath = "C:\Program Files\Cloudativ Assessment",
    [string]$DataPath = "C:\ProgramData\Cloudativ Assessment",
    [int]$HttpsPort = 5443,
    [string]$CertificateSubject = $env:COMPUTERNAME,
    [string]$ServiceName = "CloudativAssessment",
    [string]$ServiceDisplayName = "Cloudativ Assessment",
    [switch]$SkipFirewall,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ============================================================
#  Banner
# ============================================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Cloudativ Assessment Installer" -ForegroundColor Cyan
Write-Host "  Windows Service Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
#  Validate source files
# ============================================================
$SourcePath = $PSScriptRoot
$ExeName = "Cloudativ.Assessment.Web.exe"
$ExeSource = Join-Path $SourcePath $ExeName

if (-not (Test-Path $ExeSource)) {
    Write-Error @"
Application executable not found: $ExeSource

Make sure you are running this script from the published output directory.
To build: run Build-Release.ps1 first, then run this script from the publish folder.
"@
    exit 1
}

# ============================================================
#  Step 1/8 - Check existing installation
# ============================================================
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
    if (-not $Force) {
        Write-Host "Existing installation detected. Service status: $($existingService.Status)" -ForegroundColor Yellow
        $response = Read-Host "Upgrade/repair the existing installation? (Y/N)"
        if ($response -notin @('Y', 'y', 'Yes', 'yes')) {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            exit 0
        }
    }

    Write-Host "[1/8] Stopping existing service..." -ForegroundColor Yellow
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
        # Wait for the process to fully exit and release file locks
        $waited = 0
        while ($waited -lt 15) {
            Start-Sleep -Seconds 1
            $waited++
            $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
            if ($svc.Status -eq 'Stopped') { break }
        }
    }
    Write-Host "  Service stopped." -ForegroundColor Gray
} else {
    Write-Host "[1/8] Fresh installation." -ForegroundColor Green
}

# ============================================================
#  Step 2/8 - Create directory structure
# ============================================================
Write-Host "[2/8] Creating directory structure..." -ForegroundColor Cyan

$directories = @(
    $InstallPath,
    (Join-Path $DataPath "data"),
    (Join-Path $DataPath "logs"),
    (Join-Path $DataPath "certs")
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    } else {
        Write-Host "  Exists:  $dir" -ForegroundColor DarkGray
    }
}

# ============================================================
#  Step 3/8 - Copy application files
# ============================================================
Write-Host "[3/8] Copying application files..." -ForegroundColor Cyan

$excludeFiles = @(
    "Install-CloudativAssessment.ps1",
    "Uninstall-CloudativAssessment.ps1"
)

# Use robocopy for efficient file sync (built into Windows)
$robocopyArgs = @(
    $SourcePath,
    $InstallPath,
    "/MIR",        # Mirror directory tree
    "/XF",         # Exclude files
    "Install-CloudativAssessment.ps1",
    "Uninstall-CloudativAssessment.ps1",
    "/NFL",        # No file list
    "/NDL",        # No directory list
    "/NJH",        # No job header
    "/NJS",        # No job summary
    "/NC",         # No class
    "/NS",         # No size
    "/NP"          # No progress
)

$robocopyResult = & robocopy @robocopyArgs
# Robocopy exit codes 0-7 are success
if ($LASTEXITCODE -gt 7) {
    Write-Error "Failed to copy application files. Robocopy exit code: $LASTEXITCODE"
    exit 1
}

# Copy uninstaller to the install directory
Copy-Item (Join-Path $SourcePath "Uninstall-CloudativAssessment.ps1") `
    -Destination $InstallPath -Force -ErrorAction SilentlyContinue

Write-Host "  Files synced to: $InstallPath" -ForegroundColor Gray

# ============================================================
#  Step 4/8 - Generate self-signed HTTPS certificate
# ============================================================
Write-Host "[4/8] Configuring HTTPS certificate..." -ForegroundColor Cyan

$certDir = Join-Path $DataPath "certs"
$certPath = Join-Path $certDir "cloudativ-assessment.pfx"
$certPassword = ""

if (Test-Path $certPath) {
    Write-Host "  Certificate already exists. Keeping existing certificate." -ForegroundColor DarkGray

    # Read existing password from appsettings.Production.json
    $existingSettingsPath = Join-Path $InstallPath "appsettings.Production.json"
    if (Test-Path $existingSettingsPath) {
        try {
            $existingSettings = Get-Content $existingSettingsPath -Raw | ConvertFrom-Json
            $certPassword = $existingSettings.Kestrel.Endpoints.Https.Certificate.Password
        } catch {
            Write-Warning "Could not read existing certificate password. A new certificate will be generated."
            Remove-Item $certPath -Force
        }
    }
}

if (-not (Test-Path $certPath)) {
    # Generate a random password for the PFX
    $certPassword = [System.Guid]::NewGuid().ToString("N").Substring(0, 16)

    $cert = New-SelfSignedCertificate `
        -DnsName $CertificateSubject, "localhost" `
        -CertStoreLocation "Cert:\LocalMachine\My" `
        -NotAfter (Get-Date).AddYears(5) `
        -FriendlyName "Cloudativ Assessment HTTPS" `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

    # Export to PFX file
    $securePassword = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $securePassword | Out-Null

    # Remove from cert store (Kestrel loads directly from file)
    Remove-Item "Cert:\LocalMachine\My\$($cert.Thumbprint)" -Force

    Write-Host "  Generated self-signed certificate" -ForegroundColor Gray
    Write-Host "  Subject: CN=$CertificateSubject" -ForegroundColor Gray
    Write-Host "  Valid until: $((Get-Date).AddYears(5).ToString('yyyy-MM-dd'))" -ForegroundColor Gray
}

# Verify certificate is loadable
try {
    $testCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
        $certPath, $certPassword
    )
    Write-Host "  Certificate verified: $($testCert.Subject)" -ForegroundColor Gray
    $testCert.Dispose()
} catch {
    Write-Error "Certificate verification failed: $_"
    exit 1
}

# ============================================================
#  Step 5/8 - Configure appsettings.Production.json
# ============================================================
Write-Host "[5/8] Writing production configuration..." -ForegroundColor Cyan

# Use forward slashes in paths for JSON (.NET handles both on Windows)
$dbPathJson = (Join-Path $DataPath "data/cloudativ_assessment.db") -replace '\\', '/'
$logPathJson = (Join-Path $DataPath "logs/cloudativ-.log") -replace '\\', '/'
$certPathJson = $certPath -replace '\\', '/'

$productionSettings = @{
    ConnectionStrings = @{
        DefaultConnection = "Data Source=$dbPathJson"
    }
    Database = @{
        Provider = "SQLite"
    }
    Serilog = @{
        LogPath = $logPathJson
    }
    Kestrel = @{
        Endpoints = @{
            Https = @{
                Url = "https://*:$HttpsPort"
                Certificate = @{
                    Path = $certPathJson
                    Password = $certPassword
                }
            }
        }
    }
} | ConvertTo-Json -Depth 10

$settingsPath = Join-Path $InstallPath "appsettings.Production.json"
Set-Content -Path $settingsPath -Value $productionSettings -Encoding UTF8

Write-Host "  Written: appsettings.Production.json" -ForegroundColor Gray
Write-Host "  Database: $dbPathJson" -ForegroundColor DarkGray
Write-Host "  Logs:     $logPathJson" -ForegroundColor DarkGray
Write-Host "  HTTPS:    port $HttpsPort" -ForegroundColor DarkGray

# ============================================================
#  Step 6/8 - Register Windows Service
# ============================================================
Write-Host "[6/8] Registering Windows Service..." -ForegroundColor Cyan

$exePath = Join-Path $InstallPath $ExeName
$binPathArg = "`"$exePath`" --service"

if ($existingService) {
    # Update existing service binary path
    sc.exe config $ServiceName binPath= $binPathArg start= auto | Out-Null
    Write-Host "  Updated existing service." -ForegroundColor Gray
} else {
    New-Service -Name $ServiceName `
        -BinaryPathName $binPathArg `
        -DisplayName $ServiceDisplayName `
        -Description "Cloudativ Assessment - Microsoft 365 E5 Security Assessment & Compliance Platform" `
        -StartupType Automatic | Out-Null
    Write-Host "  Created service: $ServiceName" -ForegroundColor Gray
}

# Configure auto-recovery: restart after 10s on first two failures, 30s on subsequent
sc.exe failure $ServiceName reset= 86400 actions= restart/10000/restart/10000/restart/30000 | Out-Null
Write-Host "  Auto-recovery configured (restart on failure)." -ForegroundColor Gray

# Set ASPNETCORE_ENVIRONMENT=Production via service registry
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
Set-ItemProperty -Path $regPath -Name "Environment" -Value @("ASPNETCORE_ENVIRONMENT=Production") -Type MultiString
Write-Host "  Environment: Production" -ForegroundColor Gray

# ============================================================
#  Step 7/8 - Configure firewall
# ============================================================
Write-Host "[7/8] Configuring firewall..." -ForegroundColor Cyan

if ($SkipFirewall) {
    Write-Host "  Skipped (-SkipFirewall)." -ForegroundColor DarkGray
} else {
    $ruleName = "Cloudativ Assessment (HTTPS $HttpsPort)"
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

    if ($existingRule) {
        Set-NetFirewallRule -DisplayName $ruleName `
            -Direction Inbound `
            -Protocol TCP `
            -LocalPort $HttpsPort `
            -Action Allow
        Write-Host "  Updated firewall rule: $ruleName" -ForegroundColor Gray
    } else {
        New-NetFirewallRule -DisplayName $ruleName `
            -Direction Inbound `
            -Protocol TCP `
            -LocalPort $HttpsPort `
            -Action Allow `
            -Profile Domain,Private | Out-Null
        Write-Host "  Created firewall rule: $ruleName (Domain, Private)" -ForegroundColor Gray
    }
}

# ============================================================
#  Step 8/8 - Start service and verify health
# ============================================================
Write-Host "[8/8] Starting service..." -ForegroundColor Cyan

Start-Service -Name $ServiceName

# Poll the health endpoint
$maxWait = 30
$waited = 0
$healthy = $false

while ($waited -lt $maxWait) {
    Start-Sleep -Seconds 2
    $waited += 2

    try {
        # PowerShell 7+ has -SkipCertificateCheck; for PS 5.1 we disable validation
        if ($PSVersionTable.PSVersion.Major -ge 7) {
            $response = Invoke-WebRequest -Uri "https://localhost:$HttpsPort/health" `
                -SkipCertificateCheck -TimeoutSec 5 -UseBasicParsing -ErrorAction SilentlyContinue
        } else {
            # PowerShell 5.1: temporarily trust all certs for the health check
            if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
                Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) { return true; }
}
"@
            }
            [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
            $response = Invoke-WebRequest -Uri "https://localhost:$HttpsPort/health" `
                -TimeoutSec 5 -UseBasicParsing -ErrorAction SilentlyContinue
        }

        if ($response.StatusCode -eq 200) {
            $healthy = $true
            break
        }
    } catch {
        # Service still starting
    }

    Write-Host "  Waiting for service... ($waited/$maxWait sec)" -ForegroundColor DarkGray
}

# ============================================================
#  Summary
# ============================================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

if ($healthy) {
    Write-Host "  Status         : HEALTHY" -ForegroundColor Green
} else {
    Write-Host "  Status         : Service started (health check pending)" -ForegroundColor Yellow
    Write-Host "                   Check logs: $(Join-Path $DataPath 'logs')" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  URL            : https://$($CertificateSubject):$HttpsPort" -ForegroundColor White
Write-Host "  Default Login  : admin@cloudativ.local" -ForegroundColor White
Write-Host "  Default Pass   : Admin@123!" -ForegroundColor White
Write-Host ""
Write-Host "  Install Path   : $InstallPath" -ForegroundColor Gray
Write-Host "  Data Path      : $DataPath" -ForegroundColor Gray
Write-Host "  Database       : $(Join-Path $DataPath 'data\cloudativ_assessment.db')" -ForegroundColor Gray
Write-Host "  Logs           : $(Join-Path $DataPath 'logs')" -ForegroundColor Gray
Write-Host "  Certificate    : $(Join-Path $DataPath 'certs')" -ForegroundColor Gray
Write-Host "  Service        : $ServiceName" -ForegroundColor Gray
Write-Host ""
Write-Host "  Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Open https://$($CertificateSubject):$HttpsPort in your browser" -ForegroundColor White
Write-Host "  2. Accept the self-signed certificate warning" -ForegroundColor White
Write-Host "  3. Log in with the default credentials above" -ForegroundColor White
Write-Host "  4. CHANGE the default admin password immediately" -ForegroundColor Yellow
Write-Host "  5. Add your first Microsoft 365 tenant" -ForegroundColor White
Write-Host ""
Write-Host "  Useful Commands:" -ForegroundColor Cyan
Write-Host "  Restart   : Restart-Service $ServiceName" -ForegroundColor Gray
Write-Host "  Stop      : Stop-Service $ServiceName" -ForegroundColor Gray
Write-Host "  Status    : Get-Service $ServiceName" -ForegroundColor Gray
Write-Host "  Logs      : Get-Content '$(Join-Path $DataPath 'logs\cloudativ-*.log')' -Tail 50" -ForegroundColor Gray
Write-Host "  Uninstall : & '$(Join-Path $InstallPath 'Uninstall-CloudativAssessment.ps1')'" -ForegroundColor Gray
Write-Host ""
