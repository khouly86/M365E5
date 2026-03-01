# Cloudativ Assessment Tool — Product Datasheet

---

## Overview

Cloudativ Assessment Tool is an enterprise-grade security assessment and compliance platform purpose-built for **Microsoft 365 E5** environments. It delivers automated security posture evaluation across 9 assessment domains with 70+ security checks, comprehensive asset inventory across 33 entity types, and AI-powered compliance gap analysis against 41 global regulatory standards.

Designed for **MSSPs, consultants, and enterprise security teams**, the platform provides actionable findings with severity-based prioritization, branded PDF/Excel reports, and continuous posture monitoring — all through a modern web interface.

---

## Key Capabilities

### Automated Security Assessment
- **9 Assessment Domains** covering the full M365 E5 security surface
- **70+ Security Checks** with severity classification (Critical, High, Medium, Low)
- **Automated Scoring** — Overall and per-domain scores (0–100) with letter grades (A–F)
- **Actionable Remediation** — Every finding includes step-by-step remediation guidance
- **Evidence-Based** — Raw API snapshots preserved for audit trail and forensic review
- **Assessment Comparison** — Track security posture improvement across multiple runs

### Comprehensive M365 Inventory
- **33 Entity Types** across 12 inventory domains
- **Delta Tracking** — Snapshot-based inventory with change detection
- **License Utilization Analysis** — 40+ license categories with E5 value-leakage detection
- **High-Risk Findings** — Auto-generated findings from inventory anomalies
- **Export Ready** — Full inventory export to PDF and Excel

### AI-Powered Governance & Compliance
- **41 Compliance Standards** — CIS, NIST, ISO 27001, PCI DSS, GDPR, SOC 2, and 35 more
- **GPT-4 Integration** — AI-driven compliance gap analysis mapping findings to standard controls
- **Grounded Analysis** — Upload compliance documents (up to 2 MB) for context-aware mapping
- **Gap Prioritization** — Recommendations ranked by effort (Quick Win, Short Term, Long Term)
- **Multi-Standard Comparison** — Assess against multiple standards simultaneously

### Professional Reporting
- **Branded PDF Reports** with charts, donut graphs, bar charts, and stat cards
- **Excel Reports** with formatted data tables, auto-filters, and color-coded severity
- **Report Types**: Full Assessment, Domain-Specific, Governance Compliance, Progress Comparison, Impacted Resources, Resource-Specific
- **White-Label Ready** — Corporate branding with custom logos and accent colors

---

## Assessment Domains

| Domain | Checks | Key Areas |
|--------|--------|-----------|
| **Identity & Access (IAM)** | IAM-001 to IAM-008 | MFA enforcement, Conditional Access, legacy auth blocking, risky users, password policies |
| **Privileged Access (PIM)** | PAM-001 to PAM-007 | PIM configuration, eligible vs active roles, admin protection, just-in-time access |
| **Device & Endpoint** | DEV-001 to DEV-007 | Intune enrollment, device compliance, BitLocker encryption, configuration profiles |
| **Exchange & Email Security** | EXO-001 to EXO-006+ | Anti-phishing, mail flow rules, DKIM/DMARC/SPF, auto-forwarding controls |
| **Microsoft Defender XDR** | MDE-001 to MDE-006+ | Defender for Endpoint, Office 365, Identity, Cloud Apps, Secure Score |
| **Data Protection (Purview)** | DLP-001 to DLP-007 | DLP policies, sensitivity labels, retention policies, compliance policies |
| **Audit & Logging** | AUD-001 to AUD-008 | Unified audit logs, sign-in logs, alert policies, log retention |
| **App Governance & Consent** | APP-001 to APP-008 | Enterprise apps, OAuth consent grants, high-privilege permissions, service principals |
| **Collaboration Security** | COL-001 to COL-008 | Teams/SharePoint sharing, guest access, file sharing policies, external collaboration |

---

## Inventory Domains

