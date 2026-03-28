namespace FluidBus.Core.Interfaces
{
	public interface IFluidHandler
	{
		string Id { get; }
		int CallCount { get; set; }

		Type EventType { get; }

		abstract bool Handle(IFluidEvent evt);
	}
}
