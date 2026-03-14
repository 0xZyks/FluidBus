namespace FluidBus.Core.Instructions
{
    public delegate void FluidMethod<T>(T data);
    public delegate TResult FluidFunc<T, TResult>(T data);
    public delegate void FluidResult(object? result);

    public abstract class FluidInstruction<T> : IFluidInstruction
    {
        public Type DataType => typeof(T);
        public T? Data { get; protected set; }

        public event FluidResult? OnResult;

        private FluidMethod<T>[]? _methods;
        private FluidFunc<T, object>[]? _funcs;

        protected FluidInstruction(T? data, params FluidMethod<T>[] methods)
        {
            Data = data;
            _methods = methods;
        }

        protected FluidInstruction(T? data, params FluidFunc<T, object>[] funcs)
        {
            Data = data;
            _funcs = funcs;
        }

        public virtual void Execute()
        {
            foreach (var method in _methods ?? [])
                method?.Invoke(Data!);
        }

        public virtual object? ExecuteAndGet()
        {
            object? result = null;
            foreach (var func in _funcs ?? [])
                result = func?.Invoke(Data!);
            OnResult?.Invoke(result);
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
