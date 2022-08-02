using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;

namespace YKEnroll.Win.ViewModels;

internal class CodeViewModel : Presenter
{    
    public CodeViewModel(YubiKey yubiKey)
    {
        YubiKey = yubiKey;
    }

    public ICommand ChangeCodeCommand => new Command(_ => ChangeCode((Window)_!));

    public bool ChangePin { get; set; } = false;
    public bool ChangePuk { get; set; } = false;

    public bool ResetPin { get; set; } = false;
    public bool ChangeMgmtKey { get; set; } = false;

    public YubiKey YubiKey { get; private set; }
    
    private async void ChangeCode(Window window)
    {             
        try
        {
            window.Close();
            var result = await Task.Run(() =>
            {
                using (YubiKey.NewSession(new KeyCollectorPrompt()))
                {
                    if (ChangePin)
                        return YubiKey.ChangePin();
                    if (ChangePuk)
                        return YubiKey.ChangePuk();
                    if (ResetPin)
                        return YubiKey.UnblockPin();
                    if (ChangeMgmtKey)
                        return YubiKey.ChangeManagementKey();
                    return false;
                }
            });
            
            if(!result)
                return;

            if (ChangePin)
                ShowMessage.Info("Pin changed successfully!");
            if (ChangePuk)
                ShowMessage.Info("Puk changed successfully!");
            if (ResetPin)
                ShowMessage.Info("Pin reset successfully!");
            if (ChangeMgmtKey)
                ShowMessage.Info("Management key changed successfully!");

        }
        catch (Exception ex)
        {
            ShowMessage.Error("Operation failed!", ex);
        }
    }
}