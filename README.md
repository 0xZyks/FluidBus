# FluidBus

A lightweight, modular event bus for .NET 10 built around two dispatch models: **Router** (protocol-based routing with sync/async support) and **React** (channel-based reactive subscriptions), plus a **CallBack** registry and a built-in **Benchmark** toolkit.

Zero external dependencies.

## Architecture

```
FluidBus
├── FluidBus.Core        # Interfaces, protocols, tasks, errors
├── FluidBus.Router      # Protocol-based event routing (sync/async)
├── FluidBus.React       # Reactive channel-based event dispatch
├── FluidBus.CallBack    # Named callback registry
├── FluidBus.Benchmark   # Benchmarking utilities
└── FBus                 # Unified facade
```

### Router vs React

| | Router | React |
|---|---|---|
| Dispatch | Protocol-based (sync/async) | Channel-based (always async) |
| Handler count | One handler per event type | Multiple subscribers per channel |
| Registration | Manual via `FRouter.Register()` | Auto-subscribe on instantiation |
| Matching | Exact event type | All subscribers on the channel |
| Use case | Command / request patterns | Broadcast / observer patterns |

## Getting started

Reference `FluidBus` in your project. The `FBus` facade exposes all modules:

```csharp
using FluidBus;

FBus.Route(routeEvent);                           // Router dispatch
FBus.React(reactEvent);                           // React dispatch
FBus.CallBack("on_complete", someData);            // Execute a named callback
FBus.Bench("my scenario", 1000, 100, () => { }); // Benchmark a scenario
```

---

## FluidBus.Core

Shared foundation used by all modules.

### Instructions

Instructions carry the data and logic that handlers execute. They use the `FluidCallBack` delegate:

```csharp
public delegate object? FluidCallBack(object? data);
```

Inherit from `FluidInstruction<T>`:

```csharp
using FluidBus.Core.Abstracts;

public class PrintInstruction : FluidInstruction<string>
{
    public PrintInstruction(string? data, params FluidCallBack[] methods)
        : base(data, methods) { }
}
```

Each instruction can hold multiple callbacks (deduplicated by ID), executed sequentially. An `OnResult` event fires after execution.

### Protocols

Protocols define the execution strategy for the Router:

```csharp
public enum ExecutionStrategy { Sync = 0, Async = 1 }
```

A built-in `BusProtocol.System` (sync) is always available. Create custom protocols:

```csharp
using FluidBus.Core.Protocols;

public class AsyncProtocol : BusProtocol
{
    public override ExecutionStrategy Strategy => ExecutionStrategy.Async;
    public AsyncProtocol() : base("ASYNC") { }
}
```

### Tasks

`FluidTask` wraps `Task.Run()` with state tracking (`Running`, `Completed`, `Failed`, `Cancelled`) and a fluent `OnComplete()` continuation API.

### Error hierarchy

All exceptions inherit from `FluidBusError` and expose `.DisplayMessage()`.

| Exception | Thrown when |
|---|---|
| `DispatchException` | Async dispatch fails or unknown `ExecutionStrategy` |
| `ProtocolNotFoundException` | Event protocol has no registered port |
| `HandlerNotFoundException` | No handler registered for a given event type |
| `DuplicateHandlerException` | Handler already registered for that event type |
| `InstructionException` | `Execute()` called with no callbacks or null data |
| `ChannelException` | Channel write with no subscribers, or subscriber failure |
| `HandlerLinqException` | Handler registry error |

---

## FluidBus.Router

The Router dispatches events through **protocols** to registered **handlers**, matched by event type. One handler per event type.

### 1. Create a custom event

```csharp
using FluidBus.Core.Interfaces;
using FluidBus.Core.Protocols;
using FluidBus.Router.Abstracts;

public class UserCreatedEvent : RouteEvent
{
    public UserCreatedEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs)
        : base($"{nameof(UserCreatedEvent)}::{id}", protocol, instrs) { }
}
```

### 2. Create a custom handler

```csharp
using FluidBus.Core.Interfaces;
using FluidBus.Router.Abstracts;

public class UserCreatedHandler : RouteHandler<UserCreatedEvent>
{
    public UserCreatedHandler(string id)
        : base($"{nameof(UserCreatedEvent)}::{id}") { }

    public override bool Handle(IFluidEvent evt)
    {
        Console.WriteLine($"[{Id}] Handling event {evt.Id}");
        return base.Handle(evt);
    }
}
```

### 3. Register and publish

