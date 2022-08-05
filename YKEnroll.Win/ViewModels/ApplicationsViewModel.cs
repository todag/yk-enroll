using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;

namespace YKEnroll.Win.ViewModels;

internal class ApplicationsViewModel : Presenter
{    
    private readonly string[] availableNfc;
    private readonly string[] availableUsb;
    private readonly string[] currentNfc;
    private readonly string[] currentUsb;

    public ApplicationsViewModel(EnrollmentManager enrollmentManager, YubiKey yubiKey)
    {
        EnrollmentManager = enrollmentManager;
        YubiKey = yubiKey;
        availableUsb = YubiKey.AvailableUsbCapabilities;
        currentUsb = YubiKey.EnabledUsbCapabilities;
        availableNfc = YubiKey.AvailableNfcCapabilities;
        currentNfc = YubiKey.EnabledNfcCapabilities;
        BuildUIElements();        
    }

    public ICommand CancelChangesCommand => new Command(_ => CancelChanges((Window)_!));

    public ICommand CommitChangesCommand => new Command(_ => CommitChanges((Window)_!));

    public bool ChangesCommited { get; private set; } = false;

    public YubiKey YubiKey { get; private set; }

    public EnrollmentManager EnrollmentManager { get; private set; }

    public List<CheckBox> UsbApplications { get; private set; } = new();


    public List<CheckBox> NfcApplications { get; private set; } = new();

    private void BuildUIElements()
    {
        foreach (var s in availableUsb)
            if (s != "None")
                UsbApplications.Add(new CheckBox
                {
                    Content = s,
                    Tag = s,
                    IsChecked = currentUsb.Contains(s) ? true : false
                });
        foreach (var s in availableNfc)
            if (s != "None")
                NfcApplications.Add(new CheckBox
                {
                    Content = s,
                    Tag = s,
                    IsChecked = currentNfc.Contains(s) ? true : false
                });
    }

    private async void CommitChanges(Window window)
    {        
        try
        {
            window.Visibility = Visibility.Hidden;

            // Set Usb capabilities
            List<string> setUsb = new();
            foreach (var chkBox in UsbApplications)
                if (chkBox.IsChecked == true)
                    setUsb.Add(chkBox.Tag.ToString()!);
            if (setUsb.Count == 0)
                setUsb.Add("None");
            // Only set capabilities if the differ from what is currently set on the device
            if (!(availableUsb.Count() == 1 && availableUsb.Contains("None")) &&
                !setUsb.OrderBy(e => e).SequenceEqual(currentUsb.OrderBy(e => e)))
            {
                await Task.Run(() => YubiKey.SetEnabledUsbCapabilities(setUsb));
                ChangesCommited = true;
                ShowMessage.Info("Usb capabilities changed!");
            }

            // Set Nfc capabilities
            List<string> setNfc = new();
            foreach (var chkBox in NfcApplications)
                if (chkBox.IsChecked == true)
                    setNfc.Add(chkBox.Tag.ToString()!);
            if (setNfc.Count == 0)
                setNfc.Add("None");

            // Only set capabilities if the differ from what is currently set on the device
            if (!(availableNfc.Count() == 1 && availableNfc.Contains("None")) &&
                !setNfc.OrderBy(e => e).SequenceEqual(currentNfc.OrderBy(e => e)))
            {
                await Task.Run(() => YubiKey.SetEnabledNfcCapabilities(setNfc));
                ChangesCommited = true;
                ShowMessage.Info("Nfc capabilities changed!");
            }
            // Reload Yubikeys if changes made to applications. Old IYubiKeyDevices in the list might not work properly otherwise.            
            if (ChangesCommited)
            {
                await Task.Run(() => EnrollmentManager.GetDevices());
            }
            window.Close();
        }
        catch (Exception ex)
        {
            ShowMessage.Error("Failed to set usb/nfc capabilities.", ex);
            window.ShowDialog();
        }
    }

    private void CancelChanges(Window window)
    {
        window.Close();
    }
}