| Domain | Entity Types | Description |
|--------|-------------|-------------|
| Tenant Baseline | Tenant Info, License Subscriptions | Org settings, domains, service health |
| Identity & Access | Users, Groups, Roles, CA Policies, Auth Methods | Full Entra ID inventory |
| Device & Endpoint | Devices, Compliance Policies, Config Profiles | Intune managed estate |
| Microsoft Defender | Endpoint, Office 365, Identity, Cloud Apps | XDR configuration status |
| Email & Exchange | Organization Settings, Transport Rules | Exchange Online posture |
| Data Protection | Sensitivity Labels, DLP Policies, Compliance | Purview configuration |
| SharePoint & Teams | Sites, Settings, Teams Configuration | Collaboration inventory |
| Applications & OAuth | Enterprise Apps, OAuth Consents, Service Principals | App governance inventory |
| Logs & Monitoring | Audit Log Settings, Alert Policies | Monitoring configuration |
| Secure Score | Score Metrics, Improvement Actions | Microsoft Secure Score |
| License Utilization | License Categories, Feature Enablement | E5 value analysis |
| High-Risk Findings | Auto-Generated Findings | Inventory-based risk detection |

---

## Supported Compliance Standards (41)

**Middle East & Regional**
- Saudi NCA — CCC, ECC, DCC
- Saudi PDPL
- UAE Information Assurance Standards (IAS)
- Qatar National Information Assurance Policy

**International Standards**
- ISO 27001:2022, ISO 27017, ISO 27018, ISO 27701, ISO 22301

**Industry Frameworks**
- CIS Controls v8, CIS M365 Benchmark
- NIST CSF, NIST SP 800-53 Rev 5, NIST SP 800-171
- COBIT 2019

**Financial & Payment**
- PCI DSS v4.0, SOX, SWIFT CSP

**Healthcare**
- HIPAA Security Rule, HITRUST CSF

**Privacy & Data Protection**
- GDPR (EU), CCPA/CPRA (California)

**Government & Defense**
- FedRAMP, CMMC 2.0, CJIS, ITAR

**Cloud Audit**
- SOC 1 Type II, SOC 2 Type II, CSA STAR

**European Regulations**
- EU NIS2 Directive, EU DORA, UK Cyber Essentials Plus, BSI IT-Grundschutz

**Asia-Pacific**
- MAS TRM (Singapore), Australia IRAP, India CERT-In

**Microsoft-Specific**
- M365 Secure Score, Microsoft Cloud Security Benchmark (MCSB)

**Critical Infrastructure**
- NERC CIP, IEC 62443

---

## Architecture & Technology

| Component | Technology |
|-----------|-----------|
| **Runtime** | .NET 8 (LTS) |
| **UI Framework** | Blazor Server with MudBlazor (Material Design) |
| **API Integration** | Microsoft Graph SDK v5 (read-only) |
| **Database** | EF Core 8 — SQLite, PostgreSQL, or SQL Server |
| **Background Jobs** | Hangfire (parallel assessment execution) |
| **AI Engine** | OpenAI GPT-4 (configurable model) |
| **Reporting** | QuestPDF (PDF), ClosedXML (Excel) |
| **Payments** | Stripe (subscriptions, checkout) |
| **Logging** | Serilog (structured, file + console) |
| **Architecture** | Clean Architecture (4-layer) |

---

## Multi-Tenant & Role-Based Access

| Role | Capabilities |
|------|-------------|
| **SuperAdmin** | Full system access, all tenants, user management, Hangfire dashboard |
| **TenantAdmin** | Manage specific tenants, run assessments, configure settings |
| **Auditor** | View-only access to assessment results, reports, and inventory |
| **DomainAdmin** | Scoped access to specific assessment domains only |

- Multi-tenant isolation with per-tenant role assignments
- Domain-level granular permissions
- Policy-based authorization enforcement

---

## Deployment Options

- **Self-Hosted** — Deploy on any Windows/Linux server or container
- **Database Flexibility** — SQLite for small deployments, PostgreSQL or SQL Server for enterprise
- **Cloud-Ready** — Azure App Service, AWS ECS, or any .NET 8 compatible host
- **Single Binary** — No external dependencies beyond database and Graph API access

