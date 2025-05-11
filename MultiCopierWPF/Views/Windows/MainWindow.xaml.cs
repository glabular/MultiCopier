using MultiCopierWPF.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace MultiCopierWPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //DataContext = App.AppHost!.Services.GetRequiredService<MainWindowViewModel>();
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.IsBackupInProgress)
        {
            System.Windows.MessageBox.Show("Backup is in progress. Please wait until it finishes.", "Backup Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Cancel = true;
        }
    }
}
