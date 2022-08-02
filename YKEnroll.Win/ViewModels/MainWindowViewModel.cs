using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;
using YKEnroll.Win.Views.Windows;
using Microsoft.Win32;
using System.Reflection;

namespace YKEnroll.Win.ViewModels;

internal class MainViewModel : Presenter
{    
    private YubiKey? _selectedDevice;

    public MainViewModel()
    {
        try
        {
            Settings.LoadSettings();
        }
        catch (Exception ex)
        {
            ShowMessage.Error("Failed to load settings!", ex);
        }
        
        GetDevices();
        GetCAServers();
    }

    public EnrollmentManager EnrollmentManager { get; } = new();

    public ICommand ExitCommand => new Command(_ => ExitApplication());
    public ICommand GetDevicesCommand => new Command(_ => GetDevices());
    public ICommand GetCAServersCommand => new Command(_ => GetCAServers());
    public ICommand ChangeCodesCommand => new Command(_ => ChangeCodes());
    public ICommand ShowCertificateCommand => new Command(_ => ShowCertificate());
    public ICommand ShowLogCommand => new Command(_ => ShowLog());
    public ICommand RequestCommand => new Command(_ => Request());
    public ICommand CreateCsrCommand => new Command(_ => CreateCsr());
    public ICommand RetrieveCommand => new Command(_ => Retrieve());
    public ICommand AboutCommand => new Command(_ => About());
    public ICommand EnrollCommand => new Command(_ => Enroll());
    public ICommand ApplicationsCommand => new Command(_ => Applications());
    public ICommand ImportCommand => new Command(_ => Import());
    public ICommand ResetPivCommand => new Command(_ => ResetPiv());

