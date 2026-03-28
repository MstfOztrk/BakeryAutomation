using System;
using System.Windows;

namespace BakeryAutomation.Services
{
    public sealed class CommandFeedbackException : Exception
    {
        public string Title { get; }
        public MessageBoxImage Image { get; }
        public bool ShowDialog { get; }
        public string? ButtonText { get; }

        public CommandFeedbackException(
            string message,
            string title,
            MessageBoxImage image = MessageBoxImage.Warning,
            string? buttonText = null,
            bool showDialog = true)
            : base(message)
        {
            Title = title;
            Image = image;
            ShowDialog = showDialog;
            ButtonText = buttonText;
        }
    }
}
