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
    // Skills must be mapped that way in the game
    enum Skill
    {
        Blink = 1,  // F1
        Orb = 2,  // F2
        FrostShield = 3,  // F3
        EnergyShield = 4,  // F4
    }

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

        public void SelectSkill(Skill skill)
        {
            int skill_no = (int)skill;
            if (skill_no < 1 || skill_no > 8)
                throw new ArgumentException($"invalid skill: {skill}");
            User32.SetForegroundWindow(window_handle);
            ushort key = (ushort)(User32.VK_F1 + skill_no - 1);
            User32.PostMessage(window_handle, User32.WM_KEYDOWN, key, 0);
            Thread.Sleep(5);
        }

        public void Blink(int x, int y)
        {
            SelectSkill(Skill.Blink);
            RightClick(x, y);
        }

        public void CastBuffs()
        {
            Thread.Sleep(100);
            SelectSkill(Skill.EnergyShield);
            RightClick(400, 300);
            Thread.Sleep(500);
            SelectSkill(Skill.FrostShield);
            RightClick(400, 300);
            Thread.Sleep(500);
        }

        private void MoveWindow(int x, int y)
        {
            User32.GetWindowRect(window_handle, out User32.Rect rect);
            User32.MoveWindow(window_handle, x, y, rect.right - rect.left, rect.bottom - rect.top, false);
        }

        public void PressEsc()
        {
            User32.SetForegroundWindow(window_handle);
            
            // there seems to be some interference
            Thread.Sleep(5);
            User32.PostMessage(window_handle, User32.WM_KEYUP, User32.VK_CONTROL, 15);
            Thread.Sleep(5);

            User32.PostMessage(window_handle, User32.WM_KEYDOWN, User32.VK_ESCAPE, 0);
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

        public XY? DetectRunes(TemplateDetector rune_detector)
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
            public const ushort VK_F1 = 0x70;

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

    public class TemplateDetector
    {
        public XY? FindRune(Bitmap bmp)
        {
            return FindTemplate(bmp, rune_template, 0.9);
        }

        public XY? FindChest(Bitmap bmp)
        {
            var ret = FindTemplate(bmp, chest_template, 0.9);
            if (ret != null)
                return ret;

            return FindTemplate(bmp, chest2_template, 0.9);
        }

        public XY? FindDeadBody(Bitmap bmp)
        {
            return FindTemplate(bmp, dead_body_template, 0.9);
        }

        public XY? FindOrmus(Bitmap bmp)
        {
            var ret = FindTemplate(bmp, ormus1_template, 0.6);
            if (ret != null)
                return ret;

            ret = FindTemplate(bmp, ormus2_template, 0.6);
            if (ret != null)
                return ret;

            return FindTemplate(bmp, ormus3_template, 0.6);
        }

        public XY? FindWaypoint(Bitmap bmp)
        {
            return FindTemplate(bmp, waypoint_template, 0.8);
        }

        private static Mat LoadBitmap(string path)
        {
            var bmp = new Bitmap(path);
            var tmp = new Bitmap(bmp).Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.Format32bppRgb);
            return OpenCvSharp.Extensions.BitmapConverter.ToMat(tmp);
        }

        private static XY? FindTemplate(Bitmap bmp, Mat template, double threshold)
        {
            var image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            var result = new Mat();
            
            Cv2.MatchTemplate(image, template, result, TemplateMatchModes.CCoeffNormed);

            var min_index = new int[2];
            var max_index = new int[2];
            result.MinMaxIdx(out double min, out double max, min_index, max_index);
            if (max > threshold)
                return new XY(max_index[1] + template.Width / 2, max_index[0] + template.Height / 2);
            return null;
        }

        private Mat rune_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\rune.png");
        private Mat chest_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\chest.png");
        private Mat chest2_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\chest2.png");
        private Mat dead_body_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\dead_body.png");
        private Mat ormus1_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\ormus1.png");
        private Mat ormus2_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\ormus2.png");
        private Mat ormus3_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\ormus3.png");
        private Mat waypoint_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\waypoint.png");
    }

    public  class RandomWalk
    {
        private int line = 0;
        private int step = 0;
        public XY Next()
        {
            var ret = blink_points[line % blink_points.Count];

            step++;
            if (step > line)
            {
                step = 0;
                line++;
            }

            return ret;
        }

        private const int min_x = 100;
        private const int max_x = 680;
        private const int min_y = 130;
        private const int max_y = 450;
        private List<XY> blink_points = new List<XY> { new XY(min_x, min_y), new XY(max_x, min_y), new XY(max_x, max_y), new XY(min_x, max_y) };
    }

    class Program
    {

        static void Scenario1_PickUpRunes()
        {
            var diablo = new Diablo();
            var template_detector = new TemplateDetector();

            while (true)
            {
                XY? rune = diablo.DetectRunes(template_detector);
                if (rune != null)
                {
                    Console.WriteLine("Found rune");
                    diablo.PickUpItem(rune.Value.x, rune.Value.y);

                    Thread.Sleep(1500);
                }
                Thread.Sleep(100);
            }
        }

        static void Scenario2_OpenChests()
        {
            var diablo = new Diablo();
            var template_detector = new TemplateDetector();

            while (true)
            {
                XY? chest = template_detector.FindChest(diablo.DumpBitmap());
                if (chest != null)
                {
                    Console.WriteLine("Found chest");
                    diablo.Click(chest.Value.x, chest.Value.y);

                    Thread.Sleep(1500);
                }
                Thread.Sleep(100);
            }
        }

        // REMEMBER TO BUY KEYS!
        static void Scenario3_FarmLowerKurast(bool forever)
        {
            var diablo = new Diablo();
            using var text_detector = new TextDetector();
            var template_detector = new TemplateDetector();
            var random_walk = new RandomWalk();

            diablo.DumpBitmap().Save(@"C:/tmp/tmp.png");

            do
            {
                diablo.LeftClick(400, 333);  // Single Player
                diablo.DoubleLeftClick(200, 150);  // 1st character
                                                   //diablo.LeftClick(400, 305);  // Normal difficulty
                diablo.LeftClick(400, 390);  // Hell difficulty
                Thread.Sleep(1500);

                // TODO: add using orb
                if (template_detector.FindDeadBody(diablo.DumpBitmap()) != null)
                {
                    diablo.Click(400, 300);
                    Thread.Sleep(500);
                }

                diablo.CastBuffs();

                // Go from Kurast Docks to Lower Kurast 
                diablo.Click(750, 350);
                Thread.Sleep(1500);
                diablo.Click(750, 150);
                Thread.Sleep(1500);
                diablo.Click(750, 150);
                Thread.Sleep(1500);
                diablo.Click(750, 150);
                Thread.Sleep(1500);
                var ormus_pos = template_detector.FindOrmus(diablo.DumpBitmap());
                if (ormus_pos != null)
                {
                    diablo.Click(ormus_pos.Value.x, ormus_pos.Value.y);
                    Thread.Sleep(1500);
                }
                else
                {
                    Console.WriteLine("Coulnd't find Ormus");
                }
                diablo.Click(750, 550);
                Thread.Sleep(1500);
                diablo.Click(750, 30);
                Thread.Sleep(1500);
                diablo.Click(750, 350);
                Thread.Sleep(1500);
                var waypoint_pos = template_detector.FindWaypoint(diablo.DumpBitmap());
                if (waypoint_pos != null)
                {
                    diablo.Click(waypoint_pos.Value.x, waypoint_pos.Value.y);
                    Thread.Sleep(1500);
                }
                else
                {
                    Console.WriteLine("Coulnd't find Waypoint");
                    goto END_RUN;
                }
                diablo.Click(175, 300);
                Thread.Sleep(100);

                for (int i = 0; i < 50; i++)
                {
                    var blink_point = random_walk.Next();
                    diablo.Blink(blink_point.x, blink_point.y);
                    Thread.Sleep(400);

                    for (int chest_try = 0; chest_try < 2; chest_try++)  // there may be 2 chests next to each other
                    {
                        XY? chest = template_detector.FindChest(diablo.DumpBitmap());
                        if (chest == null)
                            break;

                        Console.WriteLine("Found chest");
                        diablo.Click(chest.Value.x, chest.Value.y);

                        Thread.Sleep(1500);

                        for (int rune_try = 0; rune_try < 3; rune_try++)  // rune picking fails sometimes
                        {
                            XY? rune = diablo.DetectRunes(template_detector);
                            if (rune == null)
                                break;

                            Console.WriteLine("Found rune");
                            diablo.PickUpItem(rune.Value.x, rune.Value.y);

                            Thread.Sleep(1500);
                        }
                    }
                }

                END_RUN:
                Console.WriteLine("Ending the run");
                Thread.Sleep(400);
                diablo.PressEsc();
                Thread.Sleep(100);
                diablo.LeftClick(390, 290);
                Thread.Sleep(1000);
            }
            while (forever);

            // TODO: restart diablo every 10 runs?
            // TODO: check if it's in main menu when starting
        }

        static void Main(string[] args)
        {
            //Scenario1_PickUpRunes();
            //Scenario2_OpenChests();
            Scenario3_FarmLowerKurast(true); // forver
            //Scenario3_FarmLowerKurast(false); // once
        }
    }
}
