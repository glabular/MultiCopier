using MultiCopierWPF.Interfaces;
using MultiCopierWPF.Shared;

namespace MultiCopierWPF.Services;

/// <summary>
/// Mock backup service implementation for design-time data and UI testing.
/// </summary>
public class DesignTimeBackupService : IBackupService, ISettingsService
{
    public Task AlignMasterWithDatabaseAsync(string masterFolder)
    {
        // No-op or return completed task
        return Task.CompletedTask;
    }

    public Settings Load()
    {
        throw new NotImplementedException();
    }

    // Implement interface methods with mock/dummy data
    public Task RunBackupAsync(string masterFolder, string backupFolder)
    {
        // No-op or return completed task
        return Task.CompletedTask;
    }

    public void Save(Settings settings)
    {
        throw new NotImplementedException();
    }
}
