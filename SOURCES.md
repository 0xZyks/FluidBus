## Code source complet

Tous les fichiers du projet, organises par namespace/module avec les liens de dependance entre eux.

### Carte des dependances

```
FluidBus.Core.FBus
  ├── utilise → FluidBus.BluePrint.BluePrintFactory
  ├── utilise → FluidBus.Core.BusProtocols.BusProtocol
  ├── utilise → FluidBus.Core.Herits.IFluidEvent, IFluidHandler
  ├── utilise → FluidBus.Core.HLinq.HandlerLinq
  ├── utilise → FluidBus.Core.Tasks.FluidTask
  ├── utilise → FluidBus.Errors.DispatchException
  ├── utilise → FluidBus.Event.BusLogEvent
  ├── utilise → FluidBus.Handler.BusLogHandler
  └── appelle → FluidBus.Core.FluidCoreAPI.Initialize()

FluidBus.Core.FluidCoreAPI
  └── P/Invoke → libfluid_core (Rust)

FluidBus.Core.Herits.FluidEvent
  ├── utilise → FluidBus.BluePrint.BluePrintFactory.NewEvent()
  ├── utilise → FluidBus.Core.FBus.Publish()
  ├── utilise → FluidBus.Core.Instructions.System.LogInstruction
  └── utilise → FluidBus.Event.BusLogEvent

FluidBus.Core.Instructions.Core.RustInstruction
  ├── herite → FluidBus.Core.Instructions.FluidInstruction<byte[]>
  └── utilise → FluidBus.Core.FluidCoreAPI.RequestToken(), Rotate()

FluidBus.BluePrint.BluePrintFactory
  ├── utilise → FluidBus.Core.HLinq.HandlerLinq.Register()
  └── utilise → FluidBus.Errors.BluePrintException, HandlerLinqException

FluidBus.Event.CoreEvent
  └── herite → FluidBus.Core.Herits.FluidEvent

FluidBus.Handler.CoreHandler
  └── herite → FluidBus.Core.Herits.FluidHandler<CoreEvent>

fluid_core::lib
  ├── utilise → fluid_core::core::seed::generate_seed
  ├── utilise → fluid_core::core::token::{generate_token, next_token}
  └── utilise → fluid_core::core::bytecode::generate_bytecode

LoggerTestBusCSharpPython.Program
  ├── utilise → FluidBus.Core.FBus
  ├── utilise → FluidBus.Core.FluidCoreAPI
  ├── utilise → FluidBus.Core.Instructions.Core.RustInstruction
  ├── utilise → FluidBus.Event.CoreEvent
  └── utilise → FluidBus.Handler.CoreHandler
```

---

### Configuration projet

#### `FluidBus/FluidBus.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

#### `LoggerTestBusCSharpPython/LoggerTestBusCSharpPython.csproj`

Depend de `FluidBus.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FluidBus\FluidBus.csproj" />
  </ItemGroup>

</Project>
```

#### `LoggerTestBusCSharpPython.slnx`

```xml
<Solution>
  <Folder Name="/Éléments de solution/">
    <File Path=".gitignore" />
  </Folder>
  <Project Path="FluidBus/FluidBus.csproj" />
  <Project Path="LoggerTestBusCSharpPython/LoggerTestBusCSharpPython.csproj" />
</Solution>
```

#### `fluid_core/Cargo.toml`

```toml
[lib]
crate-type = ["cdylib"]

[package]
name = "fluid_core"
version = "0.1.0"
edition = "2024"

[dependencies]
```

---

### Namespace `FluidBus.Core` — Bus principal et interop

#### `FluidBus/Core/FBus.cs`

Point d'entree du bus. Depend de : `HandlerLinq`, `BluePrintFactory`, `BusProtocol`, `FluidTask`, `FluidCoreAPI`, `BusLogHandler`.

