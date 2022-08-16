using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YKEnroll.Lib;
using Yubico.YubiKey.Piv;

namespace YKEnroll.Win.ViewModels.MVVM;

/// <summary>
///     Class that contains settings for various requests.
/// </summary>
public class RequestSettings : INotifyPropertyChanged
{
    private bool _includeUserPrincipalName = false;
    private bool _includeSecurityIdentifier = false;

    private ICertServer? _certServer;
    private CertificateTemplate? _certificateTemplate;

    private PivAlgorithm _pivAlgorithm = Settings.DefaultPivAlgorithm;
    private PivPinPolicy _pivPinPolicy = Settings.DefaultPivPinPolicy;
    private PivTouchPolicy _pivTouchPolicy = Settings.DefaultPivTouchPolicy;

    private string _subject = "";
    private string _userPrincipalName = "";
    private string _securityIdentifier = "";

    public bool IncludeUserPrincipalNames
    {
        get { return _includeUserPrincipalName; }
        set { _includeUserPrincipalName = value; NotifyPropertyChanged(); }
    }

    public bool IncludeSecurityIdentifier
    {
        get { return _includeSecurityIdentifier; }
        set { _includeSecurityIdentifier = value; NotifyPropertyChanged(); }
    }
    
    public ICertServer? CertServer
    {
        get { return _certServer; }
        set { _certServer = value; NotifyPropertyChanged(); }
    }

    public CertificateTemplate? CertificateTemplate
    {
        get { return _certificateTemplate; }
        set { _certificateTemplate = value; NotifyPropertyChanged(); }
    }

    public PivAlgorithm PivAlgorithm
    {
        get { return _pivAlgorithm; }
        set { _pivAlgorithm = value; NotifyPropertyChanged(); }
    }
    public PivPinPolicy PivPinPolicy
    {
        get { return _pivPinPolicy; }
        set { _pivPinPolicy= value; NotifyPropertyChanged(); }
    }

    public PivTouchPolicy PivTouchPolicy
    {
        get { return _pivTouchPolicy; }
        set { _pivTouchPolicy = value; NotifyPropertyChanged(); }
    }

    public string Subject
    {
        get { return _subject; }
        set { _subject = value; NotifyPropertyChanged(); }
    }

    public string SecurityIdentifier
    {
        get { return _securityIdentifier; }
        set { value = _securityIdentifier = value; NotifyPropertyChanged(); }
    }

    public string UserPrincipalName
    {
        get { return _userPrincipalName; }
        set { _userPrincipalName = value; NotifyPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
