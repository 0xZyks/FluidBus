using FluidBus.BluePrint;
using FluidBus.Core;
using FluidBus;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using FluidBus.Core.Instructions.System;
using FluidBus.Event;
using System.Xml.Linq;

namespace LoggerTestBusCSharpPython
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var (busEvt, success) = BluePrintFactory
				.NewEvent(
					typeof(TestEvt),
					"test",
					BusProtocol.System,
					[new LogInstruction("CoucouFeur", msg => Console.WriteLine(msg)),
					new LogInstruction("Hehe", msg => Console.WriteLine(msg))]
				);

			FBus.Register(new TestHdl("Bite"));
			FBus.Publish(busEvt);
		}

	}

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
}