```csharp
using FluidBus.BluePrint;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.HLinq;
using FluidBus.Core.Tasks;
using FluidBus.Errors;
using FluidBus.Event;
using FluidBus.Handler;
using System.Runtime.CompilerServices;

namespace FluidBus.Core
{
	public static class FBus
	{
		private static HashSet<BusPort> _ports;
		static FBus()
		{
            FluidCoreAPI.Initialize();
			_ports = new();
			HandlerLinq.Register(new BusLogHandler("bus_logger"));
			_ports.Add(new BusPort(BusProtocol.System));
		}

		public static bool AddPort(BusProtocol protocol)
			=> _ports.Add(new BusPort(protocol));

		public static bool Register(IFluidHandler hdl)
			=> HandlerLinq.Register(hdl);

		public static bool TryGetHandlers(IFluidEvent evt, out Dictionary<IFluidHandler, bool> handlers)
			=> HandlerLinq.TryGetHandlers(evt, out handlers);

		public static bool Publish(IFluidEvent evt)
		{
			IFluidHandler? available = null;
			foreach (var port in _ports)
			{
				if (!evt.Protocol.Equals(port.Protocol))
					continue;
				if (!HandlerLinq.TryGetHandlers(evt, out var handlers))
					return false;
				foreach (var handler in handlers)
				{
					if (!handler.Value)
					{ available = handler.Key; break; }
				}
				if (available != null)
				{ available.CallCount++; return port.Dispatch(evt, available); }
				var (hdl, success) = BluePrintFactory.NewHandler(handlers.Last().Key);
				hdl.CallCount++;
				return success && port.Dispatch(evt, hdl);
			}
			return false;
		}
	}

	internal class BusPort
	{
		public BusProtocol Protocol { get; }
		public BusPort(BusProtocol protocol)
			=> this.Protocol = protocol;

		public bool Dispatch(IFluidEvent evt, IFluidHandler hdl)
		{
			switch (this.Protocol.Strategy)
			{
				case ExecutionStrategy.Sync:
					return evt.Dispatch(hdl);
				case ExecutionStrategy.Async:
					new FluidTask(() => evt.Dispatch(hdl))
						.OnComplete(state =>
						{
							if (state == FluidTaskState.Failed)
								new DispatchException("Dispatch failed").DisplayMessage();
						});
					return true;
				default:
					return false;
			}
		}

		public override bool Equals(object? obj)
		{
			if (obj is BusPort other)
				return Protocol.Name.Equals(other.Protocol.Name);
			return false;
		}

		public override int GetHashCode()
			=> HashCode.Combine(Protocol.Name);
	}
}
```

#### `FluidBus/Core/FluidCoreAPI.cs`

Couche P/Invoke vers `libfluid_core` (Rust). Depend de : rien cote C# (bas niveau).

```csharp
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.Core
{
    public static class FluidCoreAPI
    {
        [DllImport("libfluid_core", EntryPoint="process_bytes")]
        private static extern IntPtr ProcessBytes(byte[] data, nuint len, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint="free_bytes")]
        private static extern void FreeBytes(IntPtr ptr, nuint len);

        [DllImport("libfluid_core", EntryPoint="init")]
        private static extern ulong Init();

        [DllImport("libfluid_core", EntryPoint="get_token")]
        private static extern IntPtr GetToken(byte opcode, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint="rotate_token")]
        private static extern IntPtr RotateToken(byte[] data, nuint len, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint = "get_bytecode")]
        private static extern IntPtr GetBytecode(
                byte opcode,
                byte[] typeName, nuint typeNameLen,
                byte[] methodName, nuint methodNameLen,
                byte[] argType, nuint argTypeLen,
                byte[] arg, nuint argLen,
                out nuint outLen
                );

        public static byte[] Send(byte[] data)
        {
            IntPtr resultPtr = ProcessBytes(data, (nuint)data.Length, out nuint outLen);
            byte[] result = new byte[outLen];
            Marshal.Copy(resultPtr, result, 0, (int)outLen);
            Free(resultPtr, outLen);
            return result;
        }

        public static void Free(IntPtr ptr, nuint outLen)
            => FreeBytes(ptr, outLen);

        public static ulong Initialize()
            => Init();

        public static byte[] RequestToken(byte opcode)
        {
            IntPtr ptr = GetToken(opcode, out nuint outLen);
            byte[] token = new byte[outLen];
            Marshal.Copy(ptr, token, 0, (int)outLen);
            Free(ptr, outLen);
            return token;
        }

        public static byte[] Rotate(byte[] token)
        {
            IntPtr ptr = RotateToken(token, (nuint)token.Length, out nuint outLen);
            byte[] next = new byte[outLen];
            Marshal.Copy(ptr, next, 0, (int)outLen);
            Free(ptr, outLen);
            return next;
        }

        public static byte[] GetMethod(byte[] token)
        {
            return new byte[0];
        }

        public static object? Execute(byte[] bytecode)
        {
            return null!;
        }

        public static byte[] GetBytecode(byte opcode, string typeName, string methodName, string argType, byte[] arg)
        {
            byte[] typeBytes = Encoding.UTF8.GetBytes(typeName);
            byte[] methodBytes = Encoding.UTF8.GetBytes(methodName);
            byte[] argTypeBytes = Encoding.UTF8.GetBytes(argType);

            IntPtr ptr = GetBytecode(
                    opcode,
                    typeBytes, (nuint)typeBytes.Length,
                    methodBytes, (nuint)methodBytes.Length,
                    argTypeBytes, (nuint)argTypeBytes.Length,
                    arg, (nuint)arg.Length,
                    out nuint outLen
                    );

            byte[] result = new byte[outLen];
            Marshal.Copy(ptr, result, 0, (int)outLen);
            FreeBytes(ptr, outLen);
            return result;
        }
    }
}
```

