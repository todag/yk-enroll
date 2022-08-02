using System;
using System.Windows.Input;

namespace YKEnroll.Win.ViewModels.MVVM;

internal sealed class Command : ICommand
{
    private readonly Action<object?> _action;

    public Command(Action<object?> action)
    {
        _action = action;
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public void Execute(object? parameter)
    {
        _action(parameter);
    }
}