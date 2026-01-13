# Cloudativ Assessment

A production-ready, multi-tenant web application for performing comprehensive Microsoft 365 security assessments. Built with .NET 8, Blazor Server, and Microsoft Graph SDK.

![Cloudativ Assessment](docs/dashboard-preview.png)

## Features

- **Multi-Tenant Architecture**: Onboard and manage multiple Microsoft 365 tenants
- **Comprehensive Security Assessment**: 9 security domains with 50+ checks
- **Real-time Dashboard**: Modern UI with live assessment progress
- **Modular Design**: Plugin-style assessment modules for easy extensibility
- **Background Processing**: Hangfire-based job scheduling
- **Export Capabilities**: PDF/HTML reports with corporate branding
- **Role-Based Access Control**: SuperAdmin, TenantAdmin, and Auditor roles

## Security Domains

| Domain | Description |
|--------|-------------|
| Identity & Access (IAM) | Users, admins, MFA, Conditional Access |
| Privileged Access / PIM | PIM configuration, eligible vs active roles |
| Device & Endpoint | Intune enrollment, compliance policies |
| Exchange / Email Security | Anti-phish, mail flow rules, DKIM/DMARC |
| Microsoft Defender | Defender for Office 365, Secure Score |
| Data Protection & Compliance | DLP policies, sensitivity labels, retention |
| Audit & Logging | Unified audit log, sign-in logs |
| App Governance / Consent | Enterprise apps, OAuth grants |
| Collaboration Security | Teams/SharePoint sharing settings |

## Technology Stack

- **.NET 8** (Latest LTS)
- **Blazor Server** for interactive UI
- **EF Core 8** with SQLite/PostgreSQL/SQL Server
- **Microsoft Graph SDK v5**
- **Hangfire** for background jobs
- **MudBlazor** for UI components
- **Serilog** for structured logging

## Prerequisites

- .NET 8 SDK
- An Azure AD App Registration with required permissions
- Visual Studio 2022 / VS Code / JetBrains Rider

## Required Microsoft Graph Permissions

Your Azure AD App Registration needs the following **Application** permissions:

```
User.Read.All
Directory.Read.All
RoleManagement.Read.Directory
Policy.Read.All
AuditLog.Read.All
SecurityEvents.Read.All
IdentityRiskyUser.Read.All
DeviceManagementConfiguration.Read.All
DeviceManagementManagedDevices.Read.All
Mail.Read
MailboxSettings.Read
Organization.Read.All
Application.Read.All
DelegatedPermissionGrant.Read.All
InformationProtectionPolicy.Read.All
Sites.Read.All
Team.ReadBasic.All
```

### Creating the App Registration

