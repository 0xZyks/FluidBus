using FluidBus.Herits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Handlers.Core
{
	public class HandlerOpCode
	{
		private byte			opcode;
		private Type			evt;
		private IFluidHandler	handler;

		private	HandlerOpCode(byte opCode, IFluidHandler handler, Type evt) 
		{
			opcode = opCode;
			this.handler = handler;
			this.evt = evt;
		}

		public	IFluidHandler GetHandler()
			=> this.handler;
		public byte	GetOpcode()
			=> this.opcode;
		public Type	GetEvent() 
			=> this.evt;
		
		public static HandlerOpCode	CreateOpCode(byte opcode, IFluidHandler handler, Type evt)
			=> new HandlerOpCode(opcode, handler, evt);
	}
}
