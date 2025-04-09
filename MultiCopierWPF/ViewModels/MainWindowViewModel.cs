using MultiCopierWPF.Infrastructure.Commands;
using MultiCopierWPF.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MultiCopierWPF.ViewModels;

internal class MainWindowViewModel : ViewModel
{
    #region Fields
    private string? _title = "MultiCopier v1.0";
    #endregion

    #region Properties
    /// <summary>
    /// Window title.
    /// </summary>
	public string? Title
    {
        get => _title; 
        set => Set(ref _title, value);
    }
    #endregion

    public MainWindowViewModel()
    {
        XXXCommand = new RelayCommand(OnXXXCommandExecuted, CanXXXCommandExecute);
    }

    #region Commands
    public ICommand XXXCommand { get; }
    #endregion









    private void OnXXXCommandExecuted(object? p)
    {
        Application.Current.Shutdown();
    }

    private bool CanXXXCommandExecute(object? p) => true;
}
