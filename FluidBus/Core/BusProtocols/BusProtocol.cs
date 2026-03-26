namespace FluidBus.Core.BusProtocols
{
	public enum ExecutionStrategy
	{
		Sync = 0,
		Async = 1,
	}

	public abstract class BusProtocol
	{
		public string Name { get; }
		public abstract ExecutionStrategy Strategy { get; }

		protected BusProtocol(string name)
			=> this.Name = name;

		public static readonly BusProtocol System = new SystemProtocol();

        public override bool Equals(object? obj)
            => obj is BusProtocol other && Name.Equals(other.Name, StringComparison.Ordinal);

        public override int GetHashCode()
            => Name.GetHashCode(StringComparison.Ordinal);
	}
}
