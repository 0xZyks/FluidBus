using FluidBus.Core.Interfaces;
using FluidBus.Core.Errors;

namespace FluidBus.Core.Abstracts
{
	public delegate object? FluidCallBack(object? data);

    public abstract class FluidInstruction<T> : IFluidInstruction
    {
        public Type DataType => typeof(T);
        public T? Data { get; protected set; }

        public event FluidCallBack? OnResult;

        private List<FluidCallBack>? _callbacks;

        private HashSet<string> _methodsId;

        protected FluidInstruction(T? data, params FluidCallBack[] methods)
        {
            int instrcount = 0;
            this.Data = data;
            this._callbacks = new();
            this._methodsId = new();
            foreach (var meth in methods)
                this.AddMethod((instrcount++).ToString(), meth);
        }

        public virtual void AddMethod(string id, FluidCallBack method)
        {
            if (this._callbacks != null && !this._methodsId.Contains(id))
            { this._callbacks.Add(method); this._methodsId.Add(id); }
        }

        public virtual object? Execute()
        {
            if (_callbacks is null || _callbacks.Count == 0)
                throw new InstructionException($"No funcs to execute on instruction '{typeof(T).Name}'");
            object? result = null;
            foreach (var meth in _callbacks)
            {
                if (Data is null)
                    throw new InstructionException($"Data is null on instruction '{typeof(T).Name}'");
                result = meth.Invoke(Data);
            }
            OnResult?.Invoke(result);
            return result;
        }

        public bool HasMethod
        { get { return _callbacks is { Count: > 0 }; } }

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
