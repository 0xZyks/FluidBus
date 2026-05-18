using System.IO.MemoryMappedFiles;

namespace FluidBus.Memory.Transport
{
	public class WindowsMemoryTransport : IMemoryTransport
	{
		private readonly MemoryMappedFile _writeChannel;
		private readonly MemoryMappedFile _readChannel;
		private readonly int _size;

		public WindowsMemoryTransport(string name, int size, bool create)
		{
			this._size = size;

			string hcName = $"FluidBus_{name}_HC";
			string chName = $"FluidBus_{name}_CH";

			if (create)
			{
				this._writeChannel = MemoryMappedFile.CreateNew(hcName, size);
				this._readChannel = MemoryMappedFile.CreateNew(chName, size);
			}
			else
			{
				this._writeChannel = MemoryMappedFile.OpenExisting(chName);
				this._readChannel = MemoryMappedFile.OpenExisting(hcName);
			}
		}

		public void Write(byte[] buffer)
		{
			using var accessor = this._writeChannel.CreateViewAccessor();
			accessor.WriteArray(0, buffer, 0, buffer.Length);
		}

		public byte[] Read(int length)
		{
			using var accessor = this._readChannel.CreateViewAccessor();
			var buffer = new byte[length];
			accessor.ReadArray(0, buffer, 0, length);
			return buffer;
		}

		public void Clear(int length)
		{
			using var accessor = this._readChannel.CreateViewAccessor();
			accessor.WriteArray(0, new byte[length], 0, length);
		}

		public void Dispose()
		{
			this._writeChannel.Dispose();
			this._readChannel.Dispose();
		}
	}
}
