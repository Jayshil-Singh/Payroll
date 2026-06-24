using FijiPayroll.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Provides storage capabilities writing files directly to local disk folders
/// or secure network UNC shares, completely isolated from the database tables.
/// </summary>
public sealed class FileStorageProvider : IFileStorageProvider
{
    private readonly string _baseDirectory;
    private readonly ILogger<FileStorageProvider> _logger;

    /// <summary>Initializes the storage provider, setting up base directories from configuration.</summary>
    public FileStorageProvider(ILogger<FileStorageProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        string? rootDir = configuration["FileStorage:RootDirectory"];
        if (!string.IsNullOrEmpty(rootDir))
        {
            _baseDirectory = rootDir;
        }
        else
        {
            _baseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Fiji Payroll", "Exports");
        }

        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(string storedFileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        string filePath = Path.Combine(_baseDirectory, storedFileName);
        
        // Ensure parent folder structure exists
        string? directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await fileStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation("[FileStorage] File successfully saved: {Path}", filePath);
        return filePath;
    }

    /// <inheritdoc />
    public Task<Stream> GetFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException("The requested file does not exist in storage.", storagePath);
        }

        // Return a read-only stream opened asynchronously
        Stream stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            try
            {
                File.Delete(storagePath);
                _logger.LogInformation("[FileStorage] File deleted: {Path}", storagePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FileStorage] Failed to delete file: {Path}", storagePath);
            }
        }
        return Task.CompletedTask;
    }
}
