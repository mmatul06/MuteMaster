using MuteMaster.Core;
using MuteMaster.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MuteMaster.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // ── Audio ─────────────────────────────────────────────────────────

        private List<AudioDevice> _inputDevices = new();
        public List<AudioDevice> InputDevices
        {
            get => _inputDevices;
            set { _inputDevices = value; OnPropertyChanged(); }
        }

        private List<AudioDevice> _outputDevices = new();
        public List<AudioDevice> OutputDevices
        {
            get => _outputDevices;
            set { _outputDevices = value; OnPropertyChanged(); }
        }

        private AudioDevice? _selectedInputDevice;
        public AudioDevice? SelectedInputDevice
        {
            get => _selectedInputDevice;
            set
            {
                _selectedInputDevice = value;
                if (value != null) SettingsManager.Current.InputDeviceId = value.Id;
                OnPropertyChanged();
            }
        }

        private AudioDevice? _selectedOutputDevice;
        public AudioDevice? SelectedOutputDevice
        {
            get => _selectedOutputDevice;
            set
            {
                _selectedOutputDevice = value;
                if (value != null) SettingsManager.Current.OutputDeviceId = value.Id;
                OnPropertyChanged();
            }
        }

        public bool PushToTalkEnabled
        {
            get => SettingsManager.Current.PushToTalkEnabled;
            set { SettingsManager.Current.PushToTalkEnabled = value; OnPropertyChanged(); Save(); }
        }

        public bool ToggleSoundEnabled
        {
            get => SettingsManager.Current.ToggleSoundEnabled;
            set { SettingsManager.Current.ToggleSoundEnabled = value; OnPropertyChanged(); Save(); }
        }

        public string CustomSoundPath
        {
            get => SettingsManager.Current.CustomSoundPath;
            set { SettingsManager.Current.CustomSoundPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(CustomSoundLabel)); Save(); }
        }

        public string CustomSoundLabel =>
            string.IsNullOrEmpty(CustomSoundPath) ? "Default" : System.IO.Path.GetFileName(CustomSoundPath);

        // ── Hotkeys ───────────────────────────────────────────────────────

        public string HotKeyMuteMic
        {
            get => SettingsManager.Current.HotKeyMuteMic;
            set { SettingsManager.Current.HotKeyMuteMic = value; OnPropertyChanged(); Save(); }
        }

        public string HotKeyMuteOutput
        {
            get => SettingsManager.Current.HotKeyMuteOutput;
            set { SettingsManager.Current.HotKeyMuteOutput = value; OnPropertyChanged(); Save(); }
        }

        public string HotKeyToggleOverlay
        {
            get => SettingsManager.Current.HotKeyToggleOverlay;
            set { SettingsManager.Current.HotKeyToggleOverlay = value; OnPropertyChanged(); Save(); }
        }

        public string HotKeyPushToTalk
        {
            get => SettingsManager.Current.HotKeyPushToTalk;
            set { SettingsManager.Current.HotKeyPushToTalk = value; OnPropertyChanged(); Save(); }
        }

        // ── Overlay appearance ────────────────────────────────────────────

        public double OverlaySize
        {
            get => SettingsManager.Current.OverlaySize;
            set { SettingsManager.Current.OverlaySize = value; OnPropertyChanged(); Save(); }
        }

        public double OverlayTransparencyPercent
        {
            get => SettingsManager.Current.OverlayTransparency * 100;
            set { SettingsManager.Current.OverlayTransparency = value / 100.0; OnPropertyChanged(); Save(); }
        }

        public bool OverlayLevelBarsEnabled
        {
            get => SettingsManager.Current.OverlayLevelBarsEnabled;
            set { SettingsManager.Current.OverlayLevelBarsEnabled = value; OnPropertyChanged(); Save(); }
        }

        public bool OverlayAutoHide
        {
            get => SettingsManager.Current.OverlayAutoHide;
            set { SettingsManager.Current.OverlayAutoHide = value; OnPropertyChanged(); Save(); }
        }

        public bool OverlayClickThrough
        {
            get => SettingsManager.Current.OverlayClickThrough;
            set { SettingsManager.Current.OverlayClickThrough = value; OnPropertyChanged(); Save(); }
        }

        public bool OverlayFollowCursorMonitor
        {
            get => SettingsManager.Current.OverlayFollowCursorMonitor;
            set { SettingsManager.Current.OverlayFollowCursorMonitor = value; OnPropertyChanged(); Save(); }
        }

        // ── Overlay position ──────────────────────────────────────────────

        public OverlayCorner OverlayCorner
        {
            get => SettingsManager.Current.OverlayCorner;
            set { SettingsManager.Current.OverlayCorner = value; OnPropertyChanged(); Save(); }
        }

        public double OverlayOffsetX
        {
            get => SettingsManager.Current.OverlayOffsetX;
            set { SettingsManager.Current.OverlayOffsetX = value; OnPropertyChanged(); Save(); }
        }

        public double OverlayOffsetY
        {
            get => SettingsManager.Current.OverlayOffsetY;
            set { SettingsManager.Current.OverlayOffsetY = value; OnPropertyChanged(); Save(); }
        }

        // ── System ────────────────────────────────────────────────────────

        public bool AutostartEnabled
        {
            get => SettingsManager.Current.AutostartEnabled;
            set { SettingsManager.Current.AutostartEnabled = value; OnPropertyChanged(); Save(); }
        }

        public bool HighPriorityEnabled
        {
            get => SettingsManager.Current.HighPriorityEnabled;
            set { SettingsManager.Current.HighPriorityEnabled = value; OnPropertyChanged(); Save(); }
        }

        public bool MinimizeToTray
        {
            get => SettingsManager.Current.MinimizeToTray;
            set { SettingsManager.Current.MinimizeToTray = value; OnPropertyChanged(); Save(); }
        }

        public bool ThemeIsDark
        {
            get => SettingsManager.Current.Theme == AppTheme.Dark;
            set
            {
                SettingsManager.Current.Theme = value ? AppTheme.Dark : AppTheme.Light;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThemeIsLight));
                Save();
            }
        }

        public bool ThemeIsLight
        {
            get => SettingsManager.Current.Theme == AppTheme.Light;
            set => ThemeIsDark = !value;
        }

        // ── Init ──────────────────────────────────────────────────────────

        public void LoadDevices()
        {
            InputDevices = AudioManager.GetInputDevices();
            OutputDevices = AudioManager.GetOutputDevices();

            SelectedInputDevice = InputDevices.Find(d => d.Id == SettingsManager.Current.InputDeviceId)
                ?? (InputDevices.Count > 0 ? InputDevices[0] : null);

            SelectedOutputDevice = OutputDevices.Find(d => d.Id == SettingsManager.Current.OutputDeviceId)
                ?? (OutputDevices.Count > 0 ? OutputDevices[0] : null);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void Save() => SettingsManager.Save();

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}