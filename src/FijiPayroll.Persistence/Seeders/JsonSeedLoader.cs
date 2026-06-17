using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Seeders;

/// <summary>
/// Implements <see cref="IJsonSeedLoader"/> to seed company reference data from local Seeds JSON templates.
/// </summary>
public sealed class JsonSeedLoader : IJsonSeedLoader
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initialises a new instance of the <see cref="JsonSeedLoader"/> class.
    /// </summary>
    public JsonSeedLoader(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task SeedBanksAsync(int companyId, CancellationToken cancellationToken = default)
    {
        // Check if already seeded
        bool alreadySeeded = await _context.CompanySeedVersions
            .AnyAsync(x => x.CompanyId == companyId && x.SeedCategory == SeedCategory.Banks, cancellationToken);

        if (alreadySeeded) return;

        string filePath = GetSeedFilePath("Banks.json");
        string json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var models = JsonSerializer.Deserialize<List<BankSeedModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (models is null) return;

        var seedVersion = CompanySeedVersion.Create(
            companyId,
            "1.0.0",
            "Initial banks and branches reference data",
            SeedCategory.Banks
        );

        foreach (var model in models)
        {
            var master = BankMaster.Create(companyId, model.BankCode, model.BankName, model.IsActive);
            master.CreatedBy = "system-seeder";
            master.CreatedAt = DateTime.UtcNow;

            await _context.BankMasters.AddAsync(master, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); // Save to generate Master Id

            foreach (var br in model.Branches)
            {
                var branch = BankBranch.Create(companyId, master.Id, br.BranchCode, br.BranchName, br.BsbCode, br.IsActive);
                branch.CreatedBy = "system-seeder";
                branch.CreatedAt = DateTime.UtcNow;
                await _context.BankBranches.AddAsync(branch, cancellationToken);
            }
        }

        seedVersion.CreatedBy = "system-seeder";
        seedVersion.CreatedAt = DateTime.UtcNow;
        await _context.CompanySeedVersions.AddAsync(seedVersion, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string GetSeedFilePath(string fileName)
    {
        // Try direct BaseDirectory/Seeds/
        string path1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Seeds", fileName);
        if (File.Exists(path1)) return path1;

        // Try direct BaseDirectory/
        string path2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        if (File.Exists(path2)) return path2;

        // Try walking up to find src/FijiPayroll.Persistence/Seeds/
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string prospective = Path.Combine(dir, "src", "FijiPayroll.Persistence", "Seeds", fileName);
            if (File.Exists(prospective)) return prospective;

            prospective = Path.Combine(dir, "Seeds", fileName);
            if (File.Exists(prospective)) return prospective;

            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent == dir) break;
            dir = parent ?? string.Empty;
        }

        throw new FileNotFoundException($"SEED_ERROR: Seed data file '{fileName}' could not be located in application search paths.");
    }

    private sealed class BankSeedModel
    {
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<BranchSeedModel> Branches { get; set; } = [];
    }

    private sealed class BranchSeedModel
    {
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string BsbCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
