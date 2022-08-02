using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;
using YKEnroll.Win.Views;
using YKEnroll.Win.Views.Windows;
using Microsoft.Win32;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Sample.PivSampleCode;

namespace YKEnroll.Win.ViewModels;

internal class CSRViewModel : Presenter
{             
    public CSRViewModel(YubiKey yubiKey, Slot slot)
    {        
        YubiKey = yubiKey;
        Slot = slot;
    }

    public ICommand OkCommand => new Command(_ => CreateCsr((Window)_!));
    public ICommand CancelCommand => new Command(_ => Cancel((Window)_!));
    public ICommand SearchDirectoryCommand => new Command(_ => SearchDirectory());

    public YubiKey YubiKey { get; private set; }
    
    public RequestSettings RequestSettings { get; } = new RequestSettings();
    public Slot Slot { get; set; }
    
    public string OutputFormat { get; set; } = "pem";
   
    private async void CreateCsr(Window window)
    {
        try
        {
            window.Visibility = Visibility.Hidden;
            var rs = RequestSettings;
            var csr = await Task.Run(() =>
            {
                using (YubiKey.NewSession(new KeyCollectorPrompt()))
                {
                    var publicKey = YubiKey.GenerateKeyPair(
                        slot: Slot,
                        algorithm: rs.PivAlgorithm,
                        pinPolicy: rs.PivPinPolicy,
                        touchPolicy: rs.PivTouchPolicy
                        );

                    var csr = YubiKey.GenerateCertificateSigningRequest(
                        slot: Slot,
                        publicKey: publicKey,
                        subject: rs.Subject,
                        upn: rs.UserPrincipalName,
                        sid: rs.SecurityIdentifier
                        ); ;
                    return csr;
                }
            });

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSR file (*.csr)|*.csr";
            saveFileDialog.FileName = Slot.ShortName + ".csr";
            if (saveFileDialog.ShowDialog() == true)
            {
                if (OutputFormat == "der")
                    File.WriteAllBytes(saveFileDialog.FileName, csr);
                else if (OutputFormat == "pem")
                    File.WriteAllText(saveFileDialog.FileName,
                        new string(PemOperations.BuildPem("CERTIFICATE REQUEST", csr)));
                ShowMessage.Info("CSR Generated successfully!");
            }
            window.Close();
        }
        catch (Exception ex)
        {
            ShowMessage.Error(ex.Message);
            window.ShowDialog();
        }
    }

    private void SearchDirectory()
    {
        var vm = new SelectUserViewModel();
        var win = new SelectUserView { DataContext = vm };
        win.ShowDialog();
        if (vm.SelectedUser != null)
        {
            RequestSettings.Subject = vm.SelectedUser.DistinguishedName;
            RequestSettings.UserPrincipalName = vm.SelectedUser.UserPrincipalName;
            RequestSettings.SecurityIdentifier = vm.SelectedUser.SecurityIdentifier;
        }
    }
    private void Cancel(Window window)
    {        
        window.Close();
    }
}