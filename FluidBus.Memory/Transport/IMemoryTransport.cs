namespace FluidBus.Memory.Transport
{
	public interface IMemoryTransport : IDisposable
	{
		void Write(byte[] buffer);
		byte[] Read(int length);
		void Clear(int length);
	}
}
