# PRODUCT PAGE PROMPT — Cloudativ Assessment Tool

> **IMPORTANT INSTRUCTIONS FOR THE AI:**
> - ONLY use the information provided below. Do NOT invent, assume, or hallucinate any features, pricing, metrics, customer names, testimonials, or partnerships that are not explicitly listed.
> - If information is missing (e.g., pricing, customer logos), use placeholder text like "[Contact Sales for Pricing]" or "[Customer Logo Placeholder]" — do NOT fabricate data.
> - Do NOT claim certifications, awards, or partnerships unless explicitly stated below.
> - All feature descriptions must match the actual capabilities listed. Do not exaggerate or embellish.

---

## TASK

Create a modern, professional, single-page product/landing website for **Cloudativ Assessment** — a Microsoft 365 Security & Compliance Assessment Tool. The page should be responsive, visually polished, and conversion-focused. Use the brand colors and the exact feature set provided below.

---

## BRAND IDENTITY

- **Product Name:** Cloudativ Assessment
- **Company Name:** Cloudativ
- **Tagline Suggestion:** "Deep Visibility Into Your Microsoft 365 Security Posture"
- **Primary Color:** #E0FC8E (Lime Green)
- **Secondary Color:** #51627A (Slate Blue)
- **Accent Color:** #EF7348 (Coral Orange)
- **Light Blue:** #B6CBE0
- **Font Style:** Modern, clean, SaaS-style (e.g., Inter, Plus Jakarta Sans, or similar)
- **Tone:** Professional, enterprise-grade, security-focused, trusted advisor

---

## PAGE SECTIONS (in order)

### 1. HERO SECTION
- Headline: Emphasize M365 security assessment, compliance, and license optimization
- Sub-headline: Multi-tenant Microsoft 365 security assessment platform with AI-powered compliance analysis
- CTA buttons: "Request a Demo" and "Learn More"
- Background: Subtle gradient using brand colors or abstract security/cloud visuals
- Note: The tool is a web application (Blazor Server), NOT a desktop app

### 2. KEY VALUE PROPOSITIONS (3-4 cards)
Use ONLY these real capabilities:

**Card 1 — Comprehensive Security Assessment**
- Automated assessment across 9 security domains
- Domains (EXACT list, do not add or remove):
  1. Identity & Access Management
  2. Privileged Access Management
  3. Device & Endpoint Security
  4. Exchange & Email Security
  5. Microsoft Defender (XDR)
  6. Data Protection & Compliance (DLP)
  7. Audit & Logging
  8. Application Governance
  9. Collaboration Security (SharePoint, OneDrive, Teams)
- Scoring system with severity levels: Critical, High, Medium, Low, Informational

**Card 2 — Complete M365 Inventory**
- Full tenant inventory collection via Microsoft Graph API
- Inventory categories (EXACT list):
  - Users, Groups, Directory Roles, Conditional Access Policies
  - Service Principals, Managed Identities
  - Devices, Compliance Policies, Configuration Profiles
  - Microsoft Defender: Endpoint, Office 365, Identity, Cloud Apps
  - SharePoint Sites, Teams, SharePoint/Teams Settings
  - Enterprise Applications, OAuth Consents
  - License Subscriptions & Utilization (40+ license SKU categories)
  - Audit Log Settings, Secure Score
  - High-Risk Findings (auto-detected)

**Card 3 — AI-Powered Governance & Compliance**
- AI analysis powered by GPT-4o-mini (OpenAI)
- Maps assessment findings to compliance frameworks
- Supported compliance standards (EXACT list, do not add others):
  1. NCA CCC (Saudi National Cybersecurity Authority — Cloud Computing Controls)
  2. ISO 27001:2022
  3. PCI DSS v4.0
  4. HIPAA Security Rule
  5. NIST Cybersecurity Framework (CSF)
- Generates compliance gap analysis with remediation recommendations

**Card 4 — License Optimization**
- Multi-license utilization tracking across 40+ SKU categories
- License tier grouping: Enterprise (E1/E3/E5), Business (Basic/Standard/Premium), Frontline (F1/F3), Education (A1/A3/A5), Government (G1/G3/G5)
- Identifies underutilized licenses, estimated monthly waste
- Feature utilization matrix (MFA, Conditional Access, Defender adoption per license tier)
- Visual charts comparing purchased vs assigned vs available licenses

### 3. HOW IT WORKS (Step-by-step)
1. **Onboard Your Tenant** — Register your Microsoft 365 tenant with App Registration credentials (encrypted at rest with AES Data Protection)
2. **Run Inventory Collection** — Automated collection of your full M365 environment via Microsoft Graph API (background processing with Hangfire)
3. **Execute Security Assessment** — 9-domain automated security assessment with finding-level severity scoring
4. **AI Compliance Analysis** — Map findings against selected compliance standards (NCA CCC, ISO 27001, PCI DSS, HIPAA, NIST CSF)
5. **Review & Export Reports** — Interactive dashboards, exportable PDF/Excel/CSV reports, and progress tracking over time

### 4. FEATURES DEEP DIVE

#### Multi-Tenant Management
- Support for multiple Microsoft 365 tenants
- Per-tenant onboarding workflow
- Tenant-level dashboards with score trends over time
- Role-based access: SuperAdmin, TenantAdmin, Auditor, DomainAdmin

#### Security Domains Assessment (9 Domains)
For EACH of the 9 domains listed above:
- Individual domain assessment pages with detailed findings
- Severity-based finding categorization
- Compliant vs Non-Compliant control counts
- Domain-specific scoring

