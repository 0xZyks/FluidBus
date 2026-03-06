
namespace FluidBus.Core.Instructions.Core
{
	public class RustInstruction : FluidInstruction<byte[][]>
	{
		public RustInstruction(byte[][]? data, params FluidMethod<byte[][]>[] methods) : base(data, methods)
		{ }

		public RustInstruction(byte[][]? data, params FluidFunc<byte[][], object>[] funcs) : base(data, funcs)
		{ }
	}
}
