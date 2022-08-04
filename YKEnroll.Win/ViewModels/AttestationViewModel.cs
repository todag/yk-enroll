using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;

namespace YKEnroll.Win.ViewModels;

internal class AttestationViewModel : Presenter
{
    public AttestationViewModel(AttestationStatement attestationStatement)
    {
        AttestationStatement = attestationStatement;
        StatementCert = attestationStatement.StatementCertificate;
        AttestationCert = attestationStatement.AttestationCertificate;
        RootCert = attestationStatement.RootCertificate;

        if(!attestationStatement.IsValid)
        {            
            var chain = attestationStatement.Chain;
            foreach(var chainStatus in chain.ChainStatus)
            {                
                FailStatusMessage = $"{ FailStatusMessage + chainStatus.StatusInformation }\n";
            }
        }
    }
    
    public ICommand CloseCommand => new Command(_ => Close((Window)_!));
    public ICommand ShowCertificateCommand => new Command(_ => ShowCertificate((X509Certificate2)_!));

    public AttestationStatement AttestationStatement { get; private set; }

    public string FailStatusMessage { get; private set; } = "";

    public X509Certificate2 RootCert { get; private set; }
    public X509Certificate2 AttestationCert { get; private set; }
    public X509Certificate2 StatementCert { get; private set; }

    private void ShowCertificate(X509Certificate2 certificate)
    {
        try
        {
            var outputFile = Path.GetTempPath() + "cert.cer";
            File.WriteAllBytes(outputFile, certificate.Export(X509ContentType.Cert));
            Process.Start(new ProcessStartInfo(outputFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowMessage.Error("An error occured while trying to display the certificate!", ex);
        }
    }
    private void Close(Window window)
    {
        window.Close();
    }
}
