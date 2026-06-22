using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FijiPayroll.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "company");

            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "ApprovalConfigs",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalMatrices",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MinThreshold = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaxThreshold = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalMatrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflows",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CurrentApproverRole = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflows", x => x.Id);
                    table.UniqueConstraint("AK_ApprovalWorkflows_WorkflowId", x => x.WorkflowId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobs",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankFiles",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalEmployeesCount = table.Column<int>(type: "int", nullable: false),
                    FileContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankMasters",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalName = table.Column<string>(type: "nvarchar(250)", nullable: false),
                    SecurityIsolatorKey = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", nullable: false, defaultValue: "Fiji Standard Time"),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(10)", nullable: false, defaultValue: "FJD"),
                    TradingName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TIN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FnpfEmployerNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSetupComplete = table.Column<bool>(type: "bit", nullable: false),
                    SetupCompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArchivedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveReason = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyBankAccounts",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    BankMasterId = table.Column<int>(type: "int", nullable: false),
                    BankBranchId = table.Column<int>(type: "int", nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    EncryptedAccountNumber = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    AccountNumberHash = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    Last4Digits = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanySeedVersions",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SeedVersion = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    AppliedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    SeedCategory = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySeedVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanySetupAudits",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySetupAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanySetupStates",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    WizardVersion = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "1.0.0"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySetupStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceAmendments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalSubmissionId = table.Column<int>(type: "int", nullable: false),
                    PreviousSubmissionId = table.Column<int>(type: "int", nullable: false),
                    CurrentSubmissionId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceAmendments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceBatches",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CompliancePeriodId = table.Column<int>(type: "int", nullable: false),
                    BatchName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    DigitalSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateThumbprint = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceEvents",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    User = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Machine = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceJobs",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompliancePeriods",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompliancePeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceSnapshots",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplianceBatchId = table.Column<int>(type: "int", nullable: true),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    SnapshotVersion = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SHA256Hash = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CostCentre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Tin = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    FnpfNumber = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ResidencyStatus = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    IsFnpfExempt = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsTaxExempt = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmploymentType = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "Permanent"),
                    Branch = table.Column<string>(type: "nvarchar(100)", nullable: false, defaultValue: ""),
                    Position = table.Column<string>(type: "nvarchar(100)", nullable: false, defaultValue: ""),
                    Email = table.Column<string>(type: "nvarchar(255)", nullable: false, defaultValue: ""),
                    DataQualityScore = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityEvents",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportHistories",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Report = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    User = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Filter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    ExportType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    GeneratedByVersion = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileLayoutDefinitions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    LayoutType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    HeaderTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetailTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FooterTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnDelimiter = table.Column<string>(type: "char(1)", nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLayoutDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalCalendars",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CalendarType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    GeneratedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FnpfConfigurations",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EmployerRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EmployeeRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FnpfConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FNPFSubmissions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CompliancePeriodId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    FnpfFileContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CalculationEngineVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    FormulaEngineVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ComplianceEngineVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PinnedRuleVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FNPFSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FRCSSubmissions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CompliancePeriodId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    FrcsFileContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CalculationEngineVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    FormulaEngineVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ComplianceEngineVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PinnedRuleVersion = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FRCSSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportSessions",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    UploadedSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Started = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Validated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Approved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Committed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Archived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ImportSource = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ImportHash = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    RollbackSupported = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterLookups",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArchivedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchiveReason = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdjustments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedInPayrollRunId = table.Column<int>(type: "int", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancelledBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollComponents",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ComponentCode = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CalculationMethod = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CalculationValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Formula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystemComponent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsFnpfApplicable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "Active"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollComponents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollExceptionQueues",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    OperatorResolution = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollExceptionQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollFrequencyDefinitions",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FrequencyName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    FrequencyType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    FrequencyCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PayDay = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PeriodsPerYear = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollFrequencyDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollGroups",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    FilterCriteria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultBankAccountId = table.Column<int>(type: "int", nullable: true),
                    DefaultCalendarId = table.Column<int>(type: "int", nullable: true),
                    DefaultCostCentre = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DefaultLeaveRulesPackageId = table.Column<int>(type: "int", nullable: true),
                    ApprovalWorkflowId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLedgerReversals",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    OriginalLedgerId = table.Column<int>(type: "int", nullable: false),
                    ReversalLedgerId = table.Column<int>(type: "int", nullable: false),
                    ReversalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    User = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLedgerReversals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLedgers",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    TotalGross = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalPAYE = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalFNPFEmployee = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalFNPFEmployer = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalNetPay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPeriods",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PeriodCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PayrollFrequency = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    FiscalMonth = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunHistories",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    User = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Machine = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    RunCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PeriodName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshotHash = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: true),
                    PayrollGroupId = table.Column<int>(type: "int", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalMachine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalThumbprint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalCorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSnapshots",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    JsonPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleExecutionMetrics",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    AverageExecutionTime = table.Column<double>(type: "float", nullable: false),
                    MaximumExecutionTime = table.Column<double>(type: "float", nullable: false),
                    FailureCount = table.Column<long>(type: "bigint", nullable: false),
                    LastExecuted = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleExecutionMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleModules",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    ExecutionPriority = table.Column<int>(type: "int", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleSets",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ParentRuleSetId = table.Column<int>(type: "int", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleSets_RuleSets_ParentRuleSetId",
                        column: x => x.ParentRuleSetId,
                        principalSchema: "company",
                        principalTable: "RuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SearchIndexes",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeightedScore = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchIndexes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetupCheckpoints",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Step = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetupExecutionRecords",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupExecutionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatutoryRules",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Authority = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    RuleValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutoryRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxBrackets",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxVersion = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ResidencyStatus = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    LowerLimit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UpperLimit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FixedTaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxBrackets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepLogs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ToState = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    TransitionedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TransitionedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepLogs_ApprovalWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalSchema: "audit",
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankBranches",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BankMasterId = table.Column<int>(type: "int", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    BsbCode = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankBranches_BankMasters_BankMasterId",
                        column: x => x.BankMasterId,
                        principalSchema: "company",
                        principalTable: "BankMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySetupTasks",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CompanySetupStateId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "1.0.0"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySetupTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySetupTasks_CompanySetupStates_CompanySetupStateId",
                        column: x => x.CompanySetupStateId,
                        principalSchema: "company",
                        principalTable: "CompanySetupStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePaymentMethods",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    BankSortCode = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePaymentMethods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "company",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FiscalCalendarId = table.Column<int>(type: "int", nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    PeriodName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalPeriods_FiscalCalendars_FiscalCalendarId",
                        column: x => x.FiscalCalendarId,
                        principalSchema: "company",
                        principalTable: "FiscalCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportSessionRows",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportSessionId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationStatus = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Errors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Warnings = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSessionRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportSessionRows_ImportSessions_ImportSessionId",
                        column: x => x.ImportSessionId,
                        principalSchema: "audit",
                        principalTable: "ImportSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollComponentDependencies",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentComponentId = table.Column<int>(type: "int", nullable: false),
                    ChildComponentId = table.Column<int>(type: "int", nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CalculationOrder = table.Column<int>(type: "int", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollComponentDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollComponentDependencies_PayrollComponents_ChildComponentId",
                        column: x => x.ChildComponentId,
                        principalSchema: "company",
                        principalTable: "PayrollComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollComponentDependencies_PayrollComponents_ParentComponentId",
                        column: x => x.ParentComponentId,
                        principalSchema: "company",
                        principalTable: "PayrollComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollComponentVersions",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollComponentId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    VersionHash = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    ExpressionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculationMethod = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Taxable = table.Column<bool>(type: "bit", nullable: false),
                    SubjectToFNPF = table.Column<bool>(type: "bit", nullable: false),
                    Recurring = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedFromPayrollRunId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollComponentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollComponentVersions_PayrollComponents_PayrollComponentId",
                        column: x => x.PayrollComponentId,
                        principalSchema: "company",
                        principalTable: "PayrollComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayPeriodSchedules",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PayrollFrequencyDefinitionId = table.Column<int>(type: "int", nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CutoffDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPeriodSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayPeriodSchedules_PayrollFrequencyDefinitions_PayrollFrequencyDefinitionId",
                        column: x => x.PayrollFrequencyDefinitionId,
                        principalSchema: "company",
                        principalTable: "PayrollFrequencyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLedgerEmployees",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PayrollLedgerId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    EmployeeTin = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    EmployeeFnpfNumber = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Gross = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PAYE = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FNPFEmployee = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FNPFEmployer = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLedgerEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollLedgerEmployees_PayrollLedgers_PayrollLedgerId",
                        column: x => x.PayrollLedgerId,
                        principalSchema: "payroll",
                        principalTable: "PayrollLedgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLedgerTransactions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PayrollLedgerId = table.Column<int>(type: "int", nullable: false),
                    PayrollLedgerComponentId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLedgerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollLedgerTransactions_PayrollLedgers_PayrollLedgerId",
                        column: x => x.PayrollLedgerId,
                        principalSchema: "payroll",
                        principalTable: "PayrollLedgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunEmployees",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Tin = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    FnpfNumber = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ResidencyStatus = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAllowances = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PayeTax = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FnpfEmployeeContribution = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FnpfEmployerContribution = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxVersionUsed = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    IsSuperseded = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CalculationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunEmployees_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollRunEmployees_PayrollRuns_PayrollRunId1",
                        column: x => x.PayrollRunId1,
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunStateHistories",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    PayrollRunId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunStateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunStateHistories_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollRunStateHistories_PayrollRuns_PayrollRunId1",
                        column: x => x.PayrollRunId1,
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayrollComponentRules",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComponentId = table.Column<int>(type: "int", nullable: false),
                    RuleModuleId = table.Column<int>(type: "int", nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ExpressionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompiledHash = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    CompiledVersion = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RuleVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollComponentRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollComponentRules_PayrollComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalSchema: "company",
                        principalTable: "PayrollComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollComponentRules_RuleModules_RuleModuleId",
                        column: x => x.RuleModuleId,
                        principalSchema: "company",
                        principalTable: "RuleModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLedgerComponents",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollLedgerEmployeeId = table.Column<int>(type: "int", nullable: false),
                    PayrollLedgerEmployeeId1 = table.Column<int>(type: "int", nullable: false),
                    ComponentCode = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLedgerComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollLedgerComponents_PayrollLedgerEmployees_PayrollLedgerEmployeeId",
                        column: x => x.PayrollLedgerEmployeeId,
                        principalSchema: "payroll",
                        principalTable: "PayrollLedgerEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollLedgerComponents_PayrollLedgerEmployees_PayrollLedgerEmployeeId1",
                        column: x => x.PayrollLedgerEmployeeId1,
                        principalSchema: "payroll",
                        principalTable: "PayrollLedgerEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunEmployeeTraces",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunEmployeeId = table.Column<int>(type: "int", nullable: false),
                    TraceText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunEmployeeTraces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunEmployeeTraces_PayrollRunEmployees_PayrollRunEmployeeId",
                        column: x => x.PayrollRunEmployeeId,
                        principalSchema: "payroll",
                        principalTable: "PayrollRunEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunLineItems",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ComponentId = table.Column<int>(type: "int", nullable: false),
                    ComponentCode = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    AffectsFnpf = table.Column<bool>(type: "bit", nullable: false),
                    EmployerContributionFlag = table.Column<bool>(type: "bit", nullable: false),
                    ReferenceComponentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunLineItems_PayrollRunEmployees_PayrollRunEmployeeId",
                        column: x => x.PayrollRunEmployeeId,
                        principalSchema: "payroll",
                        principalTable: "PayrollRunEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_WorkflowId",
                schema: "audit",
                table: "ApprovalWorkflows",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_CompanyId_Status",
                schema: "payroll",
                table: "BackgroundJobs",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BankBranches_BankMasterId",
                schema: "company",
                table: "BankBranches",
                column: "BankMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBankAccounts_CompanyId_AccountNumberHash",
                schema: "company",
                table: "CompanyBankAccounts",
                columns: new[] { "CompanyId", "AccountNumberHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanySetupStates_CompanyId",
                schema: "company",
                table: "CompanySetupStates",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanySetupTasks_CompanySetupStateId",
                schema: "company",
                table: "CompanySetupTasks",
                column: "CompanySetupStateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAmendments_CurrentSubmissionId",
                schema: "payroll",
                table: "ComplianceAmendments",
                column: "CurrentSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAmendments_OriginalSubmissionId",
                schema: "payroll",
                table: "ComplianceAmendments",
                column: "OriginalSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAmendments_PreviousSubmissionId",
                schema: "payroll",
                table: "ComplianceAmendments",
                column: "PreviousSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceEvents_CorrelationId",
                schema: "payroll",
                table: "ComplianceEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSnapshots_ComplianceBatchId",
                schema: "payroll",
                table: "ComplianceSnapshots",
                column: "ComplianceBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePaymentMethods_EmployeeId",
                schema: "company",
                table: "EmployeePaymentMethods",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_FnpfNumber",
                schema: "company",
                table: "Employees",
                columns: new[] { "CompanyId", "FnpfNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_Tin",
                schema: "company",
                table: "Employees",
                columns: new[] { "CompanyId", "Tin" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_FiscalCalendarId",
                schema: "company",
                table: "FiscalPeriods",
                column: "FiscalCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_JobId",
                schema: "audit",
                table: "ImportJobs",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportSessionRows_ImportSessionId_RowNumber",
                schema: "audit",
                table: "ImportSessionRows",
                columns: new[] { "ImportSessionId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportSessions_ImportHash",
                schema: "audit",
                table: "ImportSessions",
                column: "ImportHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportSessions_SessionId",
                schema: "audit",
                table: "ImportSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterLookups_CompanyId_Category_Code",
                schema: "company",
                table: "MasterLookups",
                columns: new[] { "CompanyId", "Category", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_PayPeriodSchedules_PayrollFrequencyDefinitionId",
                schema: "company",
                table: "PayPeriodSchedules",
                column: "PayrollFrequencyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_CompanyId_EmployeeId_IsApplied",
                schema: "payroll",
                table: "PayrollAdjustments",
                columns: new[] { "CompanyId", "EmployeeId", "IsApplied" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponentDependencies_ChildComponentId",
                schema: "company",
                table: "PayrollComponentDependencies",
                column: "ChildComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponentDependencies_ParentComponentId_ChildComponentId",
                schema: "company",
                table: "PayrollComponentDependencies",
                columns: new[] { "ParentComponentId", "ChildComponentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponentRules_ComponentId_EffectiveFrom",
                schema: "company",
                table: "PayrollComponentRules",
                columns: new[] { "ComponentId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponentRules_RuleModuleId",
                schema: "company",
                table: "PayrollComponentRules",
                column: "RuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponents_CompanyId",
                schema: "company",
                table: "PayrollComponents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponents_CompanyId_DisplayOrder",
                schema: "company",
                table: "PayrollComponents",
                columns: new[] { "CompanyId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_PayrollComponents_CompanyId_ComponentCode",
                schema: "company",
                table: "PayrollComponents",
                columns: new[] { "CompanyId", "ComponentCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponentVersions_PayrollComponentId_VersionNumber",
                schema: "company",
                table: "PayrollComponentVersions",
                columns: new[] { "PayrollComponentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollExceptionQueues_CompanyId_PayrollRunId_IsResolved",
                schema: "payroll",
                table: "PayrollExceptionQueues",
                columns: new[] { "CompanyId", "PayrollRunId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerComponents_PayrollLedgerEmployeeId",
                schema: "payroll",
                table: "PayrollLedgerComponents",
                column: "PayrollLedgerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerComponents_PayrollLedgerEmployeeId1",
                schema: "payroll",
                table: "PayrollLedgerComponents",
                column: "PayrollLedgerEmployeeId1");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerEmployees_CompanyId_PayrollLedgerId",
                schema: "payroll",
                table: "PayrollLedgerEmployees",
                columns: new[] { "CompanyId", "PayrollLedgerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerEmployees_EmployeeId",
                schema: "payroll",
                table: "PayrollLedgerEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerEmployees_PayrollLedgerId",
                schema: "payroll",
                table: "PayrollLedgerEmployees",
                column: "PayrollLedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerReversals_CompanyId_OriginalLedgerId",
                schema: "payroll",
                table: "PayrollLedgerReversals",
                columns: new[] { "CompanyId", "OriginalLedgerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerReversals_ReversalLedgerId",
                schema: "payroll",
                table: "PayrollLedgerReversals",
                column: "ReversalLedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgers_PayrollRunId",
                schema: "payroll",
                table: "PayrollLedgers",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerTransactions_CompanyId_PayrollLedgerId",
                schema: "payroll",
                table: "PayrollLedgerTransactions",
                columns: new[] { "CompanyId", "PayrollLedgerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLedgerTransactions_PayrollLedgerId",
                schema: "payroll",
                table: "PayrollLedgerTransactions",
                column: "PayrollLedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_CompanyId_PayrollFrequency_Status",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "CompanyId", "PayrollFrequency", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunEmployees_EmployeeId",
                schema: "payroll",
                table: "PayrollRunEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunEmployees_PayrollRunId",
                schema: "payroll",
                table: "PayrollRunEmployees",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunEmployees_PayrollRunId1",
                schema: "payroll",
                table: "PayrollRunEmployees",
                column: "PayrollRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunEmployeeTraces_PayrollRunEmployeeId",
                schema: "payroll",
                table: "PayrollRunEmployeeTraces",
                column: "PayrollRunEmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunHistories_CompanyId_PayrollRunId",
                schema: "payroll",
                table: "PayrollRunHistories",
                columns: new[] { "CompanyId", "PayrollRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineItems_ComponentId",
                schema: "payroll",
                table: "PayrollRunLineItems",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineItems_PayrollRunEmployeeId",
                schema: "payroll",
                table: "PayrollRunLineItems",
                column: "PayrollRunEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunStateHistories_PayrollRunId",
                schema: "payroll",
                table: "PayrollRunStateHistories",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunStateHistories_PayrollRunId1",
                schema: "payroll",
                table: "PayrollRunStateHistories",
                column: "PayrollRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSnapshots_CompanyId_PayrollRunId_Version",
                schema: "payroll",
                table: "PayrollSnapshots",
                columns: new[] { "CompanyId", "PayrollRunId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionMetrics_RuleId",
                schema: "audit",
                table: "RuleExecutionMetrics",
                column: "RuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleModules_Code",
                schema: "company",
                table: "RuleModules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleSets_ParentRuleSetId",
                schema: "company",
                table: "RuleSets",
                column: "ParentRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexes_CompanyId_EntityType_EntityId",
                schema: "audit",
                table: "SearchIndexes",
                columns: new[] { "CompanyId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexes_SearchId",
                schema: "audit",
                table: "SearchIndexes",
                column: "SearchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SetupExecutionRecords_CompanyId_ExecutionId",
                schema: "company",
                table: "SetupExecutionRecords",
                columns: new[] { "CompanyId", "ExecutionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatutoryRules_Authority_RuleCode",
                schema: "payroll",
                table: "StatutoryRules",
                columns: new[] { "Authority", "RuleCode" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepLogs_LogId",
                schema: "audit",
                table: "WorkflowStepLogs",
                column: "LogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepLogs_WorkflowId",
                schema: "audit",
                table: "WorkflowStepLogs",
                column: "WorkflowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalConfigs",
                schema: "company");

            migrationBuilder.DropTable(
                name: "ApprovalMatrices",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "BackgroundJobs",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "BankBranches",
                schema: "company");

            migrationBuilder.DropTable(
                name: "BankFiles",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "company");

            migrationBuilder.DropTable(
                name: "CompanyBankAccounts",
                schema: "company");

            migrationBuilder.DropTable(
                name: "CompanySeedVersions",
                schema: "company");

            migrationBuilder.DropTable(
                name: "CompanySetupAudits",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "CompanySetupTasks",
                schema: "company");

            migrationBuilder.DropTable(
                name: "ComplianceAmendments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "ComplianceBatches",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "ComplianceEvents",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "ComplianceJobs",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "CompliancePeriods",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "ComplianceSnapshots",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "EmployeePaymentMethods",
                schema: "company");

            migrationBuilder.DropTable(
                name: "EntityEvents",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ExportHistories",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "FileLayoutDefinitions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "FiscalPeriods",
                schema: "company");

            migrationBuilder.DropTable(
                name: "FnpfConfigurations",
                schema: "company");

            migrationBuilder.DropTable(
                name: "FNPFSubmissions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "FRCSSubmissions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "ImportJobs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ImportSessionRows",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "MasterLookups",
                schema: "company");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayPeriodSchedules",
                schema: "company");

            migrationBuilder.DropTable(
                name: "PayrollAdjustments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollComponentDependencies",
                schema: "company");

            migrationBuilder.DropTable(
                name: "PayrollComponentRules",
                schema: "company");

            migrationBuilder.DropTable(
                name: "PayrollComponentVersions",
                schema: "company");

            migrationBuilder.DropTable(
                name: "PayrollExceptionQueues",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollGroups",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollLedgerComponents",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollLedgerReversals",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollLedgerTransactions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollPeriods",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRunEmployeeTraces",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRunHistories",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRunLineItems",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRunStateHistories",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollSnapshots",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "RuleExecutionMetrics",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "RuleSets",
                schema: "company");

            migrationBuilder.DropTable(
                name: "SearchIndexes",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "SetupCheckpoints",
                schema: "company");

            migrationBuilder.DropTable(
                name: "SetupExecutionRecords",
                schema: "company");

            migrationBuilder.DropTable(
                name: "StatutoryRules",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "TaxBrackets",
                schema: "company");

            migrationBuilder.DropTable(
                name: "WorkflowStepLogs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "BankMasters",
                schema: "company");

            migrationBuilder.DropTable(
                name: "CompanySetupStates",
                schema: "company");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "company");

            migrationBuilder.DropTable(
                name: "FiscalCalendars",
                schema: "company");

            migrationBuilder.DropTable(
                name: "ImportSessions",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "PayrollFrequencyDefinitions",
                schema: "company");

            migrationBuilder.DropTable(
                name: "RuleModules",
                schema: "company");

            migrationBuilder.DropTable(
                name: "PayrollComponents",
                schema: "company");

            migrationBuilder.DropTable(
                name: "PayrollLedgerEmployees",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRunEmployees",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflows",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "PayrollLedgers",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRuns",
                schema: "payroll");
        }
    }
}
