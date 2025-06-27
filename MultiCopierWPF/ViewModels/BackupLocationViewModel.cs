using MultiCopierWPF.Models;
using MultiCopierWPF.ViewModels.Base;

namespace MultiCopierWPF.ViewModels;

public class BackupLocationViewModel : ViewModel
{
    private string? _path;
    private BackupStatus _status;
    private bool _encryptFiles;


    public string? Path
    {
        get => _path;
        set => Set(ref _path, value);
    }

    public BackupStatus Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public bool EncryptFiles
    {
        get => _encryptFiles;
        set => Set(ref _encryptFiles, value);
    }
}
