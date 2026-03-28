using FluidBus.Core.Interfaces;
using FluidBus.Core.Errors;

namespace FluidBus.Core.Abstracts
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
            if (_methods is null || _methods.Length == 0)
                throw new InstructionException($"No methods to execute on instruction '{typeof(T).Name}'");
            foreach (var method in _methods)
            {
                if (Data is null)
                    throw new InstructionException($"Data is null on instruction '{typeof(T).Name}'");
                method.Invoke(Data);
            }
        }

        public virtual object? ExecuteAndGet()
        {
            if (_funcs is null || _funcs.Length == 0)
                throw new InstructionException($"No funcs to execute on instruction '{typeof(T).Name}'");
            object? result = null;
            foreach (var func in _funcs)
            {
                if (Data is null)
                    throw new InstructionException($"Data is null on instruction '{typeof(T).Name}'");
                result = func.Invoke(Data);
            }
            OnResult?.Invoke(result);
            return result;
        }

        public bool HasMethod
        { get { return _methods is { Length: > 0 }; } }

        public bool HasFuncs
        { get { return _funcs is { Length: > 0 }; } }

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
