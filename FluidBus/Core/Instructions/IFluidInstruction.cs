using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Instructions
{
	public interface IFluidInstruction
	{
		Type DataType { get; }

		void Execute();
		object? ExecuteAndGet();
	}
}