1. Go to [Azure Portal](https://portal.azure.com) > Azure Active Directory > App registrations
2. Click "New registration"
3. Name: "Cloudativ Assessment"
4. Supported account types: "Accounts in this organizational directory only"
5. Click Register
6. Go to "API permissions" and add the permissions listed above
7. Click "Grant admin consent"
8. Go to "Certificates & secrets" and create a new client secret
9. Note down: Application (client) ID, Directory (tenant) ID, Client secret value

## Quick Start

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/cloudativ/assessment.git
   cd assessment
   ```

2. **Configure user secrets**
   ```bash
   cd src/Cloudativ.Assessment.Web
   dotnet user-secrets init
   dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
   dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
   dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
   ```

3. **Restore and build**
   ```bash
   cd ../..
   dotnet restore
   dotnet build
   ```

4. **Run database migrations**
   ```bash
   cd src/Cloudativ.Assessment.Web
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - URL: https://localhost:5001 or http://localhost:5000
   - Default credentials: `admin@cloudativ.local` / `Admin@123!`

### Docker Deployment

1. **Build and run with Docker Compose (SQLite)**
   ```bash
   docker-compose up -d
   ```

2. **Build and run with PostgreSQL**
   ```bash
   docker-compose --profile postgres up -d
   ```

3. **Access the application**
   - URL: http://localhost:5000

### Azure App Service Deployment

1. **Create Azure resources**
   ```bash
   # Create resource group
   az group create --name rg-cloudativ --location eastus

   # Create App Service plan
   az appservice plan create --name plan-cloudativ --resource-group rg-cloudativ --sku B1 --is-linux

   # Create Web App
   az webapp create --name cloudativ-assessment --resource-group rg-cloudativ --plan plan-cloudativ --runtime "DOTNET|8.0"
   ```

2. **Configure environment variables**
   ```bash
   az webapp config appsettings set --name cloudativ-assessment --resource-group rg-cloudativ --settings \
     "Database__Provider=SQLite" \
     "ConnectionStrings__DefaultConnection=Data Source=/home/data/cloudativ.db"
   ```

3. **Deploy**
   ```bash
   dotnet publish -c Release -o ./publish
   cd publish
   zip -r ../app.zip .
   az webapp deployment source config-zip --name cloudativ-assessment --resource-group rg-cloudativ --src ../app.zip
   ```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=cloudativ_assessment.db"
  },
  "Database": {
    "Provider": "SQLite"  // SQLite, PostgreSQL, or SqlServer
  },
  "Authentication": {
    "UseEntraId": false
  },
  "OpenAI": {
    "Enabled": false,
    "Model": "gpt-4"
  }
}
```

### Database Providers

Switch between database providers by changing the `Database:Provider` setting:

| Provider | Connection String Example |
|----------|---------------------------|
| SQLite | `Data Source=cloudativ.db` |
| PostgreSQL | `Host=localhost;Database=cloudativ;Username=user;Password=pass` |
| SQL Server | `Server=localhost;Database=cloudativ;Trusted_Connection=true` |

## Project Structure

```
CloudativAssessment/
├── src/
│   ├── Cloudativ.Assessment.Domain/       # Entities, enums, interfaces
│   ├── Cloudativ.Assessment.Application/  # DTOs, services, business logic
│   ├── Cloudativ.Assessment.Infrastructure/ # EF Core, Graph SDK, modules
│   └── Cloudativ.Assessment.Web/          # Blazor UI, auth, pages
├── tests/
│   └── Cloudativ.Assessment.Application.Tests/
├── Dockerfile
├── docker-compose.yml
└── README.md
```

## Assessment Modules

Each security domain is implemented as a pluggable module implementing `IAssessmentModule`:

```csharp
public interface IAssessmentModule
{
    AssessmentDomain Domain { get; }
    string DisplayName { get; }
    IReadOnlyList<string> RequiredPermissions { get; }

    Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken ct);
    NormalizedFindings Normalize(CollectionResult rawData);
    DomainScore Score(NormalizedFindings findings);
}
```

### Adding a New Module

1. Create a new class in `Infrastructure/Modules/`
2. Inherit from `BaseAssessmentModule`
3. Implement the required methods
4. Register in `DependencyInjection.cs`

## Branding

The application follows Cloudativ brand guidelines:

| Element | Value |
|---------|-------|
| Primary Color | #E0FC8E (Light Green) |
| Secondary Color | #51627A (Dark Blue/Grey) |
| Accent Color | #EF7348 (Orange) |
| Tertiary Color | #B6CBE0 (Light Blue) |
| Primary Font | Poppins |

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `/health` | Health check endpoint |
| `/hangfire` | Background jobs dashboard (SuperAdmin only) |

## Troubleshooting

### Common Issues

1. **Graph API Permission Errors**
   - Ensure all permissions are granted admin consent
   - Verify the app registration has the correct permissions

2. **Database Migration Errors**
   - Delete the database file and re-run migrations
   - Check connection string format

3. **Authentication Failures**
   - Verify client ID, tenant ID, and client secret
   - Check that the secret hasn't expired

### Logs

Logs are written to:
- Console (development)
- `logs/cloudativ-{date}.log` (file)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## License

Proprietary - Cloudativ © 2024

## Support

For support, contact: support@cloudativ.com
