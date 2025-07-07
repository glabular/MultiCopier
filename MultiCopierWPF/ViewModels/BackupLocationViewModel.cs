using MultiCopierWPF.Models;
using MultiCopierWPF.ViewModels.Base;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
        set
        {
            if (Set(ref _status, value))
            {
                OnPropertyChanged(nameof(IsStatusOk));
            }
        }
    }

    public bool EncryptFiles
    {
        get => _encryptFiles;
        set => Set(ref _encryptFiles, value);
    }

    public bool IsStatusOk => Status == BackupStatus.OK;
}
