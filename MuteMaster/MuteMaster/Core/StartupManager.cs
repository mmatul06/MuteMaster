using Microsoft.Win32;
using System;
using System.Reflection;

namespace MuteMaster.Core
{
    public static class StartupManager
    {
        private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MuteMaster";

        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch { return false; }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                        key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
            catch { }
        }
    }
}
