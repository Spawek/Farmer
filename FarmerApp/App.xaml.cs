using Farmer;
using SysCalls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FarmerApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
            Syscall.AllocConsole();

            farming_thread = new Thread(new ThreadStart(FarmingLoop));
            scenario = new FarmingScenario();
            farming_thread.Start();
        }

        private void FarmingLoop()
        {
            Thread.Sleep(TimeSpan.FromSeconds(4));  // wait for the app to initialize

            scenario.Loop();
        }

        public static FarmingScenario scenario;
        private static Thread farming_thread;
    }
}
