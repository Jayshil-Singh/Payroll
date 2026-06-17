using System;

namespace FijiPayroll.Infrastructure.Services.BankGenerators;

/// <summary>
/// Bank South Pacific (BSP) payment clearing file generator.
/// </summary>
public sealed class BSPBankGenerator : BaseBankGenerator
{
    /// <inheritdoc/>
    public override string BankCode => "BSP";

    /// <inheritdoc/>
    public override string BankName => "Bank South Pacific";
}

/// <summary>
/// ANZ Bank payment clearing file generator.
/// </summary>
public sealed class ANZBankGenerator : BaseBankGenerator
{
    /// <inheritdoc/>
    public override string BankCode => "ANZ";

    /// <inheritdoc/>
    public override string BankName => "ANZ Bank";
}

/// <summary>
/// Westpac Fiji payment clearing file generator.
/// </summary>
public sealed class WestpacBankGenerator : BaseBankGenerator
{
    /// <inheritdoc/>
    public override string BankCode => "WBC";

    /// <inheritdoc/>
    public override string BankName => "Westpac Fiji";
}

/// <summary>
/// BRED Bank payment clearing file generator.
/// </summary>
public sealed class BREDBankGenerator : BaseBankGenerator
{
    /// <inheritdoc/>
    public override string BankCode => "BRED";

    /// <inheritdoc/>
    public override string BankName => "BRED Bank";
}

/// <summary>
/// HFC Bank payment clearing file generator.
/// </summary>
public sealed class HFCBankGenerator : BaseBankGenerator
{
    /// <inheritdoc/>
    public override string BankCode => "HFC";

    /// <inheritdoc/>
    public override string BankName => "HFC Bank";
}

/// <summary>
/// Kontiki Finance payment clearing file generator.
/// </summary>
public sealed class KontikiBankGenerator : BaseBankGenerator
{
    /// <inheritdoc/>
    public override string BankCode => "KNTK";

    /// <inheritdoc/>
    public override string BankName => "Kontiki Finance";
}