---

### Namespace `FluidBus.Core.Herits` — Interfaces et classes abstraites

#### `FluidBus/Core/Herits/IFluidEvent.cs`

Interface de base pour tous les evenements. Depend de : `BusProtocol`, `IFluidInstruction`, `IFluidHandler`.

```csharp
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Instructions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Herits
{
	public interface IFluidEvent
	{
		string Id { get; }
		BusProtocol Protocol { get; }
		HashSet<IFluidInstruction> Instructions { get; }

		bool Dispatch(IFluidHandler hdl);
	}
}
```

#### `FluidBus/Core/Herits/FluidEvent.cs`

Classe abstraite pour les evenements. Depend de : `BluePrintFactory`, `FBus`, `BusLogEvent`, `LogInstruction`.

```csharp
using FluidBus.BluePrint;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.HLinq;
using FluidBus.Core.Instructions;
using FluidBus.Core.Instructions.System;
using FluidBus.Event;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FluidBus.Core.Herits
{
	public abstract class FluidEvent : IFluidEvent
	{
		public string Id { get; }
		public BusProtocol Protocol { get; }
		public HashSet<IFluidInstruction> Instructions { get; }

		public FluidEvent(string id, BusProtocol protocol, params IFluidInstruction[] instructions)
		{
			Id = id;
			Protocol = protocol;
			Instructions = new(instructions);
		}

		public bool Dispatch(IFluidHandler handler)
		{
			if (this is not BusLogEvent)
			{
				FBus.Publish(createBusLogEvent("bus_log_dispatch", $"[SYS] - Event Dispatched: {this.Id}"));
			}

			handler.Handle(this);
			if (this is not BusLogEvent)
				FBus.Publish(createBusLogEvent("bus_log_dispatch", $"[SYS] - Handler Trigered: {handler.Id}"));
			return true;
		}

		private IFluidEvent createBusLogEvent(string name, string message)
		{
			var (busEvt, success) = BluePrintFactory
					.NewEvent(
						typeof(BusLogEvent),
						name,
						BusProtocol.System,
						new LogInstruction(message, msg => Console.WriteLine(msg))
					);
			if (success)
				return busEvt;
			return null!;
		}
	}
}
```

#### `FluidBus/Core/Herits/IFluidHandler.cs`

Interface de base pour tous les handlers. Depend de : `IFluidEvent`.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Herits
{
	public interface IFluidHandler
	{
		string Id { get; }
		int CallCount { get; set; }

		Type EventType { get; }

		abstract bool Handle(IFluidEvent evt);
	}
}
```

#### `FluidBus/Core/Herits/FluidHandler.cs`

Classe abstraite generique pour les handlers. Depend de : `IFluidHandler`.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Herits
{
	public abstract class FluidHandler<T> : IFluidHandler
	{
		public string Id { get; }
		public int CallCount { get; set; }

		public Type EventType { get; }

		public FluidHandler(string id)
		{
			this.Id = id;
			this.EventType = typeof(T);
		}


		public abstract bool Handle(IFluidEvent evt);
	}
}
```

---

### Namespace `FluidBus.Core.Instructions` — Systeme d'instructions

#### `FluidBus/Core/Instructions/IFluidInstruction.cs`

Interface pour les instructions executables.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Instructions
{
	public interface IFluidInstruction
	{
		Type DataType { get; }

		void Execute();
		object? ExecuteAndGet();
	}
}
```

#### `FluidBus/Core/Instructions/FluidInstruction.cs`

Classe abstraite generique avec delegates et event OnResult. Depend de : `IFluidInstruction`.

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
```

