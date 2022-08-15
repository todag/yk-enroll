using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace YKEnroll.Win.ViewModels;

internal class RequestViewModel : Presenter
{
    
    private string _csrFile = "";
    private DecodedCsr? _csr;

    public RequestViewModel(EnrollmentManager enrollmentManager, YubiKey yubiKey, Slot slot)
    {
        EnrollmentManager = enrollmentManager;
        YubiKey = yubiKey;
        Slot = slot;
    }

    public ICommand OkCommand => new Command(_ => Request((Window)_!));
    public ICommand CancelCommand => new Command(_ => Cancel((Window)_!));

    public ICommand SelectFileCommand => new Command(_ => SelectFile());
    
    public DecodedCsr? Csr
    {
        get { return _csr; }
        private set { Update(ref _csr, value); }
    }

    public EnrollmentManager EnrollmentManager { get; private set; }
    public YubiKey YubiKey { get; set; }

    public Slot? Slot { get; set; }

    public string CsrFile
    {
        get { return _csrFile; }
        set 
        {             
            Update(ref _csrFile, value);
            ReadCsr();
        }
    }
   
    public string Output { get; set; } = "";
        
    public RequestSettings RequestSettings { get; } = new RequestSettings();

    private void ReadCsr()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(CsrFile))
            {
                Csr = new DecodedCsr(File.ReadAllText(CsrFile));
            }
            else
            {
                Csr = null;
            }
        }
        catch(Exception ex)
        {
            ShowMessage.Error("Failed to read csr!", ex);
        }
        
    }
    private void Request(Window window)
    {        
        if(RequestSettings.CAServer == null || RequestSettings.CertificateTemplate == null)
        {
            ShowMessage.Info("You must select a CA Server and a Template!");
            return;
        }
        try
        {
            window.Visibility = Visibility.Hidden;
            var rs = RequestSettings;
            var caResponse = rs.CAServer.RequestCertificate(certTemplate: rs.CertificateTemplate, csrData: File.ReadAllText(CsrFile));
            
            switch(caResponse.ResponseString)
            {
                case "CR_DISP_ISSUED":
                    if (Output == "save")
                        SaveCertificate(caResponse.Certificate!);
                    if (Output == "import")
                        ImportCertificate(caResponse.Certificate!);
                    break;
                case "CR_DISP_UNDER_SUBMISSION":
                    ShowMessage.Info(
                        $" Certificate request Id [{caResponse.RequestId.ToString()}] is pending approval by CA manager.\n" +
                        $"When the certificate has been issued, you can use the \"Retrieve\" button to finish enrollment.",
                        "Issuance pending");
                    break;
                case "CR_DISP_DENIED":
                    ShowMessage.Error(
                    $" Unhandled response from CA Server\nResponse: {caResponse.ResponseCode.ToString()}\nResponse code:" +
                    $"{caResponse.ResponseString}", "CA Response");
                    break;
                default:
                    ShowMessage.Error(
                    $" Unhandled response from CA Server\nResponse: {caResponse.ResponseCode.ToString()}\nResponse code:" +
                    $"{caResponse.ResponseString}", "CA Response");
                    break;
            }            
            window.Close();
        }
        catch (Exception ex)
        {
            ShowMessage.Error("An exception occured while trying to request a certificate!", ex);
            window.ShowDialog();
        }
    }

    private void ImportCertificate(X509Certificate2 certificate)
    {
        var result = ShowMessage.Dialog(
            $"Import certificate? This will overwrite any existing certificate or data in slot {Slot.Name}" +
            $", this will overwrite any existing certificates in slot\nAre you sure?",
            "Import certificate",
            DialogButtons.YesCancel,
            DialogIcon.Warning);            

        if (result == DialogResult.Yes)
        {
            using (YubiKey.NewSession(new KeyCollectorPrompt()))
            {
                YubiKey.ImportCertificate(Slot, certificate);
            }
        }

    }

    private void SaveCertificate(X509Certificate2 certificate)
    {
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Certificate (*.cer)|*.cer";
        if (saveFileDialog.ShowDialog() == true)
            File.WriteAllBytes(saveFileDialog.FileName, certificate.Export(X509ContentType.Cert));
    }

    private void SelectFile()
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "CSR file (*.csr)|*.csr";
        if (openFileDialog.ShowDialog() == true) CsrFile = openFileDialog.FileName;
    }

    private void Cancel(Window window)
    {
        window.Close();
    }
}