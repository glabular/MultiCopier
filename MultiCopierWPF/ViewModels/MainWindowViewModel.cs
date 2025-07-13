using MultiCopierWPF.Exceptions;
using MultiCopierWPF.Infrastructure.Commands;
using MultiCopierWPF.Interfaces;
using MultiCopierWPF.Models;
using MultiCopierWPF.Services;
using MultiCopierWPF.Shared;
using MultiCopierWPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace MultiCopierWPF.ViewModels;

public class MainWindowViewModel : ViewModel
{
    #region Fields
    private readonly Settings _settings;

    private string? _masterFolder;

    private const int _maxBackups = 10;

    private bool _isBackupInProgress;

    private readonly IBackupService _backupService;

    private readonly ISettingsService _settingsManager;

    private ObservableCollection<BackupLocationViewModel> _backupLocations = [];
    #endregion

    // Parameterless constructor for design-time
    public MainWindowViewModel() : this(new DesignTimeServices(), new DesignTimeServices())
    {
    }

    public MainWindowViewModel(IBackupService backupService, ISettingsService settingsManager)
    {
        _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        _settings = settingsManager.Load();

        AddBackupCommand = new RelayCommand(OnAddBackupExecuted, CanAddBackupExecute);
        BackupCommand = new RelayCommand(OnBackupExecuted, CanBackupExecute);
        ShallowCheckCommand = new RelayCommand(OnShallowCheckExecuted, CanShallowCheckExecute);
        RemoveBackupCommand = new RelayCommand(OnRemoveBackupExecuted, CanRemoveBackupExecute);
        SetMasterFolderCommand = new RelayCommand(OnSetMasterFolderExecuted);
        OpenBackupCommand = new RelayCommand(OnOpenBackupExecuted, CanOpenBackupExecute);

        InitializeLocations();

        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    #region Properties

    /// <summary>
    /// Window title.
    /// </summary>
    public static string? Title => "MultiCopier v0.2.1 [Stable]";

    public string? MasterFolder
    {
        get => _masterFolder;
        set
        {
            if (Set(ref _masterFolder, value))
            {
                OnPropertyChanged(nameof(MasterFolderButtonText)); // Button label depends on MasterFolder, so I notify its change too
            }
        }
    }

    public string MasterFolderButtonText => string.IsNullOrWhiteSpace(MasterFolder) ? "Set Folder" : "Edit";

    public ObservableCollection<BackupLocationViewModel> BackupLocations
    {
        get => _backupLocations;
        set => Set(ref _backupLocations, value);
    }

    public bool IsBackupInProgress
    {
        get => _isBackupInProgress;
        set => Set(ref _isBackupInProgress, value);
    }

    #endregion

    #region Commands

    public ICommand AddBackupCommand { get; }

    public ICommand BackupCommand { get; }

    public ICommand RemoveBackupCommand { get; }

    public ICommand ShallowCheckCommand { get; }

    public ICommand SetMasterFolderCommand { get; }

    public ICommand OpenBackupCommand { get; }

    #endregion

    private async void OnBackupExecuted(object? obj)
    {
        IsBackupInProgress = true;

        try
        {
            #region Folders validation 
            if (IsFolderInvalid(MasterFolder))
            {
                MessageBox.Show("Master folder is not set or does not exist.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate backup locations
            if (BackupLocations.Any(loc => IsFolderInvalid(loc.Path)))
            {
                MessageBox.Show("One or more backup locations are not set or do not exist.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!PathCollisionValidator.TryValidate(MasterFolder!, BackupLocations.Select(b => b.Path!), out var errorMessage))
            {
                MessageBox.Show(errorMessage, "Path Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            #endregion

            foreach (var location in BackupLocations)
            {
                location.Status = BackupStatus.Processing;
            }

            // Adds a short delay so the "OK" status doesn't appear too quickly, giving the user time to notice the update.
            await Task.Delay(350);

            //await _backupService.AlignMasterWithDatabaseAsync(MasterFolder!);            

            foreach (var location in BackupLocations)
            {
                try
                {
                    await _backupService.RunBackupAsync(MasterFolder!, location.Path!, location.EncryptFiles);

                    location.Status = BackupStatus.OK;
                }
                catch (NotEnoughDiskSpaceException ex)
                {
                    location.Status = BackupStatus.Failed;

                    MessageBox.Show(
                        $"{ex.Message}\n\n" +
                        "Free up space or use a different backup location and try again.",
                        "Disk Space Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (DirectoryNotFoundException ex)
                {
                    location.Status = BackupStatus.Failed;

                    MessageBox.Show($"{ex.Message}", "Folder error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (IOException ioEx)
                {
                    location.Status = BackupStatus.Failed;

                    MessageBox.Show(
                        $"File error: {ioEx.Message}\n\n" +
                        "Make sure all files are accessible and not in use, then try again.",
                        "File Access Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (FolderMismatchException ex)
                {
                    location.Status = BackupStatus.Failed;

                    MessageBox.Show(
                        $"{ex.Message}\n\nPlease try again or restart the app and attempt the backup again.",
                        "Backup Verification Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    location.Status = BackupStatus.Failed;
                    MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (location.Status == BackupStatus.Processing)
                    {
                        location.Status = BackupStatus.Failed;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var location in BackupLocations)
            {
                location.Status = BackupStatus.Failed;
                MessageBox.Show($"Critical error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            _settings.BackupFolders = BackupLocations
                .Select(b => new BackupLocationSetting
                {
                    Path = b.Path!,
                    EncryptFiles = b.EncryptFiles,
                    Status = b.Status
                }).ToList();

            _settingsManager.Save(_settings);

            IsBackupInProgress = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Calleld on "Control + Shift + S"
    private async void OnShallowCheckExecuted(object? parameter)
    {
        var result = MessageBox.Show(
            "This will perform a shallow comparison.\n\nNo files will be copied or deleted.\n\nProceed?",
            "Shallow Check Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var allPassed = true;

            Guard.AgainstInvalidPath(MasterFolder, nameof(MasterFolder));

            foreach (var backupLocation in BackupLocations)
            {
                Guard.AgainstInvalidPath(backupLocation.Path, "Backup Location");

                var comparisonResult = await FolderComparer.CompareCountAsync(
                    MasterFolder!,
                    backupLocation.Path!);

                if (comparisonResult.HasMismatch)
                {
                    allPassed = false;
                    MessageBox.Show(
                        $"Shallow check failed for:\n{backupLocation.Path}\n\n" +
                        $"{comparisonResult.FileMismatchCount} file(s) and " +
                        $"{comparisonResult.DirectoryMismatchCount} directory(ies) mismatch.",
                        "Mismatch Detected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            if (allPassed)
            {
                MessageBox.Show(
                    "Shallow check passed.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                $"A folder path is missing or invalid.\n\nDetails: {ex.Message}",
                "Path Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (DirectoryNotFoundException ex)
        {
            MessageBox.Show(
                $"A specified folder could not be found.\n\nDetails: {ex.Message}",
                "Directory Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool CanShallowCheckExecute(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(MasterFolder) && BackupLocations.Any();
    }

    /// <summary>
    /// Validates whether the specified folder path is set and exists on disk.
    /// </summary>
    /// <param name="folderPath">The folder path to validate.</param>
    /// <returns>
    /// <c>true</c> if the folder path is not null, not whitespace, and points to an existing directory;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool IsFolderInvalid(string? folderPath)
    {
        return string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath);
    }

    /// <summary>
    /// Loads master folder and backup locations from settings.
    /// </summary> 
    private void InitializeLocations()
    {
        if (_settings == null)
        {
            MessageBox.Show(
                "Application settings could not be loaded. Please check your configuration and try again.",
                "Settings not found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (_settings.BackupFolders is not null)
        {
            foreach (var setting in _settings.BackupFolders)
            {
                var vm = new BackupLocationViewModel
                {
                    Path = setting.Path,
                    EncryptFiles = setting.EncryptFiles,
                    Status = setting.Status
                };

                vm.PropertyChanged += OnEncryptFilesCheckboxToggled;

                BackupLocations.Add(vm);
            }
        }

        MasterFolder = _settings.MasterFolder;
    }

    private void OnEncryptFilesCheckboxToggled(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BackupLocationViewModel.EncryptFiles))
        {
            return;
        }

        if (sender is not BackupLocationViewModel vm)
        {
            return;
        }

        bool requestedState = vm.EncryptFiles;

        string action = requestedState ? "encrypt" : "decrypt";
        string message = $"Are you sure you want to {action} files in this backup location?\n\n" +
                         "This may take some time depending on the size of the data." +
                         (requestedState
                            ? string.Empty
                            : "\n\nNote: Removing encryption may expose sensitive data.");

        var result = MessageBox.Show(message, $"{action.ToUpper()} Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            // Sync view models with saved settings by recreating the full list from UI state.
            // This ensures settings file always reflects the current checkbox states.
            _settings.BackupFolders = BackupLocations
            .Select(b => new BackupLocationSetting
            {
                Path = b.Path!,
                EncryptFiles = b.EncryptFiles,
                Status = b.Status
            }).ToList();

            _settingsManager.Save(_settings);

            // Call encryption/decryption logic
            // Example placeholder:
            _ = Task.Run(() => ProcessEncryptionAsync(vm.Path!, requestedState));

            MessageBox.Show(
                $"Files in {vm.Path} will be {(requestedState ? "encrypted" : "decrypted")} shortly.",
                $"{action.ToUpper()} Scheduled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            // Revert checkbox (unsubscribe to prevent infinite loop)
            vm.PropertyChanged -= OnEncryptFilesCheckboxToggled;
            vm.EncryptFiles = !requestedState;
            vm.PropertyChanged += OnEncryptFilesCheckboxToggled;

            MessageBox.Show(
                $"No changes made to the encryption state for {vm.Path}.",
                "Action Cancelled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async Task ProcessEncryptionAsync(string path, bool encrypt)
    {
        Debug.WriteLine($"{(encrypt ? "Encrypting" : "Decrypting")} {path}");
        await Task.Delay(100);
        Debug.WriteLine($"{(encrypt ? "Encrypted" : "Decrypted")} {path}");
    }

    /// <summary>
    /// Determines whether the backup operation can be executed.
    /// </summary>
    /// <returns>
    /// <c>true</c> if there is at least one backup location and none have the <see cref="BackupStatus.Processing"/> status;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool CanBackupExecute(object? arg)
    {
        if (!BackupLocations.Any())
        {
            return false;
        }

        return BackupLocations.All(b => b.Status != BackupStatus.Processing);
    }

    private void OnAddBackupExecuted(object? obj)
    {
        if (IsBackupInProgress)
        {
            MessageBox.Show("Backup is in progress. Please wait until it finishes.", "Backup Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select a backup folder",
            UseDescriptionForTitle = true
        };

        var result = dialog.ShowDialog();
        if (result != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var selectedPath = dialog.SelectedPath;

        if (IsFolderInvalid(selectedPath))
        {
            MessageBox.Show("The selected folder does not exist.", "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var simulatedList = BackupLocations
            .Select(b => b.Path)
            .Where(p => p != null)
            .Cast<string>()
            .Append(selectedPath);

        if (!PathCollisionValidator.TryValidate(MasterFolder!, simulatedList, out var errorMessage))
        {
            MessageBox.Show(errorMessage, "Cannot Add Backup Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dirInfo = new DirectoryInfo(selectedPath);
        var entries = dirInfo.EnumerateFileSystemInfos().ToList();

        if (entries.Count != 0)
        {
            var itemCount = entries.Count;
            var response = MessageBox.Show(
                $"The selected folder is not empty and contains {itemCount} item{(itemCount == 1 ? string.Empty : "s")}. " +
                "Are you sure you want to use it as a backup location? All existing content may be overwritten or deleted.",
                "Folder Not Empty",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (response != MessageBoxResult.Yes)
            {
                return;
            }
        }

        // TODO: Uncomment this for encryption.
        // Ask user if the backup location should be encrypted.
        //var encryptResponse = MessageBox.Show(
        //    $"Do you want to have the files encrypted in this backup location?\n\n{selectedPath}",
        //    "Encrypt Files",
        //    MessageBoxButton.YesNo,
        //    MessageBoxImage.Question);

        //bool encryptFiles = encryptResponse == MessageBoxResult.Yes;

        var vm = new BackupLocationViewModel
        {
            Path = selectedPath,
            Status = BackupStatus.Unknown,
            //EncryptFiles = encryptFiles
        };

        vm.PropertyChanged += OnEncryptFilesCheckboxToggled;

        BackupLocations.Add(vm);

        _settings.BackupFolders.Add(new BackupLocationSetting
        {
            Path = selectedPath,
            //EncryptFiles = encryptFiles,
            Status = BackupStatus.Unknown
        });

        _settingsManager.Save(_settings);
        CommandManager.InvalidateRequerySuggested(); // Reevaluate CanExecute

        BackupCommand.Execute(null); // Start backup immediately after adding a new location
    }

    private void OnSetMasterFolderExecuted(object? obj)
    {
        if (IsBackupInProgress)
        {
            MessageBox.Show(
                "Backup is in progress. Please wait until it finishes.",
                "Backup Running",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Ask for confirmation if a master folder is already set
        bool confirmOverwrite = !string.IsNullOrWhiteSpace(MasterFolder);
        if (confirmOverwrite && !ConfirmMasterFolderOverwrite())
        {
            return;
        }

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the master folder",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        // Only now — after the user selects a new folder — do we clear the old data
        if (confirmOverwrite)
        {
            BackupLocations.Clear();
            _settings.BackupFolders.Clear();
        }

        MasterFolder = dialog.SelectedPath;
        _settings.MasterFolder = MasterFolder;
        _settingsManager.Save(_settings);

        CommandManager.InvalidateRequerySuggested();
    }

    private static bool ConfirmMasterFolderOverwrite()
    {
        var result = MessageBox.Show(
            "The master folder is already set!\n\n"
          + "Changing it will remove all existing backups from the program's list. The files will remain on disk.\n\n"
          + "This action cannot be undone. Are you sure you want to proceed?",
            "Confirm folder change",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    private bool CanAddBackupExecute(object? obj) => BackupLocations.Count < _maxBackups;

    private void OnRemoveBackupExecuted(object? param)
    {
        if (IsBackupInProgress)
        {
            MessageBox.Show("Backup is in progress. Please wait until it finishes.", "Backup Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (param is BackupLocationViewModel backup)
        {
            var message = $"Are you sure you want to remove the backup location:\n\n{backup.Path}\n\n" +
                      "This will remove it from the program but will NOT delete any files on disk.";

            var result = MessageBox.Show(
                message,
                "Confirm Remove Backup Location",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            BackupLocations.Remove(backup);

            var itemToRemove = _settings.BackupFolders.FirstOrDefault(x => string.Equals(x.Path, backup.Path, StringComparison.OrdinalIgnoreCase));
            if (itemToRemove is not null)
            {
                _settings.BackupFolders.Remove(itemToRemove);
                _settingsManager.Save(_settings);
            }

            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool CanRemoveBackupExecute(object? param) => param is BackupLocationViewModel;

    /// <summary>
    /// Executes the command to open the specified backup folder using File Explorer.
    /// </summary>
    private void OnOpenBackupExecuted(object? param)
    {
        if (param is BackupLocationViewModel backup && !string.IsNullOrWhiteSpace(backup.Path))
        {
            if (Directory.Exists(backup.Path))
            {
                try
                {
                    Process.Start("explorer.exe", backup.Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open the folder.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("The folder does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private bool CanOpenBackupExecute(object? param)
    {
        return param is BackupLocationViewModel backup && !string.IsNullOrWhiteSpace(backup.Path);
    }
}
