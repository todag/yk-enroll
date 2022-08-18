using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKey;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.Sample.PivSampleCode;

namespace YKEnroll.Lib;

/// <summary>
///     This class contains and interacts with a YubiKey
///     through the Yubico YubiKey SDK.
/// </summary>
public class YubiKey : INotifyPropertyChanged
{
    private IYubiKeyDevice IYubiKeyDevice;
    private Session? _session;

    private List<PivAlgorithm> _pivAlgorithms = new List<PivAlgorithm>()
    { 
        PivAlgorithm.Rsa2048,
        PivAlgorithm.EccP256,
        PivAlgorithm.EccP384 
    };

    private List<PivPinPolicy> _pivPinPolicies = new List<PivPinPolicy>()
    {
        PivPinPolicy.Default,
        PivPinPolicy.Always,
        PivPinPolicy.Once,
        PivPinPolicy.Never
    };

    private List<PivTouchPolicy> _pivTouchPolicies = new List<PivTouchPolicy>()
    {
        PivTouchPolicy.Default,
        PivTouchPolicy.Cached,
        PivTouchPolicy.Always,
        PivTouchPolicy.Never
    };

    public YubiKey(IYubiKeyDevice yubiKeyDevice)
    {
        IYubiKeyDevice = yubiKeyDevice;
        Slots = new List<Slot>();
        foreach (var (Name, SlotNumber, DataTag) in SlotDefinitions.AllAvailableSlots)
        {
            if (Settings.EnableSlot.Length > 0 && !Settings.EnableSlot.Contains(SlotNumber.ToString("X2")))
                // Filtered out because slot is not in Settings.EnableSlot
                continue;
            var y = Settings.EnableSlot;
            Slots.Add(new Slot(Name, SlotNumber, DataTag));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region functions

    public bool ChangePin()
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Changing Pin...");
        return PivSession.TryChangePin();
    }

    public bool ChangePuk()
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Changing Puk...");
        return PivSession.TryChangePuk();
    }

