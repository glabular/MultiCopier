namespace MultiCopierWPF.Interfaces;

public interface IBackupService
{
    Task RunBackupAsync(string masterFolder, string backupFolder, bool encrypt);

    Task AlignMasterWithDatabaseAsync(string masterFolder);
}