#### `FluidBus/Core/Instructions/System/LogInstruction.cs`

Instruction de logging. Herite de : `FluidInstruction<string>`.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Instructions.System
{
	public class LogInstruction : FluidInstruction<string>
	{
		public LogInstruction(string? data, params FluidMethod<string>[] methods) : base(data, methods)
		{ }

		public LogInstruction(string? data, params FluidFunc<string, object>[] funcs) : base(data, funcs)
		{ }
	}
}
```

#### `FluidBus/Core/Instructions/Core/RustInstruction.cs`

Instruction avec token rotatif Rust. Herite de : `FluidInstruction<byte[]>`. Depend de : `FluidCoreAPI`.

```csharp

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
```

---

### Namespace `FluidBus.Core.BusProtocols` — Protocoles de routage

#### `FluidBus/Core/BusProtocols/BusProtocol.cs`

Classe abstraite et enum ExecutionStrategy.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.BusProtocols
{
	public enum ExecutionStrategy
	{
		Sync = 0,
		Async = 1,
	}

	public abstract class BusProtocol
	{
		public string Name { get; }
		public abstract ExecutionStrategy Strategy { get; }

		protected BusProtocol(string name)
			=> this.Name = name;

		public static readonly BusProtocol System = new SystemProtocol();
	}
}
```

#### `FluidBus/Core/BusProtocols/SystemProtocol.cs`

Protocole synchrone par defaut. Herite de : `BusProtocol`.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.BusProtocols
{
	public class SystemProtocol : BusProtocol
	{
		public override ExecutionStrategy Strategy => ExecutionStrategy.Sync;
		public SystemProtocol() : base ("SYSTEM")
		{ }
	}
}
```

---

### Namespace `FluidBus.Core.HLinq` — Registre de handlers

#### `FluidBus/Core/HLinq/HandlerLinq.cs`

Registre statique avec lookup par type. Depend de : `IFluidHandler`, `IFluidEvent`.

```csharp
using FluidBus.Core.Herits;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.HLinq
{
	public static class HandlerLinq
	{
		private static Dictionary<Type, Dictionary<IFluidHandler, bool>> handlers = new();

		public static bool Register(IFluidHandler handler)
		{
			if (!handlers.ContainsKey(handler.EventType))
				handlers[handler.EventType] = new();
			handlers[handler.EventType].Add(handler, false);
			return true;
		}

		public static bool Drop(IFluidHandler handler)
		{
			if (handlers.ContainsKey(handler.EventType))
				return handlers[handler.EventType].Remove(handler);
			return false;
		}

		public static bool TryGetHandlers(IFluidEvent evt, out Dictionary<IFluidHandler, bool> hdls)
		{
			foreach (var target in handlers.Keys)
				if (evt.GetType() == target)
				{ hdls = handlers[target]; return true; }
			hdls = null!;
			return false;
		}
	}
}
```

---

### Namespace `FluidBus.Core.Tasks` — Async

#### `FluidBus/Core/Tasks/FluidTask.cs`

Wrapper Task avec etats. Depend de : rien (standard library).

```csharp
namespace FluidBus.Core.Tasks
{
	public enum FluidTaskState
	{
		Running,
		Completed,
		Failed,
		Cancelled,
	}

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

		public FluidTask OnComplete(Action<FluidTaskState> callback)
		{
			_task.ContinueWith(t => callback(GetState()));
			return this;
		}
	}
}
```

---

### Namespace `FluidBus.Core.VM` — Machine virtuelle

#### `FluidBus/Core/VM/FluidVM.cs`

VM stub. Depend de : `FluidCoreAPI`.

```csharp
using FluidBus.Core;

namespace FluidBus;

public class FluidVM
{
    public object? Run(byte[] token)
        => FluidCoreAPI.Execute(FluidCoreAPI.GetMethod(token));
}
```

---

### Namespace `FluidBus.Event` — Implementations d'evenements

#### `FluidBus/Event/BusLogEvent.cs`

Evenement de log systeme. Herite de : `FluidEvent`.

```csharp
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Event
{
	public class BusLogEvent : FluidEvent
	{
		public BusLogEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base ($"[EVT::{nameof(BusLogEvent)}::{id}]", protocol, instrs)
		{

		}
	}
}
```

#### `FluidBus/Event/CoreEvent.cs`

Evenement metier principal. Herite de : `FluidEvent`.

```csharp
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;

