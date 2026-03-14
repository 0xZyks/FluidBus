using FluidBus;
using FluidBus.BluePrint;
using FluidBus.Core;
//using FluidBus.Core.VM;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using FluidBus.Core.Instructions.System;
using FluidBus.Errors;

//using FluidBus.Core.Instructions.Core;
using FluidBus.Event;
using FluidBus.Handler;
using System.Text;

namespace LoggerTestBusCSharpPython
{
	public class TestEvt : FluidEvent
	{
		public TestEvt(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base($"{nameof(TestEvt)}::{id}", protocol, instrs)
		{ }
	}

	public class TestHandler : FluidHandler<TestEvt>
	{
		public TestHandler(string id) : base($"{nameof(TestHandler)}::{id}")
		{ }

		public override bool Handle(IFluidEvent evt)
		{
			foreach (var instr in evt.Instructions)
			{
				instr.Execute();
				instr.ExecuteAndGet();
			}
			this.CallCount++;
			return true;
		}
	}

    internal class Program
    {
        static void Main(string[] args)
        {
			FBus.Register(new TestHandler("test_hdl"));

			var instr = new LogInstruction(
				"Hello From FluidBus",
				(data) => Console.WriteLine(data));

			instr.OnResult += (result) => {
				if (result != null)
					Console.WriteLine(result);
			};

			var (evt, success) = BluePrintFactory.NewEvent(
				typeof(TestEvt),
				"test_evt",
				BusProtocol.System,
				instr);

			FBus.Publish(evt);
			Console.WriteLine("Off");
        }
    }
}