#### Inventory Dashboard
- Resource statistics: Users (total/enabled/guest/admin/risky), Groups (security/M365), Apps, Devices (managed/compliant), Policies (CA/DLP), Exchange mailboxes, SharePoint sites, Teams, Licenses
- Change detection between inventory snapshots
- High-risk findings auto-detection
- Per-entity detail pages with filtering and search

#### Reporting Center
Exact report types available (do not add others):
1. M365 Resources Report
2. Impacted Resources Report
3. Domain Assessment Report (per-domain or all domains)
4. Progress Comparison Report (date-range based)
5. Governance Compliance Report
6. Full Assessment Report

#### Export Capabilities
- **Formats:** PDF, Excel (XLSX), CSV, JSON, HTML
- **Exportable entities:** Users, Groups, Devices, Enterprise Apps, Directory Roles, Conditional Access Policies, Service Principals, Governance Reports, Full Assessment Reports
- **Filtering support:** Export with applied filters (search, user type, MFA status, risk level, license, OS, compliance status)

#### Assessment Comparison
- Compare multiple assessment runs over time
- Track progress and score improvements per domain

### 5. TECHNOLOGY & SECURITY SECTION
- **Platform:** .NET 8 Blazor Server Application
- **UI Framework:** MudBlazor (Material Design)
- **Database:** Supports SQLite, PostgreSQL, SQL Server
- **API Integration:** Microsoft Graph API (read-only permissions)
- **AI Engine:** OpenAI GPT-4o-mini for compliance analysis
- **Background Processing:** Hangfire job scheduler
- **Credential Security:** AES encryption for stored client secrets using ASP.NET Data Protection
- **Authentication:** Local Identity or Microsoft Entra ID (OIDC)
- **Logging:** Serilog with rolling daily log files (30-day retention)
- **Payment Integration:** Stripe (for subscription management)

#### Graph API Permissions Used (read-only)
- Organization.Read.All, Directory.Read.All
- User.Read.All, Group.Read.All
- RoleManagement.Read.Directory, Policy.Read.All
- Application.Read.All, IdentityRiskyUser.Read.All
- Sites.Read.All, Team.ReadBasic.All, TeamSettings.Read.All
- Channel.ReadBasic.All
- (All read-only — no write permissions required)

### 6. COMPLIANCE & LOCAL STANDARDS
- **Saudi NCA CCC:** First-class support for National Cybersecurity Authority Cloud Computing Controls
- **Regional Relevance:** Designed with GCC and Middle East compliance requirements in mind
- **ISO 27001:2022:** International information security standard mapping
- **PCI DSS v4.0:** Payment card industry compliance mapping
- **HIPAA:** Healthcare data protection compliance mapping
- **NIST CSF:** US cybersecurity framework mapping
- NOTE: Do NOT claim the tool itself is certified by any of these standards. It MAPS assessment findings TO these standards for gap analysis.

### 7. SUBSCRIPTION PLANS
- **Plans Available:** Trial, Monthly, Yearly
- **[Contact Sales for actual pricing — do NOT invent prices]**
- Subscription management via Stripe
- Per-tenant assessment limits based on plan

### 8. DASHBOARD PREVIEW SECTION
- Show a mockup/screenshot placeholder of the dashboard
- Mention key dashboard metrics:
  - Overall security score across tenants
  - Critical and high findings count
  - Score trends over time
  - Top-performing and lowest-performing tenants
  - Resource statistics (users, devices, apps, policies)
  - Domain-level score breakdown

### 9. CALL TO ACTION (Bottom)
- "Start Securing Your Microsoft 365 Environment Today"
- CTA: "Request a Demo" / "Contact Sales"
- Contact information placeholder: [Insert actual contact email/phone]

### 10. FOOTER
- Company: Cloudativ
- Links: Product, Features, Compliance, Contact
- Copyright notice
- [Social media links — placeholder only, do not invent URLs]

---

## DESIGN GUIDELINES

- Clean, modern SaaS landing page style
- Use brand colors consistently (#E0FC8E primary, #51627A secondary, #EF7348 accent)
- Dark header/hero with lime green accents
- White or light gray content sections
- Use icons for each security domain and feature
- Mobile-responsive design
- Subtle animations on scroll (optional)
- NO stock photos of generic "cybersecurity" imagery — prefer abstract geometric patterns, dashboard mockups, or icon-based illustrations
- RTL support consideration (for Arabic language version in the future)

---

## STRICT RULES FOR THE AI GENERATING THIS PAGE

1. **DO NOT** invent customer testimonials, case studies, or logos
2. **DO NOT** claim specific percentages (e.g., "reduces risk by 80%") unless provided
3. **DO NOT** add features not listed above (e.g., SIEM integration, SOC dashboard, threat hunting — these do NOT exist in the product)
4. **DO NOT** claim compliance certifications for the product itself — it maps TO standards, not certified BY them
5. **DO NOT** invent pricing numbers — use "[Contact Sales]" placeholders
6. **DO NOT** reference competitor products by name
7. **DO NOT** claim real-time monitoring or 24/7 SOC capabilities — the tool runs on-demand assessments and scheduled background jobs
8. **DO NOT** claim the tool modifies or remediates tenant configurations — it is READ-ONLY assessment and reporting
9. **USE** only the exact 9 security domains listed — no more, no less
10. **USE** only the exact 5 compliance standards listed — no more, no less
