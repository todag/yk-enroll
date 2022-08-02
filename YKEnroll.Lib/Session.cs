using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Yubico.YubiKey;
using Yubico.YubiKey.Piv;

namespace YKEnroll.Lib;

/// <summary>
///     This class will hold PivSession object and ensure it
///     is disposed of when done.
///     
///     *Any* operation requiring a PivSession should
///     use an instance of this object in a "using" statement.
///     Multiple operations can be chained in the same "using"
///     statement to keep the authentication active over
///     multiple actions. 
///     
///     E.g.:
///     using(_yubiKey.NewSession(new KeyCollectorPrompt())
///     {
///         GenerateCertificateRequest();
///         ImportCertificate();
///     }
///     
///     The class also holds a Status object to be able
///     to report back progress to a potential UI. The
///     Status object will be set to Status.Busy then the
///     Connect() method is called. And automatically
///     be set to Status.Busy=False then the Session
///     object is disposed of.
///     
/// </summary>
public class Session : IDisposable, INotifyPropertyChanged
{
    private readonly IYubiKeyDevice device;

    public Session(IYubiKeyDevice device, Status status) : this(device)
    {
        Status = status;
    }

    public Session(IYubiKeyDevice device)
    {
        this.device = device;
    }

    public PivSession? PivSession { get; private set; }
    public Status Status { get; private set; } = new();

    public void Dispose()
    {
        if(PivSession != null)
        {
            PivSession.Dispose();
            PivSession = null;
        }
        Status.Stopped();
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Connect(IKeyCollectorPrompt prompt)
    {
        try
        {
            Status.Started();
            PivSession = new PivSession(device);
            PivSession.KeyCollector = new KeyCollector(prompt).KeyCollectorDelegate;
        }
        catch
        {
            throw;
        }
        finally
        {
            Status.Stopped();
        }        
    }  
    

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}