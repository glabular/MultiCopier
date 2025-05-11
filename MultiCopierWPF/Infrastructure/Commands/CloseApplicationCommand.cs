using MultiCopierWPF.Infrastructure.Commands.Base;
using Application = System.Windows.Application;

namespace MultiCopierWPF.Infrastructure.Commands;

internal class CloseApplicationCommand : CommandBase
{
    public override bool CanExecute(object? parameter) => true;

    public override void Execute(object? parameter) => Application.Current.Shutdown();
}
