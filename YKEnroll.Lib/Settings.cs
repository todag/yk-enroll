using System;
using System.Collections.Generic;
using System.IO;
using Yubico.YubiKey.Piv;

namespace YKEnroll.Lib;

public static class Settings
{
    private static readonly Dictionary<string, string> defaults = new()
    {
        { "DefaultPivAlgorithm", "Rsa2048" },
        { "DefaultPivPinPolicy", "Default" },
        { "DefaultPivTouchPolicy", "Default" },
        { "IncludeTemplateOid", "1.3.6.1.5.5.7.3.2, 1.3.6.1.4.1.311.20.2.2" },
        { "ExcludeTemplateOid", "1.3.6.1.5.5.7.3.1, 1.3.6.1.5.2.3.5" },
        { "IncludeTemplateName", "" },
        { "EnableSlot", "" },
        { "HideIncompleteDevices", "True" },
        { "ResetChuidOnImport", "True" },
        { "CapabilitiesChangeSleepTime", "5" },
        { "LogFilePath", $"{Path.GetTempPath()}\\ykenroll.log.txt" }
    };
   
    private static readonly Dictionary<string, string> settings = new();

    public static bool ResetChuidOnImport => (bool)GetSetting(typeof(bool), "ResetChuidOnImport");

    public static bool HideIncompleteDevices => (bool)GetSetting(typeof(bool), "HideIncompleteDevices");

    public static int CapabilitiesChangeSleepTime => (int)GetSetting(typeof(int), "CapabilitiesChangeSleepTime");

    public static PivAlgorithm DefaultPivAlgorithm =>
        (PivAlgorithm)GetSetting(typeof(PivAlgorithm), "DefaultPivAlgorithm");

    public static PivPinPolicy DefaultPivPinPolicy =>
        (PivPinPolicy)GetSetting(typeof(PivPinPolicy), "DefaultPivPinPolicy");

    public static PivTouchPolicy DefaultPivTouchPolicy =>
        (PivTouchPolicy)GetSetting(typeof(PivTouchPolicy), "DefaultPivTouchPolicy");

    public static string LogFilePath => (string)GetSetting(typeof(string), "LogFilePath");

    public static string[] IncludeTemplateOid => (string[])GetSetting(typeof(string[]), "IncludeTemplateOid");

    public static string[] ExcludeTemplateOid => (string[])GetSetting(typeof(string[]), "ExcludeTemplateOid");

    public static string[] IncludeTemplateName => (string[])GetSetting(typeof(string[]), "IncludeTemplateName");

    public static string[] EnableSlot => (string[])GetSetting(typeof(string[]), "EnableSlot");

    public static void LoadSettings(string path = "")
    {
        if(string.IsNullOrWhiteSpace(path))
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = System.IO.Path.GetDirectoryName(exePath) + "\\ykenroll.config.txt";
        }
                
        try
        {
            Logger.Log($"Reading config from \"{path}\"");
            foreach (var line in File.ReadAllLines(path))
            {
                if (line == string.Empty || !line.Contains('=') || !defaults.ContainsKey(line.Split('=')[0].Trim()))
                {
                    Logger.Log($"Skipping bad line \"{line}\"");
                    continue;
                }                    

                var key = line.Split(new[] { '=' }, 2)[0].Trim();
                var a = key;
                var value = line.Split(new[] { '=' }, 2)[1].Trim();
                var b = value;

                if (defaults.ContainsKey(key))
                {
                    if (!settings.ContainsKey(key))
                        settings.Add(key, value.Trim());
                    else
                        settings[key] = value.Trim();
                    Logger.Log($"Read setting key=\"{key}\" value=\"{value.Trim()}\"");
                }
                else
                {
                    Logger.Log($"Skipping bad line \"{line}\" unknown key!");
                }
            }
        }        
        catch (Exception ex)
        {
            Logger.Log($"Failed to load settings file from {path}, Exception: {ex.Message}");
        }
    }

    public static void SaveSettings()
    {
    }

    private static object GetSetting(Type type, string key)
    {
        if (type.BaseType == typeof(Enum))
        {
            if (settings.ContainsKey(key) && Enum.IsDefined(type, settings[key]))
                return Enum.Parse(type, settings[key], true);
            return Enum.Parse(type, defaults[key], true);
        }

        if (type == typeof(int))
        {
            int i;
            if (settings.ContainsKey(key) && int.TryParse(settings[key], out i))
                return i;
            return Convert.ToInt32(defaults[key]);
        }

        if (type == typeof(string[]))
        {
            if (settings.ContainsKey(key))
            {
                if (!string.IsNullOrEmpty(settings[key]))
                    return Array.ConvertAll(settings[key].Split(','), p => p.Trim());
                return new string[] { };
            }

            if (!string.IsNullOrEmpty(defaults[key]))
                return Array.ConvertAll(defaults[key].Split(','), p => p.Trim());
            return new string[] { };
        }

        if (type == typeof(bool))
        {
            bool b;
            if (settings.ContainsKey(key) && bool.TryParse(settings[key], out b))
                return b;
            return Convert.ToBoolean(defaults[key]);
        }

        if (type == typeof(string))
        {
            if (settings.ContainsKey(key))
                return settings[key];
            return defaults[key];
        }

        throw new NotSupportedException("Cannot parse settings type.");
    }
}