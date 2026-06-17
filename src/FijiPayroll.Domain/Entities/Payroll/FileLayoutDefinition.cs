using System;
using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model storing custom string formatting templates for bank files, FNPF columns, and FRCS exports.
/// Allows layouts to be customized in the database without recompiling the main binary.
/// </summary>
public sealed class FileLayoutDefinition : AuditableEntity
{
    /// <summary>Gets the bank identifier code or authority name (e.g., "BSP", "ANZ", "FRCS").</summary>
    public string OwnerCode { get; private set; } = string.Empty;

    /// <summary>Gets the layout formatting category key.</summary>
    public string LayoutType { get; private set; } = string.Empty;

    /// <summary>Gets the string template used for the header record.</summary>
    public string HeaderTemplate { get; private set; } = string.Empty;

    /// <summary>Gets the string template used for repeating detail records.</summary>
    public string DetailTemplate { get; private set; } = string.Empty;

    /// <summary>Gets the string template used for the footer record.</summary>
    public string FooterTemplate { get; private set; } = string.Empty;

    /// <summary>Gets the column separator character (e.g. ',' or '|').</summary>
    public char ColumnDelimiter { get; private set; } = ',';

    /// <summary>Gets the target file extension suffix (e.g., "txt", "csv", "dat").</summary>
    public string FileExtension { get; private set; } = "csv";

    private FileLayoutDefinition() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new FileLayoutDefinition.
    /// </summary>
    public static FileLayoutDefinition Create(
        string ownerCode,
        string layoutType,
        string headerTemplate,
        string detailTemplate,
        string footerTemplate,
        char columnDelimiter = ',',
        string fileExtension = "csv")
    {
        if (string.IsNullOrWhiteSpace(ownerCode)) throw new ArgumentException("Owner code cannot be empty.", nameof(ownerCode));
        if (string.IsNullOrWhiteSpace(layoutType)) throw new ArgumentException("Layout type cannot be empty.", nameof(layoutType));

        return new FileLayoutDefinition
        {
            OwnerCode = ownerCode,
            LayoutType = layoutType,
            HeaderTemplate = headerTemplate,
            DetailTemplate = detailTemplate,
            FooterTemplate = footerTemplate,
            ColumnDelimiter = columnDelimiter,
            FileExtension = fileExtension
        };
    }
}
