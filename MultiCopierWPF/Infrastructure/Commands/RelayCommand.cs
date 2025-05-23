﻿using MultiCopierWPF.Infrastructure.Commands.Base;
using System.Threading.Channels;
using System.Windows.Media.Media3D;

namespace MultiCopierWPF.Infrastructure.Commands;

internal class RelayCommand : CommandBase
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public override void Execute(object? parameter) => _execute(parameter);

}
