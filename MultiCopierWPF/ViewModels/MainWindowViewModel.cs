using MultiCopierWPF.Infrastructure.Commands;
using MultiCopierWPF.Models;
using MultiCopierWPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MultiCopierWPF.ViewModels;

internal class MainWindowViewModel : ViewModel
{
    #region Fields
    private string? _title = "MultiCopier v0.1.0";
    private int _maxBackups = 10;
    private ObservableCollection<BackupLocationViewModel> _backupLocations = [];
    #endregion
        
    public MainWindowViewModel()
    {
        AddBackupCommand = new RelayCommand(OnAddBackupExecuted, CanAddBackupExecute);

        // Add 3 initial backups
        for (int i = 0; i < 3; i++)
        {
            BackupLocations.Add(new BackupLocationViewModel { Status = (BackupStatus)new Random().Next(3) });
        }

        RemoveBackupCommand = new RelayCommand(OnRemoveBackupExecuted, CanRemoveBackupExecute);


        XXXCommand = new RelayCommand(OnXXXCommandExecuted, CanXXXCommandExecute);
    }

    #region Properties

    /// <summary>
    /// Window title.
    /// </summary>
	public string? Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    public ObservableCollection<BackupLocationViewModel> BackupLocations
    {
        get => _backupLocations;
        set => Set(ref _backupLocations, value);
    }

    #endregion

    #region Commands
    public ICommand XXXCommand { get; }

    public ICommand AddBackupCommand { get; }

    public ICommand RemoveBackupCommand { get; }


    #endregion


    private void OnAddBackupExecuted(object? obj)
    {
        BackupLocations.Add(new BackupLocationViewModel());

        CommandManager.InvalidateRequerySuggested(); // Forces WPF to recheck CanExecute
    }

    private bool CanAddBackupExecute(object? obj) => BackupLocations.Count < _maxBackups;


    private void OnRemoveBackupExecuted(object? param)
    {
        if (param is BackupLocationViewModel backup)
        { 
            BackupLocations.Remove(backup);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool CanRemoveBackupExecute(object? param) => param is BackupLocationViewModel;


    private void OnXXXCommandExecuted(object? p)
    {
        Application.Current.Shutdown();
    }

    private bool CanXXXCommandExecute(object? p) => true;
}