    public YubiKey? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            Update(ref _selectedDevice, value);
            LoadSlotData();
        }
    }
    
    public Slot? SelectedSlot { get; set; }

    private async void GetDevices()
    {
        try
        {
            await Task.Run(() => EnrollmentManager.GetDevices());

            if (EnrollmentManager.YubiKeys.Count == 0)
                ShowMessage.Warning("No compatible smart cards found!");
            
        }
        catch (Exception ex)
        {
            ShowMessage.Error("Failed to load devices!", ex);
        }
    }

    private async void GetCAServers()
    {
        try
        {
            await Task.Run(() => EnrollmentManager.GetCAServers());
        }
        catch (Exception ex)
        {
            ShowMessage.Error("Failed to load CA servers!", ex);
        }
    }

    private async void ResetPiv()
    {
        try
        {
            var result = ShowMessage.Dialog(
                "All keys and certificates on the PIV application will be lost!\nAre you sure?",
                "Reset PIV?",
                DialogButtons.YesCancel,
                DialogIcon.Warning
                );                
            if (result == DialogResult.Yes)
                await Task.Run(() =>
                {
                    using (SelectedDevice!.NewSession(new KeyCollectorPrompt()))
                    {
                        SelectedDevice.ResetPiv();
                    }
                });
        }
        catch (Exception ex)
        {
            ShowMessage.Error("An error occured while trying to reset the PIV application.", ex);
        }
    }

    private void ShowLog()
    {
        var win = new LogHistoryView { DataContext = new LogHistoryViewModel() };
        win.ShowDialog();
    }

    private async void Import()
    {                
        try
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Certificate file (*.cer, *.crt, *.pem)|*.cer|*.crt|*.pem";
            if (openFileDialog.ShowDialog() != true) return;

            var result = ShowMessage.Dialog($"Import certificate? This will overwrite any existing certificates in slot " +
                $"{SelectedSlot!.Name}" + $"\nAre you sure?", "Import certificate", DialogButtons.YesCancel);
                                
            if (result == DialogResult.Yes)
            {
                X509Certificate2 cert = new X509Certificate2(File.ReadAllBytes(openFileDialog.FileName));
                await Task.Run(() => {
                    using(this.SelectedDevice!.NewSession(new KeyCollectorPrompt()))
                    {
                        this.SelectedDevice.ImportCertificate(this.SelectedSlot, cert, true);
                    }                    
                    });                    
                ShowMessage.Info("Certificate imported successfully!");
            }            
        }
        catch (Exception ex)
        {
            ShowMessage.Error("Certificate import failed!", ex);
        }
    }
    
    private void Retrieve()
    {
        var win = new RetrieveView { DataContext = new RetrieveViewModel(EnrollmentManager, SelectedDevice!, SelectedSlot!) };
        win.ShowDialog();
    }

    private void About()
    {
        string winBuildInfo = "";
        string libBuildInfo = "";
        string copyright = "";
        try
        {
            Assembly win = Assembly.GetExecutingAssembly();
            using (Stream stream = win.GetManifestResourceStream("YKEnroll.Win.buildinfo.txt")!)
            using (StreamReader reader = new StreamReader(stream))
            {
                winBuildInfo = reader.ReadToEnd();
            }

            object[] attribs = win.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            if (attribs.Length > 0)
            {
                copyright = ((AssemblyCopyrightAttribute)attribs[0]).Copyright;
            }

            Assembly lib = typeof(EnrollmentManager).Assembly;
            using (Stream stream = lib.GetManifestResourceStream("YKEnroll.Lib.buildinfo.txt")!)
            using (StreamReader reader = new StreamReader(stream))
            {
                libBuildInfo = reader.ReadToEnd();
            }
        }
        catch { }
        
        string winV = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        string libV = typeof(EnrollmentManager).Assembly.GetName().Version!.ToString();
        string msg = $"YK-Enroll\n" +
            $"{copyright}\n" +            
            $"\n" +
            $"\n" +            
            $"YKEnroll.Win version {winV} buildinfo: {(string.IsNullOrWhiteSpace(winBuildInfo) ? "<no data>" : winBuildInfo)}\n" +
            $"YKEnroll.Lib version {libV} buildinfo: {(string.IsNullOrWhiteSpace(libBuildInfo) ? "<no data>" : libBuildInfo)}\n\n" +
            $"Attributions:\nIcons from https://icons8.com & https://materialdesignicons.com/";
        
        ShowMessage.Info(msg);        
    }

    private void Enroll()
    {
        var win = new EnrollView { DataContext = new EnrollViewModel(EnrollmentManager, SelectedDevice!, SelectedSlot!) };
        win.ShowDialog();
    }

    private void Applications()
    {
        var win = new ApplicationsView { DataContext = new ApplicationsViewModel(EnrollmentManager, SelectedDevice!) };
        win.ShowDialog();       
    }

    private void Request()
    {
        var win = new RequestView { DataContext = new RequestViewModel(EnrollmentManager, SelectedDevice!, SelectedSlot!) };
        win.ShowDialog();
    }

    private void CreateCsr()
    {
        var win = new CSRView
            { DataContext = new CSRViewModel(SelectedDevice!, SelectedSlot!) };
        win.ShowDialog();
    }       
    
    private void ShowCertificate()
    {
        try
        {
            var outputFile = Path.GetTempPath() + "cert.cer";
            File.WriteAllBytes(outputFile, SelectedSlot!.Certificate!.Export(X509ContentType.Cert));
            Process.Start(new ProcessStartInfo(outputFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowMessage.Error("An error occured while trying to display the certificate!", ex);
        }
    }

    private void ChangeCodes()
    {
        var win = new CodeView { DataContext = new CodeViewModel(SelectedDevice!) };
        win.ShowDialog();
    }

    /// <summary>
    ///     Loads certificates and slot data from all slots.
    /// </summary>
    public async void LoadSlotData()
    {
        try
        {
            if (SelectedDevice != null)
                await Task.Run(() =>
                {
                    using (SelectedDevice.NewSession(new KeyCollectorPrompt()))
                    {
                        SelectedDevice.LoadCertificates();
                    }
                });
        }
        catch(Exception ex)
        {
            ShowMessage.Error("Failed to load slot data!", ex);
        }
        
    }

    /// <summary>
    ///     Exits the application
    /// </summary>
    public void ExitApplication()
    {
        Application.Current.Shutdown();
    }
}