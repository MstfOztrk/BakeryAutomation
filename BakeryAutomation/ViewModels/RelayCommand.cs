using System;
using System.Windows.Input;
using BakeryAutomation.Services;

namespace BakeryAutomation.ViewModels
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter)
        {
            CommandButtonFeedback.BeginExecution();

            try
            {
                _execute(parameter);
                CommandButtonFeedback.CompleteDefaultSuccess();
            }
            catch (CommandFeedbackException ex)
            {
                CommandButtonFeedback.Fail(ex.ButtonText);

                if (ex.ShowDialog)
                {
                    System.Windows.MessageBox.Show(
                        ex.Message,
                        ex.Title,
                        System.Windows.MessageBoxButton.OK,
                        ex.Image);
                }
            }
            catch (Exception ex)
            {
                var logPath = AppLogService.LogException("RelayCommand", ex);
                CommandButtonFeedback.Fail();

                System.Windows.MessageBox.Show(
                    $"Islem tamamlanamadi.\n\nDetaylar log dosyasina yazildi:\n{logPath}",
                    "Islem Hatasi",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                CommandButtonFeedback.EndExecution();
            }
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
