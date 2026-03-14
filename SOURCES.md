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
  └── utilise → FluidBus.Handler.BusLogHandler

FluidBus.Core.Herits.FluidEvent
  ├── utilise → FluidBus.BluePrint.BluePrintFactory.NewEvent()
  ├── utilise → FluidBus.Core.FBus.Publish()
  ├── utilise → FluidBus.Core.Instructions.System.LogInstruction
  └── utilise → FluidBus.Event.BusLogEvent

FluidBus.BluePrint.BluePrintFactory
  ├── utilise → FluidBus.Core.HLinq.HandlerLinq.Register()
  └── utilise → FluidBus.Errors.BluePrintException, HandlerLinqException

FluidBus.Event.BusLogEvent
  └── herite → FluidBus.Core.Herits.FluidEvent

FluidBus.Handler.BusLogHandler
  └── herite → FluidBus.Core.Herits.FluidHandler<BusLogEvent>

LoggerTestBusCSharpPython.Program
  ├── utilise → FluidBus.Core.FBus
  ├── utilise → FluidBus.BluePrint.BluePrintFactory
  ├── utilise → FluidBus.Core.Instructions.System.LogInstruction
  └── definit → TestEvt (FluidEvent), TestHandler (FluidHandler<TestEvt>)
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
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
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

---

### Namespace `FluidBus.Core` — Bus principal

#### `FluidBus/Core/FBus.cs`

Point d'entree du bus. Depend de : `HandlerLinq`, `BluePrintFactory`, `BusProtocol`, `FluidTask`, `BusLogHandler`.

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
			Id = $"[EVT::{id}]";
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
			this.Id = $"[HDL::{id}]";
			this.EventType = typeof(T);
		}


		public virtual bool Handle(IFluidEvent evt)
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
		public BusLogEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base ($"{nameof(BusLogEvent)}::{id}", protocol, instrs)
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
		public BusLogHandler(string id) : base($"{nameof(BusLogEvent)}::{id}")
		{ }
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

### Programme de test

#### `LoggerTestBusCSharpPython/Program.cs`

Main de test. Depend de : `FBus`, `BluePrintFactory`, `LogInstruction`, `BusProtocol`.

```csharp
using FluidBus;
using FluidBus.BluePrint;
using FluidBus.Core;
//using FluidBus.Core.VM;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using FluidBus.Core.Instructions.System;
using FluidBus.Errors;

//using FluidBus.Core.Instructions.Core;
using FluidBus.Event;
using FluidBus.Handler;
using System.Text;

namespace LoggerTestBusCSharpPython
{
	public class TestEvt : FluidEvent
	{
		public TestEvt(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base($"{nameof(TestEvt)}::{id}", protocol, instrs)
		{ }
	}

	public class TestHandler : FluidHandler<TestEvt>
	{
		public TestHandler(string id) : base($"{nameof(TestHandler)}::{id}")
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

    internal class Program
    {
        static void Main(string[] args)
        {
			FBus.Register(new TestHandler("test_hdl"));

			var instr = new LogInstruction(
				"Hello From FluidBus",
				(data) => Console.WriteLine(data));

			instr.OnResult += (result) => {
				if (result != null)
					Console.WriteLine(result);
			};

			var (evt, success) = BluePrintFactory.NewEvent(
				typeof(TestEvt),
				"test_evt",
				BusProtocol.System,
				instr);

			FBus.Publish(evt);
			Console.WriteLine("Off");
        }
    }
}
```
