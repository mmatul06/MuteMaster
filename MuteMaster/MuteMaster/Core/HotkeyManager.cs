using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace MuteMaster.Core
{
    public static class HotkeyManager
    {
        private static readonly Dictionary<string, EventHandler<HotkeyEventArgs>> _handlers = new();

        public static bool Register(string name, string hotkeyString, Action onTriggered)
        {
            if (string.IsNullOrEmpty(hotkeyString)) return false;

            try
            {
                Unregister(name);

                if (!TryParse(hotkeyString, out Key key, out ModifierKeys modifiers))
                    return false;

                EventHandler<HotkeyEventArgs> handler = (s, e) =>
                {
                    onTriggered();
                    e.Handled = true;
                };

                _handlers[name] = handler;
                NHotkey.Wpf.HotkeyManager.Current.AddOrReplace(name, key, modifiers, handler);
                return true;
            }
            catch { return false; }
        }

        public static void Unregister(string name)
        {
            try
            {
                NHotkey.Wpf.HotkeyManager.Current.Remove(name);
                _handlers.Remove(name);
            }
            catch { }
        }

        public static void UnregisterAll()
        {
            foreach (var name in new List<string>(_handlers.Keys))
                Unregister(name);
        }

        // ── Hotkey string parsing ─────────────────────────────────────────
        // Format: "Ctrl+Shift+M" or "F8" or "Alt+F4"

        public static bool TryParse(string hotkeyString, out Key key, out ModifierKeys modifiers)
        {
            key = Key.None;
            modifiers = ModifierKeys.None;

            if (string.IsNullOrWhiteSpace(hotkeyString)) return false;

            var parts = hotkeyString.Split('+');
            string keyPart = parts[^1].Trim();

            foreach (var part in parts[..^1])
            {
                switch (part.Trim().ToLower())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= ModifierKeys.Control; break;
                    case "shift":
                        modifiers |= ModifierKeys.Shift; break;
                    case "alt":
                        modifiers |= ModifierKeys.Alt; break;
                    case "win":
                    case "windows":
                        modifiers |= ModifierKeys.Windows; break;
                }
            }

            if (Enum.TryParse<Key>(keyPart, true, out key))
                return key != Key.None;

            return false;
        }

        public static string FormatHotkey(Key key, ModifierKeys modifiers)
        {
            var parts = new List<string>();
            if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            parts.Add(key.ToString());
            return string.Join("+", parts);
        }
    }
}
