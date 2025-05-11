namespace MultiCopierWPF.Shared;

public class Settings
{
    public string? MasterFolder { get; set; }

    public List<BackupLocationSetting> BackupFolders { get; set; } = [];
}
