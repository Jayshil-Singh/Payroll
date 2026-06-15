using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Contract for storing and retrieving files from local folders, UNC network shares, or cloud storage.
/// </summary>
public interface IFileStorageProvider
{
    /// <summary>Saves a file stream to storage and returns the resulting storage path/URL.</summary>
    Task<string> SaveFileAsync(string storedFileName, Stream fileStream, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a read stream of a file from storage.</summary>
    Task<Stream> GetFileAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file from storage.</summary>
    Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
}
