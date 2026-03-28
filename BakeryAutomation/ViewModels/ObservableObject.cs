using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows;
using BakeryAutomation.Services;

namespace BakeryAutomation.ViewModels
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void Raise([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DoesNotReturn]
        protected static void FailCommand(
            string message,
            string title,
            MessageBoxImage image = MessageBoxImage.Warning,
            string? buttonText = null)
        {
            throw new CommandFeedbackException(message, title, image, buttonText);
        }

        protected static bool ConfirmCommand(
            string message,
            string title,
            MessageBoxImage image = MessageBoxImage.Question)
        {
            var result = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                image);

            if (result == MessageBoxResult.Yes)
            {
                return true;
            }

            CommandButtonFeedback.Cancel();
            return false;
        }

        protected static void CancelCommand()
        {
            CommandButtonFeedback.Cancel();
        }

        protected static void SucceedCommand(string? buttonText = null)
        {
            CommandButtonFeedback.Success(buttonText);
        }
    }
}