namespace FluidBus.Event
{
    public class CoreEvent : FluidEvent
    {
        public CoreEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base($"[EVT::{nameof(CoreEvent)}::{id}]", protocol, instrs)
        {

        }
    }
}
```

---

### Namespace `FluidBus.Handler` — Implementations de handlers

#### `FluidBus/Handler/BusLogHandler.cs`

Handler pour les logs systeme. Herite de : `FluidHandler<BusLogEvent>`.

```csharp
using FluidBus.Core.Herits;
using FluidBus.Event;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace FluidBus.Handler
{
	public class BusLogHandler : FluidHandler<BusLogEvent>
	{
		public BusLogHandler(string id) : base($"[HDL::{nameof(BusLogEvent)}::{id}]")
		{ }

		public override bool Handle(IFluidEvent evt)
		{
			foreach (var instr in evt.Instructions)
			{
				instr.Execute();
				instr.ExecuteAndGet();
			}
			this.CallCount++;
			return true;
		}
	}
}
```

#### `FluidBus/Handler/CoreHandler.cs`

Handler pour les evenements metier. Herite de : `FluidHandler<CoreEvent>`.

```csharp
using FluidBus.Core.Herits;
using FluidBus.Event;

namespace FluidBus.Handler
{
    public class CoreHandler : FluidHandler<CoreEvent>
    {
        public CoreHandler(string id) : base($"HDL::{nameof(CoreHandler)}::{id}")
        { }

        public override bool Handle(IFluidEvent evt)
        {
            foreach (var instr in evt.Instructions)
            {
                instr.Execute();
                instr.ExecuteAndGet();
            }
            this.CallCount++;
            return true;
        }
    }
}
```

---

### Namespace `FluidBus.BluePrint` — Factory dynamique

#### `FluidBus/BluePrint/BluePrintFactory.cs`

Factory par reflexion. Depend de : `HandlerLinq`, `BluePrintException`, `HandlerLinqException`.

```csharp
using FluidBus.Core;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.HLinq;
using FluidBus.Core.Instructions;
using FluidBus.Errors;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FluidBus.BluePrint
{
	public static class BluePrintFactory
	{
		public static (IFluidHandler, bool) NewHandler(IFluidHandler baseHdl)
		{
			try
			{
				IFluidHandler handler = CreateInstance<IFluidHandler>(baseHdl.GetType());
				if (!HandlerLinq.Register(handler))
					throw new HandlerLinqException("Can't Register this handler");
				return (handler, true);
			}
			catch (FluidBusError e)
			{ e.DisplayMessage(); return (null!, false); }
		}

		public static (IFluidEvent, bool) NewEvent(Type baseEvt, string name, BusProtocol protocol, params IFluidInstruction[] instrs)
		{
			try
			{ var evt = CreateInstance<IFluidEvent>(baseEvt, [name, protocol, instrs])!; return (evt, true); }
			catch (FluidBusError e)
			{ e.DisplayMessage(); return (null!, false); }
		}

		private static T CreateInstance<T>(Type concreteType, params object[] args) where T : class
		{
			try
			{ return (T)Activator.CreateInstance(concreteType, args)!; }
			catch (MissingMethodException)
			{ throw new BluePrintException("No matching constructor found"); }
			catch (TargetInvocationException e)
			{throw new BluePrintException($"Constructor threw : {e.InnerException?.Message}"); }
		}
	}
}
```

---

### Namespace `FluidBus.Errors` — Exceptions

#### `FluidBus/Errors/FluidBusError.cs`

Exception de base abstraite.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Errors
{
	public abstract class FluidBusError : Exception
	{
		public FluidBusError(string message) : base(message)
		{ }

		public void DisplayMessage()
			=> Console.WriteLine(this.Message);

	}
}
```

#### `FluidBus/Errors/BluePrintException.cs`

Erreur factory. Herite de : `FluidBusError`.

```csharp
namespace FluidBus.Errors
{
	public class BluePrintException : FluidBusError
	{
		public BluePrintException(string msg) : base($"[{nameof(BluePrintException)}]: {msg}")
		{ }
	}
}
```

#### `FluidBus/Errors/DispatchException.cs`

