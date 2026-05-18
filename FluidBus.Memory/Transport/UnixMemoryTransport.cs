using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace FluidBus.Memory.Transport
{
	public class UnixMemoryTransport : IMemoryTransport
	{
		private readonly Socket _writeChannel;
		private readonly Socket _readChannel;
		private readonly Socket _writeConnection;
		private readonly Socket _readConnection;
		private readonly bool _isHost;

		public UnixMemoryTransport(string name, bool create)
		{
			this._isHost = create;

			bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
			string hcPath = isLinux ? $"\0FluidBus_{name}_HC" : $"/tmp/FluidBus_{name}_HC";
			string chPath = isLinux ? $"\0FluidBus_{name}_CH" : $"/tmp/FluidBus_{name}_CH";

			this._writeChannel = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
			this._readChannel = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

			if (create)
			{
				if (!isLinux)
				{
					if (File.Exists(hcPath)) File.Delete(hcPath);
					if (File.Exists(chPath)) File.Delete(chPath);
				}

				this._writeChannel.Bind(new UnixDomainSocketEndPoint(hcPath));
				this._writeChannel.Listen(1);

				this._readChannel.Bind(new UnixDomainSocketEndPoint(chPath));
				this._readChannel.Listen(1);

				this._writeConnection = this._writeChannel.Accept();
				this._readConnection = this._readChannel.Accept();
			}
			else
			{
				this._writeChannel.Connect(new UnixDomainSocketEndPoint(chPath));
				this._readChannel.Connect(new UnixDomainSocketEndPoint(hcPath));

				this._writeConnection = this._writeChannel;
				this._readConnection = this._readChannel;
			}
		}

		public void Write(byte[] buffer)
			=> this._writeConnection.Send(buffer);

		public byte[] Read(int length)
		{
			var buffer = new byte[length];
			int total = 0;
			while (total < length)
			{
				int received = this._readConnection.Receive(buffer, total, length - total, SocketFlags.None);
				if (received <= 0) break;
				total += received;
			}
			return total == length ? buffer : buffer[..total];
		}

		public void Clear(int length)
		{

		}

		public void Dispose()
		{
			if (this._isHost)
			{
				this._writeConnection.Dispose();
				this._readConnection.Dispose();
			}
			this._writeChannel.Dispose();
			this._readChannel.Dispose();
		}
	}
}
