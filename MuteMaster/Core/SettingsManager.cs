using MuteMaster.Models;
using System;
using System.IO;
using System.Text.Json;

namespace MuteMaster.Core
{
    public static class SettingsManager
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MuteMaster");

        private static readonly string SettingsPath =
            Path.Combine(SettingsDir, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public static AppSettings Current { get; private set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    Current = new AppSettings();
                    Save();
                    return;
                }

                string json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                Current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public static bool ExportTo(string filePath)
        {
            try
            {
                string json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch { return false; }
        }

        public static bool ImportFrom(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var imported = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (imported == null) return false;
                Current = imported;
                Save();
                return true;
            }
            catch { return false; }
        }
    }
}
