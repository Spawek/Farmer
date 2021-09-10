using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Farmer
{
    class Program
    {
        public static Bitmap PrintWindow(string procName)
        {
            var processes = Process.GetProcesses();
            var proc = Process.GetProcessesByName(procName)[0];
            var rect = new User32.Rect();
            IntPtr hwnd = proc.MainWindowHandle;
            User32.GetWindowRect(hwnd, out rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            User32.PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
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
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            PrintWindow("Game");
            PrintWindow("Game");

        }
    }
}
