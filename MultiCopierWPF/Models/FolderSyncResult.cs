namespace MultiCopierWPF.Models;

public class FolderSyncResult
{
    public List<BackupEntry> NewFiles { get; init; } = [];

    public List<BackupEntry> ModifiedFiles { get; init; } = [];

    public List<BackupEntry> DeletedFiles { get; init; } = [];
}