Erreur dispatch. Herite de : `FluidBusError`.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Errors
{
	public class DispatchException : FluidBusError
	{
		public DispatchException(string  message) : base(message) { }
	}
}
```

#### `FluidBus/Errors/HandlerLinqException.cs`

Erreur registre. Herite de : `FluidBusError`.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Errors
{
	public class HandlerLinqException : FluidBusError
	{
		public HandlerLinqException(string msg) : base($"[{nameof(HandlerLinqException)}]: {msg}")
		{ }
	}
}
```

---

### Module Rust `fluid_core` — Core natif

#### `fluid_core/src/lib.rs`

Point d'entree FFI. Depend de : `core::seed`, `core::token`, `core::bytecode`.

```rust
mod core;

use core::seed::generate_seed;
use core::token::{generate_token, next_token};
use core::bytecode::{generate_bytecode};
use std::{slice};
use std::sync::atomic::{AtomicU64, Ordering};


static SEED: AtomicU64 = AtomicU64::new(0);

#[unsafe(no_mangle)]
pub extern "C" fn process_bytes(data: *const u8, len: usize, out_len: *mut usize) -> *mut u8 {
    unsafe {
        let input = slice::from_raw_parts(data, len);
        println!("Rust Received {:?}", input);
    };

    let mut bytes: Vec<u8> = "Received OpCode, sending Token".as_bytes().to_vec();
    unsafe { *out_len = bytes.len(); };
    let ptr = bytes.as_mut_ptr();
    std::mem::forget(bytes);
    ptr
}

#[unsafe(no_mangle)]
pub extern "C" fn free_bytes(ptr: *mut u8, len: usize) {
    unsafe {
        let _ = Vec::from_raw_parts(ptr, len, len);
    }
    println!("Freed !");
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn init() -> u64 {
    let seed = generate_seed();
    SEED.store(seed, Ordering::SeqCst);
    seed
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_token(opcode: u8, out_len: *mut usize) -> *mut u8 {
    let seed = SEED.load(Ordering::SeqCst);
    let mut token = generate_token(opcode, seed);
    unsafe {
        *out_len = token.len();
        let ptr = token.as_mut_ptr();
        std::mem::forget(token);
        ptr
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn rotate_token(data: *const u8, len: usize, out_len: *mut usize) -> *mut u8 {
    let seed = SEED.load(Ordering::SeqCst);
    let current = unsafe { std::slice::from_raw_parts(data, len) };
    let mut next = next_token(current, seed);
    unsafe {
        *out_len = next.len();
        let ptr = next.as_mut_ptr();
        std::mem::forget(next);
        ptr
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_bytecode(
    opcode: u8,
    type_name: *const u8, type_name_len: usize,
    method_name: *const u8, method_name_len: usize,
    arg_type: *const u8, arg_type_len: usize, arg: *const u8, arg_len: usize,
    out_len: *mut usize) -> *mut u8 {
    let seed = SEED.load(Ordering::SeqCst);

    let type_name = unsafe { std::str::from_utf8(std::slice::from_raw_parts(type_name, type_name_len)).unwrap() };
    let method_name = unsafe { std::str::from_utf8(std::slice::from_raw_parts(method_name, method_name_len)).unwrap() };
    let arg_type = unsafe { std::str::from_utf8(std::slice::from_raw_parts(arg_type, arg_type_len)).unwrap() };
    let arg = unsafe { std::slice::from_raw_parts(arg, arg_len) };

    let mut bytecode = generate_bytecode(seed, opcode, type_name, method_name, arg_type, arg);

    unsafe { *out_len = bytecode.len() };
    let ptr = bytecode.as_mut_ptr();
    std::mem::forget(bytecode);
    ptr
}
```

#### `fluid_core/src/core/mod.rs`

Declarations de sous-modules.

```rust
pub mod seed;
pub mod token;
pub mod bytecode;
```

#### `fluid_core/src/core/seed.rs`

Generation de seed. Depend de : standard library uniquement.

```rust
use std::time::{SystemTime, UNIX_EPOCH};

pub fn generate_seed() -> u64 {
    let time = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos() as u64;
    let pid = std::process::id() as u64;
    time ^ pid
}
```

#### `fluid_core/src/core/token.rs`

Generation et rotation de tokens. Depend de : `DefaultHasher`.

```rust
use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

pub fn generate_token(opcode: u8, seed: u64) -> Vec<u8> {
    let mut hasher = DefaultHasher::new();
    opcode.hash(&mut hasher);
    seed.hash(&mut hasher);
    let hash = hasher.finish();
    hash.to_le_bytes().to_vec()
}

pub fn next_token(current_token: &[u8], seed: u64) -> Vec<u8> {
    let mut hasher = DefaultHasher::new();
    current_token.hash(&mut hasher);
    seed.hash(&mut hasher);
    let hash = hasher.finish();
    hash.to_le_bytes().to_vec()
}
```

