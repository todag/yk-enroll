using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YKEnroll.Lib;

/// <summary>
///     This class holds status information.
///     Mainly to be used to report progress
///     and status back to UI.
/// </summary>
public class Status : INotifyPropertyChanged
{    
    private bool _busy;
    private string _text = string.Empty;

    public bool Busy
    {
        get => _busy;
        set
        {
            _busy = value;
            NotifyPropertyChanged();
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            NotifyPropertyChanged();
        }
    }

    public void Started(string text = "")
    {
        Text = text;
        Busy = true;
    }

    public void Stopped()
    {
        Text = string.Empty;
        Busy = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}