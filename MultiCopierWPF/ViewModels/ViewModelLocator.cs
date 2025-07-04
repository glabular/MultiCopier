using Microsoft.Extensions.DependencyInjection;
using MultiCopierWPF.Services;
using System.ComponentModel;
using System.Windows;

namespace MultiCopierWPF.ViewModels;

/// <summary>
/// Acts as a bridge between XAML views and their corresponding view models,
/// supporting both runtime and design-time scenarios in an MVVM application.
/// </summary>
public class ViewModelLocator
{
    /// <summary>
    /// Provides an instance of <see cref="MainWindowViewModel"/> for binding in XAML.
    /// During design-time, returns a mock view model for previewing the UI.
    /// At runtime, resolves the view model via the application's dependency injection container.
    /// </summary>
    public MainWindowViewModel MainWindowViewModel
    {
        get
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return new MainWindowViewModel(new DesignTimeServices(), new DesignTimeServices());
            }
            else
            {
                if (App.AppHost is null)
                {
                    throw new InvalidOperationException("AppHost is not initialized.");
                }

                return App.AppHost.Services.GetService<MainWindowViewModel>()
                    ?? throw new InvalidOperationException("MainWindowViewModel not registered in DI container.");
            }
        }
    }
}
