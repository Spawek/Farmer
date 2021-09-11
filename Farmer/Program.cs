using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;

namespace Farmer
{
    class Diablo
    {
        public Diablo()
        {
            var processes = Process.GetProcessesByName("Game");
            if (processes.Length == 0)
            {
                Process.Start(new ProcessStartInfo(@"F:\gry\Diablo II\Diablo II.exe", "-w"));
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            processes = Process.GetProcessesByName("Game");
            if (processes.Length != 1)
            {
                throw new ApplicationException("more than 1 'Game' process found");
            }
            window_handle = processes[0].MainWindowHandle;

            MoveWindow(-1000, 500);
            Click(400, 300);  // skip enter screen
        }

        // FROM: https://stackoverflow.com/questions/891345/get-a-screenshot-of-a-specific-application/911225
        // Note: the function is slow (20ms per call), but it's not caused by `new`
        // TODO: try the other foo for fetching images from this stack post (the other doesn't work in the background though)
        public Bitmap PrintWindow()
        {
            User32.GetWindowRect(window_handle, out User32.Rect rect);
            Bitmap bmp = new Bitmap(rect.right - rect.left, rect.bottom - rect.top, PixelFormat.Format32bppArgb);  
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            User32.PrintWindow(window_handle, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }

        // https://stackoverflow.com/questions/10355286/programmatically-mouse-click-in-another-window - uses `Cursor` not available on .NET core
        // FROM: https://www.codeproject.com/Articles/32556/Auto-Clicker-C
        private void SetCursor(int x, int y)
        {
            User32.GetWindowRect(window_handle, out User32.Rect rect);
            User32.SetCursorPos(x + rect.left, y + rect.top);
        }

        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        public void LeftClick(int x, int y)
        {
            User32.SetForegroundWindow(window_handle);
            SetCursor(x, y);
            User32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            User32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        public void DoubleLeftClick(int x, int y)
        {
            LeftClick(x, y);
            LeftClick(x, y);
        }

        // Mouse must be on the object for some time for the click to work.
        public void Click(int x, int y)
        {
            MoveMouse(x, y);
            Thread.Sleep(TimeSpan.FromMilliseconds(5));
            LeftClick(x, y);
        }

        public void RightClick(int x, int y)
        {
            User32.SetForegroundWindow(window_handle);
            SetCursor(x, y);
            User32.mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            User32.mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public void MoveMouse(int x, int y)
        {
            User32.SetForegroundWindow(window_handle);
            SetCursor(x, y);
        }

        private void MoveWindow(int x, int y)
        {
            User32.GetWindowRect(window_handle, out User32.Rect rect);
            User32.MoveWindow(window_handle, x, y, rect.right - rect.left, rect.bottom - rect.top, false);
        }

        public void PressEsc()
        {
            User32.SetForegroundWindow(window_handle);
            User32.PostMessage(window_handle, User32.WM_KEYDOWN, User32.VK_ESCAPE, 0);
            User32.PostMessage(window_handle, User32.WM_KEYUP, User32.VK_ESCAPE, 0);
            Thread.Sleep(TimeSpan.FromMilliseconds(5));
        }

        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

            [DllImport("user32.dll")]
            public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

            [DllImport("user32.dll")]
            public static extern int SetCursorPos(int x, int y);

            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            // FROM: https://docs.microsoft.com/en-us/windows/win32/learnwin32/keyboard-input
            // Codes: https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            public const ushort WM_KEYDOWN = 0x0100;
            public const ushort WM_KEYUP = 0x0101;
            public const ushort WM_SYSKEYDOWN = 0x0104;
            public const ushort WM_SYSKEYUP = 0x0105;
            public const ushort WM_SYSCHAR = 0x0106;
            public const ushort VK_ESCAPE = 0x1B;
            [DllImport("user32.dll")]
            public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

            // MoveWindow moves a window or changes its size based on a window handle.
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        }

        private IntPtr window_handle;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var diablo = new Diablo();

            // Menu options:
            diablo.LeftClick(400, 333);  // Single Player
            diablo.DoubleLeftClick(200, 150);  // 1st character
            diablo.LeftClick(400, 305);  // Normal difficulty
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            // Pick up dead body
            diablo.Click(400, 300);
            Thread.Sleep(TimeSpan.FromMilliseconds(500));

            // Go from Kurast Docks to Lower Kurast 
            diablo.Click(750, 350);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(750, 150);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(750, 150);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(750, 150);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(750, 336);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(750, 150);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(750, 150);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(550, 400);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            diablo.Click(175, 300);
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            //Back To main menu
            diablo.PressEsc();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            diablo.LeftClick(390, 290);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
        }
    }
}
