using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Yubico.YubiKey;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Sample.PivSampleCode;

namespace YKEnroll.Lib;

public class EnrollmentManager : INotifyPropertyChanged
{
    private List<ICertServer> _certServers = new();
    private List<YubiKey> _yubiKeys = new();
    public Status Status { get; set; } = new();
    
    /// <summary>
    ///     Holds a list of all available/connected YubiKeys.
    /// </summary>
    public List<YubiKey> YubiKeys
    {
        get { return _yubiKeys; }
        set { _yubiKeys = value; NotifyPropertyChanged(); }
    }

    /// <summary>
    ///     Holds a list of all available CA servers.
    /// </summary>
    public List<ICertServer> CertServers
    {
        get { return _certServers; }
        private set { _certServers = value; NotifyPropertyChanged(); }
    }    
    
    /// <summary>
    ///     Load/Refresh the list of connected YubiKeys.
    /// </summary>
    public void GetDevices()
    {        
        lock(Status)
        {
            try
            {

                Status.Started("Loading devices...");
                var yubiKeys = new List<YubiKey>();
                foreach (YubiKeyDevice yubiKey in YubiKeyDevice.FindByTransport(Transport.SmartCard)) //Transport.YubiKey
                {
                    Logger.Log($"Found Yubikey: " +
                        $" Formfactor: [{yubiKey.FormFactor}]" +
                        $" Serial: [{yubiKey.SerialNumber}]" +
                        $" USB Capabilities: [{yubiKey.AvailableUsbCapabilities}]" +
                        $" NFC Capabilities [{yubiKey.AvailableNfcCapabilities}]" +
                        $"");
                    var x = yubiKey.SerialNumber;

                    if (yubiKey.SerialNumber == null && Settings.HideIncompleteDevices)
                    {
                        Logger.Log("Skipped adding device because (Settings.HideIncompleteDevices = True) and no SerialNumber found on device.");
                    }
                    else
                    {
                        Logger.Log("Added device to device list.");
                        yubiKeys.Add(new YubiKey(yubiKey));
                    }

                }
                YubiKeys = yubiKeys;
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
       
    }

    /// <summary>
    ///     Load/Refresh the list of available CA servers.
    /// </summary>
    public void GetCertServers()
    {
        lock (Status)
        {
            try
            {
                Status.Started("Loading CA servers...");
                CertServers = ADManager.GetCertServers();
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
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}