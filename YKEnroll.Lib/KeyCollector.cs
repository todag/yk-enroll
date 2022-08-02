using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKey;

namespace YKEnroll.Lib;

/// <summary>
///     This class provides a KeyCollector delegate to the PivSession class from Yubico.
///     It tries to keep codes as secure as possible, but there is always a risk that
///     the codes linger in clear text for a while.
///     
///     A potential UI or other interface must implement the IKeyCollector interface
///     to interact with this KeyCollector.
/// </summary>
public class KeyCollector
{
    private const string DefaultMgmtKeyString = "010203040506070801020304050607080102030405060708";
    private const string DefaultPinString = "123456";
    private const string DefaultPukString = "12345678";
    private IKeyCollectorPrompt prompt;
    public KeyCollector(IKeyCollectorPrompt prompt)
    {
        this.prompt = prompt;
    }
    
    // This method is called when the PivSession needs to authenticate
    // Using the IKeyCollectorPrompt interface this can be propagated
    // to the UI to allow prompting for user input.
    public bool KeyCollectorDelegate(KeyEntryData keyEntryData)
    {
        if (keyEntryData is null) return false;

        byte[] currentValue = Array.Empty<byte>();
        byte[] newValue = Array.Empty<byte>();

        KeyCollectorResult? result = null; ;

        try
        {
        
            if (keyEntryData.Request == KeyEntryRequest.Release)
            {
                return true;
            }

            result = prompt.Prompt(keyEntryData);
            if (result.Cancelled)
                return false;

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.VerifyPivPin:
                    currentValue = result.UseDefault ? StringToPinPuk(DefaultPinString) : result.CurrentValue;
                    break;

                case KeyEntryRequest.ChangePivPin:
                    currentValue = result.UseDefault ? StringToPinPuk(DefaultPinString) : result.CurrentValue;
                    newValue = result.NewValue;
                    break;

                case KeyEntryRequest.ChangePivPuk:
                    currentValue = result.UseDefault ? StringToPinPuk(DefaultPukString) : result.CurrentValue;
                    newValue = result.NewValue;
                    break;

                case KeyEntryRequest.ResetPivPinWithPuk:
                    currentValue = result.UseDefault ? StringToPinPuk(DefaultPukString) : result.CurrentValue;
                    newValue = result.NewValue;
                    break;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    currentValue = result.UseDefault
                        ? HexStringToManagementKey(DefaultMgmtKeyString)
                        : HexByteToManagementKey(result.CurrentValue);
                    break;

                case KeyEntryRequest.ChangePivManagementKey:
                    currentValue = result.UseDefault
                        ? HexStringToManagementKey(DefaultMgmtKeyString)
                        : HexByteToManagementKey(result.CurrentValue);
                    newValue = HexByteToManagementKey(result.NewValue);
                    break;
            }

            if (newValue is null || newValue.Length == 0)
                keyEntryData.SubmitValue(currentValue);
            else
                keyEntryData.SubmitValues(currentValue, newValue);

            return true;
        }
        catch
        {
        }
        finally
        {            
            CryptographicOperations.ZeroMemory(currentValue);
            CryptographicOperations.ZeroMemory(newValue);
            if(result != null)
            {
                CryptographicOperations.ZeroMemory(result.CurrentValue);
                CryptographicOperations.ZeroMemory(result.NewValue);
            }            
        }
        return true;
    }

    /// <summary>
    ///     Returns a byte encoded Management Key from a 48 character hex encoded string.
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    private static byte[] HexStringToManagementKey(string hex)
    {
        var b = new byte[24];
        var indexV = 0;
        var indexB = 0;

        while (indexV < hex.Length)
        {
            var a = hex[indexV] + hex[indexV + 1].ToString();
            b[indexB] = Convert.ToByte(a, 16);
            indexB++;
            indexV = indexV + 2;
        }

        var x = Encoding.ASCII.GetBytes(hex);
        return b;
    }

    /// <summary>
    ///     Returns a byte encoded Management Key from a Hex byte encoded byte array.
    /// </summary>
    /// <param name="mgmtKey"></param>
    /// <returns></returns>
    private static byte[] HexByteToManagementKey(byte[] mgmtKey)
    {
        var b = new byte[24];
        var indexV = 0;
        var indexB = 0;

        while (indexV < 48)
        {
            b[indexB] = Convert.ToByte(Encoding.ASCII.GetString(mgmtKey, indexV, 2), 16);
            indexB++;
            indexV = indexV + 2;
        }

        CryptographicOperations.ZeroMemory(mgmtKey);
        return b;
    }

    /// <summary>
    ///     Returns a byte array representation of the string str
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static byte[] StringToPinPuk(string str)
    {
        var bytes = Encoding.ASCII.GetBytes(str);
        return bytes;
    }
}