    public bool ChangeManagementKey()
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Changing management key...");
        return PivSession.TryChangeManagementKey();
    }

    public string[] AvailableUsbCapabilities => RenderCapabilities(IYubiKeyDevice.AvailableUsbCapabilities);

    public string[] EnabledUsbCapabilities => RenderCapabilities(IYubiKeyDevice.EnabledUsbCapabilities);        

    public string[] AvailableNfcCapabilities => RenderCapabilities(IYubiKeyDevice.AvailableNfcCapabilities);

    public string[] EnabledNfcCapabilities => RenderCapabilities(IYubiKeyDevice.EnabledNfcCapabilities);

    /// <summary>
    /// The SDK seems to report YubiKeyCapabilities.All for some devices,
    /// since we cannot work with just this value, render this to a string
    /// array containing all capabilities except YubiKeyCapabilities.None
    /// and YubiKeyCapabilities.All
    /// </summary>
    /// <param name="capabilities"></param>
    /// <returns>List of available or enabled capabilities.</returns>
    private string[] RenderCapabilities(YubiKeyCapabilities capabilities)
    {
        if (capabilities == YubiKeyCapabilities.All)
        {
            return Enum.GetValues(typeof(YubiKeyCapabilities)).Cast<YubiKeyCapabilities>().Select(v => v.ToString()).Where(v => !v.Equals("All")).Where(v => !v.Equals("None")).ToArray();
        }
        else
        {
            return Array.ConvertAll(capabilities.ToString().Split(','), s => s.Trim());
        }
    }

    public byte[] GenerateCertificateSigningRequest(Slot slot, AsymmetricAlgorithm publicKey,
        string subject = "", string upn = "", string sid = "")
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Generating csr...");
        var csr = CertHelper.CreateCertificateRequest(publicKey, subject, upn, sid);
        var signer = new YubiKeySignatureGenerator(PivSession, slot.SlotNumber, KeyConverter.GetPivPublicKeyFromDotNet(publicKey));
        var requestDer = csr.CreateSigningRequest(signer);
        return requestDer;
    }    

    public AsymmetricAlgorithm GenerateKeyPair(Slot slot, PivAlgorithm algorithm,
        PivPinPolicy pinPolicy, PivTouchPolicy touchPolicy)
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Generating keypair...");
        var publicKey = PivSession.GenerateKeyPair(slot.SlotNumber, algorithm, pinPolicy, touchPolicy);
        return KeyConverter.GetDotNetFromPivPublicKey(publicKey);
    }

    public void ImportCertificate(Slot slot, X509Certificate2 certificate, bool resetChuid = true)
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Importing certificate...");
        PivSession.ImportCertificate(slot.SlotNumber, certificate);
        LoadCertificates(slot);
        if (resetChuid) ResetChuid();
    }
        
    public AttestationStatement Attest(Slot slot)
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started($"Generating attestation for slot {slot.Name}");        
        var attestationCert = PivSession.GetAttestationCertificate();
        var statementCert = PivSession.CreateAttestationStatement(slot.SlotNumber);
        slot.AttestationStatement = new(statementCert, attestationCert);        
        return slot.AttestationStatement;
    }   

    public void LoadCertificates(Slot? slot = null)
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        List<Slot> slots;
        if (slot != null)
            slots = new() { slot };
        else
            slots = Slots;
        // Load all slot data
        foreach (var s in slots)
        {            
            try
            {
                Session.Status.Started($"Loading data from slot {s.Name.PadRight(20, '.')}");                
                var cmd = new GetDataCommand(s.DataTag);
                var response = PivSession.Connection.SendCommand(cmd);

                if (response.Status == ResponseStatus.Success)
                {
                    try
                    {
                        s.Data = response.GetData();
                        s.Certificate = PivSession.GetCertificate(s.SlotNumber);
                        s.SlotStatus = SlotStatus.Success;
                        s.SlotStatusMessage = "Slot contains certificate.";
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to load slot data from slot \"{s.Name}\" Exception: \"{ex.Message}\"");
                        s.SlotStatus = SlotStatus.Warning;
                        s.SlotStatusMessage = $"Unable to render slot data to certificate! Exception: {ex.Message}";
                    }
                }
                else if (response.Status == ResponseStatus.NoData)
                {
                    s.SlotStatus = SlotStatus.NoData;
                    s.SlotStatusMessage = "Slot contains no data.";
                }
                else
                {
                    s.SlotStatus = SlotStatus.Error;
                    s.SlotStatusMessage = response.StatusMessage;
                }
            }
            catch(Exception ex)
            {
                s.SlotStatus = SlotStatus.Error;
                s.SlotStatusMessage = $"Failed to read slot: {ex.Message}";
            }
        }
    }

    private void ResetChuid()
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Resetting chuid...");
        var chuid = new CardholderUniqueId();
        chuid.SetRandomGuid();
        PivSession.WriteObject(chuid);
    }

    public void ResetPiv()
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Resetting Piv application...");
        PivSession.ResetApplication();        
    }

    public void SetEnabledUsbCapabilities(List<string> capabilities)
    {
        // Since this does not make use of the PivSession from NewSession() the Session.Dispose()
        // call will not set Status.Stopped(). So wrap it in a try/catch/finally so Status.Stopped()
        // is always called.            
        try
        {
            Session.Status.Started("Setting enabled Usb capabilities...");
            var cap = YubiKeyCapabilities.None;
            foreach (var str in capabilities)
                cap = cap | (YubiKeyCapabilities)Enum.Parse(typeof(YubiKeyCapabilities), str);
            IYubiKeyDevice.SetEnabledUsbCapabilities(cap);
            Session.Status.Started($"Setting enabled Usb capabilities...Done. Sleeping set time ({Settings.CapabilitiesChangeSleepTime}) to allow device reboot and redetection...");
            Thread.Sleep(Settings.CapabilitiesChangeSleepTime * 1000);
            // Reload this IYubikeyDevice after device reboot
            this.IYubiKeyDevice = YubiKeyDevice.FindAll().Where(d => d.SerialNumber == this.SerialNumber).First();
        }
        finally
        {
            Session.Status.Stopped();
        }        
    }

    public void SetEnabledNfcCapabilities(List<string> capabilities)
    {
        // Since this does not make use of the PivSession from NewSession() the Session.Dispose()
        // call will not set Status.Stopped(). So wrap it in a try/catch/finally so Status.Stopped()
        // is always called.            
        try
        {
            Session.Status.Started("Setting enabled Nfc capabilities...");
            var cap = YubiKeyCapabilities.None;
            foreach (var str in capabilities)
                cap = cap | (YubiKeyCapabilities)Enum.Parse(typeof(YubiKeyCapabilities), str);
            IYubiKeyDevice.SetEnabledNfcCapabilities(cap);
            Session.Status.Started($"Setting enabled Nfc capabilities...Done. Sleeping set time ({Settings.CapabilitiesChangeSleepTime}) to allow device reboot and redetection...");
            Thread.Sleep(Settings.CapabilitiesChangeSleepTime * 1000);
            // Reload this IYubikeyDevice after device reboot
            this.IYubiKeyDevice = YubiKeyDevice.FindAll().Where(d => d.SerialNumber == this.SerialNumber).First();
        }
        finally
        {
            Session.Status.Stopped();
        }        
    }

    public bool UnblockPin()
    {
        if (PivSession == null)
            throw new NullReferenceException("The YubiKey PivSession must be initialized with the NewSession method!");
        Session.Status.Started("Unblocking Pin...");
        return PivSession.TryResetPin();
    }

    #endregion

    #region properties
    public Session Session
    {
        get
        {
            if(_session == null)
            {
                _session = new Session(IYubiKeyDevice);
            }
            return _session;
        }
        private set
        {
            _session = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc />
    public Session NewSession(IKeyCollectorPrompt prompt, Status? status = null)
    {
        if (status != null)
            Session = new Session(IYubiKeyDevice, status);
        else
            Session = new Session(IYubiKeyDevice);
        Session.Connect(prompt);
        return Session;
    }    

    private PivSession? PivSession => Session.PivSession;

    public List<Slot> Slots { get; set; }
    public string FirmwareVersion => IYubiKeyDevice.FirmwareVersion.ToString();
    public int? SerialNumber => IYubiKeyDevice.SerialNumber;    

    public string FormFactor
    {
        get
        {
            return IYubiKeyDevice.FormFactor.ToString() switch
            {
                "UsbAKeychain"          => "USB A Keychain",
                "UsbANano"              => "USB A Nano",
                "UsbCKeychain"          => "USB C Keychain",
                "UsbCNano"              => "USB C Nano",
                "UsbCLightning"         => "USB C Lightning",
                "UsbABiometricKeychain" => "USB A Biometric Keychain",
                "UsbCBiometricKeychain" => "USB C Biometric Keychain",
                _ => IYubiKeyDevice.FormFactor.ToString(),
            };
        }
    }

    public bool ConfigurationLocked => IYubiKeyDevice.ConfigurationLocked;

    public int AutoEjectTimeout => IYubiKeyDevice.AutoEjectTimeout;

    public CardholderUniqueId CardholderUniqueId { get; } = new();

    /// <summary>
    ///     Returns list of this Smart cards supported Piv Algorithms
    /// </summary>
    public List<PivAlgorithm> PivAlgorithms => _pivAlgorithms;

    /// <summary>
    ///     Returns list of this Smart cards supported Piv Pin Policies
    /// </summary>
    public List<PivPinPolicy> PivPinPolicies => _pivPinPolicies;

    /// <summary>
    ///     Returns list of this Smart cards supported Piv Touch Policies
    /// </summary>
    public List<PivTouchPolicy> PivTouchPolicies => _pivTouchPolicies;        

    #endregion
}