using FluidBus.Core.Abstracts;
using FluidBus.Memory.Transport;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.FluidBus.Memory.Core
{
	public class FluidIPC : IDisposable
	{
		private IMemoryTransport memoryChannel;
		private readonly Mutex writeMutex;
		private readonly Mutex readMutex;

		public FluidIPC(string name, bool isHost = false)
		{
			this.memoryChannel = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
				new WindowsMemoryTransport(name, 1024 * 1024,isHost) : 
				new UnixMemoryTransport(name, isHost);

			string wMutex = isHost ? $"FluidBusWrite_{name}_host" : $"FluidBusWrite_{name}_child";
			string rMutex = isHost ? $"FluidBusRrite_{name}_child" : $"FluidBusRrite_{name}_host";

			this.writeMutex = new Mutex(false, wMutex);
			this.readMutex = new Mutex(false, rMutex);
		}

		public void Write(byte[] buffer)
		{
			this.writeMutex.WaitOne();
			this.memoryChannel.Write(buffer);
			this.writeMutex.ReleaseMutex();
		}

		public byte[] Read(int len)
		{
			byte[] data;
			this.readMutex.WaitOne();
			data = this.memoryChannel.Read(len);
			this.readMutex.ReleaseMutex();
			return data;
		}

		public void Clear(int length)
		{
			this.readMutex.WaitOne();
			this.memoryChannel.Clear(length);
			this.readMutex.ReleaseMutex();
		}

		public void OnReceive(FluidCallBack callback)
		{
			Task.Run(() =>
			{
				while (true)
				{
					var data = this.Read(256);
					this.Clear(256);
					string msg = Encoding.UTF8.GetString(data).TrimEnd('\0');
					if (!string.IsNullOrEmpty(msg))
						callback(msg);
					else
						Thread.Sleep(10);
				}
			});
		}

		public void Dispose()
		{
			this.memoryChannel.Dispose();
			this.readMutex.Dispose();
			this.writeMutex.Dispose();
		}
	}
}
