using FluidBus.Events;
using FluidBus.Herits;
using FluidBus.Utils;
using System.Runtime.InteropServices;
using FluidBus.Core;

namespace FluidBus.Handlers
{
	public unsafe class NativHandler : FluidHandler<NativEvent>
	{
		public NativHandler() : base (IdGenerator.GetNewId(IdGenerator.IdType.Handler))
		{ }

		public override void Handle(NativEvent evt)
		{
			if (evt == null)
				return;
			FluidInstruction instr = evt.FlInstr!;
			nint lib = this.loadLibrary(instr.GetLib());
			if (!tryGetAddress(lib, instr.MethodName, out nint addr))
				return;
			if (instr.TrySetInstruction((delegate* unmanaged[Cdecl]<char*, void>)addr))
				instr.Instruction(instr.Message);
			if (!this.tryFreeLib(lib))
				return;
		}

		private nint loadLibrary(string libName)
			=> NativeLibrary.Load(libName);

		private bool tryFreeLib(nint lib)
		{
			if (lib == IntPtr.Zero) 
				return (false);
			NativeLibrary.Free(lib);
			return (true);
		}

		private bool tryGetAddress(nint lib, string methName, out nint address)
		{
			if (NativeLibrary.TryGetExport(lib, methName, out address))
				return (true);

			return (false);
		}
	}
}
