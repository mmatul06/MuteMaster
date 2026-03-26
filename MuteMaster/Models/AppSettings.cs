using System.Text.Json.Serialization;

namespace MuteMaster.Models
{
    public class AppSettings
    {
        // Audio
        [JsonPropertyName("inputDeviceId")]
        public string InputDeviceId { get; set; } = string.Empty;

        [JsonPropertyName("outputDeviceId")]
        public string OutputDeviceId { get; set; } = string.Empty;

        [JsonPropertyName("pushToTalkEnabled")]
        public bool PushToTalkEnabled { get; set; } = false;

        [JsonPropertyName("toggleSoundEnabled")]
        public bool ToggleSoundEnabled { get; set; } = true;

        [JsonPropertyName("customMuteSound")]
        public string CustomMuteSound { get; set; } = string.Empty;

        [JsonPropertyName("customUnmuteSound")]
        public string CustomUnmuteSound { get; set; } = string.Empty;

        // Keep for backward compat
        [JsonPropertyName("customSoundPath")]
        public string CustomSoundPath { get; set; } = string.Empty;

        // Hotkeys
        [JsonPropertyName("hotKeyMuteMic")]
        public string HotKeyMuteMic { get; set; } = "Ctrl+Shift+M";

        [JsonPropertyName("hotKeyMuteOutput")]
        public string HotKeyMuteOutput { get; set; } = "Ctrl+Shift+S";

        [JsonPropertyName("hotKeyToggleOverlay")]
        public string HotKeyToggleOverlay { get; set; } = "Ctrl+Shift+O";

        [JsonPropertyName("hotKeyPushToTalk")]
        public string HotKeyPushToTalk { get; set; } = string.Empty;

        // Overlay appearance
        [JsonPropertyName("overlaySize")]
        public double OverlaySize { get; set; } = 55;

        [JsonPropertyName("overlayTransparency")]
        public double OverlayTransparency { get; set; } = 0.72;

        [JsonPropertyName("overlayLevelBarsEnabled")]
        public bool OverlayLevelBarsEnabled { get; set; } = true;

        [JsonPropertyName("overlayAutoHide")]
        public bool OverlayAutoHide { get; set; } = false;

        [JsonPropertyName("overlayClickThrough")]
        public bool OverlayClickThrough { get; set; } = true;

        [JsonPropertyName("overlayFollowCursorMonitor")]
        public bool OverlayFollowCursorMonitor { get; set; } = true;

        [JsonPropertyName("overlayVisible")]
        public bool OverlayVisible { get; set; } = true;

        // Overlay position
        [JsonPropertyName("overlayCorner")]
        public OverlayCorner OverlayCorner { get; set; } = OverlayCorner.TopLeft;

        [JsonPropertyName("overlayOffsetX")]
        public double OverlayOffsetX { get; set; } = 20;

        [JsonPropertyName("overlayOffsetY")]
        public double OverlayOffsetY { get; set; } = 20;

        // System
        [JsonPropertyName("autostartEnabled")]
        public bool AutostartEnabled { get; set; } = false;

        [JsonPropertyName("highPriorityEnabled")]
        public bool HighPriorityEnabled { get; set; } = false;

        [JsonPropertyName("minimizeToTray")]
        public bool MinimizeToTray { get; set; } = true;

        [JsonPropertyName("theme")]
        public AppTheme Theme { get; set; } = AppTheme.Dark;
    }

    public enum OverlayCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum AppTheme
    {
        Light,
        Dark
    }
}
