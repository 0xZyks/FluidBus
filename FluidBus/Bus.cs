using FluidBus.Events;
using FluidBus.Handlers;
using FluidBus.Handlers.Core;
using FluidBus.Herits;
using FluidBus.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus
{
	public class Bus
	{
		public static void	Init()
		{
			HandlerLinq.RegisterNewOpCode(0x01, new BusLogHandler(), typeof(BusLogEvent));
			RegisterByHandler(new BusLogHandler());
		}

		public static void	RegisterByHandler<T>(FluidHandler<T> handler)
			=> HandlerLinq.Register(handler);
		public static void	RegisterByOpCode(byte opcode)
			=> HandlerLinq.Register(opcode);

		public static void	DropByHandler(IFluidHandler handler)
			=> HandlerLinq.Drop(handler);
		public static void DropByOpCode(byte opcode)
			=> HandlerLinq.Drop(opcode);

		public static bool	TryGetHandlersByOpCode(byte opcode, out IFluidHandler? handlers)
			=> HandlerLinq.TryResolve(opcode, out handlers);
		public static bool	TryGetHandlersByEvent(IFluidEvent evt, out IFluidHandler? handlers)
			=> HandlerLinq.TryResolve(evt, out handlers);

		public static bool	TryGetHandlers(byte opcode, out List<IFluidHandler>? handlers)
		{
			handlers = HandlerLinq.GetHandlers(opcode);

			if (handlers == null)
				return (false);

			return (true);
		}

		public static bool	TryGetHandlers(IFluidHandler evtType, out List<IFluidHandler>? handlers)
		{
			handlers = HandlerLinq.GetHandlers(evtType);

			if (handlers == null)
				return (false);

			return (true);
		}

		public static bool	Publish<T>(T evt)
		{
			((dynamic)evt!).Dispatch();
			return (true);
		}
	}
}
