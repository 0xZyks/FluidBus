namespace FluidBus.Core.Protocols
{
	public class SystemProtocol : BusProtocol
	{
		public override ExecutionStrategy Strategy => ExecutionStrategy.Sync;
		public SystemProtocol() : base ("SYSTEM")
		{ }
	}
}
