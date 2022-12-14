using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;
using YKEnroll.Win.Views;
using YKEnroll.Win.Views.Windows;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Sample.PivSampleCode;

namespace YKEnroll.Win.ViewModels;

internal class EnrollViewModel : Presenter
{    
    public EnrollViewModel(EnrollmentManager enrollmentManager, YubiKey yubiKey, Slot slot)
    {
        YubiKey = yubiKey;
        Slot = slot;
        EnrollmentManager = enrollmentManager;
    }       
    
    public EnrollmentManager EnrollmentManager { get; private set; }
    public ICommand OkCommand => new Command(_ => Enroll((Window)_!));
    public ICommand CancelCommand => new Command(_ => Cancel((Window)_!));
    public ICommand SearchDirectoryCommand => new Command(_ => SearchDirectory());
    public YubiKey YubiKey { get; set; }
               
    public RequestSettings RequestSettings { get; } = new RequestSettings();
        
    public Slot Slot { get; set; }
    
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

    private async void Enroll(Window window)
    {
        window.Visibility = Visibility.Collapsed;
        try
        {
            var rs = RequestSettings;
            CertServerResponse certServerResponse = await Task.Run(() =>
            {
                using (YubiKey.NewSession(new KeyCollectorPrompt()))
                {                    
                    var publicKey = YubiKey.GenerateKeyPair
                        (
                            slot: Slot,
                            algorithm: rs.PivAlgorithm,
                            pinPolicy: rs.PivPinPolicy,
                            touchPolicy: rs.PivTouchPolicy
                        );
                    var derCsrData = YubiKey.GenerateCertificateSigningRequest
                        (
                            slot: Slot,
                            publicKey: publicKey,
                            subject: rs.Subject,
                            upn: rs.IncludeUserPrincipalNames ? rs.UserPrincipalName : string.Empty,
                            sid: rs.IncludeSecurityIdentifier ? rs.SecurityIdentifier : string.Empty
                        );                        
                    var pemCsrData = new string(PemOperations.BuildPem("CERTIFICATE REQUEST", derCsrData));
                    certServerResponse = rs.CertServer!.RequestCertificate(rs.CertificateTemplate!, pemCsrData);
                    if (certServerResponse.Status == RequestStatus.CR_ISSUED)
                        YubiKey.ImportCertificate(Slot, certServerResponse.Certificate!);
                    return certServerResponse;
                }
            });
            switch (certServerResponse.Status)
            {
                case RequestStatus.CR_ISSUED:
                    ShowMessage.Info("Enrollment successful!");
                    break;
                case RequestStatus.CR_PENDING:
                    ShowMessage.Info(
                        $"Certificate request Id [{certServerResponse.RequestId}] is pending approval by CA manager.\n\nWhen the certificate has been issued, you can use the \"Retrieve\" button to finish enrollment.");
                    break;
                case RequestStatus.CR_DENIED:
                    ShowMessage.Warning("The request was denied");
                    break;
                default:
                    ShowMessage.Error($"Unknown response from Cert Server");
                    break;
            }
            window.Close();
        }
        catch (Exception ex)
        {
            ShowMessage.Error("Enrollment failed!", ex);
            window.ShowDialog();
        }
    }

    private void Cancel(Window window)
    {
        window.Close();
    }
}