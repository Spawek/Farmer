using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace SysCalls
{
    public static class Syscall
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
        public const ushort VK_CONTROL = 0x11;
        public const ushort VK_F1 = 0x70;

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        // MoveWindow moves a window or changes its size based on a window handle.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("User32.dll")]
        public static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);

        [DllImport("User32.dll")]
        public static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        public static uint Key(Key key)
        {
            return (uint)KeyInterop.VirtualKeyFromKey(key);
        }
    }
}
