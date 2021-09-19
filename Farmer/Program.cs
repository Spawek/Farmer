using SysCalls;

namespace Farmer
{
    class Program { 
        static void Main(string[] args)
        {
            Syscall.AllocConsole();

            var scenario = new FarmingScenario();
            scenario.Loop();
        }
    }
}
