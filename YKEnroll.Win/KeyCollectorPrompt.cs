using YKEnroll.Lib;
using YKEnroll.Win.ViewModels;
using YKEnroll.Win.Views.Windows;
using System;
using System.Windows;
using Yubico.YubiKey;

namespace YKEnroll.Win
{
    public class KeyCollectorPrompt : IKeyCollectorPrompt
    {       
        public KeyCollectorResult Prompt(KeyEntryData keyEntryData)
        {
            var result = new KeyCollectorResult();
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                var win = new KeyCollectorView()
                { DataContext = new KeyCollectorViewModel(keyEntryData, result) };
                win.ShowDialog();
            });
            return result;
        }       
    }
}