#### `fluid_core/src/core/bytecode.rs`

Generation de bytecode structure. Depend de : `DefaultHasher`.

```rust
use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

fn make_sig(seed: u64, section_id: u8) -> [u8; 2] {
    let mut hasher = DefaultHasher::new();
    seed.hash(&mut hasher);
    section_id.hash(&mut hasher);
    let hash = hasher.finish();
    let bytes = hash.to_le_bytes();
    [bytes[0], bytes[1]]
}

fn sig_header(seed: u64) -> [u8; 2] { make_sig(seed, 0x01) }
fn sig_opcode(seed: u64) -> [u8; 2] { make_sig(seed, 0x02) }
fn sig_type(seed: u64)   -> [u8; 2] { make_sig(seed, 0x03) }
fn sig_method(seed: u64) -> [u8; 2] { make_sig(seed, 0x04) }
fn sig_args(seed: u64)   -> [u8; 2] { make_sig(seed, 0x05) }

pub fn generate_bytecode(
    seed: u64,
    opcode: u8,
    type_name: &str,
    method_name: &str,
    arg_type: &str,
    arg: &[u8],
) -> Vec<u8> {
    let mut buf = Vec::new();

    // Header
    buf.extend_from_slice(&sig_header(seed));
    buf.push(0x04); // nb sections

    // Opcode
    buf.extend_from_slice(&sig_opcode(seed));
    buf.push(0x01); // len
    buf.push(opcode);

    // Type
    let type_bytes = type_name.as_bytes();
    buf.extend_from_slice(&sig_type(seed));
    buf.push(type_bytes.len() as u8);
    buf.extend_from_slice(type_bytes);

    // Method
    let method_bytes = method_name.as_bytes();
    buf.extend_from_slice(&sig_method(seed));
    buf.push(method_bytes.len() as u8);
    buf.extend_from_slice(method_bytes);

    // Args
    let arg_type_bytes = arg_type.as_bytes();
    buf.extend_from_slice(&sig_args(seed));
    buf.push(arg_type_bytes.len() as u8);
    buf.extend_from_slice(arg_type_bytes);
    buf.push(arg.len() as u8);
    buf.extend_from_slice(arg);

    buf
}
```

---

### Programme de test

#### `LoggerTestBusCSharpPython/Program.cs`

Main de test. Depend de : `FBus`, `FluidCoreAPI`, `RustInstruction`, `CoreEvent`, `CoreHandler`.

```csharp
using FluidBus;
using FluidBus.Core.Herits;
using FluidBus.BluePrint;
using FluidBus.Core;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Instructions.Core;
using FluidBus.Event;
using FluidBus.Handler;
using System.Text;

namespace LoggerTestBusCSharpPython
{
	internal class Program
	{
		static void Main(string[] args)
		{
            FBus.Register(new CoreHandler("core"));

            var instr = new RustInstruction(
                    0x01,
                    (data) => FluidCoreAPI.Send(data)
            );

            instr.OnResult += (result) => {
                if (result is byte[] bytes)
                    Console.WriteLine($"C# Received: {Encoding.UTF8.GetString(bytes)}");
            };

            var evt = new CoreEvent(
                    "core_evt",
                    BusProtocol.System,
                    [instr]
            );

            FBus.Publish(evt);

            byte[] bytecode = FluidCoreAPI.GetBytecode(
                    0x04,
                    "Console",
                    "WriteLine",
                    "string",
                    Encoding.UTF8.GetBytes("Fluid.Guard")
                    );

            Console.WriteLine($"Bytecode: {string.Join(", ", bytecode)}");
            Console.WriteLine($"Len: {bytecode.Length}");

            Test();
		}

        static void Test()
        {
            byte[] token = FluidCoreAPI.RequestToken(0x01);
            Console.WriteLine($"Token v1: {string.Join(", ", token)}");

            token = FluidCoreAPI.Rotate(token);
            Console.WriteLine($"Token v2: {string.Join(", ", token)}");

            token = FluidCoreAPI.Rotate(token);
            Console.WriteLine($"Token v3: {string.Join(", ", token)}");
        }
	}
}
```
