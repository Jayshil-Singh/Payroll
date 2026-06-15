using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FijiPayroll.Persistence.Seeders;

/// <summary>
/// Handles database seeding for standard Fiji payroll components.
/// Executes only when the database is first created or during migration bootstrapping.
/// </summary>
public sealed class PayrollComponentSeeder
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initialises a new instance of the <see cref="PayrollComponentSeeder"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PayrollComponentSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Seeds the standard payroll components if none exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if already seeded to ensure idempotency
        if (await _context.PayrollComponents.AnyAsync(cancellationToken))
        {
            return;
        }

        var components = new List<PayrollComponent>
        {
            PayrollComponent.Create(
                companyId: 1,
                componentCode: "BASIC",
                componentName: "Basic Salary",
                componentType: ComponentType.Earning,
                calculationMethod: CalculationMethod.Manual,
                calculationValue: null,
                formula: null,
                isTaxable: true,
                isFnpfApplicable: true,
                displayOrder: 10,
                description: "Base contract earnings",
                isSystemComponent: false),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "PAYE",
                componentName: "PAYE Tax Deduction",
                componentType: ComponentType.Statutory,
                calculationMethod: CalculationMethod.Manual,
                calculationValue: null,
                formula: null,
                isTaxable: false,
                isFnpfApplicable: false,
                displayOrder: 90,
                description: "Fiji Revenue and Customs Service PAYE Tax",
                isSystemComponent: true),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "FNPF_EE",
                componentName: "FNPF Employee Contribution",
                componentType: ComponentType.Statutory,
                calculationMethod: CalculationMethod.Percentage,
                calculationValue: 8.00m,
                formula: null,
                isTaxable: false,
                isFnpfApplicable: false,
                displayOrder: 91,
                description: "FNPF Employee Contribution (8%)",
                isSystemComponent: true),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "FNPF_ER",
                componentName: "FNPF Employer Contribution",
                componentType: ComponentType.Statutory,
                calculationMethod: CalculationMethod.Percentage,
                calculationValue: 10.00m,
                formula: null,
                isTaxable: false,
                isFnpfApplicable: false,
                displayOrder: 92,
                description: "FNPF Employer Contribution (10%)",
                isSystemComponent: true),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "OVERTIME",
                componentName: "Overtime Pay (1.5x)",
                componentType: ComponentType.Earning,
                calculationMethod: CalculationMethod.Formula,
                calculationValue: null,
                formula: "{HourlyRate} * {OvertimeHours} * 1.5",
                isTaxable: true,
                isFnpfApplicable: true,
                displayOrder: 20,
                description: "Standard Overtime pay multiplier",
                isSystemComponent: false),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "BONUS",
                componentName: "Bonus Pay",
                componentType: ComponentType.Earning,
                calculationMethod: CalculationMethod.Manual,
                calculationValue: null,
                formula: null,
                isTaxable: true,
                isFnpfApplicable: true,
                displayOrder: 30,
                description: "Performance or discretionary bonus",
                isSystemComponent: false),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "HALLOWANCE",
                componentName: "Housing Allowance",
                componentType: ComponentType.Allowance,
                calculationMethod: CalculationMethod.Fixed,
                calculationValue: 200.00m,
                formula: null,
                isTaxable: false,
                isFnpfApplicable: true,
                displayOrder: 40,
                description: "Housing support allowance",
                isSystemComponent: false),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "TALLOWANCE",
                componentName: "Transport Allowance",
                componentType: ComponentType.Allowance,
                calculationMethod: CalculationMethod.Fixed,
                calculationValue: 100.00m,
                formula: null,
                isTaxable: false,
                isFnpfApplicable: true,
                displayOrder: 50,
                description: "Travel support allowance",
                isSystemComponent: false),

            PayrollComponent.Create(
                companyId: 1,
                componentCode: "MALLOWANCE",
                componentName: "Meal Allowance",
                componentType: ComponentType.Allowance,
                calculationMethod: CalculationMethod.Fixed,
                calculationValue: 50.00m,
                formula: null,
                isTaxable: false,
                isFnpfApplicable: true,
                displayOrder: 60,
                description: "Meal support allowance",
                isSystemComponent: false)
        };

        foreach (var component in components)
        {
            component.CreatedBy = "System";
            component.CreatedAt = DateTime.UtcNow;
            await _context.PayrollComponents.AddAsync(component, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
