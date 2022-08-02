using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;
using Microsoft.Win32;

namespace YKEnroll.Win.ViewModels;

internal class RetrieveViewModel : Presenter
{    
    private YubiKey _yubiKey;
    private Slot? _slot;

    public RetrieveViewModel(EnrollmentManager enrollmentManager, YubiKey yubiKey, Slot slot)
    {
        EnrollmentManager = enrollmentManager;
        _yubiKey = yubiKey;
        Slot = slot;
    }

    public CAServer? SelectedCAServer { get; set; }

    public ICommand OkCommand => new Command(_ => Retrieve((Window)_!));
    public ICommand CancelCommand => new Command(_ => Cancel((Window)_!));

    public EnrollmentManager EnrollmentManager { get; private set; }
    public YubiKey YubiKey
    {
        get => _yubiKey;
        private set => Update(ref _yubiKey, value);
    }
    
    public Slot? Slot
    {
        get => _slot;
        private set => Update(ref _slot, value);
    }

    public string? RequestId { get; set; }
    public string? Output { get; set; }
   
    private async void Retrieve(Window window)
    {        
        if (!int.TryParse(RequestId, out var x))
        {
            ShowMessage.Warning($"\"{RequestId}\" is not a valid Request Id");
            return;
        }

        if(SelectedCAServer == null)
        {
            ShowMessage.Info("You must select a CA Server");
            return;
        }

        try
        {
            window.Visibility = Visibility.Hidden;
            var caResponse = await Task.Run(() => SelectedCAServer.RetrieveCertificate(Convert.ToInt32(RequestId)));
            if (caResponse.ResponseString == "CR_DISP_ISSUED")
            {
                // Certifiate issued
                if (Output == "save")
                {
                    SaveCertificate(caResponse.Certificate!);
                }
                else if (Output == "import")
                {
                    var result = ShowMessage.Dialog(
                            $"Import certificate to slot {Slot!.Name}, this will overwrite any existing certificates in slot\n Are you sure?",
                            "Import certificate",
                            DialogButtons.YesCancel,
                            DialogIcon.Warning
                        );                       
                    if (result == DialogResult.Yes)
                        await Task.Run(() =>
                        {
                            using (YubiKey.NewSession(new KeyCollectorPrompt()))
                            {
                                YubiKey.ImportCertificate(Slot, caResponse.Certificate!);
                            }
                        });
                }
            }
            else if (caResponse.ResponseString == "CR_DISP_UNDER_SUBMISSION")
            {
                ShowMessage.Info(
                    "Certificate issuance is pending approval by CA manager.\n\nWhen the certificate has been issued, you can use the \"Retrieve\" button to finish enrollment.",
                    "Issuance pending");
            }
            else
            {
                ShowMessage.Info(
                    $"Unhandled response from CA Server\nResponse: {caResponse.ResponseCode.ToString()}\nResponse code:{caResponse.ResponseString}",
                    "CA Response");
            }
            window.Close();
        }
        catch(Exception ex)
        {
            ShowMessage.Error("Retrieve failed!", ex);
            window.ShowDialog();
        }
        
    }

    private void SaveCertificate(X509Certificate2 certificate)
    {
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "CSR file (*.csr)|*.csr";
        if (saveFileDialog.ShowDialog() == true)
            File.WriteAllBytes(saveFileDialog.FileName, certificate.Export(X509ContentType.Cert));
    }

    private void Cancel(Window window)
    {        
        window.Close();
    }
}