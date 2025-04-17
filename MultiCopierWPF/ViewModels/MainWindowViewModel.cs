using MultiCopierWPF.Infrastructure.Commands;
using MultiCopierWPF.Models;
using MultiCopierWPF.Services;
using MultiCopierWPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MultiCopierWPF.ViewModels;

internal class MainWindowViewModel : ViewModel
{
    #region Fields
    private string? _title = "MultiCopier v0.1.0";
    private string? _masterLocation = @"D:\temp\Root";
    private int _maxBackups = 10;
    private ObservableCollection<BackupLocationViewModel> _backupLocations = [];
    private readonly BackupService _backupService = new();
    #endregion

    public MainWindowViewModel()
    {
        AddBackupCommand = new RelayCommand(OnAddBackupExecuted, CanAddBackupExecute);

        BackupCommand = new RelayCommand(OnBackupExecuted, CanBackupExecute);

        // Add 3 initial backups
        for (int i = 1; i < 3 + 1; i++)
        {
            //BackupLocations.Add(new BackupLocationViewModel { Status = (BackupStatus)new Random().Next(3) });
            BackupLocations.Add(new BackupLocationViewModel { Status = BackupStatus.OK, Path = $@"D:\temp\b{i}" });
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

    public string? MasterFolder
    {
        get => _masterLocation;
        set => Set(ref _masterLocation, value);
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

    public ICommand BackupCommand { get; }

    public ICommand RemoveBackupCommand { get; }


    #endregion


    private async void OnBackupExecuted(object? obj)
    {
        if (IsMasterFolderInvalid())
        {
            MessageBox.Show("Master folder is not set or does not exist.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var backupPaths = BackupLocations
            .Select(loc => loc.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

            await BackupService.RunBackupAsync(MasterFolder!, backupPaths);

            MessageBox.Show("Backup completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Validates whether the master folder is set and exists on disk.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the master folder path is not null, not whitespace, and points to an existing directory;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool IsMasterFolderInvalid()
    {
        return string.IsNullOrWhiteSpace(MasterFolder) && !Directory.Exists(MasterFolder);
    }

    /// <summary>
    /// Determines whether the backup operation can be executed.
    /// </summary>
    /// <returns>
    /// <c>true</c> if there is at least one backup location and all backup locations have the <see cref="BackupStatus.OK"/> status;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool CanBackupExecute(object? arg)
    {
        if (BackupLocations.Count == 0)
        {
            return false;
        }

        return BackupLocations.All(b => b.Status == BackupStatus.OK);
    }

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
