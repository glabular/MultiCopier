using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiCopierWPF.Data;
using MultiCopierWPF.Exceptions;
using MultiCopierWPF.Interfaces;
using MultiCopierWPF.Models;
using MultiCopierWPF.Utilities;
using System.IO;

namespace MultiCopierWPF.Services;

public class BackupService : IBackupService
{
    private readonly BackupDbContext _context;
    private readonly IFolderSyncService _folderSyncService;
    private readonly IHashCalculator _hashCalculator;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        BackupDbContext context,
        IFolderSyncService folderSyncService,
        IHashCalculator hashCalculator,
        ILogger<BackupService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _folderSyncService = folderSyncService ?? throw new ArgumentNullException(nameof(folderSyncService));
        _hashCalculator = hashCalculator ?? throw new ArgumentNullException(nameof(hashCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunBackupAsync(string masterFolder, string backupFolder, bool encrypt)
    {
        Guard.AgainstInvalidPath(masterFolder, nameof(masterFolder));
        Guard.AgainstInvalidPath(backupFolder, nameof(backupFolder));

        var masterDirInfo = new DirectoryInfo(masterFolder);
        var backupDirInfo = new DirectoryInfo(backupFolder);

        // Abort the backup if the target backup location doesn't have enough free space.
        FileSystemHelper.EnsureEnoughDiskSpace(masterDirInfo, backupFolder);

        var context = new SyncContext();

        await Task.Run(() =>
        {
            _folderSyncService.Mirror(masterDirInfo, backupDirInfo, context);
        });

        await EnsureFolderCountsMatch(masterFolder, backupFolder);
    }

    /// <summary>
    /// Ensures that the database mirrors the master folder.
    /// </summary>
    public async Task AlignMasterWithDatabaseAsync(string masterFolder)
    {
        // The state of files in the master folder.
        var masterFolderContents = await DirectoryScanner.GetMasterFolderFilesAsync(masterFolder);

        var scannedPaths = new HashSet<string>(masterFolderContents.Select(f => f.FullPath));

        // Load existing DB entries into dictionary for quick lookup
        var entries = await LoadDbEntriesAsync(); // Dictionary<string fullPath, BackupEntry>

        var deleted = new List<BackupEntry>();
        var updated = new List<BackupEntry>();
        var added = new List<BackupEntry>();

        await Task.Run(() =>
        {
            var scannedPaths = new HashSet<string>(masterFolderContents.Select(f => f.FullPath));

            // Detect deleted files (in DB but not in master folder)
            foreach (var dbEntry in entries)
            {
                if (!scannedPaths.Contains(dbEntry.Key))
                {
                    deleted.Add(dbEntry.Value);
                }
            }

            // Detect new & modified files
            foreach (var scannedFile in masterFolderContents)
            {
                if (!entries.TryGetValue(scannedFile.FullPath, out var dbEntry))
                {
                    // New file - add to DB
                    var sha = _hashCalculator.ComputeHash(scannedFile.FullPath);

                    var entry = new BackupEntry(scannedFile.FileName, scannedFile.FullPath, sha, scannedFile.FileSize)
                    {
                        LastModified = scannedFile.LastModified
                    };

                    added.Add(entry);
                }
                else if (scannedFile.FileSize != dbEntry.FileSize ||
                     scannedFile.LastModified != dbEntry.LastModified)
                {
                    // Likely modified — calculate SHA to confirm
                    var sha = _hashCalculator.ComputeHash(scannedFile.FullPath);

                    if (sha != dbEntry.SHA512)
                    {
                        dbEntry.SHA512 = sha;
                        dbEntry.FileSize = scannedFile.FileSize;
                        dbEntry.LastModified = scannedFile.LastModified;
                        updated.Add(dbEntry);
                    }
                }
            }
        });

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Remove deleted entries
        _context.BackupEntries.RemoveRange(deleted);

        // Add new files
        await _context.BackupEntries.AddRangeAsync(added);

        // Update modified files
        _context.BackupEntries.UpdateRange(updated);

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        // Commit transaction and flush WAL changes to the main database file
        await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");

        // Run VACUUM only if deleted count is large enough (e.g., 1000 or more)
        if (deleted.Count >= 1000)
        {
            await _context.Database.ExecuteSqlRawAsync("VACUUM;");
        }
    }

    private async Task<Dictionary<string, BackupEntry>> LoadDbEntriesAsync()
    {
        var entries = await _context.BackupEntries.ToListAsync();
        return entries.ToDictionary(e => e.OriginalFilePath);
    }

    private static async Task EnsureFolderCountsMatch(string masterFolder, string backupFolder)
    {
        var result = await FolderComparer.CompareCountAsync(masterFolder, backupFolder);

        if (result.HasMismatch)
        {
            throw new FolderMismatchException(backupFolder, result.FileMismatchCount, result.DirectoryMismatchCount);
        }
    }      
}