namespace FluidBus.Core.Errors
{
	public class ChannelException : FluidBusError
	{
		public ChannelException(string channelName, string message)
			: base($"[{nameof(ChannelException)}]: Channel '{channelName}' - {message}") { }
	}
}
