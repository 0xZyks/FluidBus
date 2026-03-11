
namespace FluidBus.Core.Instructions.Core
{
	public class RustInstruction : FluidInstruction<byte[]>
	{
        private byte[] _token;

		public RustInstruction(byte opcode, params FluidFunc<byte[], object>[] funcs) : base(null, funcs)
		{ this._token = FluidCoreAPI.RequestToken(opcode); this.Data = this._token; }

        public override void Execute()
        {
            base.Execute();
            this._token = FluidCoreAPI.Rotate(this._token);
        }

        public override object? ExecuteAndGet()
        {
            var result = base.ExecuteAndGet();
            this._token = FluidCoreAPI.Rotate(this._token);
            this.Data = this._token;
            return result;
        }
	}
}
