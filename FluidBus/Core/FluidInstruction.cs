using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Core
{
	public unsafe class FluidInstruction
	{
		private char*									message;
		private string									methName;
		private string									libName;
		private delegate* unmanaged[Cdecl]<char*, void>	_instruction;

		public FluidInstruction(char* msg, string meth, string lib)
		{
			this.message = msg;
			this.methName = meth;
			this.libName = lib;
		}

		public bool TrySetInstruction(delegate* unmanaged[Cdecl]<char*, void> instr)
		{
			if (instr == null || this._instruction != null)
				return (false);

			this._instruction = instr;
			return (true);
		}

		public string GetLib()
			=> this.libName;

		public char* Message
			=> this.message;
		public string MethodName
			=> this.methName;
		public delegate* unmanaged[Cdecl]<char*, void> Instruction
			=> this._instruction;
	}
}
