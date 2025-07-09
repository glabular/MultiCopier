using MultiCopierWPF.Models;

namespace MultiCopierWPF.Shared;

public class BackupLocationSetting
{
    public string Path { get; set; } = string.Empty;

    public bool EncryptFiles { get; set; } = false;

    public BackupStatus Status { get; set; } = BackupStatus.Unknown;
}
