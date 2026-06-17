using System;

namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Cache interface for compiled AST rules to avoid parsing overhead.
/// </summary>
public interface IFormulaCache
{
    /// <summary>
    /// Gets or compiles and adds a rule AST node to the cache.
    /// </summary>
    AstNode GetOrAdd(
        int companyId,
        int? ruleSetId,
        int fiscalYear,
        int componentId,
        int ruleVersion,
        string compiledHash,
        Func<AstNode> compileFunc);
}
