namespace FluidBus.Core.Tasks
{
	public class FluidTask
	{
		private Task _task;
		public bool IsCompleted => this._task.IsCompleted;
		public bool IsFailed => this._task.IsFaulted;

		public FluidTask(Action action)
			=> this._task = Task.Run(action);
		public FluidTask(Func<bool> action)
			=> this._task = Task.Run(action);

		public FluidTaskState GetState()
			=> _task.IsCanceled ? FluidTaskState.Cancelled
			 : _task.IsFaulted ? FluidTaskState.Failed
			 : _task.IsCompleted ? FluidTaskState.Completed
			 : FluidTaskState.Running;

		public void Wait()
			=> _task.Wait();

		public FluidTask OnComplete(Action<FluidTaskState> callback)
		{
			_task = _task.ContinueWith(t => callback(GetState()));
			return this;
		}
	}
}
