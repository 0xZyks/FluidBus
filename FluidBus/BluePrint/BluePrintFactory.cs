using FluidBus.Core;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.HLinq;
using FluidBus.Core.Instructions;
using FluidBus.Errors;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FluidBus.BluePrint
{
	public static class BluePrintFactory
	{
		public static (IFluidHandler, bool) NewHandler(IFluidHandler baseHdl)
		{
			try
			{
				IFluidHandler handler = CreateInstance<IFluidHandler>(baseHdl.GetType());
				if (!HandlerLinq.Register(handler))
					throw new HandlerLinqException("Can't Register this handler");
				return (handler, true);
			}
			catch (FluidBusError e)
			{ e.DisplayMessage(); return (null!, false); }
		}

		public static (IFluidEvent, bool) NewEvent(Type baseEvt, string name, BusProtocol protocol, params IFluidInstruction[] instrs)
		{
			try
			{ var evt = CreateInstance<IFluidEvent>(baseEvt, [name, protocol, instrs])!; return (evt, true); }
			catch (FluidBusError e)
			{ e.DisplayMessage(); return (null!, false); }
		}

		private static T CreateInstance<T>(Type concreteType, params object[] args) where T : class
		{
			try
			{ return (T)Activator.CreateInstance(concreteType, args)!; }
			catch (MissingMethodException)
			{ throw new BluePrintException("No matching constructor found"); }
			catch (TargetInvocationException e)
			{throw new BluePrintException($"Constructor threw : {e.InnerException?.Message}"); }
		}
	}
}
