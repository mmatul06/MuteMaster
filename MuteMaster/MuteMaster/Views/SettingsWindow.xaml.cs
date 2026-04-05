using MuteMaster.Core;
using MuteMaster.Models;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MuteMaster.Views
{
    public partial class SettingsWindow : Window
    {
        private bool _loading = true;
        private TextBox? _activeHotkeyBox;

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadDevices();
            LoadSettings();
            ApplyTheme();
            _loading = false;
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SettingsManager.Current.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
            }
        }

        // ── Load devices into combos ──────────────────────────────────────

        private void LoadDevices()
        {
            var inputs = AudioManager.GetInputDevices();
            InputDeviceCombo.ItemsSource = inputs;
            InputDeviceCombo.DisplayMemberPath = "Name";
            InputDeviceCombo.SelectedItem = inputs.Find(d => d.Id == SettingsManager.Current.InputDeviceId)
                ?? (inputs.Count > 0 ? inputs[0] : null);

            var outputs = AudioManager.GetOutputDevices();
            OutputDeviceCombo.ItemsSource = outputs;
            OutputDeviceCombo.DisplayMemberPath = "Name";
            OutputDeviceCombo.SelectedItem = outputs.Find(d => d.Id == SettingsManager.Current.OutputDeviceId)
                ?? (outputs.Count > 0 ? outputs[0] : null);
        }

        // ── Load all settings into controls ──────────────────────────────

        private void LoadSettings()
        {
            var s = SettingsManager.Current;

            PushToTalkToggle.IsChecked = s.PushToTalkEnabled;
            ToggleSoundToggle.IsChecked = s.ToggleSoundEnabled;
            MuteSoundLabel.Text = string.IsNullOrEmpty(s.CustomMuteSound) ? "Default" :
                System.IO.Path.GetFileName(s.CustomMuteSound);
            UnmuteSoundLabel.Text = string.IsNullOrEmpty(s.CustomUnmuteSound) ? "Default" :
                System.IO.Path.GetFileName(s.CustomUnmuteSound);

            HotkeyMicBox.Text = s.HotKeyMuteMic;
            HotkeyOutputBox.Text = s.HotKeyMuteOutput;
            HotkeyOverlayBox.Text = s.HotKeyToggleOverlay;
            HotkeyPTTBox.Text = s.HotKeyPushToTalk;

            SizeSlider.Value = s.OverlaySize;
            SizeLabel.Text = ((int)s.OverlaySize).ToString();
            TransparencySlider.Value = s.OverlayTransparency * 100;
            TransparencyLabel.Text = $"{(int)(s.OverlayTransparency * 100)}%";

            LevelBarsToggle.IsChecked = s.OverlayLevelBarsEnabled;
            AutoHideToggle.IsChecked = s.OverlayAutoHide;
            ClickThroughToggle.IsChecked = s.OverlayClickThrough;
            FollowMonitorToggle.IsChecked = s.OverlayFollowCursorMonitor;

            OffsetXSlider.Value = s.OverlayOffsetX;
            OffsetXLabel.Text = $"{(int)s.OverlayOffsetX}px";
            OffsetYSlider.Value = s.OverlayOffsetY;
            OffsetYLabel.Text = $"{(int)s.OverlayOffsetY}px";

            UpdateCornerButtons(s.OverlayCorner);

            AutostartToggle.IsChecked = s.AutostartEnabled;
            HighPriorityToggle.IsChecked = s.HighPriorityEnabled;
            MinimizeToTrayToggle.IsChecked = s.MinimizeToTray;

            ThemeLight.IsChecked = s.Theme == AppTheme.Light;
            ThemeDark.IsChecked = s.Theme == AppTheme.Dark;
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void InputDevice_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (InputDeviceCombo.SelectedItem is AudioDevice d)
            {
                SettingsManager.Current.InputDeviceId = d.Id;
                SaveAndApply();
            }
        }

        private void OutputDevice_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (OutputDeviceCombo.SelectedItem is AudioDevice d)
            {
                SettingsManager.Current.OutputDeviceId = d.Id;
                SaveAndApply();
            }
        }

        private void PushToTalk_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.PushToTalkEnabled = PushToTalkToggle.IsChecked == true;
            SaveAndApply();
        }

        private void ToggleSound_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.ToggleSoundEnabled = ToggleSoundToggle.IsChecked == true;
            SettingsManager.Save();
        }

        private void BrowseMuteSound_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Audio files|*.wav;*.mp3|All files|*.*", Title = "Select mute sound" };
            if (dlg.ShowDialog() == true)
            {
                SettingsManager.Current.CustomMuteSound = dlg.FileName;
                MuteSoundLabel.Text = System.IO.Path.GetFileName(dlg.FileName);
                SettingsManager.Save();
            }
        }

        private void BrowseUnmuteSound_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Audio files|*.wav;*.mp3|All files|*.*", Title = "Select unmute sound" };
            if (dlg.ShowDialog() == true)
            {
                SettingsManager.Current.CustomUnmuteSound = dlg.FileName;
                UnmuteSoundLabel.Text = System.IO.Path.GetFileName(dlg.FileName);
                SettingsManager.Save();
            }
        }

        private void RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            _loading = true;
            LoadDevices();
            _loading = false;
        }

        // ── Hotkey capture ────────────────────────────────────────────────

        private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _activeHotkeyBox = sender as TextBox;
            if (_activeHotkeyBox != null)
                _activeHotkeyBox.Text = "Press a key…";
        }

        private void HotkeyBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (_activeHotkeyBox == null) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Escape)
            {
                _activeHotkeyBox.Text = "";
                ApplyHotkeyChange(_activeHotkeyBox, "");
                _activeHotkeyBox = null;
                return;
            }

            // Ignore lone modifiers
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin) return;

            var modifiers = Keyboard.Modifiers;
            string hotkey = HotkeyManager.FormatHotkey(key, modifiers);
            _activeHotkeyBox.Text = hotkey;
            ApplyHotkeyChange(_activeHotkeyBox, hotkey);
            _activeHotkeyBox = null;
            Keyboard.ClearFocus();
        }

        private void ApplyHotkeyChange(TextBox box, string value)
        {
            string tag = box.Tag?.ToString() ?? "";
            switch (tag)
            {
                case "HotKeyMuteMic":
                    SettingsManager.Current.HotKeyMuteMic = value; break;
                case "HotKeyMuteOutput":
                    SettingsManager.Current.HotKeyMuteOutput = value; break;
                case "HotKeyToggleOverlay":
                    SettingsManager.Current.HotKeyToggleOverlay = value; break;
                case "HotKeyPushToTalk":
                    SettingsManager.Current.HotKeyPushToTalk = value; break;
            }
            SaveAndApply();
        }

        // ── Overlay sliders ───────────────────────────────────────────────

        private void Size_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            SettingsManager.Current.OverlaySize = SizeSlider.Value;
            SizeLabel.Text = ((int)SizeSlider.Value).ToString();
            SaveAndApply();
        }

        private void Transparency_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            double val = TransparencySlider.Value / 100.0;
            SettingsManager.Current.OverlayTransparency = val;
            TransparencyLabel.Text = $"{(int)TransparencySlider.Value}%";
            SaveAndApply();
        }

        private void LevelBars_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.OverlayLevelBarsEnabled = LevelBarsToggle.IsChecked == true;
            SaveAndApply();
        }

        private void AutoHide_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.OverlayAutoHide = AutoHideToggle.IsChecked == true;
            SaveAndApply();
        }

        private void ClickThrough_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.OverlayClickThrough = ClickThroughToggle.IsChecked == true;
            SaveAndApply();
        }

        private void FollowMonitor_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.OverlayFollowCursorMonitor = FollowMonitorToggle.IsChecked == true;
            SaveAndApply();
        }

        // ── Corner buttons ────────────────────────────────────────────────

        private void Corner_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                if (Enum.TryParse<OverlayCorner>(tag, out var corner))
                {
                    SettingsManager.Current.OverlayCorner = corner;
                    UpdateCornerButtons(corner);
                    SaveAndApply();
                }
            }
        }

        private void UpdateCornerButtons(OverlayCorner active)
        {
            CornerTL.Style = active == OverlayCorner.TopLeft ? (Style)Resources["CornerBtnActive"] : (Style)Resources["CornerBtn"];
            CornerTR.Style = active == OverlayCorner.TopRight ? (Style)Resources["CornerBtnActive"] : (Style)Resources["CornerBtn"];
            CornerBL.Style = active == OverlayCorner.BottomLeft ? (Style)Resources["CornerBtnActive"] : (Style)Resources["CornerBtn"];
            CornerBR.Style = active == OverlayCorner.BottomRight ? (Style)Resources["CornerBtnActive"] : (Style)Resources["CornerBtn"];
        }

        private void OffsetX_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            SettingsManager.Current.OverlayOffsetX = OffsetXSlider.Value;
            OffsetXLabel.Text = $"{(int)OffsetXSlider.Value}px";
            SaveAndApply();
        }

        private void OffsetY_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            SettingsManager.Current.OverlayOffsetY = OffsetYSlider.Value;
            OffsetYLabel.Text = $"{(int)OffsetYSlider.Value}px";
            SaveAndApply();
        }

        // ── System ────────────────────────────────────────────────────────

        private void Autostart_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            bool enabled = AutostartToggle.IsChecked == true;
            SettingsManager.Current.AutostartEnabled = enabled;
            StartupManager.SetStartup(enabled);
            SettingsManager.Save();
        }

        private void HighPriority_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            bool enabled = HighPriorityToggle.IsChecked == true;
            SettingsManager.Current.HighPriorityEnabled = enabled;
            Process.GetCurrentProcess().PriorityClass = enabled
                ? ProcessPriorityClass.High
                : ProcessPriorityClass.Normal;
            SettingsManager.Save();
        }

        private void MinimizeToTray_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.MinimizeToTray = MinimizeToTrayToggle.IsChecked == true;
            SettingsManager.Save();
        }

        private void Theme_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsManager.Current.Theme = ThemeDark.IsChecked == true ? AppTheme.Dark : AppTheme.Light;
            SettingsManager.Save();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var s = SettingsManager.Current;
            bool dark = s.Theme == AppTheme.Dark;

            // Window & card backgrounds
            Resources["WindowBg"]   = Brush(dark ? "1E1E1E" : "F2F0EB");
            Resources["CardBg"]     = Brush(dark ? "2A2A2A" : "FFFFFF");
            Resources["TitleBg"]    = Brush(dark ? "252525" : "E8E6E0");

            // Text
            Resources["TextPrimary"]    = Brush(dark ? "F0F0F0" : "2C2C2A");
            Resources["TextSecondary"]  = Brush(dark ? "888888" : "888780");

            // Borders & separators
            Resources["BorderColor"]    = Brush(dark ? "3A3A3A" : "D3D1C7");
            Resources["SepColor"]       = Brush(dark ? "333333" : "EBEBEB");

            // Input controls
            Resources["HotkeyBg"]       = Brush(dark ? "333333" : "F8F7F4");
            Resources["CornerBtnBg"]    = Brush(dark ? "333333" : "F2F0EB");

            // Toggle & slider
            Resources["SliderTrack"]    = Brush(dark ? "555555" : "D3D1C7");
            Resources["ToggleOff"]      = Brush(dark ? "555555" : "B4B2A9");
        }

        private static System.Windows.Media.SolidColorBrush Brush(string hex)
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(r, g, b));
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON files|*.json|All files|*.*",
                Title = "Import settings"
            };
            if (dlg.ShowDialog() == true)
            {
                if (SettingsManager.ImportFrom(dlg.FileName))
                {
                    _loading = true;
                    LoadSettings();
                    LoadDevices();
                    _loading = false;
                    SaveAndApply();
                    MessageBox.Show("Settings imported successfully.", "MuteMaster", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Could not import settings. The file may be invalid.", "MuteMaster", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON files|*.json|All files|*.*",
                FileName = "MuteMaster_settings.json",
                Title = "Export settings"
            };
            if (dlg.ShowDialog() == true)
            {
                if (SettingsManager.ExportTo(dlg.FileName))
                    MessageBox.Show("Settings exported successfully.", "MuteMaster", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Could not export settings.", "MuteMaster", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }

        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                switch (tag)
                {
                    case "HotKeyMuteMic":
                        SettingsManager.Current.HotKeyMuteMic = "";
                        HotkeyMicBox.Text = "";
                        break;
                    case "HotKeyMuteOutput":
                        SettingsManager.Current.HotKeyMuteOutput = "";
                        HotkeyOutputBox.Text = "";
                        break;
                    case "HotKeyToggleOverlay":
                        SettingsManager.Current.HotKeyToggleOverlay = "";
                        HotkeyOverlayBox.Text = "";
                        break;
                    case "HotKeyPushToTalk":
                        SettingsManager.Current.HotKeyPushToTalk = "";
                        HotkeyPTTBox.Text = "";
                        break;
                }
                SaveAndApply();
            }
        }

        // ── Save and notify overlay ───────────────────────────────────────

        private void SaveAndApply()
        {
            SettingsManager.Save();
            (Application.Current as App)?.ApplyOverlaySettings();
            (Application.Current as App)?.ReloadHotkeys();
        }
    }
}
