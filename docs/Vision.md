# Fiji Enterprise Payroll System — Vision Document

**Version:** 1.0.0  
**Date:** June 2026  
**Status:** Approved  
**Owner:** Enterprise Solution Architect  

---

## 1. Executive Summary

The Fiji Enterprise Payroll System (FEPS) is a modern, on-premise commercial payroll application designed specifically for Fiji businesses of all sizes. It provides a robust, scalable, and compliant payroll management solution that integrates with Fiji Revenue and Customs Service (FRCS) reporting requirements, Fiji National Provident Fund (FNPF) obligations, and local banking institutions.

---

## 2. Problem Statement

Fiji businesses currently rely on:
- Manual spreadsheet-based payroll calculations
- Generic international payroll systems not tailored to Fiji tax law
- Outdated legacy systems lacking modern UI/UX
- Systems without FRCS or FNPF integration
- Systems without offline capability

FEPS addresses all of these gaps with a purpose-built, enterprise-grade solution.

---

## 3. Vision Statement

> *To be the definitive payroll management platform for Fiji businesses — accurate, compliant, scalable, and trusted by payroll professionals across every industry.*

---

## 4. Goals

| # | Goal | Success Metric |
|---|------|---------------|
| 1 | 100% FRCS Compliance | Zero FRCS filing errors |
| 2 | 100% FNPF Compliance | Zero FNPF filing errors |
| 3 | Sub-second payroll calculations | < 1 second per employee |
| 4 | Unlimited scalability | Support 10,000+ employees |
| 5 | Offline operation | Full functionality without internet |
| 6 | Audit completeness | Every action logged |
| 7 | Role-based security | Zero unauthorized access |
| 8 | Multi-company support | Unlimited companies per license |

---

## 5. Target Users

| User Role | Description |
|-----------|-------------|
| Payroll Administrator | Day-to-day payroll processing |
| HR Manager | Employee management and reporting |
| Finance Manager | Reporting and financial oversight |
| System Administrator | Configuration, users, and maintenance |
| Company Director | Executive dashboard and approvals |
| Auditor | Read-only audit trail access |

---

## 6. Key Features

### Core Payroll
- Weekly, Fortnightly, Bi-Monthly, Monthly payroll frequencies
- Salary, Hourly, Daily, and Overtime employee types
- Full payroll component engine (earnings, deductions, allowances)
- PAYE tax calculation per FRCS tax tables
- FNPF employer and employee contribution management
- Loan management and deductions
- Leave loading and liability calculations

### Compliance
- FRCS Monthly Employer Return (MER) generation
- FNPF contribution file generation
- Bank payment file generation (BSP, ANZ, Westpac, HFC)

### Enterprise Features
- Unlimited companies and employees
- Multi-branch and multi-department support
- Multi-user concurrent access
- Role-based access control (RBAC)
- Full audit trail
- SSRS reporting engine
- Import/Export framework

### Security
- SQL Server Authentication
- Windows Authentication
- Offline RSA-signed licensing
- Data encryption at rest and in transit

---

## 7. Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Performance | Payroll run < 30 seconds for 500 employees |
| Availability | 99.9% uptime during business hours |
| Scalability | Horizontal scaling via SQL Server AlwaysOn |
| Security | AES-256 encryption, RSA-2048 licensing |
| Usability | Onboarding < 2 hours for payroll officers |
| Maintainability | < 4 hours for patch deployment |
| Portability | Windows Server 2016+ and Windows 10/11 |

---

## 8. Technology Decisions

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| UI Framework | WPF (.NET 8) | Rich desktop UI, enterprise-grade |
| Architecture | Clean Architecture | Separation of concerns, testability |
| Database | SQL Server 2019+ | Reliability, SSRS integration |
| Reporting | SSRS | Industry standard, SQL Server native |
| Patterns | CQRS + MediatR | Scalable command/query separation |
| DI Container | Microsoft.Extensions.DI | Native .NET, lightweight |
| Logging | Serilog | Structured logging, flexible sinks |
| Validation | FluentValidation | Expressive, testable rules |
| ORM | Entity Framework Core 8 | Code-first migrations, LINQ |
| Licensing | RSA + SHA256 | Offline, tamper-proof |

---

## 9. Deployment Model

FEPS is deployed as an **on-premise** application:
- Application server: Windows Server 2016+
- Database server: SQL Server 2019+
- Client: Windows 10/11 workstations
- Network: LAN only (no internet required)
- Licensing: Offline file-based activation

---

## 10. Roadmap

| Release | Version | Features |
|---------|---------|---------|
| Alpha | 0.1.0 | Core architecture, authentication, basic configuration |
| Beta | 0.5.0 | Employee management, payroll engine, basic reports |
| RC | 0.9.0 | FRCS/FNPF modules, bank files, full reporting |
| GA | 1.0.0 | Full feature set, testing complete, documentation done |
| v1.1 | 1.1.0 | AI Assistant integration, enhanced analytics |

---

*Document maintained by: Enterprise Solution Architect*  
*Last updated: June 2026*
