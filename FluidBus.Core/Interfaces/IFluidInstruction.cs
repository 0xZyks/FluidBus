namespace FluidBus.Core.Interfaces
{
	public interface IFluidInstruction
	{
		Type DataType { get; }

		object? Execute();
        bool HasMethod { get; }
	}
}
