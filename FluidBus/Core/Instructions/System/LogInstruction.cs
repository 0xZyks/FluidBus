using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Instructions.System
{
	public class LogInstruction : FluidInstruction<string>
	{
		public LogInstruction(string? data, params FluidMethod<string>[] methods) : base(data, methods)
		{ }

		public LogInstruction(string? data, params FluidFunc<string, object>[] funcs) : base(data, funcs)
		{ }
	}
}
