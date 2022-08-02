using System.Text;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;
using YKEnroll.Win.Views;
using YKEnroll.Win.Views.Windows;
using Yubico.YubiKey;

namespace YKEnroll.Win.ViewModels;

internal class KeyCollectorViewModel : Presenter
{
    private string _purpose = string.Empty;

    private bool _useDefault;
    
    private readonly KeyCollectorResult keyCollectorResult;
    private KeyEntryData keyEntryData;

    public KeyCollectorViewModel(KeyEntryData keyEntryData, KeyCollectorResult keyCollectorResult)
    {
        this.keyEntryData = keyEntryData;
        this.keyCollectorResult = keyCollectorResult;
        this.keyCollectorResult.Cancelled = true; // Set this to false here in case Window closes, will be set to true if Ok is clicked.
        IsRetry = keyEntryData.IsRetry;
        RetriesRemaining = keyEntryData.RetriesRemaining;
        Purpose = keyEntryData.Request.ToString();
    }

    public bool UseDefault
    {
        get => _useDefault;
        set => Update(ref _useDefault, value);
    }

    public bool IsRetry { get; set; }
    public ICommand OkCommand => new Command(_ => Ok(_ as Window ?? null));
    public ICommand CancelCommand => new Command(_ => Cancel(_ as Window ?? null));
    public int? RetriesRemaining { get; set; }

    public string Purpose
    {
        get => _purpose;
        private set => Update(ref _purpose, value);
    }

    private void Ok(Window? window)
    {
        if(window != null)
        {
            var v = (KeyCollectorView)window;
            keyCollectorResult.Cancelled = false;
            keyCollectorResult.UseDefault = UseDefault;
            /// Sensitive data here!
            keyCollectorResult.CurrentValue = Encoding.ASCII.GetBytes(v.PasswordBox1.Password);
            keyCollectorResult.NewValue = Encoding.ASCII.GetBytes(v.PasswordBox2.Password);
            /// Sensitive data here!
            window.Close();
        }        
    }

    private void Cancel(Window? window)
    {
        keyCollectorResult.Cancelled = true;
        if(window != null)
            window.Close();
    }
}