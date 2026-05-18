using FluidBus.Core.Abstracts;
using FluidBus.FluidBus.Memory.Core;
using FluidBus.Memory.Core;
using System.Diagnostics;

namespace FluidBus.Memory.Abstracts
{
	public enum ProcessState
	{
		Running,
		Stopped,
		Faulted,
	}

	public class FluidProcess : IDisposable
	{
		private List<FluidCallBack> callbacks;
		public string Name { get; set; }
		private Process? _process;
		private FluidIPC _ipc;
		private readonly object _lock;

		public FluidProcess(string name)
		{
			this.callbacks = new();
			this.Name = name;
			this._ipc = new FluidIPC(name, true);
			this._lock = new();
			this.Start();
			this.OnReceive();
		}

		public void AddConnection(FluidCallBack callback)
		{
			lock (this._lock)
			{
				if (this.callbacks.Contains(callback))
					return;
				this.callbacks.Add(callback);
			}
		}

		public void Send(byte[] data)
			=> this._ipc.Write(data);

		private void OnReceive()
		{
			Task.Run(() => 
			{
				while (true)
				{
					var buffer = this._ipc.Read(256);
					this._ipc.Clear(256);
					if (buffer.Length > 0)
						lock (this._lock)
							foreach (var callback in this.callbacks)
								callback(buffer);
					else
						Thread.Sleep(10);
				}
			});
		}

		private void Start()
		{
			this._process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Environment.ProcessPath,
					Arguments = $"--child {this.Name}",
					UseShellExecute = false
				}
			};
			this._process.Start();
		}

		private void Stop()
		{
			if (this._process is { HasExited: false})
				this._process.Kill();
		}

		public void Dispose()
		{
			this.Stop();
			this._ipc.Dispose();
			this._process?.Dispose();
		}
	}
}