```csharp
using FluidBus;
using FluidBus.Core.Abstracts;
using FluidBus.Core.Protocols;
using FluidBus.Router.Core;

// Register the handler
FRouter.Register(new UserCreatedHandler("user_handler"));

// Create an instruction with a callback
var instruction = new PrintInstruction("Hello from FluidBus!", msg =>
{
    Console.WriteLine(msg);
    return null;
});

// Publish on the System protocol (sync)
FBus.Route(new UserCreatedEvent("evt_1", BusProtocol.System, instruction));
```

### Custom protocols

```csharp
// Register a port for your protocol
FRouter.AddPort(new AsyncProtocol());

// Events using this protocol will dispatch asynchronously
FBus.Route(new UserCreatedEvent("evt_2", new AsyncProtocol(), instruction));
```

### Dispatch flow

```
FBus.Route(event)
  -> FRouter.Publish(event)
    -> Lookup port by event.Protocol
    -> Lookup handler by event type (HandlerLinq)
    -> RouterPort.Dispatch(event, handler)
       ├─ Sync:  event.Dispatch(handler) — blocking
       └─ Async: FluidTask wrapping event.Dispatch(handler)
         -> handler.Handle(event)
           -> Execute each instruction's callbacks sequentially
```

---

## FluidBus.React

React uses **channels** (`System.Threading.Channels`) instead of protocols. Handlers auto-subscribe to their event type's channel on creation. Events are dispatched asynchronously to all subscribers.

### 1. Create a custom event

```csharp
using FluidBus.Core.Interfaces;
using FluidBus.React.Abstracts;

public class ChatMessageEvent : ReactEvent
{
    public ChatMessageEvent(string id, params IFluidInstruction[] instrs)
        : base($"{nameof(ChatMessageEvent)}::{id}", instrs) { }
}
```

### 2. Create a custom handler

Handlers subscribe automatically to their channel on instantiation:

```csharp
using FluidBus.Core.Interfaces;
using FluidBus.React.Abstracts;

public class ChatMessageHandler : ReactHandler<ChatMessageEvent>
{
    public ChatMessageHandler(string id)
        : base($"{nameof(ChatMessageEvent)}::{id}") { }

    public override bool Handle(IFluidEvent evt)
    {
        Console.WriteLine($"[{Id}] Received message");
        return base.Handle(evt);
    }
}
```

### 3. Register and publish

```csharp
using FluidBus;
using FluidBus.Core.Abstracts;
using FluidBus.React.Core;

// Instantiating the handler auto-subscribes it to the ChatMessageEvent channel
var handler = new ChatMessageHandler("chat_listener");

// Optionally register it for lifecycle management
FReact.RegisterHandler(handler);

// Publish — all subscribed handlers are notified asynchronously
var instruction = new PrintInstruction("New message!", msg =>
{
    Console.WriteLine(msg);
    return null;
});
FBus.React(new ChatMessageEvent("msg_1", instruction));

// Drop a handler
FReact.DropHandler(handler);
```

### Dispatch flow

```
FBus.React(event)
  -> FReact.Publish(event)
    -> GetOrCreateChannel(event type)
    -> channel.Write(event) — enqueued in unbounded Channel<T>
    -> Background reader loop (per channel)
       -> Broadcast to all ReactReceive subscribers
         -> handler.Handle(event)
           -> Execute each instruction's callbacks
```

---

## FluidBus.CallBack

A simple named callback registry using the `FluidCallBack` delegate.

```csharp
using FluidBus.Core.Abstracts;
using FluidBus.CallBack.Core;

// Register a callback
FCallBack.RegisterCallBack("on_complete", data =>
{
    Console.WriteLine($"Completed with: {data}");
    return data;
});

// Execute by name (through the facade)
FBus.CallBack("on_complete", "some result");

// Remove a callback
FCallBack.DropCallBack("on_complete");
```

Returns `null` silently if the callback doesn't exist.

---

## FluidBus.Benchmark

Built-in benchmarking with warmup support and nanosecond precision (`Stopwatch.GetTimestamp()`).

```csharp
using FluidBus;
using FluidBus.Benchmark.Core;

BenchResult result = FBus.Bench("route 1000 events", iterations: 1000, warmup: 100, () =>
{
    FBus.Route(myEvent);
});

result.Print(); // Prints iterations, duration (ms), avg ns/iteration
```

`BenchResult` exposes: `Iteration`, `Warmup`, `Case`, `Start`, `End`, `Duration` (ms).

---

## License

[Business Source License 1.1](LICENSE) - See LICENSE file for details.