---

## Microsoft Graph API — Read-Only Integration

The platform connects to Microsoft 365 via **Azure AD App Registration** using the OAuth 2.0 Client Credentials flow. All permissions are **read-only** — no modifications are made to the tenant.

**Required Permissions (Application):**
User.Read.All | Directory.Read.All | RoleManagement.Read.Directory | Policy.Read.All | AuditLog.Read.All | IdentityRiskyUser.Read.All | SecurityEvents.Read.All | DeviceManagementConfiguration.Read.All | DeviceManagementManagedDevices.Read.All | MailboxSettings.Read | Organization.Read.All | Application.Read.All | DelegatedPermissionGrant.Read.All | InformationProtectionPolicy.Read.All | Sites.Read.All | Team.ReadBasic.All

---

## Subscription Plans

| Feature | Trial | Monthly | Yearly |
|---------|-------|---------|--------|
| Full Assessment | Limited | Unlimited | Unlimited |
| Inventory Collection | Limited | Unlimited | Unlimited |
| Governance Analysis | Limited | Unlimited | Unlimited |
| PDF/Excel Reports | Limited | Unlimited | Unlimited |
| Multi-Tenant | Limited | Unlimited | Unlimited |
| Priority Support | — | Included | Included |

---

## Platform Metrics

| Metric | Value |
|--------|-------|
| Assessment Domains | 9 |
| Security Checks | 70+ |
| Inventory Entity Types | 33 |
| Compliance Standards | 41 |
| License Categories Tracked | 40+ |
| Application Pages | 50+ |
| Report Types | 7 |

---

## Security Datasheet

### Security Architecture Overview

Cloudativ Assessment Tool is built on a **defense-in-depth** security architecture using .NET 8 (LTS) and ASP.NET Core best practices. The platform enforces strict separation of concerns through Clean Architecture, with security controls applied at every layer — from network transport to database storage.

```
┌──────────────────────────────────────────────────────────────┐
│                      Client (Browser)                        │
│              HTTPS/TLS 1.2+ · HSTS Enforced                 │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                  ASP.NET Core Middleware                      │
│  HTTPS Redirect · Cookie Auth · RBAC · SameSite · HttpOnly   │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                   Application Layer                          │
│  Input Validation · Authorization Policies · Domain Guards   │
└──────┬──────────────────┬───────────────────┬────────────────┘
       │                  │                   │
┌──────▼──────┐  ┌────────▼────────┐  ┌───────▼───────┐
│  Database   │  │  Microsoft      │  │  External     │
│  EF Core    │  │  Graph API      │  │  Services     │
│  Encrypted  │  │  OAuth 2.0      │  │  Stripe/OpenAI│
│  Secrets    │  │  Read-Only      │  │  HTTPS Only   │
└─────────────┘  └─────────────────┘  └───────────────┘
```

---

### Authentication

| Control | Implementation |
|---------|---------------|
| **Method** | Cookie-based authentication with ASP.NET Core Identity |
| **Password Hashing** | PBKDF2 with SHA-256 — 100,000 iterations, 16-byte salt, 32-byte hash |
| **Session Duration** | 8-hour expiration with sliding expiration |
| **Cookie Security** | `HttpOnly`, `SameSite=Lax`, `SecurePolicy=SameAsRequest` |
| **External Auth** | Azure AD / Entra ID via OpenID Connect (optional) |
| **Account Protection** | Generic error messages prevent user enumeration |
| **Active Status** | Accounts must be explicitly activated; deactivated users are denied login |

---

### Authorization & Access Control

| Role | Access Scope |
|------|-------------|
| **SuperAdmin** | Unrestricted — all tenants, users, system administration, Hangfire dashboard |
| **TenantAdmin** | Manage assigned tenants — run assessments, configure settings, view reports |
| **Auditor** | Read-only — view assessment results, reports, and inventory data |
| **DomainAdmin** | Scoped — access restricted to assigned assessment domains only |

