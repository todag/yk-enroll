using System;
using System.Windows;
using YKEnroll.Win.Views.Windows;

namespace YKEnroll.Win.ViewModels.MVVM
{
    public static class ShowMessage
    {
        public static void Info(string message)
        {
            Show(message, "Info", DialogIcon.Info);
        }
        public static void Info(string message, string title)
        {
            Show(message, title, DialogIcon.Info);
        }

        public static void Warning(string message)
        {
            Show(message, "Warning", DialogIcon.Warning);
        }
        public static void Warning(string message, string title)
        {
            Show(message, title, DialogIcon.Warning);
        }

        public static void Error(string message, Exception? ex = null)
        {
            Show(message, "Error", DialogIcon.Error, ex);
        }

        public static void Error(string message, string title, Exception? ex = null)
        {
            Show(message, title, DialogIcon.Error, ex);
        }

        public static DialogResult Dialog(string message, string title, DialogButtons dialogButtons, DialogIcon dialogIcon = DialogIcon.Question)
        {
            var vm = new DialogViewModel(
                    message: message,
                    title: title,
                    dialogIcon: dialogIcon,
                    dialogButtons: dialogButtons
                    );
            var win = new DialogView { DataContext = vm };
            win.ShowDialog();
            return vm.DialogResult;
        }
        
        private static void Show(string message, string title, DialogIcon dialogIcon, Exception? ex = null)
        {
            var win = new DialogView
            {
                DataContext = new DialogViewModel(
                    message: message,
                    title: title,
                    dialogIcon: dialogIcon,
                    exception: ex)
            };
            win.ShowDialog();
        }        
    }
}
