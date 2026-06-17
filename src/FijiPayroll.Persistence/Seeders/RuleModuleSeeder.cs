using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FijiPayroll.Persistence.Seeders;

/// <summary>
/// Seeds standard rule modules like PAYROLL, LEAVE, etc.
/// </summary>
public sealed class RuleModuleSeeder
{
    private readonly ApplicationDbContext _context;

    public RuleModuleSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.RuleModules.AnyAsync(cancellationToken))
        {
            return;
        }

        var modules = new List<RuleModule>
        {
            new("PAYROLL", "Payroll Module", "Handles general payroll and salary components", 10, true),
            new("LEAVE", "Leave Module", "Handles leave accrual and deductions", 20, true),
            new("OVERTIME", "Overtime Module", "Handles overtime multipliers and configurations", 30, true),
            new("LOANS", "Loans Module", "Handles loan repayments and interest rates", 40, true),
            new("FNPF", "FNPF Statutory Module", "Fiji National Provident Fund calculations", 50, true),
            new("FRCS", "FRCS Tax Module", "Fiji Revenue and Customs Service tax rules", 60, true),
            new("BANKFILE", "Bank File Generation", "Handles generation of bank transfer files", 70, true),
            new("HR", "HR Core Module", "Core HR data rules", 80, true),
            new("ESS", "Employee Self Service", "ESS access and validation rules", 90, true),
        };

        foreach (var m in modules)
        {
            await _context.RuleModules.AddAsync(m, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
