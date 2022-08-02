using System;
using System.Windows.Input;

namespace YKEnroll.Win.ViewModels.MVVM;

internal sealed class DelegateCommand<T> : ICommand
{
    private readonly Func<object, bool> CanExecuteFunc;
    private readonly Action<object> ExecuteAction;

    public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecuteFunc)
    {
        ExecuteAction = executeAction;
        CanExecuteFunc = canExecuteFunc;
    }

    public bool CanExecute(object? parameter)
    {
        if(parameter != null)
            return CanExecuteFunc == null || CanExecuteFunc(parameter);
        return false;
    }

    public event EventHandler? CanExecuteChanged;

    public void Execute(object? parameter)
    {
        if (parameter != null)
            ExecuteAction(parameter);        
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}