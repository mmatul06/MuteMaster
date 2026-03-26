using Hardcodet.Wpf.TaskbarNotification;
using MuteMaster.Core;
using MuteMaster.Models;
using MuteMaster.Views;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MuteMaster
{
    public partial class App : Application
    {
        private TaskbarIcon? _trayIcon;
        private OverlayWindow? _overlay;
        private SettingsWindow? _settingsWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mutex = new Mutex(true, "MuteMaster_SingleInstance", out bool isNew);
            if (!isNew)
            {
                MessageBox.Show("MuteMaster is already running.", "MuteMaster",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            Thread.Sleep(600);
            SettingsManager.Load();

            if (SettingsManager.Current.HighPriorityEnabled)
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            InitTrayIcon();

            _overlay = new OverlayWindow();
            _overlay.Show();

            ReloadHotkeys();
            SettingsManager.Current.AutostartEnabled = StartupManager.IsStartupEnabled();
        }

        // ── Tray icon ─────────────────────────────────────────────────────

        private void InitTrayIcon()
        {
            _trayIcon = new TaskbarIcon();
            _trayIcon.ToolTipText = "MuteMaster";
            _trayIcon.TrayMouseDoubleClick += (s, e) => OpenSettings();

            UpdateTrayIcon(AudioManager.IsMicMuted(SettingsManager.Current.InputDeviceId));
            RebuildTrayMenu();

            AudioManager.MicMuteChanged += muted =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateTrayIcon(muted);
                    UpdateTrayTooltip();
                    RebuildTrayMenu();
                });
            };

            UpdateTrayTooltip();
        }

        private void UpdateTrayIcon(bool muted)
        {
            try
            {
                string iconPath = muted
                    ? "pack://application:,,,/Assets/tray_muted.ico"
                    : "pack://application:,,,/Assets/tray_active.ico";

                var stream = Application.GetResourceStream(new Uri(iconPath))?.Stream;
                if (stream != null)
                    _trayIcon!.Icon = new System.Drawing.Icon(stream);
            }
            catch { }
        }

        public void RebuildTrayMenu()
        {
            bool micMuted = AudioManager.IsMicMuted(SettingsManager.Current.InputDeviceId);
            bool outMuted = AudioManager.IsOutputMuted(SettingsManager.Current.OutputDeviceId);
            bool overlayVisible = _overlay?.IsVisible == true && _overlay.Opacity > 0.05;

            // Load styles from merged resource dictionary
            var menuStyle = TryFindResource("TrayContextMenuStyle") as Style;
            var itemStyle = TryFindResource("TrayMenuItemStyle") as Style;
            var sepStyle  = TryFindResource("TraySeparatorStyle") as Style;

            var menu = new ContextMenu();
            if (menuStyle != null) menu.Style = menuStyle;

            menu.Items.Add(MakeItem(
                micMuted ? "✓  Mic muted" : "Mute microphone",
                () => { AudioManager.ToggleMicMute(SettingsManager.Current.InputDeviceId); RebuildTrayMenu(); },
                itemStyle, isDanger: false));

            menu.Items.Add(MakeItem(
                outMuted ? "✓  Speakers muted" : "Mute speakers",
                () => { AudioManager.ToggleOutputMute(SettingsManager.Current.OutputDeviceId); RebuildTrayMenu(); },
                itemStyle, isDanger: false));

            menu.Items.Add(MakeSep(sepStyle));

            menu.Items.Add(MakeItem(
                overlayVisible ? "Hide overlay" : "Show overlay",
                () => { _overlay?.SetVisible(!overlayVisible); RebuildTrayMenu(); },
                itemStyle));

            menu.Items.Add(MakeSep(sepStyle));
            menu.Items.Add(MakeItem("Open settings", OpenSettings, itemStyle));
            menu.Items.Add(MakeItem("About MuteMaster", OpenAbout, itemStyle));
            menu.Items.Add(MakeSep(sepStyle));
            menu.Items.Add(MakeItem("Quit", () => Shutdown(), itemStyle, isDanger: true));

            _trayIcon!.ContextMenu = menu;
        }

        private MenuItem MakeItem(string header, Action action, Style? style,
            bool isDanger = false)
        {
            var item = new MenuItem
            {
                Header = header,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(28, 28, 28)),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    isDanger
                        ? System.Windows.Media.Color.FromRgb(220, 80, 80)
                        : System.Windows.Media.Color.FromRgb(240, 240, 240)),
                FontSize = 13,
                Padding = new Thickness(16, 8, 16, 8)
            };
            if (style != null) item.Style = style;
            item.Click += (s, e) => action();
            return item;
        }

        private Separator MakeSep(Style? style)
        {
            var sep = new Separator();
            if (style != null) sep.Style = style;
            return sep;
        }

        private void UpdateTrayTooltip()
        {
            bool micMuted = AudioManager.IsMicMuted(SettingsManager.Current.InputDeviceId);
            bool outputMuted = AudioManager.IsOutputMuted(SettingsManager.Current.OutputDeviceId);
            if (_trayIcon != null)
                _trayIcon.ToolTipText = $"MuteMaster  |  Mic: {(micMuted ? "Muted" : "Active")}  ·  Speakers: {(outputMuted ? "Muted" : "On")}";
        }

        // ── Hotkeys ───────────────────────────────────────────────────────

        public void ReloadHotkeys()
        {
            HotkeyManager.UnregisterAll();
            PushToTalkManager.Stop();
            var s = SettingsManager.Current;

            HotkeyManager.Register("MuteMic", s.HotKeyMuteMic, () =>
                AudioManager.ToggleMicMute(s.InputDeviceId));

            HotkeyManager.Register("MuteOutput", s.HotKeyMuteOutput, () =>
                AudioManager.ToggleOutputMute(s.OutputDeviceId));

            HotkeyManager.Register("ToggleOverlay", s.HotKeyToggleOverlay, () =>
            {
                Dispatcher.Invoke(() =>
                {
                    bool newState = !(_overlay?.IsVisible == true && _overlay.Opacity > 0.05);
                    _overlay?.SetVisible(newState);
                });
            });

            if (!string.IsNullOrEmpty(s.HotKeyPushToTalk) && s.PushToTalkEnabled)
            {
                AudioManager.SetMicMuteNoSound(true, s.InputDeviceId);
                PushToTalkManager.Start(s.HotKeyPushToTalk, s.InputDeviceId);
            }
        }

        // ── Settings & overlay ────────────────────────────────────────────

        public void ApplyOverlaySettings()
        {
            _overlay?.ApplySettings();
            _overlay?.UpdatePosition();
        }

        public void ApplyTheme() { }

        // ── Windows ───────────────────────────────────────────────────────

        public void OpenSettings()
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
                _settingsWindow = new SettingsWindow();
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void OpenAbout()
        {
            new AboutWindow().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            HotkeyManager.UnregisterAll();
            PushToTalkManager.Stop();
            AudioManager.Dispose();
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