**Enforcement Mechanisms:**
- ASP.NET Core policy-based authorization on all endpoints
- Per-tenant role assignments via `TenantUserAccess`
- Domain-level access control via `UserDomainAccess` and `DomainAuthorizationGuard`
- Self-modification prevention (users cannot delete, deactivate, or escalate their own accounts)

---

### Data Encryption

| Layer | Method |
|-------|--------|
| **In Transit** | HTTPS with TLS 1.2+ enforced via `UseHttpsRedirection()` and HSTS |
| **At Rest — Secrets** | ASP.NET Core Data Protection API (DPAPI) with keys persisted to database |
| **At Rest — Passwords** | PBKDF2-SHA256 (100,000 iterations) — no reversible password storage |
| **At Rest — Credentials** | Azure AD client secrets and API keys encrypted before database storage |
| **Database** | Supports SQLite, PostgreSQL, and SQL Server encryption capabilities |

**Encrypted Fields:**
- `ClientSecretEncrypted` — Azure AD application secret
- `OpenAiApiKeyEncrypted` — Optional AI service API key
- `PasswordHash` — Salted and hashed user credentials

---

### API & Integration Security

**Microsoft Graph API:**
- OAuth 2.0 Client Credentials flow (application-level, not user-delegated)
- **All permissions are read-only** — the platform never modifies tenant data
- Credentials encrypted at rest and decrypted only at runtime
- Per-tenant credential isolation — each tenant has its own Azure AD app registration
- Connection validation before use; graceful degradation on permission failures

**Stripe Payment Processing:**
- Webhook signature verification via `EventUtility.ConstructEvent()`
- API keys stored in configuration (never in source code)
- HTTPS-only communication

**OpenAI Integration (Optional):**
- API keys encrypted in database per tenant
- HTTPS-only communication
- Can be disabled entirely via configuration

---

### Multi-Tenant Data Isolation

| Control | Implementation |
|---------|---------------|
| **Data Segregation** | All entities linked via `TenantId` foreign key with database-enforced constraints |
| **Query Filtering** | Repository layer filters all queries by `TenantId` |
| **Credential Isolation** | Each tenant's Azure AD secrets encrypted independently |
| **Access Control** | Users mapped to specific tenants via `TenantUserAccess` table |
| **Cross-Tenant Prevention** | Foreign key constraints and application-layer validation prevent data leakage |
| **Subscription Isolation** | Per-tenant subscription limits and assessment counters |

---

### Input Validation & Application Security

| Control | Implementation |
|---------|---------------|
| **Data Annotations** | `[Required]`, `[EmailAddress]`, `[StringLength]`, `[RegularExpression]` on all DTOs |
| **Password Policy** | Minimum 8 characters, requires uppercase, lowercase, digit, and special character |
| **Service-Layer Validation** | Duplicate checks, existence validation, permission verification |
| **CSRF Protection** | `SameSite=Lax` cookies; Blazor Server-side rendering eliminates client-side CSRF surface |
| **XSS Protection** | `HttpOnly` cookies; Blazor auto-escapes all rendered output; no raw HTML injection |
| **Injection Prevention** | Entity Framework parameterized queries; no raw SQL construction |
| **Server-Side Rendering** | All UI logic executes on the server — no sensitive data exposed to the client |

---

### Secrets Management

| Environment | Method |
|-------------|--------|
| **Development** | .NET User Secrets (`dotnet user-secrets`) — secrets stored outside project directory |
| **Production** | Environment variables or Azure Key Vault (via `Azure.Identity`) |
| **Database Secrets** | Encrypted via ASP.NET Core Data Protection API before storage |
| **Configuration Priority** | Environment Variables > Key Vault > User Secrets > appsettings.json |

---

### Logging, Monitoring & Audit Trail

| Capability | Details |
|------------|---------|
| **Framework** | Serilog structured logging |
| **Output** | Console (development) + rolling file logs (`logs/cloudativ-{date}.log`) |
| **Retention** | 30-day rolling log retention |
| **Audited Events** | Login success/failure, user CRUD, tenant management, assessment runs, export operations, Graph API calls, Stripe webhooks |
| **Sensitive Data** | Passwords, tokens, secrets, and API keys are **never logged** in plaintext |
| **Health Checks** | `/health` endpoint for infrastructure monitoring |
| **Job Monitoring** | Hangfire dashboard (SuperAdmin access only) for background job status |

