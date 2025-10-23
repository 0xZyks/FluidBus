using FluidBus.Herits;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Handlers.Core
{
	public class HandlerLinq
	{
		private static HashSet<HandlerOpCode>	opHandlers = new();
		private static List<IFluidHandler>		handlers = new();

		public static bool	RegisterNewOpCode(byte opcode, IFluidHandler handler, Type evt)
		{
			List<byte> temp = new();
			foreach (var handlerOpCode in opHandlers)
				temp.Add(handlerOpCode.GetOpcode());

			if (temp.Contains(opcode))
				return (false);
			
			opHandlers.Add(HandlerOpCode.CreateOpCode(opcode, handler, evt));
			return (true);
		}

		public static bool	Register(byte opCode)
		{
			foreach (var handlerOpCode in opHandlers)
			{
				if (handlerOpCode.GetOpcode() == opCode)
				{
					if (!handlers.Contains(handlerOpCode.GetHandler()))
					{
						handlers.Add(handlerOpCode.GetHandler());
						return (true);
					}
				}
			}

			return (false);
		}

		public static bool	Register(IFluidHandler handler)
		{
			foreach (var handlerOpCode in opHandlers)
			{
				if (handlerOpCode.GetHandler().GetType() == handler.GetType() && !handlers.Contains(handler))
				{
					handlers.Add(handler);
					return (true);
				}
			}

			return (false);
		}

		public static bool	Drop(byte opCode)
		{
			foreach (var handlerOpCode in opHandlers)
			{
				var currhdl = handlerOpCode.GetHandler();
				if (handlers.Contains(currhdl) && handlerOpCode.GetOpcode().Equals(opCode))
				{
					handlers.Remove(currhdl);
					return (true);
				}
			}

			return (false);
		}

		public static bool	Drop(IFluidHandler handler)
		{
			foreach (var handlerOpCode in opHandlers)
			{
				var currhdl = handlerOpCode.GetHandler();
				if (handlers.Contains(currhdl))
				{
					handlers.Remove(currhdl);
					return (true);
				}
			}

			return (false);
		}

		public static List<IFluidHandler>	GetHandlers(byte opcode)
		{
			List<IFluidHandler> temp = new();

			foreach (var hdl in opHandlers)
			{
				if (hdl.GetOpcode().Equals(opcode))
				{
					foreach(var handler in handlers)
						if (handler.GetType().Equals(hdl.GetHandler().GetType()))
							temp.Add(handler);		
				}
			}

			return (temp);
		}

		public static List<IFluidHandler>	GetHandlers(IFluidHandler handler)
		{
			List<IFluidHandler> temp = new();

			foreach (var hdl in opHandlers)
			{
				if (hdl.GetHandler().GetType().Equals(handler.GetType()))
				{
					foreach(var target in handlers)
						if (handler.GetType().Equals(hdl.GetHandler().GetType()))
							temp.Add(target);		
				}
			}

			return (temp);
		}

		public static bool	TryResolve(byte opCode, out IFluidHandler handler)
		{
			foreach(var handlerOpCode in opHandlers)
			{
				if (handlerOpCode.GetOpcode().Equals(opCode))
				{
					handler = handlerOpCode.GetHandler();
					return (true);
				}
			}

			handler = null!;
			return (false);
		}

		public static bool	TryResolve(IFluidEvent evt, out IFluidHandler handler)
		{
			foreach(var handlerOpCode in opHandlers)
			{
				if (handlerOpCode.GetEvent().Equals(evt.GetType()))
				{
					handler = handlerOpCode.GetHandler();
					return (true);
				}
			}

			handler = null!;
			return (false);
		}
	}
}
