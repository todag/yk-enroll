using System.Runtime.CompilerServices;

namespace YKEnroll.Lib;

public static class Logger
{        
    private static Object Lock = new Object();

    private static List<string> history = new List<string>();
    public static List<string> History
    {
        get
        {
            return new List<string>(history);
        }
    }

    public static void Log(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        string logFilePath = Settings.LogFilePath;
        message = $"[{DateTime.Now.ToString()}] [{Path.GetFileName(callerFilePath)}][{callerLineNumber}][{callerName}]: {message}";            
        lock (Lock)
        {
            history.Add(message);
            try
            {
                File.AppendAllText(logFilePath, $"{message}{Environment.NewLine}");
            }
            catch(Exception ex)
            {
                history.Add($"[{DateTime.Now.ToString()}] [{Path.GetFileName(callerFilePath)}][{callerLineNumber}][{callerName}]: Failed to write to log file! Exception: {ex.Message}");
            }
            
        }            
    }        
}
