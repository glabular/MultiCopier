namespace MultiCopierWPF.Interfaces;

public interface IBackupService
{
    Task RunBackupAsync(string masterFolder, string backupFolder);

    Task AlignMasterWithDatabaseAsync(string masterFolder);
}