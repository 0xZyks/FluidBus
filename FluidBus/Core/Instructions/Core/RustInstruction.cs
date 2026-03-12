using FluidBus.Core.VM;

namespace FluidBus.Core.Instructions.Core
{
    public class RustInstruction : FluidInstruction<byte[]>
    {
        private byte[] _token;
        private readonly bool _isVmFlow;

        // Nouveau constructeur — token + arg, FluidVM dans la func
        public RustInstruction(byte[] token, byte[]? arg = null)
            : base(token, (data) => (object?)FluidVM.Run(data, arg))
            {
                _token = token;
                _isVmFlow = true;
                Data = _token;
            }

        // Constructeur legacy — opcode + funcs custom
        public RustInstruction(byte opcode, params FluidFunc<byte[], object>[] funcs)
            : base(null, funcs)
        {
            _token = FluidCoreAPI.RequestToken(opcode);
            _isVmFlow = false;
            Data = _token;
        }

        public override void Execute()
        {
            if (_isVmFlow) return; // VM flow — pas de rotation ici
            base.Execute();
            _token = FluidCoreAPI.Rotate(_token);
            Data = _token;
        }

        public override object? ExecuteAndGet()
        {
            var result = base.ExecuteAndGet();
            if (result is VMResult vm)
            {
                _token = vm.NextToken;
                Data = _token;
            }
            else if (!_isVmFlow)
            {
                _token = FluidCoreAPI.Rotate(_token);
                Data = _token;
            }
            return result;
        }
    }
}
