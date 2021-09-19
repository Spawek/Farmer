using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SysCalls;

namespace FarmerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FarmerWindow : Window
    {
        public FarmerWindow()
        {
            Syscall.AllocConsole();

            InitializeComponent();
        }

        // Register global hotkey code from:
        // https://gist.github.com/rincew1nd/8a106f4c3e54ef694e934bb4ff737512
        private HwndSource _source;
        private const int PAUSE_HOTKEY_ID = 9000;
        private const int UNPAUSE_HOTKEY_ID = 9001;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKeys();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
            base.OnClosed(e);
        }

        private void RegisterHotKeys()
        {
            RegisterHotkey(PAUSE_HOTKEY_ID, Syscall.Key(Key.F10), (uint)ModifierKeys.Control);
            RegisterHotkey(UNPAUSE_HOTKEY_ID, Syscall.Key(Key.F11), (uint)ModifierKeys.Control);
        }

        private void RegisterHotkey(int id, uint key, uint mod)
        {
            var helper = new WindowInteropHelper(this);
            Console.Write($"Hotkey registration: ID {id}, key: {key}, mod: {mod}: ");
            if (!Syscall.RegisterHotKey(helper.Handle, id, mod, key))
            {
                uint err = Syscall.GetLastError();
                // https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes--1300-1699-
                Console.WriteLine($"failed: {err}");
            }
            else
            {
                Console.WriteLine($"succeded!");
            }
        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            Syscall.UnregisterHotKey(helper.Handle, PAUSE_HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case PAUSE_HOTKEY_ID:
                            PauseKeyPressed();
                            handled = true;
                            break;

                        case UNPAUSE_HOTKEY_ID:
                            UnpauseKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void PauseKeyPressed()
        {
            Console.WriteLine("Pause key pressed");
        }

        private void UnpauseKeyPressed()
        {
            Console.WriteLine("Unpause key pressed");
        }
    }
}
