using FluidBus.Core;
using FluidBus.Herits;
using FluidBus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Events
{
	public unsafe class NativEvent : FluidEvent
	{
		public FluidInstruction? FlInstr { get; }
		public NativEvent(FluidInstruction @delegate) : base (IdGenerator.GetNewId(IdGenerator.IdType.Event))
			=> this.FlInstr = @delegate;
	}
}
