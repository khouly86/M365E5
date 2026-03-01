# Cloudativ Assessment - Deployment Guide

## Deployment Structure

```
deploy/
├── DEPLOYMENT.md                          # This guide
├── config/
│   ├── .env.production                    # Environment variables (Docker)
│   └── appsettings.Production.json.template  # App config template (Windows)
├── windows-service/
│   ├── Build-Release.ps1                  # Build self-contained package
│   ├── Install-CloudativAssessment.ps1    # Install as Windows Service
│   └── Uninstall-CloudativAssessment.ps1  # Uninstall service
├── docker/
│   ├── Dockerfile                         # Production Docker image
│   ├── docker-compose.yml                 # Docker Compose (SQLite)
│   └── docker-compose.postgres.yml        # PostgreSQL overlay
└── iis/
    ├── web.config                         # IIS configuration
    └── Setup-IIS.ps1                      # IIS site setup script
```

---

## Option 1: Windows Service (Recommended for Windows Server)

Self-contained deployment. Bundles .NET 8 runtime — no prerequisites on target server.

### Build

```powershell
cd deploy\windows-service
.\Build-Release.ps1
```

Output: `deploy\windows-service\publish\` — copy this folder to the target server.

### Install

On the target server, open PowerShell **as Administrator**:

```powershell
.\Install-CloudativAssessment.ps1
```

This will:
1. Create `C:\Program Files\Cloudativ Assessment\` (binaries)
2. Create `C:\ProgramData\Cloudativ Assessment\` (data, logs, certs)
3. Generate a self-signed HTTPS certificate
4. Register a Windows Service with auto-start and auto-recovery
5. Open firewall port 5443
6. Start the service

### Custom Options

```powershell
# Custom port
.\Install-CloudativAssessment.ps1 -HttpsPort 8443

# Custom paths
.\Install-CloudativAssessment.ps1 -InstallPath "D:\Apps\Cloudativ" -DataPath "D:\Data\Cloudativ"

# Skip firewall (handled by external firewall)
.\Install-CloudativAssessment.ps1 -SkipFirewall

# Silent upgrade
.\Install-CloudativAssessment.ps1 -Force
```

### Manage

```powershell
Restart-Service CloudativAssessment
Stop-Service CloudativAssessment
Get-Service CloudativAssessment
```

### Uninstall

```powershell
# Remove service and binaries (keeps data)
.\Uninstall-CloudativAssessment.ps1

# Remove everything including database
.\Uninstall-CloudativAssessment.ps1 -RemoveData
```

---

## Option 2: Docker

### Quick Start (SQLite)

```bash
docker compose up -d
```

Application available at `http://localhost:5000`

### With PostgreSQL

```bash
docker compose --profile postgres up -d
```

### Custom Port

```bash
APP_PORT=8080 docker compose up -d
```

### Using deploy/docker/ Files

```bash
cd deploy/docker
docker compose up -d

# With PostgreSQL overlay
docker compose -f docker-compose.yml -f docker-compose.postgres.yml up -d
```

### Configuration

Edit `deploy/config/.env.production` before starting:

```bash
# Enable OpenAI
OpenAI__Enabled=true
OpenAI__ApiKey=sk-...

# Custom branding
Branding__CompanyName=YourCompany
```

### Data Persistence

| Volume | Path | Content |
|--------|------|---------|
| `cloudativ-data` | `/app/data/` | SQLite database |
| `cloudativ-logs` | `/app/logs/` | Application logs |

### Backup

```bash
# Backup database
docker cp cloudativ-assessment:/app/data/cloudativ_assessment.db ./backup.db

# Backup logs
docker cp cloudativ-assessment:/app/logs/ ./logs-backup/
```

---

## Option 3: IIS

Requires the ASP.NET Core 8.0 Hosting Bundle installed on the server.

### Prerequisites

1. Install IIS:
   ```powershell
   Install-WindowsFeature -Name Web-Server -IncludeManagementTools
   ```

2. Download and install the [ASP.NET Core 8.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build

```powershell
cd deploy\windows-service
.\Build-Release.ps1
```

### Setup

```powershell
cd deploy\iis
.\Setup-IIS.ps1 -SourcePath "..\windows-service\publish"
```

This will:
1. Copy files to `C:\inetpub\CloudativAssessment`
2. Create an IIS Application Pool (No Managed Code, Always Running)
3. Create the IIS Website
4. Configure `appsettings.Production.json`

### SSL Certificate

After setup, bind an SSL certificate in IIS Manager:
1. Open IIS Manager
2. Select the site > Bindings
3. Add HTTPS binding with your certificate

---

## Post-Installation

### Default Credentials

| Field | Value |
|-------|-------|
| URL | `https://hostname:5443` (Windows Service) or `http://localhost:5000` (Docker) |
| Email | `admin@cloudativ.local` |
| Password | `Admin@123!` |

**Change the default password immediately after first login.**

### Add a Microsoft 365 Tenant

1. Log in as admin
2. Navigate to Tenants > Add Tenant
3. Enter your Azure AD Tenant ID
4. The app will guide you through App Registration setup
5. Grant the required read-only Graph API permissions

### Configuration Reference

| Setting | Description | Default |
|---------|-------------|---------|
| `Database:Provider` | `SQLite`, `PostgreSQL`, or `SqlServer` | `SQLite` |
| `OpenAI:Enabled` | Enable AI compliance analysis | `false` |
| `OpenAI:ApiKey` | OpenAI API key | (empty) |
| `Stripe:SecretKey` | Stripe payment processing | (empty) |
| `Branding:CompanyName` | Company name in reports | `Cloudativ` |
| `Authentication:UseEntraId` | Enable Azure AD SSO | `false` |

### Health Check

All deployments expose a health endpoint:

```
GET /health
```

Returns `200 OK` when the application and database are healthy.

### Logs

| Deployment | Log Location |
|------------|-------------|
| Windows Service | `C:\ProgramData\Cloudativ Assessment\logs\` |
| Docker | `/app/logs/` (inside container) |
| IIS | `C:\ProgramData\Cloudativ Assessment\logs\` + `C:\inetpub\CloudativAssessment\logs\stdout` |

Logs rotate daily with 30-day retention.

---

## System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| OS | Windows Server 2019 / Linux | Windows Server 2022 / Ubuntu 22.04 |
| CPU | 2 cores | 4 cores |
| RAM | 2 GB | 4 GB |
| Disk | 1 GB (app) + database | 10 GB |
| Network | Outbound HTTPS to `graph.microsoft.com` | Same |
| .NET | Bundled (self-contained) or .NET 8 Runtime | Same |
