# AcxiomCRM — Enterprise Sales & CRM Platform

AcxiomCRM is a role-based Customer Relationship Management web application built with **ASP.NET Core 8 MVC**, **Entity Framework Core**, **ASP.NET Core Identity**, and **Bootstrap 5**. It covers the complete customer-sales lifecycle from lead capture through follow-up and opportunity management, with strict role-based authorization, client & server-side validation, business rules enforcement, audit logging, interactive Chart.js analytics, and a secured REST API with Swagger documentation.

---

## 🚀 Quick Start

The application dev server is already running and accessible at:
- **Web Application**: [http://localhost:5222](http://localhost:5222)
- **REST API Swagger Documentation**: [http://localhost:5222/swagger](http://localhost:5222/swagger)

### Demo Credentials

| Role | Email / Username | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@12345` | Full application administration, users/roles, audit logs, all CRM records and reports. |
| **Manager** | `manager@acxiomcrm.com` | `Manager@12345` | Team pipeline/customers/leads/opportunities/follow-ups, management reports, audit log viewer. |
| **Sales Executive** | `sales1@acxiomcrm.com` | `Sales@12345` | Own assigned customers, leads, opportunities, follow-ups, and sales dashboard. |
| **Sales Executive 2** | `sales2@acxiomcrm.com` | `Sales@12345` | Own assigned customers, leads, opportunities, follow-ups, and sales dashboard. |

*(A 1-click **Quick Fill** button is provided on the Login page for convenience)*

---

## 🏛️ Application Architecture

```
Users / Browser / Mobile API Client
                  ↓
          Presentation Layer
(Razor MVC Views, Bootstrap 5, Chart.js, Swagger UI)
                  ↓
          Application Layer
(Controllers: Customers, Leads, Opportunities, FollowUps, Activities, Reports, Users, AuditLogs, REST APIs)
                  ↓
        Domain / Business Layer
(CrmService, AuditService, Business Rule Validators, Lead Conversion Workflow)
                  ↓
          Data Access Layer
(ApplicationDbContext, EF Core 8 Repositories, Indexes, Cascades)
                  ↓
          Database Layer
(Microsoft SQL Server 2022 / Express / LocalDB / SQLite supported)
```

---

## 💾 Database Architecture & Data Storage

### How Data Is Stored
The application uses **Microsoft SQL Server** (`localhost\SQLEXPRESS`, LocalDB, or Azure SQL) as its enterprise database engine.
* **Auto-Provisioning & Seeding**: When anyone clones this repository and executes `dotnet run`, Entity Framework Core automatically creates the **`AcxiomCRM`** database, provisions all 13 tables, foreign keys, and indexes, and automatically populates the seed data via [DbInitializer.cs](file:///c:/Users/_MSI_/OneDrive/Desktop/acxiom%20crm/Data/DbInitializer.cs).
* **Pre-Seeded Data Includes**:
  * **System Users & Roles**: Admin (`admin@acxiomcrm.com`), Manager (`manager@acxiomcrm.com`), Sales Executives (`sales1@acxiomcrm.com`, `sales2@acxiomcrm.com`).
  * **Sample Customers**: Complete customer profiles (Apex Global, Starlight Retailers, Nexus Finance).
  * **Inbound Leads**: Leads across various stages (`Qualified`, `Contacted`, `New`).
  * **Opportunity Pipeline**: Opportunities totaling over $330,000 across multiple pipeline stages.
  * **Follow-ups & Activities**: Scheduled meetings, calls, and completed interactions.
  * **Audit Trail**: Security & data audit logs recording all system actions.

### Standalone SQL Script
For database administrators or evaluators who want to inspect or run the SQL schema directly in **SQL Server Management Studio (SSMS)**:
* Open and execute [`AcxiomCRM_Database_Setup.sql`](file:///c:/Users/_MSI_/OneDrive/Desktop/acxiom%20crm/AcxiomCRM_Database_Setup.sql).

### Connection String Configuration
Configured in [`appsettings.json`](file:///c:/Users/_MSI_/OneDrive/Desktop/acxiom%20crm/appsettings.json):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=AcxiomCRM;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```
*(If you are running on LocalDB instead of SQLEXPRESS, change `Server=localhost\\SQLEXPRESS` to `Server=(localdb)\\mssqllocaldb`).*

---

## 📋 Implemented Modules & Features

### 1. Authentication & Security (Sections 4.2, 6, 17.4, 17.9)
- **Framework Managed Passwords**: Uses ASP.NET Core Identity; no plain-text passwords stored; password hashes never exposed.
- **Password Policy**: Minimum 8 characters requiring uppercase, lowercase, digit, and special symbol.
- **Account Lockout**: Locks account for 15 minutes after 5 consecutive failed login attempts; audited upon trigger.
- **Anti-Forgery Protection**: State-changing POST actions guarded with `@Html.AntiForgeryToken()` and `[ValidateAntiForgeryToken]`.
- **Role Scoping**: Enforced on server-side queries (not just by hiding UI links).

### 2. Dashboard & Analytics (Sections 4.3, 17.11, 17.12)
- **KPI Cards**:
  - Total Customers
  - Total Leads (with Open count)
  - Open Opportunities (with Total count)
  - Total Pipeline Value
  - Weighted Pipeline Value ($Amount \times Probability / 100$)
  - Won Opportunities & Lost Opportunities count
  - Pending Follow-Ups & Overdue alerts
- **Interactive Chart.js Visualizations**:
  - **Lead Status Breakdown**: Doughnut chart (`New`, `Contacted`, `Qualified`, `Lost`, `Converted`).
  - **Pipeline by Stage**: Bar chart displaying deal value across stages (`Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`).
  - **Monthly Sales**: Line chart displaying Won deal value over the past 6 months.
- **Date Filters**: Filter metrics by `Today`, `This Week`, `This Month`, or `All Time`.

### 3. Customer Management (Sections 4.4, 17.5)
- Customer master data with auto-generated code (`CUST-100X`).
- Contact details, status (`Active`, `Inactive`, `Prospect`), company, address, city, state, assigned sales executive.
- **Validation**: Email uniqueness, phone uniqueness, duplicate customer detection (Name + Company).
- **360° Profile**: View linked opportunities, scheduled follow-ups, logged activities, and record-level audit trail.

### 4. Lead Management & Conversion (Sections 4.5, 8, 17.6)
- Inbound lead capture with auto-generated code (`LEAD-100X`).
- Status workflow: `New` → `Contacted` → `Qualified` → `Converted` / `Lost`.
- **Lead-to-Customer Conversion Workflow**: 1-click conversion promoting a qualified lead into a new Customer profile and optional linked Opportunity, with automated status updating and audit logging.

### 5. Opportunity Management (Sections 4.8, 5.3, 17.7)
- Deal pipeline tracking across stages: `Qualification` (20%), `Proposal` (50%), `Negotiation` (80%), `Won` (100%), `Lost` (0%).
- **Business Validations**:
  - Amount must be greater than 0 ($Amount > 0$).
  - Probability must be between 0 and 100 inclusive ($0 \le Prob \le 100$).
  - Expected Close Date cannot be in the past for active deals ($CloseDate \ge Today$).
- Derives weighted pipeline automatically: $Amount \times Probability / 100$.

### 6. Follow-Up Management (Sections 4.6, 5.3, 17.7)
- Schedule `Call`, `Meeting`, `Email`, or `Task` against customers, leads, or opportunities.
- **Business Validation**: Follow-up date cannot be earlier than today for new or planned follow-ups.
- **Overdue Detection**: Highlights overdue planned follow-ups automatically.
- **Quick Complete**: One-click completion that automatically converts the follow-up into a completed `Activity` history record!

### 7. Activities Management (Sections 17.2, 17.3)
- Real-time logging of phone calls, meetings, client emails, and tasks.
- Filters by Activity Type, Status, and assigned user.

### 8. User & Role Management (Admin Only - Sections 4.7, 7, 17.10)
- View user accounts, lockout states, and failed login counts.
- Assign primary roles (`Admin`, `Manager`, `SalesExecutive`).
- 1-click Account Unlock for locked-out users.
- Activate / Deactivate user accounts.
- Administrative password reset enforcing password complexity.

### 9. Audit Logging (Sections 4.9, 17.16)
- Append-only immutable log capturing `Login`, `Failed Login`, `Logout`, `Lockout`, `Unlock`, `Create`, `Update`, `Delete`, `Convert`, and `Role Change`.
- Records `UserId`, `UserName`, `Module`, `Action`, `RecordId`, `OldValue`, `NewValue`, `Timestamp`, `Result`, `Details`, and `IpAddress`.
- Comprehensive filter bar by user, module, action, and date range.

### 10. Reports & Intelligence (Sections 4.11, 11)
- **Pipeline Report**: Stage-wise and owner-wise counts, total value, and weighted value.
- **Sales & Conversion Report**: Lead conversion rate and opportunity win/loss metrics.
- **Team Productivity Report**: Activity counts, follow-ups handled, and pipeline managed per user.
- **CSV Export**: Export customer master lists, leads, and opportunities directly into CSV format.

### 11. REST API Specification (Sections 4.10, 10, 17.14)
- Exposes secure endpoints using REST conventions and DTOs:
  - `POST /api/auth/login` — Public, rate-limited JWT issuance
  - `POST /api/auth/logout` — Authenticated session termination
  - `GET /api/customers` — List/search customers
  - `GET /api/customers/{id}` — Get customer details
  - `POST /api/customers` — Create customer
  - `PUT /api/customers/{id}` — Update customer
  - `DELETE /api/customers/{id}` — Delete customer
  - `GET /api/leads` — List/search leads
  - `POST /api/leads` — Create lead
  - `GET /api/opportunities` — List opportunities
  - `POST /api/opportunities` — Create opportunity
  - `GET /api/followups` — List follow-ups
  - `POST /api/followups` — Create follow-up
  - `GET /api/reports/pipeline` — Stage-wise and weighted pipeline data
- Interactive Swagger UI: [http://localhost:5222/swagger](http://localhost:5222/swagger)

---

## 🧪 Automated Acceptance Test Suite

Two automated test suites are included to verify all criteria:
1. `test_acceptance.ps1`:
   - Anonymous access protection (302 redirect to Login)
   - REST API JWT login
   - Customer query
   - Business rule rejections (Amount <= 0, Probability > 100, Past close date, Past follow-up date, Duplicate customer)
   - Pipeline report API calculation
2. `test_advanced_workflows.ps1`:
   - Role-based scoping comparison (Admin sees all 4 opportunities; Sales Executive 2 sees only assigned records)
   - Account lockout after 5 consecutive failed login attempts (HTTP 423)
   - Swagger / OpenAPI endpoint status (200 OK)

To run the tests:
```powershell
powershell -ExecutionPolicy Bypass -File .\test_acceptance.ps1
powershell -ExecutionPolicy Bypass -File .\test_advanced_workflows.ps1
```