---

### Network & Transport Security

| Control | Status |
|---------|--------|
| **HTTPS Enforcement** | `UseHttpsRedirection()` — all HTTP requests redirected to HTTPS |
| **HSTS** | Enabled in production — `Strict-Transport-Security` header prevents protocol downgrade |
| **CORS** | No cross-origin access configured — same-origin only by default |
| **Cookie Scope** | Authentication cookies scoped to application domain |
| **SignalR Security** | Blazor Server SignalR connection validates origin |

---

### Compliance & Regulatory Readiness

The platform both **assesses compliance** and is **built with compliance in mind**:

| Principle | Implementation |
|-----------|---------------|
| **Data Minimization** | Read-only Graph API permissions — collects only what is needed for assessment |
| **Purpose Limitation** | Data used solely for security assessment, inventory, and compliance analysis |
| **User Rights** | Account deletion with cascading data removal; password reset; account disable |
| **Audit Trail** | Structured logs of all administrative and assessment operations |
| **Encryption** | Data encrypted in transit (TLS) and at rest (DPAPI) |
| **Access Control** | Role-based access with tenant isolation and domain-level scoping |
| **Data Residency** | Self-hosted model — data stays in customer's chosen infrastructure |
| **Retention Control** | Configurable per deployment; 30-day log retention by default |

---

### Dependency & Supply Chain Security

| Control | Details |
|---------|---------|
| **Runtime** | .NET 8 LTS — supported with security patches through November 2026 |
| **Package Sources** | NuGet.org (official .NET package registry) |
| **Key Dependencies** | Microsoft Graph SDK 5.x, Azure.Identity 1.17, EF Core 8.x — all Microsoft-maintained |
| **UI Framework** | MudBlazor 6.x — open-source, actively maintained |
| **Payment** | Stripe.net 45.x — PCI DSS Level 1 certified SDK |
| **Update Cadence** | Quarterly dependency review recommended |

---

### Deployment Security

| Model | Security Controls |
|-------|-------------------|
| **Self-Hosted** | Full control over infrastructure, network, and data |
| **Docker** | Multi-stage build, non-root user execution, health checks |
| **Cloud (Azure/AWS)** | Compatible with managed identity, Key Vault, VNet integration |
| **Database** | Customer-chosen provider with native encryption support |
| **No Outbound Data** | Assessment data stays within customer infrastructure (except optional OpenAI calls) |

---

### Security Controls Summary

| Category | Control | Status |
|----------|---------|--------|
| Authentication | Cookie-based with PBKDF2-SHA256 hashing | Implemented |
| Authorization | 4-role RBAC with tenant + domain scoping | Implemented |
| Encryption (Transit) | HTTPS with HSTS enforcement | Implemented |
| Encryption (At Rest) | DPAPI for secrets, parameterized DB access | Implemented |
| Session Management | 8-hour HttpOnly cookies with sliding expiration | Implemented |
| Input Validation | Data annotations + service-layer validation | Implemented |
| CSRF Protection | SameSite cookies, server-side rendering | Implemented |
| XSS Protection | HttpOnly cookies, Blazor auto-escaping | Implemented |
| SQL Injection | EF Core parameterized queries | Implemented |
| Secrets Management | DPAPI, User Secrets, Key Vault support | Implemented |
| Multi-Tenant Isolation | FK constraints, query filtering, credential isolation | Implemented |
| Audit Logging | Serilog structured logging, 30-day retention | Implemented |
| API Security | OAuth 2.0, read-only permissions, webhook validation | Implemented |
| Network Security | HTTPS redirect, HSTS, same-origin CORS | Implemented |
| Dependency Security | .NET 8 LTS, Microsoft-maintained packages | Implemented |

---

*Cloudativ Assessment Tool — Comprehensive M365 E5 Security Assessment & Compliance Platform*

*For more information, contact: info@cloudativ.com*
