using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace MuteMaster.Core
{
    public static class PushToTalkManager
    {
        private static IntPtr _hookHandle = IntPtr.Zero;
        private static NativeHookProc? _hookProc;
        private static Key _pttKey = Key.None;
        private static bool _isActive;
        private static string _deviceId = "";
        private static bool _keyIsDown = false; // prevent repeat firing

        private delegate IntPtr NativeHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, NativeHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN    = 0x0100;
        private const int WM_KEYUP      = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP   = 0x0105;

        public static void Start(string hotkeyString, string deviceId)
        {
            Stop();
            if (string.IsNullOrEmpty(hotkeyString)) return;
            if (!HotkeyManager.TryParse(hotkeyString, out Key key, out _)) return;

            _pttKey    = key;
            _deviceId  = deviceId;
            _isActive  = true;
            _keyIsDown = false;
            _hookProc  = HookCallback;

            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(null), 0);
        }

        public static void Stop()
        {
            _isActive = false;
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            // Remute on stop
            if (_keyIsDown && !string.IsNullOrEmpty(_deviceId))
            {
                try { AudioManager.SetMicMute(true, _deviceId); } catch { }
            }
            _pttKey    = Key.None;
            _keyIsDown = false;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isActive && _pttKey != Key.None)
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var pressedKey = KeyInterop.KeyFromVirtualKey((int)kb.vkCode);

                if (pressedKey == _pttKey)
                {
                    int msg = (int)wParam;
                    if ((msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) && !_keyIsDown)
                    {
                        // First key-down only — unmute without playing sound
                        _keyIsDown = true;
                        try { AudioManager.SetMicMuteNoSound(false, _deviceId); } catch { }
                    }
                    else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                    {
                        // Key released — remute without sound
                        _keyIsDown = false;
                        try { AudioManager.SetMicMuteNoSound(true, _deviceId); } catch { }
                    }
                }
            }
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
    }
}
