using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FluidBus.Core.Instructions
{
	public delegate void FluidMethod<T>(T data);
	public delegate TResult FluidFunc<T, TResult>(T data);

	public abstract class FluidInstruction<T> : IFluidInstruction
	{
		public Type DataType => typeof(T);
		public T? Data { get; protected set; }

		private FluidMethod<T>[]? _methods;
		private FluidFunc<T, object>[]? _funcs;

		public FluidInstruction(T? data, params FluidMethod<T>[] methods)
		{
			this.Data = data;
			this._methods = methods;
		}

		public FluidInstruction(T? data, params FluidFunc<T, object>[] funcs)
		{
			this.Data = data;
			this._funcs = funcs;
		}

		public void Execute()
		{
			foreach (var method in this._methods ?? [])
				method?.Invoke(this.Data!);
			foreach (var func in this._funcs ?? [])
				func?.Invoke(this.Data!);
		}

		public object? ExecuteAndGet()
		{
			object? result = null;
			foreach (var func in this._funcs ?? [])
				result = func?.Invoke(this.Data!);
			return result;
		}

		public override bool Equals(object? obj)
		{
			if (obj is FluidInstruction<T> other)
				return EqualityComparer<T>.Default.Equals(Data, other.Data);
			return false;
		}

		public override int GetHashCode()
			=> HashCode.Combine(typeof(T), Data);
	}
}
