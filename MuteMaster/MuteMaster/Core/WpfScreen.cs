using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace MuteMaster.Core
{
    public class ScreenInfo
    {
        public Rect WorkingArea { get; set; }
        public Rect Bounds { get; set; }
    }

    public static class WpfScreen
    {
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        public static ScreenInfo GetScreenFrom(Point wpfPoint)
        {
            var pt = new POINT { X = (int)wpfPoint.X, Y = (int)wpfPoint.Y };
            var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            return BuildScreenInfo(hMonitor);
        }

        public static ScreenInfo GetPrimaryScreen()
        {
            var pt = new POINT { X = 0, Y = 0 };
            var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            return BuildScreenInfo(hMonitor);
        }

        private static ScreenInfo BuildScreenInfo(IntPtr hMonitor)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            GetMonitorInfo(hMonitor, ref mi);

            double scale = 1.0;
            if (GetDpiForMonitor(hMonitor, 0, out uint dpiX, out _) == 0)
                scale = dpiX / 96.0;

            return new ScreenInfo
            {
                Bounds = RectFromRECT(mi.rcMonitor, scale),
                WorkingArea = RectFromRECT(mi.rcWork, scale)
            };
        }

        private static Rect RectFromRECT(RECT r, double scale) =>
            new Rect(r.Left / scale, r.Top / scale,
                     (r.Right - r.Left) / scale, (r.Bottom - r.Top) / scale);
    }
}
