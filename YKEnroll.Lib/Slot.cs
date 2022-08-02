using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace YKEnroll.Lib;

public enum SlotStatus
{
    Error,
    NotRead,
    NoData,
    Success,
    Warning
}

public class Slot : INotifyPropertyChanged
{
    private SlotStatus _slotStatus = SlotStatus.NotRead;
    private X509Certificate2? _certificate;
    private string _name = "";
    private string _slotStatusMessage = "Data not read";

    public Slot(string name, byte slotNumber, int dataTag)
    {
        Name = name;
        SlotNumber = slotNumber;
        DataTag = dataTag;
    }

    /// <summary>
    ///     Returns Key algorithm and size
    /// </summary>
    /// <returns>Key algorithm and size</returns>
    private (string algorithm, int keySize) GetAlgorithmAndKeySize()
    {
        if (Certificate == null)
            return (string.Empty, 0);

        AsymmetricAlgorithm algo;

        var pubKey = Certificate.PublicKey;
        if (pubKey.GetRSAPublicKey() != null)
            algo = pubKey.GetRSAPublicKey()!;
        else if (pubKey.GetECDsaPublicKey() != null)
            algo = pubKey.GetECDsaPublicKey()!;
        else
        {
            return ("Unknown", 0);
        }

        if (algo != null)
        {
            return (algo.SignatureAlgorithm!, algo.KeySize!);
        }
        return ("Unknown", 0);
    }

    public SlotStatus SlotStatus
    {
        get => _slotStatus;
        set { _slotStatus = value; NotifyPropertyChanged(); }
    }

    public string SlotStatusMessage
    {
        get => _slotStatusMessage;
        set { _slotStatusMessage = value; NotifyPropertyChanged(); }
    }
    /// <summary>
    ///     "9A PIV Authentication" or "9C Digital Signature" or "82 Retired1"
    /// </summary>
    public string Name
    {
        get => $"{SlotNumber.ToString("X2")} {_name}";
        set
        {
            _name = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    ///     Returns short name of slot. eg. "9A" if the slot is 9A Authentication or "82" if the slot name is 82 Retired1
    /// </summary>
    public string ShortName => SlotNumber.ToString("X2");
    

    public byte SlotNumber { get; set; }
    public int DataTag { get; set; }
    
    /// <summary>
    ///     Contains the raw byte data of the slot contents.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; set; }

    /// <summary>
    ///     Returns the length of the Data object.
    /// </summary>
    public int DataSize => Data.Length;
    

    /// <summary>
    ///     Returns the X509Certificate2 object for this slot.
    ///     Can be null if the slot data is empty or cannot be
    ///     rendered to a X509Certificate2 object.    
    /// </summary>
    public X509Certificate2? Certificate
    {
        get => _certificate;
        set
        {
            _certificate = value;
            // Call Notify with String.Empty for force all properties
            // to update. Otherwise update to properties like
            // UserPrincipalName and IssuerCN won't be noticed.
            NotifyPropertyChanged(string.Empty);
        }
    }

    /// <summary>
    ///     Returns the Certificate key algorithm.
    ///     Can be empty if slot does not contain a
    ///     certificate.
    /// </summary>
    public string CertificateKeyAlgorithm => GetAlgorithmAndKeySize().algorithm;

    public string CertificateKeyInfo
    {
        get
        {
            if (CertificateKeySize != 0)
                return $"{CertificateKeyAlgorithm} {CertificateKeySize.ToString()}";
            else
                return string.Empty;
        }
    }    
    
    /// <summary>
    ///     Returns the certificate key size.
    /// </summary>
    public int CertificateKeySize => GetAlgorithmAndKeySize().keySize;            

    /// <summary>
    ///     Returns the Issuer Common Name of
    ///     the certicicate.
    ///     Can be empty is slot does not contain a
    ///     certificate.
    /// </summary>
    public string IssuerCN => Certificate == null ? string.Empty : Certificate.Issuer.Split(',')[0].Replace("CN=", "");

    /// <summary>
    ///     Returns a stripped rendition of the certificates
    ///     subject. Everything after the first "," delimiter
    ///     will be stripped.
    /// </summary>
    public string StrippedCN => Certificate == null ? string.Empty : Certificate.Subject.Split(',')[0];

    /// <summary>
    ///     Returns the User Principal name from
    ///     the certificate.
    ///     Can be empty is slot does not contain a
    ///     certificate.    
    /// </summary>
    public string UserPrincipalName =>
        Certificate == null ? string.Empty : Certificate.GetNameInfo(X509NameType.UpnName, false);

    /// <summary>
    ///     Returns the number of days left until
    ///     the certificate expires.
    ///     Can be empty if slot does not contain a
    ///     certificate.
    /// </summary>
    public int? ValidDaysLeft => Certificate == null ? null : Certificate.NotAfter.Subtract(DateTime.Now).Days;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}