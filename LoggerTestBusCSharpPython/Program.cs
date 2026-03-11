using FluidBus;
using FluidBus.Core.Herits;
using FluidBus.BluePrint;
using FluidBus.Core;
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
            FBus.Register(new CoreHandler("core"));

            var instr = new RustInstruction(
                    0x01,
                    (data) => FluidCoreAPI.Send(data)
            );

            instr.OnResult += (result) => {
                if (result is byte[] bytes)
                    Console.WriteLine($"C# Received: {Encoding.UTF8.GetString(bytes)}");
            };

            var evt = new CoreEvent(
                    "core_evt",
                    BusProtocol.System,
                    [instr]
            );

            FBus.Publish(evt);

            Test();
		}

        static void Test()
        {
            byte[] token = FluidCoreAPI.RequestToken(0x01);
            Console.WriteLine($"Token v1: {string.Join(", ", token)}");

            token = FluidCoreAPI.Rotate(token);
            Console.WriteLine($"Token v2: {string.Join(", ", token)}");

            token = FluidCoreAPI.Rotate(token);
            Console.WriteLine($"Token v3: {string.Join(", ", token)}");
        }
	}

/*
	public class TestEvt : FluidEvent
	{
		public TestEvt(string name, BusProtocol protocol, params IFluidInstruction[] instrs) : base($"[EVT::{nameof(TestEvt)}::{name}]", protocol, instrs)
		{ }
	}

	public class TestHdl : FluidHandler<TestEvt>
	{
		public TestHdl(string name) : base ($"[HDL::{nameof(TestHdl)}::{name}]")
		{ }

		public override bool Handle(IFluidEvent evt)
		{
			foreach (var instr in evt.Instructions)
			{
				instr.Execute();
				instr.ExecuteAndGet();
			}
			return true;
		}
	}
*/
}
