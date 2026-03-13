using dnlib.DotNet;

namespace FluidPacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var module = ModuleDefMD.Load("/home/zyks/Documents/FluidBus/LoggerTestBusCSharpPython/bin/Debug/net10.0/LoggerTestBusCSharpPython.dll");
            Console.WriteLine($"Module: {module.Name}");
        }
    }
}
