using FluidBus;
using FluidBus.Core;
using FluidBus.Core.VM;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Instructions.Core;
using FluidBus.Event;
using FluidBus.Handler;
using System.Text;

namespace LoggerTestBusCSharpPython
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Fluid.Guard PoC ===\n");

            FBus.Register(new CoreHandler("core"));

            // Token initial pour opcode 0x04
            byte[] token = FluidCoreAPI.RequestToken(0x04);
            Console.WriteLine($"Token initial : {string.Join(", ", token)}");

            // Premier appel
            var instr = new RustInstruction(
                token,
                Encoding.UTF8.GetBytes("Fluid.Guard")
            );

            instr.OnResult += (result) => {
                if (result is VMResult vm)
                {
                    Console.WriteLine($"Method     : {vm.Method}");
                    Console.WriteLine($"Next token : {string.Join(", ", vm.NextToken)}");
                    Console.WriteLine($"Result     : {vm.Result ?? "void"}");
                }
                else
                    Console.WriteLine("Pas le bon retour");
            };

            FBus.Publish(new CoreEvent(
                "core_evt",
                BusProtocol.System,
                instr
            ));

            Console.WriteLine();

            // Deuxieme appel avec token mis a jour depuis instr.Data
            var instr2 = new RustInstruction(
                instr.Data!,
                Encoding.UTF8.GetBytes("Hello from MiniVM !")
            );

            instr2.OnResult += (result) => {
                if (result is VMResult vm)
                {
                    Console.WriteLine($"Method     : {vm.Method}");
                    Console.WriteLine($"Next token : {string.Join(", ", vm.NextToken)}");
                    Console.WriteLine($"Result     : {vm.Result ?? "void"}");
                }
                else
                    Console.WriteLine("Pas le bon retour");
            };

            FBus.Publish(new CoreEvent(
                "core_evt_2",
                BusProtocol.System,
                instr2
            ));
        }
    }
}
