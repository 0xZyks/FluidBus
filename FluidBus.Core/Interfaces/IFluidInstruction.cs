namespace FluidBus.Core.Interfaces
{
	public interface IFluidInstruction
	{
		Type DataType { get; }

		void Execute();
		object? ExecuteAndGet();
        bool HasMethod { get; }
        bool HasFuncs { get; }
	}
}
