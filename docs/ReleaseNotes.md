# Fiji Enterprise Payroll System — Release Notes

**Product:** Fiji Enterprise Payroll System  
**Publisher:** [Your Company Name]  
**Support:** support@fijienterprispayroll.fj  

---

## Version 1.0.0 — Initial Release

**Release Date:** TBD (Target: Q4 2026)  
**Status:** In Development  

### Features Included in v1.0.0

#### Phase 01 — Enterprise Architecture
- ✅ Clean Architecture foundation
- ✅ CQRS + MediatR implementation
- ✅ Serilog structured logging
- ✅ FluentValidation pipeline
- ✅ Audit trail infrastructure

#### Phase 02 — Database
- ✅ SQL Server schema (all modules)
- ✅ EF Core migrations
- ✅ Seed data (roles, permissions, tax tables, banks)
- ✅ Performance indexes

#### Phase 03 — Installer
- ✅ 10-step setup wizard
- ✅ Database creation automation
- ✅ Seed data setup
- ✅ Rollback and retry support

#### Phase 04 — Authentication & Licensing
- ✅ SQL and Windows authentication
- ✅ Role-based access control
- ✅ RSA-signed offline licensing
- ✅ License expiry reminders

#### Phase 05 — Core Configuration
- ✅ Company management
- ✅ Fiscal calendar
- ✅ Payroll frequencies
- ✅ Departments, branches, positions
- ✅ Payroll components
- ✅ Leave types

#### Phase 06 — Employee Management
- ✅ Full employee lifecycle
- ✅ Personal, employment, payroll details
- ✅ Bank accounts, emergency contacts
- ✅ Documents and notes
- ✅ Transfer and promotion tracking
- ✅ Termination workflow

#### Phase 07 — Payroll Engine
- ✅ Weekly, Fortnightly, Bi-Monthly, Monthly payroll
- ✅ Salary, Hourly, Daily pay types
- ✅ PAYE calculation (FRCS tax tables)
- ✅ FNPF calculation
- ✅ Loan deductions
- ✅ Overtime processing
- ✅ Payroll approval workflow

#### Phase 08 — Import/Export
- ✅ Excel and CSV import for all modules
- ✅ Template download
- ✅ Preview and validate before commit
- ✅ Error reports

#### Phase 09 — SSRS Reporting
- ✅ Payroll Register
- ✅ Payroll Summary
- ✅ Employee Listing
- ✅ Leave Summary and Liability
- ✅ Loan Summary
- ✅ Department Summary
- ✅ Bank Summary
- ✅ Audit Trail Report
- ✅ Variance Report

#### Phase 10 — FRCS / FNPF / Bank Files
- ✅ FRCS Monthly Employer Return (PDF + CSV)
- ✅ FNPF Contribution File (CSV)
- ✅ Bank Direct Credit Files (BSP, ANZ, Westpac, HFC, Bred, Kontiki)

#### Phase 11 — Dashboard & Analytics
- ✅ Executive dashboard
- ✅ Payroll cost trends
- ✅ Headcount analytics
- ✅ Leave liability chart

#### Phase 12 — Audit Trail & Notifications
- ✅ Full audit trail viewer
- ✅ Login history
- ✅ In-app notifications
- ✅ License expiry alerts

#### Phase 13 — Backup & Deployment
- ✅ Automated backup scheduler
- ✅ Manual backup/restore
- ✅ Deployment guide

---

## Version History

| Version | Date | Type | Summary |
|---------|------|------|---------|
| 1.0.0 | TBD | GA Release | Initial production release |
| 0.9.0 | TBD | Release Candidate | Feature complete, bug fixes |
| 0.5.0 | TBD | Beta | Core modules functional |
| 0.1.0 | TBD | Alpha | Architecture and auth |

---

## Known Limitations — v1.0.0

1. AI Assistant integration scheduled for v1.1.0
2. Mobile companion app not included in v1.0.0
3. Multi-currency support planned for v1.2.0

---

## System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| OS (Client) | Windows 10 64-bit | Windows 11 |
| OS (Server) | Windows Server 2016 | Windows Server 2022 |
| SQL Server | SQL Server 2019 Express | SQL Server 2019 Standard+ |
| RAM (Client) | 4 GB | 8 GB |
| RAM (Server) | 8 GB | 16 GB |
| Disk (App) | 500 MB | 1 GB |
| Disk (Data) | 10 GB | 100 GB |
| .NET Runtime | .NET 8 Desktop Runtime | .NET 8 Desktop Runtime |
| Display | 1024×768 | 1920×1080 |

---

*Maintained by: Development Team*  
*Last updated: June 2026*
