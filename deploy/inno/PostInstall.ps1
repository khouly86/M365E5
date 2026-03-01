# Cloudativ Assessment - Post-Installation Script
# Called by Inno Setup after files are copied.
# Receives parameters from the installer wizard.

param(
    [Parameter(Mandatory)][string]$InstallPath,
    [Parameter(Mandatory)][string]$DataPath,
    [Parameter(Mandatory)][int]$HttpsPort,
    [Parameter(Mandatory)][string]$ServiceName,
    [string]$CertificateSubject = $env:COMPUTERNAME,
    [switch]$SkipFirewall
)

$ErrorActionPreference = "Stop"

$logFile = Join-Path $DataPath "logs\install.log"
function Write-Log($msg) {
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts] $msg"
    Add-Content -Path $logFile -Value $line -ErrorAction SilentlyContinue
    Write-Host $line
}

# ============================================================
#  Ensure data directories exist
# ============================================================
@("data", "logs", "certs") | ForEach-Object {
    $dir = Join-Path $DataPath $_
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

Write-Log "Post-install started"
Write-Log "  InstallPath: $InstallPath"
Write-Log "  DataPath:    $DataPath"
Write-Log "  HttpsPort:   $HttpsPort"
Write-Log "  ServiceName: $ServiceName"

# ============================================================
#  1. Generate HTTPS certificate (if not exists)
# ============================================================
$certDir = Join-Path $DataPath "certs"
$certPath = Join-Path $certDir "cloudativ-assessment.pfx"
$certPassword = ""

if (Test-Path $certPath) {
    Write-Log "Certificate already exists, keeping it"
    # Try to read existing password
    $existingSettings = Join-Path $InstallPath "appsettings.Production.json"
    if (Test-Path $existingSettings) {
        try {
            $settings = Get-Content $existingSettings -Raw | ConvertFrom-Json
            $certPassword = $settings.Kestrel.Endpoints.Https.Certificate.Password
            Write-Log "Read existing certificate password from config"
        } catch {
            Write-Log "Could not read existing password, regenerating certificate"
            Remove-Item $certPath -Force
        }
    }
}

if (-not (Test-Path $certPath)) {
    $certPassword = [System.Guid]::NewGuid().ToString("N").Substring(0, 16)

    Write-Log "Generating self-signed certificate (CN=$CertificateSubject)"

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

    $securePassword = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $securePassword | Out-Null
    Remove-Item "Cert:\LocalMachine\My\$($cert.Thumbprint)" -Force

    Write-Log "Certificate saved to: $certPath"
}

# Verify certificate
try {
    $testCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath, $certPassword)
    Write-Log "Certificate verified: $($testCert.Subject), expires $($testCert.NotAfter.ToString('yyyy-MM-dd'))"
    $testCert.Dispose()
} catch {
    Write-Log "ERROR: Certificate verification failed: $_"
    exit 1
}

# ============================================================
#  2. Write appsettings.Production.json
# ============================================================
$dbPath = (Join-Path $DataPath "data/cloudativ_assessment.db") -replace '\\', '/'
$logPath = (Join-Path $DataPath "logs/cloudativ-.log") -replace '\\', '/'
$certPathJson = $certPath -replace '\\', '/'

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
Write-Log "Written: appsettings.Production.json"

# ============================================================
#  3. Register Windows Service
# ============================================================
$exePath = Join-Path $InstallPath "Cloudativ.Assessment.Web.exe"
$binPathArg = "`"$exePath`" --service"

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 3
    }
    sc.exe config $ServiceName binPath= $binPathArg start= auto | Out-Null
    Write-Log "Updated existing service"
} else {
    New-Service -Name $ServiceName `
        -BinaryPathName $binPathArg `
        -DisplayName "Cloudativ Assessment" `
        -Description "Cloudativ Assessment - Microsoft 365 E5 Security Assessment & Compliance Platform" `
        -StartupType Automatic | Out-Null
    Write-Log "Created service: $ServiceName"
}

# Auto-recovery
sc.exe failure $ServiceName reset= 86400 actions= restart/10000/restart/10000/restart/30000 | Out-Null

# Set environment to Production
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
Set-ItemProperty -Path $regPath -Name "Environment" -Value @("ASPNETCORE_ENVIRONMENT=Production") -Type MultiString
Write-Log "Service configured with auto-recovery and Production environment"

# ============================================================
#  4. Firewall rule
# ============================================================
if (-not $SkipFirewall) {
    $ruleName = "Cloudativ Assessment (HTTPS $HttpsPort)"
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

    if ($existingRule) {
        Set-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $HttpsPort -Action Allow
        Write-Log "Updated firewall rule: $ruleName"
    } else {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $HttpsPort -Action Allow -Profile Domain,Private | Out-Null
        Write-Log "Created firewall rule: $ruleName (Domain, Private)"
    }
} else {
    Write-Log "Firewall configuration skipped"
}

# ============================================================
#  5. Start service
# ============================================================
Write-Log "Starting service..."
Start-Service -Name $ServiceName

$maxWait = 30
$waited = 0
$healthy = $false

while ($waited -lt $maxWait) {
    Start-Sleep -Seconds 2
    $waited += 2
    try {
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
        $response = Invoke-WebRequest -Uri "https://localhost:$HttpsPort/health" -TimeoutSec 5 -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $healthy = $true
            break
        }
    } catch { }
}

if ($healthy) {
    Write-Log "Service is HEALTHY"
} else {
    Write-Log "Service started (health check pending - check logs)"
}

Write-Log "Post-install completed"
