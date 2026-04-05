using MuteMaster.Core;
using MuteMaster.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace MuteMaster.ViewModels
{
    public class OverlayViewModel : INotifyPropertyChanged
    {
        private bool _isMicMuted;
        private float _micLevel;
        private bool _isVisible;
        private double _windowOpacity;
        private DispatcherTimer? _autoHideTimer;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<double>? OpacityChangeRequested;

        public bool IsMicMuted
        {
            get => _isMicMuted;
            set
            {
                _isMicMuted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MuteIconVisible));
                OnPropertyChanged(nameof(ActiveIconVisible));
                OnPropertyChanged(nameof(LevelBarsVisible));
            }
        }

        public float MicLevel
        {
            get => _micLevel;
            set
            {
                _micLevel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Bar1H));
                OnPropertyChanged(nameof(Bar2H));
                OnPropertyChanged(nameof(Bar3H));
                OnPropertyChanged(nameof(Bar4H));
                OnPropertyChanged(nameof(Bar5H));
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public bool MuteIconVisible => IsMicMuted;
        public bool ActiveIconVisible => !IsMicMuted;
        public bool LevelBarsVisible => !IsMicMuted && SettingsManager.Current.OverlayLevelBarsEnabled;

        // Level bars — amplified so they're visible even at low mic levels
        private static readonly Random _rng = new();
        private double Amp(double factor) => IsMicMuted ? 3 :
            Math.Max(3, Math.Min(18, (Math.Sqrt(MicLevel) * 18 * factor) + (MicLevel > 0.01 ? _rng.NextDouble() * 3 : 0)));

        public double Bar1H => Amp(0.7);
        public double Bar2H => Amp(1.0);
        public double Bar3H => Amp(0.85);
        public double Bar4H => Amp(1.0);
        public double Bar5H => Amp(0.75);

        public void Initialize()
        {
            IsVisible = SettingsManager.Current.OverlayVisible;
            IsMicMuted = AudioManager.IsMicMuted(SettingsManager.Current.InputDeviceId);

            AudioManager.MicMuteChanged += OnMicMuteChanged;
            AudioManager.MicLevelChanged += OnMicLevelChanged;
            AudioManager.StartLevelMonitoring(SettingsManager.Current.InputDeviceId);

            // Apply initial auto-hide state
            if (SettingsManager.Current.OverlayAutoHide && !IsMicMuted)
                OpacityChangeRequested?.Invoke(0.08);
            else
                OpacityChangeRequested?.Invoke(SettingsManager.Current.OverlayTransparency);
        }

        public void Dispose()
        {
            AudioManager.MicMuteChanged -= OnMicMuteChanged;
            AudioManager.MicLevelChanged -= OnMicLevelChanged;
            AudioManager.StopLevelMonitoring();
            _autoHideTimer?.Stop();
        }

        public void RefreshLevelBarsVisibility()
        {
            OnPropertyChanged(nameof(LevelBarsVisible));
        }

        private void OnMicMuteChanged(bool muted)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsMicMuted = muted;
                HandleAutoHide(muted);
            });
        }

        private void OnMicLevelChanged(float level)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MicLevel = level;
            });
        }

        private void HandleAutoHide(bool muted)
        {
            if (!SettingsManager.Current.OverlayAutoHide) return;

            // Always show at full opacity right after toggle
            OpacityChangeRequested?.Invoke(SettingsManager.Current.OverlayTransparency);

            _autoHideTimer?.Stop();
            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoHideTimer.Tick += (s, e) =>
            {
                _autoHideTimer?.Stop();
                // Fade out only when mic is active (not muted)
                if (!IsMicMuted)
                    OpacityChangeRequested?.Invoke(0.08);
            };
            _autoHideTimer.Start();
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
