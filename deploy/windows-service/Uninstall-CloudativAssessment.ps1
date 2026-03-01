#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Uninstalls Cloudativ Assessment Windows Service.
.DESCRIPTION
    Removes the Windows Service, firewall rule, and application files.
    By default, preserves the database, logs, and certificates in ProgramData.
    Use -RemoveData to also delete all data (requires typing DELETE to confirm).
.PARAMETER ServiceName
    Windows Service name. Default: CloudativAssessment
.PARAMETER InstallPath
    Application binary directory. Default: C:\Program Files\Cloudativ Assessment
.PARAMETER DataPath
    Data directory. Default: C:\ProgramData\Cloudativ Assessment
.PARAMETER RemoveData
    Also remove database, logs, and certificates.
.EXAMPLE
    .\Uninstall-CloudativAssessment.ps1
    .\Uninstall-CloudativAssessment.ps1 -RemoveData
#>
[CmdletBinding()]
param(
    [string]$ServiceName = "CloudativAssessment",
    [string]$InstallPath = "C:\Program Files\Cloudativ Assessment",
    [string]$DataPath = "C:\ProgramData\Cloudativ Assessment",
    [switch]$RemoveData
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  Cloudativ Assessment Uninstaller" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

# ============================================================
#  Step 1 - Stop and remove the Windows Service
# ============================================================
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($service) {
    if ($service.Status -eq 'Running') {
        Write-Host "Stopping service: $ServiceName..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force
        $waited = 0
        while ($waited -lt 15) {
            Start-Sleep -Seconds 1
            $waited++
            $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
            if ($null -eq $svc -or $svc.Status -eq 'Stopped') { break }
        }
    }

    Write-Host "Removing service: $ServiceName..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    # Wait for SCM to process deletion
    Start-Sleep -Seconds 2
    Write-Host "  Service removed." -ForegroundColor Gray
} else {
    Write-Host "Service '$ServiceName' not found (already removed)." -ForegroundColor DarkGray
}

# ============================================================
#  Step 2 - Remove firewall rules
# ============================================================
$firewallRules = Get-NetFirewallRule -DisplayName "Cloudativ Assessment*" -ErrorAction SilentlyContinue

if ($firewallRules) {
    $firewallRules | Remove-NetFirewallRule
    Write-Host "Firewall rule(s) removed." -ForegroundColor Gray
} else {
    Write-Host "No firewall rules found." -ForegroundColor DarkGray
}

# ============================================================
#  Step 3 - Remove application files
# ============================================================
if (Test-Path $InstallPath) {
    Write-Host "Removing application files: $InstallPath..." -ForegroundColor Yellow
    Remove-Item -Path $InstallPath -Recurse -Force
    Write-Host "  Application files removed." -ForegroundColor Gray
} else {
    Write-Host "Install directory not found: $InstallPath" -ForegroundColor DarkGray
}

# ============================================================
#  Step 4 - Optionally remove data
# ============================================================
if ($RemoveData) {
    Write-Host ""
    Write-Warning "This will PERMANENTLY delete:"
    Write-Host "  - Database:     $(Join-Path $DataPath 'data')" -ForegroundColor Red
    Write-Host "  - Logs:         $(Join-Path $DataPath 'logs')" -ForegroundColor Red
    Write-Host "  - Certificates: $(Join-Path $DataPath 'certs')" -ForegroundColor Red
    Write-Host ""
    $confirm = Read-Host "Type 'DELETE' to confirm data removal"

    if ($confirm -eq 'DELETE') {
        if (Test-Path $DataPath) {
            Remove-Item -Path $DataPath -Recurse -Force
            Write-Host "  Data directory removed." -ForegroundColor Gray
        }
    } else {
        Write-Host "  Data removal cancelled. Data preserved at: $DataPath" -ForegroundColor DarkGray
    }
} else {
    Write-Host ""
    Write-Host "Data preserved at: $DataPath" -ForegroundColor Cyan
    Write-Host "  (Database, logs, and certificates were NOT removed)" -ForegroundColor Gray
    Write-Host "  To also remove data, re-run with: -RemoveData" -ForegroundColor Gray
}

# ============================================================
#  Done
# ============================================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Uninstall Complete" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
