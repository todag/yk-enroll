using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Win.ViewModels.MVVM;

namespace YKEnroll.Win.ViewModels
{
    public enum DialogIcon
    {
        Info,
        Warning,
        Error,
        Question
    }

    public enum DialogButtons
    {
        Ok,
        YesNo,
        YesCancel
    }

    public enum DialogResult
    {
        None,
        Ok,
        Yes,
        No,
        Cancel
    }

    internal class DialogViewModel : Presenter
    {

        public DialogViewModel(string message, string title, DialogIcon dialogIcon, DialogButtons dialogButtons = DialogButtons.Ok, Exception? exception = null)
        {
            Title = title;
            Message = message;
            DialogIcon = dialogIcon;            
            DialogButtons = dialogButtons;

            if(exception != null)
            {
                Message = Message + $"\nException:\n{exception.Message}";
            }
        }

        public bool Ok { get; set; }
        public bool Yes { get; set; }
        public bool No { get; set; }
        public bool Cancel { get; set; }

        public ICommand CloseWindowCommand => new Command(_ => CloseWindow((Window)_!));
        
        public DialogButtons DialogButtons { get; private set; }

        public DialogResult DialogResult
        { 
            get 
            {
                if (Ok)
                    return DialogResult.Ok;
                if (Yes)
                    return DialogResult.Yes;
                if (No)
                    return DialogResult.No;
                if (Cancel)
                    return DialogResult.Cancel;
                return DialogResult.None;
            }
        }

        public DialogIcon DialogIcon { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string Message { get; private set; } = string.Empty;                

        public void CloseWindow(Window window)
        {            
            window.Close();
        }
    }
}
