using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _executeAction;
        private readonly Func<object?, bool> _canExecuteAction;

        public RelayCommand(Action<object?> executeAction, Func<object?, bool> canExecuteAction)
        {
            _executeAction = executeAction;
            _canExecuteAction = canExecuteAction;
        }

        public bool CanExecute(object? parameter) => _canExecuteAction?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _executeAction(parameter);

        public event EventHandler? CanExecuteChanged;

        public void InvokeCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
