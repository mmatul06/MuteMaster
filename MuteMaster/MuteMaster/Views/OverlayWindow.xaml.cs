using MuteMaster.Core;
using MuteMaster.Models;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MuteMaster.Views
{
    public partial class OverlayWindow : Window
    {
        private const int GWL_EXSTYLE    = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED    = 0x00080000;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr h, int i, int v);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT p);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        private bool _isMuted;
        private System.Windows.Threading.DispatcherTimer? _autoHideTimer;
        private static readonly Random _rng = new();

        public OverlayWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyClickThrough();
            ApplySettings();

            // Subscribe to audio events
            AudioManager.MicMuteChanged  += OnMicMuteChanged;
            AudioManager.MicLevelChanged += OnMicLevelChanged;

            // Set initial mute state
            _isMuted = AudioManager.IsMicMuted(SettingsManager.Current.InputDeviceId);
            UpdateMuteVisuals(_isMuted);

            // Start level monitoring
            AudioManager.StartLevelMonitoring(SettingsManager.Current.InputDeviceId);
        }

        // ── Mute state ─────────────────────────────────────────────────

        private void OnMicMuteChanged(bool muted)
        {
            Dispatcher.Invoke(() =>
            {
                _isMuted = muted;
                UpdateMuteVisuals(muted);
                HandleAutoHide(muted);
            });
        }

        private void UpdateMuteVisuals(bool muted)
        {
            MutedCircle.Visibility = muted ? Visibility.Visible : Visibility.Collapsed;
            ActiveCircle.Visibility = muted ? Visibility.Collapsed : Visibility.Visible;
            MutedIcon.Visibility  = muted ? Visibility.Visible : Visibility.Collapsed;
            ActiveIcon.Visibility  = muted ? Visibility.Collapsed : Visibility.Visible;

            // Hide bars when muted
            bool barsVisible = !muted && SettingsManager.Current.OverlayLevelBarsEnabled;
            LevelBarsPanel.Visibility = barsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Level bars ─────────────────────────────────────────────────

        private void OnMicLevelChanged(float level)
        {
            Dispatcher.Invoke(() =>
            {
                // Always update bar heights even if overlay is faded (auto-hide)
                // but only if bars are supposed to be visible
                if (_isMuted || !SettingsManager.Current.OverlayLevelBarsEnabled) return;

                double amp = Math.Pow(Math.Max(0, level), 0.3) * 22;
                double noise = level > 0.005 ? (_rng.NextDouble() * 5 - 1) : 0;

                Bar1.Height = Math.Max(3, amp * 0.75 + noise);
                Bar2.Height = Math.Max(3, amp * 1.00 + noise);
                Bar3.Height = Math.Max(3, amp * 0.88 + noise);
                Bar4.Height = Math.Max(3, amp * 1.00 + noise);
                Bar5.Height = Math.Max(3, amp * 0.80 + noise);
            });
        }

        // ── Auto-hide ──────────────────────────────────────────────────

        private void HandleAutoHide(bool muted)
        {
            if (!SettingsManager.Current.OverlayAutoHide) return;

            _autoHideTimer?.Stop();
            _autoHideTimer = null;

            // Always show at full opacity immediately on any mute toggle
            Opacity = SettingsManager.Current.OverlayTransparency;

            if (!muted)
            {
                // Schedule fade after 2s when mic becomes active (unmuted)
                _autoHideTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                _autoHideTimer.Tick += (s, e) =>
                {
                    _autoHideTimer?.Stop();
                    _autoHideTimer = null;
                    if (!_isMuted) Opacity = 0.08;
                };
                _autoHideTimer.Start();
            }
            // When muted, stay visible at full opacity (no timer needed)
        }

        // ── Apply settings ─────────────────────────────────────────────

        public void ApplySettings()
        {
            var s = SettingsManager.Current;

            // Scale icon viewbox and bar heights proportionally
            double scale = s.OverlaySize / 55.0;
            double iconSize = 32 * scale;
            IconViewbox.Width  = iconSize;
            IconViewbox.Height = iconSize;

            // Scale bar dimensions
            double barW = Math.Max(2, 3 * scale);
            double barR = barW / 2;
            foreach (var bar in new[] { Bar1, Bar2, Bar3, Bar4, Bar5 })
            {
                bar.Width   = barW;
                bar.RadiusX = barR;
                bar.RadiusY = barR;
            }
            LevelBarsPanel.Margin = new Thickness(Math.Max(4, 8 * scale), 0, 0, 2);

            if (!s.OverlayAutoHide || _isMuted)
                Opacity = s.OverlayTransparency;

            ApplyClickThrough();
            Visibility = s.OverlayVisible ? Visibility.Visible : Visibility.Hidden;

            bool barsVisible = !_isMuted && s.OverlayLevelBarsEnabled;
            LevelBarsPanel.Visibility = barsVisible ? Visibility.Visible : Visibility.Collapsed;

            // PTT indicator
            PttIndicator.Visibility = s.PushToTalkEnabled ? Visibility.Visible : Visibility.Collapsed;

            UpdatePosition();
        }

        // ── Position ───────────────────────────────────────────────────

        public void UpdatePosition()
        {
            var s = SettingsManager.Current;
            double sl, st, sr, sb;

            if (s.OverlayFollowCursorMonitor)
            {
                GetCursorPos(out POINT p);
                var screen = WpfScreen.GetScreenFrom(new Point(p.X, p.Y));
                sl = screen.WorkingArea.Left;  st = screen.WorkingArea.Top;
                sr = screen.WorkingArea.Right; sb = screen.WorkingArea.Bottom;
            }
            else
            {
                sl = SystemParameters.WorkArea.Left;  st = SystemParameters.WorkArea.Top;
                sr = SystemParameters.WorkArea.Right; sb = SystemParameters.WorkArea.Bottom;
            }

            double w = ActualWidth  > 0 ? ActualWidth  : 80;
            double h = ActualHeight > 0 ? ActualHeight : 52;
            double ox = s.OverlayOffsetX, oy = s.OverlayOffsetY;

            switch (s.OverlayCorner)
            {
                case OverlayCorner.TopLeft:     Left = sl + ox;      Top = st + oy;      break;
                case OverlayCorner.TopRight:    Left = sr - w - ox;  Top = st + oy;      break;
                case OverlayCorner.BottomLeft:  Left = sl + ox;      Top = sb - h - oy;  break;
                case OverlayCorner.BottomRight: Left = sr - w - ox;  Top = sb - h - oy;  break;
            }
        }

        // ── Click-through ──────────────────────────────────────────────

        private void ApplyClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (SettingsManager.Current.OverlayClickThrough)
                SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE);
            else
                SetWindowLong(hwnd, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
        }

        // ── Visible toggle ─────────────────────────────────────────────

        public void SetVisible(bool visible)
        {
            Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            SettingsManager.Current.OverlayVisible = visible;
            SettingsManager.Save();
        }

        protected override void OnClosed(EventArgs e)
        {
            _autoHideTimer?.Stop();
            AudioManager.MicMuteChanged  -= OnMicMuteChanged;
            AudioManager.MicLevelChanged -= OnMicLevelChanged;
            AudioManager.StopLevelMonitoring();
            base.OnClosed(e);
        }
    }
}
