using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCvSharp;
using NumSharp;
using Cv2 = OpenCvSharp.Cv2;

namespace Farmer
{
    class Diablo
    {
        public Diablo()
        {
            var processes = Process.GetProcessesByName("Game");
            bool started_game = false;
            if (processes.Length == 0)
            {
                Process.Start(new ProcessStartInfo(@"F:\gry\Diablo II\Diablo II.exe", "-w"));
                Thread.Sleep(500);
                started_game = true;
            }

            processes = Process.GetProcessesByName("Game");
            if (processes.Length != 1)
            {
                throw new ApplicationException("more than 1 'Game' process found");
            }
            window_handle = processes[0].MainWindowHandle;

            if (started_game)
            {
                MoveWindow(-1000, 500);
                Click(400, 300);  // skip enter screen
            }
        }

        // FROM: https://stackoverflow.com/questions/891345/get-a-screenshot-of-a-specific-application/911225
        // Note: the function is slow (20ms per call), but it's not caused by `new`
        // TODO: try the other foo for fetching images from this stack post (the other doesn't work in the background though)
        public Bitmap DumpBitmap()
        {
            User32.GetWindowRect(window_handle, out User32.Rect rect);
            Bitmap bmp = new Bitmap(rect.right - rect.left, rect.bottom - rect.top, PixelFormat.Format32bppRgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            User32.PrintWindow(window_handle, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }

        public string DetectMapLocation(TextDetector detector)
        {
            var bmp = DumpBitmap().Clone(new Rectangle(600, 30, 200, 25), PixelFormat.Format32bppRgb);
            return detector.Detect(bmp);
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
            Thread.Sleep(20);
            LeftClick(x, y);
        }

        // Mouse must be on the object for some time for the click to work.
        public void PickUpItem(int x, int y)
        {
            MoveMouse(x, y);
            ShowItems();
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
            Thread.Sleep(5);
        }

        // https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keydown - lparam
        public void PutCtrlKeyDown()
        {
            User32.SetForegroundWindow(window_handle);
            User32.PostMessage(window_handle, User32.WM_KEYDOWN, User32.VK_CONTROL, 15);
        }

        public void ShowItems()
        {
            for (int i = 0; i < 5; i++)
            {
                PutCtrlKeyDown();
                Thread.Sleep(1);
            }
        }

        public void PutCtrlKeyUp()
        {
            User32.SetForegroundWindow(window_handle);
            User32.PostMessage(window_handle, User32.WM_KEYUP, User32.VK_CONTROL, 0);
        }

        public XY? DetectRunes(RuneDetector rune_detector)
        {
            ShowItems();
            var bmp = DumpBitmap();
            var rune = rune_detector.FindRune(bmp);
            return rune;
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
            public const ushort VK_CONTROL = 0x11;
            [DllImport("user32.dll")]
            public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

            // MoveWindow moves a window or changes its size based on a window handle.
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        }

        private IntPtr window_handle;
    }

    public class TextDetector : IDisposable
    {
        public string Detect(Bitmap bmp)
        {
            var image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            detector.Run(image,
                out var outputText, out var componentRects, out var componentTexts, out var componentConfidences, OpenCvSharp.Text.ComponentLevels.Word);
            string name = $"img_{DateTime.Now.ToLongTimeString()}_{outputText}".Replace(":", "_").Replace("\n", "");
            bmp.Save($@"C:/tmp/{name}.png");
            image.SaveImage($@"C:/tmp/{name}.jpg");
            return outputText;
        }

        public void Dispose()
        {
            detector.Dispose();
        }

        private OpenCvSharp.Text.OCRTesseract detector = OpenCvSharp.Text.OCRTesseract.Create(@"C:\maciek\programowanie\Farmer\Farmer\TesseractData\");
    }

    public struct XY
    {
        public XY(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is XY))
                return false;
            var rhs = (XY)obj;

            return this.x == rhs.x && this.y == rhs.y;

        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public int x;
        public int y;

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }

    public class RuneDetector
    {
        public RuneDetector()
        {
            {
                var tmpl_bmp2 = new Bitmap(@"C:\maciek\programowanie\Farmer\templates\rune.png");
                var tmpl_bmp = new Bitmap(tmpl_bmp2).Clone(new Rectangle(0, 0, tmpl_bmp2.Width, tmpl_bmp2.Height), PixelFormat.Format32bppRgb);
                rune_tmpl = OpenCvSharp.Extensions.BitmapConverter.ToMat(tmpl_bmp);
            }
        }
        Mat rune_tmpl;

        public XY? FindRune(Bitmap bmp)
        {
            DateTime before = DateTime.Now;
            //const int KERNEL_SIZE = 5;
            //const int MATCHES_THRESHOLD = 10;
            //var pixel_matches_per_location = new Dictionary<XY, int>();

            //Color rune_text_color = Color.FromArgb(208, 132, 32);
            //for (int x = 0; x < bmp.Width; x++)
            //{
            //    for (int y = 0; y < bmp.Height; y++)
            //    {
            //        if (bmp.GetPixel(x, y) == rune_text_color)
            //        {
            //            int trimmed_x = x - x % KERNEL_SIZE;
            //            int trimmed_y = y - y % KERNEL_SIZE;
            //            XY xy = new XY(trimmed_x, trimmed_y);
            //            if (!pixel_matches_per_location.ContainsKey(xy))
            //                pixel_matches_per_location.Add(xy, 0);
            //            pixel_matches_per_location[xy]++;
            //        }
            //    }
            //}
            var image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            var result = new Mat();

            
            Cv2.MatchTemplate(image, rune_tmpl, result, TemplateMatchModes.CCoeffNormed);
            //image.SaveImage(@"C:\tmp\image.png");
            //result.SaveImage(@"C:\tmp\detect.png");

            var min_index = new int[2];
            var max_index = new int[2];
            result.MinMaxIdx(out double min, out double max, min_index, max_index);
            //Console.WriteLine($"rune detection took: {DateTime.Now - before}");
            if (max > 0.9)
            {
                var ret = new XY(max_index[1] + rune_tmpl.Width / 2, max_index[0] + rune_tmpl.Height / 2);

                //Bitmap dbg = (Bitmap)bmp.Clone();
                //for (int dx = -1; dx <= 1; dx++)
                //{
                //    for (int dy = -1; dy <= 1; dy++)
                //    {
                //        dbg.SetPixel(ret.x + dx, ret.y + dy, Color.Red);
                //    }
                //}
                //string name = $"img_{DateTime.Now.ToLongTimeString()}_runes".Replace(":", "_").Replace("\n", "");
                //dbg.Save($@"C:/tmp/{name}.png");

                return ret;
            }
            return null;


            //var matches = pixel_matches_per_location
            //    .Where(x => x.Value >= MATCHES_THRESHOLD)
            //    .Select(x => x.Key)
            //    .Select(x => new XY { x = x.x + KERNEL_SIZE / 2, y = x.y + KERNEL_SIZE / 2 })
            //    .ToList();

            //if (matches.Count > 0)
            //{
            //    Bitmap dbg = (Bitmap)bmp.Clone();
            //    foreach (var match in matches)
            //    {
            //        for (int dx = -1; dx <= 1; dx++)
            //        {
            //            for (int dy = -1; dy <= 1; dy++)
            //            {
            //                dbg.SetPixel(match.x + dx, match.y + dy, Color.Red);
            //            }
            //        }
            //        string name = $"img_{DateTime.Now.ToLongTimeString()}_runes".Replace(":", "_").Replace("\n", "");
            //        dbg.Save($@"C:/tmp/{name}.png");
            //    }
            //}



            //if (pixel_matches_per_location.Count == 0)
            //    return null;

            //var max_match = pixel_matches_per_location.MaxElement(x => x.Value);
            //if (max_match.Value >= MATCHES_THRESHOLD)
            //    return max_match.Key;
            //return null;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var diablo = new Diablo();

            //TODO: add location detection foo returning enum
            using var text_detector = new TextDetector();
            var rune_detector = new RuneDetector();

            // Demo 1: picking up runes
            while (true)
            {
                XY? rune = diablo.DetectRunes(rune_detector);
                if (rune != null)
                {
                    Console.WriteLine("Found rune");
                    diablo.PickUpItem(rune.Value.x, rune.Value.y);

                    Thread.Sleep(1000);
                }
                Thread.Sleep(100);
            }

            // TODO: add turning on map after starting the game
            //Console.WriteLine($"current location: {diablo.DetectMapLocation(text_detector)}");

            //var bmp = diablo.DumpBitmap();
            //bmp = bmp.Clone(new Rectangle(300, 200, 300, 300), PixelFormat.Format32bppRgb);
            //var bmp = new Bitmap(@"C:\tmp\img_15_12_35_.png");
            //var found_runes = rune_detector.FindRunes(bmp);
            //Console.WriteLine($"Found runes: {found_runes.Count}");
            //Console.WriteLine($"detected items: {text_detector.Detect(bmp)}"); // TODO: can detect items by just spotting the color of the runes

            //// Menu options:
            //diablo.LeftClick(400, 333);  // Single Player
            //diablo.DoubleLeftClick(200, 150);  // 1st character
            //diablo.LeftClick(400, 305);  // Normal difficulty
            //Thread.Sleep(1500);

            //// Pick up dead body
            //diablo.Click(400, 300);
            //Thread.Sleep(500);

            //// Go from Kurast Docks to Lower Kurast 
            //diablo.Click(750, 350);
            //Thread.Sleep(1500);
            //diablo.Click(750, 150);
            //Thread.Sleep(1500);
            //diablo.Click(750, 150);
            //Thread.Sleep(1500);
            //diablo.Click(750, 150);
            //Thread.Sleep(1500);
            //diablo.Click(750, 336);
            //Thread.Sleep(1500);
            //diablo.Click(750, 150);
            //Thread.Sleep(1500);
            //diablo.Click(750, 150);
            //Thread.Sleep(1500);
            //diablo.Click(550, 400);
            //Thread.Sleep(1500);
            //diablo.Click(175, 300);
            //Thread.Sleep(100);

            ////Back To main menu
            //diablo.PressEsc();
            //Thread.Sleep(100);
            //diablo.LeftClick(390, 290);
            //Thread.Sleep(1000);
        }
    }
}
