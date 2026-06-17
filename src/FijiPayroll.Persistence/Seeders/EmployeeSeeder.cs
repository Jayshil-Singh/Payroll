using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Seeders;

/// <summary>
/// Idempotent database seeder for employees.
/// </summary>
public sealed class EmployeeSeeder
{
    private readonly ApplicationDbContext _context;

    public EmployeeSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Seeds default employees if none exist.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Employees.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        var employees = new List<Employee>
        {
            Employee.Create(
                companyId: 1,
                fullName: "John Doe",
                tin: "998877665",
                fnpfNumber: "12345-X",
                residencyStatus: "Resident",
                department: "Finance",
                baseSalary: 1500.00m, // Fortnightly rate
                frequency: PayrollFrequencyType.Fortnightly,
                isFnpfExempt: false,
                isTaxExempt: false,
                isActive: true
            ),
            Employee.Create(
                companyId: 1,
                fullName: "Jane Smith",
                tin: "112233445",
                fnpfNumber: "67890-Z",
                residencyStatus: "Resident",
                department: "Engineering",
                baseSalary: 5000.00m, // Monthly rate
                frequency: PayrollFrequencyType.Monthly,
                isFnpfExempt: false,
                isTaxExempt: false,
                isActive: true
            ),
            Employee.Create(
                companyId: 1,
                fullName: "Devon Contractor",
                tin: "554433221",
                fnpfNumber: "",
                residencyStatus: "NonResident",
                department: "Consulting",
                baseSalary: 6000.00m, // Monthly rate
                frequency: PayrollFrequencyType.Monthly,
                isFnpfExempt: true,
                isTaxExempt: false,
                isActive: true
            ),
            Employee.Create(
                companyId: 1,
                fullName: "Low Income Earner",
                tin: "777888999",
                fnpfNumber: "99999-E",
                residencyStatus: "Resident",
                department: "Logistics",
                baseSalary: 800.00m, // Fortnightly rate -> Annualised is 20,800 (under 30k threshold -> $0 PAYE)
                frequency: PayrollFrequencyType.Fortnightly,
                isFnpfExempt: false,
                isTaxExempt: false,
                isActive: true
            )
        };

        foreach (var emp in employees)
        {
            emp.CreatedBy = "system-seeder";
            emp.CreatedAt = DateTime.UtcNow;
        }

        await _context.Employees.AddRangeAsync(employees, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
