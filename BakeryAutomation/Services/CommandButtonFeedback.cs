using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace BakeryAutomation.Services
{
    public enum CommandButtonFeedbackState
    {
        Idle,
        Success,
        Failure
    }

    public static class CommandButtonFeedback
    {
        [ThreadStatic]
        private static ExecutionContext? _currentExecution;

        private sealed class ExecutionContext
        {
            public ButtonBase? Button { get; set; }
            public bool IsHandled { get; set; }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(CommandButtonFeedback),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.RegisterAttached(
                "DisplayText",
                typeof(string),
                typeof(CommandButtonFeedback),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached(
                "State",
                typeof(CommandButtonFeedbackState),
                typeof(CommandButtonFeedback),
                new PropertyMetadata(CommandButtonFeedbackState.Idle));

        private static readonly DependencyProperty ResetTimerProperty =
            DependencyProperty.RegisterAttached(
                "ResetTimer",
                typeof(DispatcherTimer),
                typeof(CommandButtonFeedback),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        public static string GetDisplayText(DependencyObject obj) => (string)obj.GetValue(DisplayTextProperty);
        public static void SetDisplayText(DependencyObject obj, string value) => obj.SetValue(DisplayTextProperty, value);

        public static CommandButtonFeedbackState GetState(DependencyObject obj) => (CommandButtonFeedbackState)obj.GetValue(StateProperty);
        public static void SetState(DependencyObject obj, CommandButtonFeedbackState value) => obj.SetValue(StateProperty, value);

        private static DispatcherTimer? GetResetTimer(DependencyObject obj) => (DispatcherTimer?)obj.GetValue(ResetTimerProperty);
        private static void SetResetTimer(DependencyObject obj, DispatcherTimer? value) => obj.SetValue(ResetTimerProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ButtonBase button)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                button.Click += Button_Click;
            }
            else
            {
                button.Click -= Button_Click;
            }
        }

        private static void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ButtonBase button)
            {
                return;
            }

            Clear(button);
            _currentExecution = new ExecutionContext
            {
                Button = button
            };
        }

        internal static void BeginExecution()
        {
            _currentExecution ??= new ExecutionContext();
            _currentExecution.IsHandled = false;
        }

        internal static void CompleteDefaultSuccess()
        {
            if (_currentExecution == null || _currentExecution.IsHandled)
            {
                return;
            }

            Success();
        }

        internal static void EndExecution()
        {
            _currentExecution = null;
        }

        public static void Success(string? buttonText = null)
        {
            Apply(CommandButtonFeedbackState.Success, buttonText);
        }

        public static void Fail(string? buttonText = null)
        {
            Apply(CommandButtonFeedbackState.Failure, buttonText);
        }

        public static void Cancel()
        {
            if (_currentExecution == null)
            {
                return;
            }

            _currentExecution.IsHandled = true;
            Clear(_currentExecution.Button);
        }

        private static void Apply(CommandButtonFeedbackState state, string? buttonText)
        {
            if (_currentExecution == null)
            {
                return;
            }

            _currentExecution.IsHandled = true;

            var button = _currentExecution.Button;
            if (button == null)
            {
                return;
            }

            SetDisplayText(button, buttonText ?? BuildDefaultLabel(button, state));
            SetState(button, state);

            var timer = GetResetTimer(button);
            if (timer == null)
            {
                timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2.2)
                };

                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    Clear(button);
                };

                SetResetTimer(button, timer);
            }

            timer.Stop();
            timer.Start();
        }

        private static void Clear(ButtonBase? button)
        {
            if (button == null)
            {
                return;
            }

            GetResetTimer(button)?.Stop();
            SetState(button, CommandButtonFeedbackState.Idle);
            SetDisplayText(button, string.Empty);
        }

        private static string BuildDefaultLabel(ButtonBase button, CommandButtonFeedbackState state)
        {
            var content = (button.Content?.ToString() ?? string.Empty).Trim();
            var normalized = content.ToLowerInvariant();

            if (state == CommandButtonFeedbackState.Success)
            {
                if (normalized.Contains("kaydet")) return "Kaydedildi";
                if (normalized.Contains("guncelle")) return "Guncellendi";
                if (normalized.Contains("yenile") || normalized.Contains("uygula")) return "Yenilendi";
                if (normalized.Contains("yedek")) return "Yedeklendi";
                if (normalized.Contains("yukle")) return "Yuklendi";
                if (normalized.Contains("hesap")) return "Hesaplandi";
                if (normalized.Contains("yaz")) return "Yazildi";
                if (normalized.Contains("sil")) return "Silindi";
                if (normalized.Contains("kaldir")) return "Kaldirildi";
                if (normalized.Contains("ekle")) return "Eklendi";
                if (normalized.Contains("aktar") || normalized.Contains("export")) return "Aktarildi";
                if (normalized.Contains("print") || normalized.Contains("yazdir")) return "Hazir";
                if (normalized.Contains("ac") || normalized.Contains("git") || normalized.Contains("goster") || normalized.Contains("gor")) return "Acildi";
                if (normalized.Contains("yeni")) return "Hazir";
                if (normalized == "<" || normalized == ">") return "Guncellendi";
                return "Tamamlandi";
            }

            if (normalized.Contains("kaydet")) return "Kaydedilemedi";
            if (normalized.Contains("guncelle")) return "Guncellenemedi";
            if (normalized.Contains("yenile") || normalized.Contains("uygula")) return "Yenilenemedi";
            if (normalized.Contains("yedek")) return "Yedeklenemedi";
            if (normalized.Contains("yukle")) return "Yuklenemedi";
            if (normalized.Contains("hesap")) return "Hesaplanamadi";
            if (normalized.Contains("yaz")) return "Yazilamadi";
            if (normalized.Contains("sil")) return "Silinemedi";
            if (normalized.Contains("kaldir")) return "Kaldirilamadi";
            if (normalized.Contains("ekle")) return "Eklenemedi";
            if (normalized.Contains("aktar") || normalized.Contains("export")) return "Aktarilamadi";
            if (normalized.Contains("print") || normalized.Contains("yazdir")) return "Hazirlanamadi";
            if (normalized.Contains("ac") || normalized.Contains("git") || normalized.Contains("goster") || normalized.Contains("gor")) return "Acilamadi";
            if (normalized.Contains("yeni")) return "Hazirlanamadi";
            if (normalized == "<" || normalized == ">") return "Hata";
            return "Hata";
        }
    }
}
