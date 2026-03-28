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

        private List<FluidMethod<T>>? _methods;
        private List<FluidFunc<T, object>>? _funcs;

        private HashSet<string> _methodsId;
        private HashSet<string> _funcsId;

        protected FluidInstruction(T? data, params FluidMethod<T>[] methods)
        {
            int instrcount = 0;
            this.Data = data;
            this._methods = new();
            this._methodsId = new();
            foreach (var meth in methods)
                this.AddMethod((instrcount++).ToString(), meth);
        }

        protected FluidInstruction(T? data, params FluidFunc<T, object>[] funcs)
        {
            int instrcount = 0;
            this.Data = data;
            this._funcs = new();
            this._funcsId = new();
            foreach (var func in funcs)
                this.AddFunc((instrcount++).ToString(), func);
        }

        public virtual void AddMethod(string id, FluidMethod<T> method)
        {
            if (this._methods != null && !this._methodsId.Contains(id))
            { this._methods.Add(method); this._methodsId.Add(id); }
        }

        public virtual void AddFunc(string id, FluidFunc<T, object> func)
        {
            if (this._funcs != null && !this._funcsId.Contains(id))
            { this._funcs.Add(func); this._funcsId.Add(id); }
        }

        public virtual void Execute()
        {
            if (_methods is null || _methods.Count == 0)
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
            if (_funcs is null || _funcs.Count == 0)
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
        { get { return _methods is { Count: > 0 }; } }

        public bool HasFuncs
        { get { return _funcs is { Count: > 0 }; } }

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
