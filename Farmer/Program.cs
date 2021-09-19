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
using System.Windows.Interop;
using Cv2 = OpenCvSharp.Cv2;
using SysCalls;

namespace System.Windows.Input
{
    using ModifierKey = System.UInt16;
    using Key = System.UInt16;
}

namespace Farmer
{
    // Skills must be mapped that way in the game
    // Keep Orb on left mouse button
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

        public void Kill()
        {
            var processes = Process.GetProcessesByName("Game");
            foreach (var p in processes)
            {
                p.Kill(true);
            }
        }

        // FROM: https://stackoverflow.com/questions/891345/get-a-screenshot-of-a-specific-application/911225
        // Note: the function is slow (20ms per call), but it's not caused by `new`
        // TODO: try the other foo for fetching images from this stack post (the other doesn't work in the background though)
        public Bitmap DumpBitmap()
        {
            Syscall.SetForegroundWindow(window_handle);
            Syscall.GetWindowRect(window_handle, out SysCalls.Syscall.Rect rect);
            Bitmap bmp = new Bitmap(rect.right - rect.left, rect.bottom - rect.top, PixelFormat.Format32bppRgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            Syscall.PrintWindow(window_handle, hdcBitmap, 0);

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
            Syscall.SetForegroundWindow(window_handle);
            Syscall.GetWindowRect(window_handle, out SysCalls.Syscall.Rect rect);
            Syscall.SetCursorPos(x + rect.left, y + rect.top);
        }

        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        public void LeftClick(int x, int y)
        {
            Syscall.SetForegroundWindow(window_handle);
            SetCursor(x, y);
            Syscall.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Syscall.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
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
            Syscall.SetForegroundWindow(window_handle);
            SetCursor(x, y);
            Syscall.mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Syscall.mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public void MoveMouse(int x, int y)
        {
            Syscall.SetForegroundWindow(window_handle);
            SetCursor(x, y);
        }

        public void SelectSkill(Skill skill)
        {
            int skill_no = (int)skill;
            if (skill_no < 1 || skill_no > 8)
                throw new ArgumentException($"invalid skill: {skill}");
            Syscall.SetForegroundWindow(window_handle);
            ushort key = (ushort)(Syscall.VK_F1 + skill_no - 1);
            Syscall.PostMessage(window_handle, Syscall.WM_KEYDOWN, key, 0);
            Thread.Sleep(5);
        }

        public void Blink(int x, int y)
        {
            SelectSkill(Skill.Blink);
            RightClick(x, y);
        }

        public void CastOrb(int x, int y)
        {
            SelectSkill(Skill.Orb);
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
            Syscall.SetForegroundWindow(window_handle);
            Syscall.GetWindowRect(window_handle, out Syscall.Rect rect);
            Syscall.MoveWindow(window_handle, x, y, rect.right - rect.left, rect.bottom - rect.top, false);
        }

        public void PressEsc()
        {
            Syscall.SetForegroundWindow(window_handle);

            // there seems to be some interference
            Thread.Sleep(5);
            Syscall.PostMessage(window_handle, Syscall.WM_KEYUP, Syscall.VK_CONTROL, 15);
            Thread.Sleep(5);

            Syscall.PostMessage(window_handle, Syscall.WM_KEYDOWN, Syscall.VK_ESCAPE, 0);
            Thread.Sleep(5);
        }

        // https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keydown - lparam
        public void PutCtrlKeyDown()
        {
            Syscall.SetForegroundWindow(window_handle);
            Syscall.PostMessage(window_handle, Syscall.WM_KEYDOWN, Syscall.VK_CONTROL, 15);
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
            Syscall.SetForegroundWindow(window_handle);
            Syscall.PostMessage(window_handle, Syscall.WM_KEYUP, Syscall.VK_CONTROL, 0);
        }

        public XY? DetectItemsWorthPicking(TemplateDetector rune_detector)
        {
            ShowItems();
            var bmp = DumpBitmap();
            var rune = rune_detector.FindItemWorthPicking(bmp);
            return rune;
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
        public XY? FindItemWorthPicking(Bitmap bmp)
        {
            return FindTemplate(bmp, rune_template, 0.9) ??
                   FindTemplate(bmp, small_charm_template, 0.9) ??
                   FindTemplate(bmp, gold_ring_template, 0.9) ??
                   FindTemplate(bmp, gold_amulet_template, 0.9);
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
            return FindTemplate(bmp, ormus1_template, 0.6) ??
                   FindTemplate(bmp, ormus2_template, 0.6) ??
                   FindTemplate(bmp, ormus3_template, 0.6);
        }

        public XY? FindWaypoint(Bitmap bmp)
        {
            return FindTemplate(bmp, waypoint_template, 0.8);
        }

        public XY? FindKurastSpawnpoint(Bitmap bmp)
        {
            return FindTemplate(bmp, kurast_spawnpoint, 0.8);
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
        private Mat small_charm_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\small_charm.png");
        private Mat gold_ring_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\gold_ring.png");
        private Mat gold_amulet_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\gold_amulet.png");
        private Mat chest_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\chest.png");
        private Mat chest2_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\chest2.png");
        private Mat dead_body_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\dead_body.png");
        private Mat ormus1_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\ormus1.png");
        private Mat ormus2_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\ormus2.png");
        private Mat ormus3_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\ormus3.png");
        private Mat waypoint_template = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\waypoint.png");
        private Mat kurast_spawnpoint = LoadBitmap(@"C:\maciek\programowanie\Farmer\templates\kurast_spawnpoint.png");
    }

    public class RandomWalk
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
        private const int max_x = 730;
        private const int min_y = 130;
        private const int max_y = 450;
        private List<XY> blink_points = new List<XY> { new XY(min_x, min_y), new XY(max_x, min_y), new XY(max_x, max_y), new XY(min_x, max_y) };
    }

    class Program
    {

        static void Scenario1_PickUpItems()
        {
            var diablo = new Diablo();
            var template_detector = new TemplateDetector();

            while (true)
            {
                XY? rune = diablo.DetectItemsWorthPicking(template_detector);
                if (rune != null)
                {
                    Console.WriteLine("!!!!!!!!!!!!! Found item");
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

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        // REMEMBER TO BUY KEYS!
        // REMEMBER TO RUN VS AS ADMINISTRATOR SO IT CAN KILL DIABLO.
        // REMEMBER TO HIDE CONSOLE (AS CLICKING AT IT PAUSES THE GAME)
        static void Scenario3_FarmLowerKurast(bool forever)
        {
            int runs = 0;
            int items_found = 0;
            int chests_found = 0;

            DateTime start_time = DateTime.Now;
            do
            {
                using var text_detector = new TextDetector();
                var template_detector = new TemplateDetector();
                try
                {
                LOOP_START:
                    var random_walk = new RandomWalk();  // NOTE: this is statefull (initialize once per run)

                    Console.WriteLine($"Stats: runs:{++runs}, items found: {items_found}, chests found: {chests_found}, time elapsed: {DateTime.Now - start_time}");

                    var diablo = new Diablo();

                    diablo.DumpBitmap().Save(@"C:/tmp/tmp.png");
                    Thread.Sleep(300);
                    diablo.LeftClick(400, 333);  // Single Player
                    diablo.DoubleLeftClick(200, 150);  // 1st character
                                                       //diablo.LeftClick(400, 305);  // Normal difficulty
                    diablo.LeftClick(400, 390);  // Hell difficulty
                    Thread.Sleep(1500);

                    if (template_detector.FindKurastSpawnpoint(diablo.DumpBitmap()) == null)
                    {
                        Console.WriteLine("Doesn't look like Kurast spawnpoint: restarting the game");
                        diablo.Kill();
                        goto LOOP_START;
                    }

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

                    for (int i = 0; i < 70; i++)
                    {
                        var blink_point = random_walk.Next();
                        diablo.Blink(blink_point.x, blink_point.y);
                        Thread.Sleep(400);

                        //if ((i%3) == 0)
                        //{
                        //    diablo.CastOrb(blink_point.x, blink_point.y);
                        //    Thread.Sleep(600);
                        //}

                        for (int chest_try = 0; chest_try < 2; chest_try++)  // there may be 2 chests next to each other
                        {
                            XY? chest = template_detector.FindChest(diablo.DumpBitmap());
                            if (chest == null)
                                break;

                            Console.WriteLine("Found chest");
                            chests_found++;
                            diablo.Click(chest.Value.x, chest.Value.y);

                            Thread.Sleep(1500);

                            for (int item_try = 0; item_try < 3; item_try++)  // rune picking fails sometimes
                            {
                                XY? item = diablo.DetectItemsWorthPicking(template_detector);
                                if (item == null)
                                    break;

                                Console.WriteLine("!!!!!!!!!!!!! Found item");
                                items_found++;
                                diablo.PickUpItem(item.Value.x, item.Value.y);

                                Thread.Sleep(1500);
                            }
                        }
                    }

                END_RUN:
                    Console.WriteLine($"Ending the run");
                    Thread.Sleep(400);
                    diablo.PressEsc();
                    Thread.Sleep(100);
                    diablo.LeftClick(390, 290);
                    Thread.Sleep(1000);

                    if ((runs % 10) == 0)
                    {
                        Thread.Sleep(5000);
                        Console.WriteLine("Killing the process after 10 runs");
                        diablo.Kill();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Uncaught exception: {e}");
                }
            }
            while (forever);

            // TODO: restart diablo every 10 runs?
            // TODO: check if it's in main menu when starting

            // 1st night: 4 runes / hour, then clicked in console and paused
            // BUG: bot clicked in the console somehow which paused the run
            // Bot picked few random items
            // most of keys are gone
            // TODO: screen was asleep in the morning - it couldve been the reason the bot stopped

            // TODO: make move window work on single screen
            // TODO: fixing items + buying keys?
            // TODO: throwing away shitty runes
            // TODO: add pause/unpause bot by clicking some button
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        static void Main(string[] args)
        {
            AllocConsole();

            //Scenario1_PickUpItems();
            //Scenario2_OpenChests();
            Scenario3_FarmLowerKurast(true); // forver
            //Scenario3_FarmLowerKurast(false); // once
        }
    }
}
