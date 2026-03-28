# FluidBus

A lightweight, modular event bus for .NET built around two dispatch models: **Router** (protocol-based routing with sync/async support) and **React** (channel-based reactive subscriptions).

## Architecture

```
FluidBus
├── FluidBus.Core      # Interfaces, protocols, tasks, errors
├── FluidBus.Router     # Protocol-based event routing (sync/async)
├── FluidBus.React      # Reactive channel-based event dispatch
└── FBus                # Unified facade
```

## Getting started

Reference `FluidBus` in your project. The `FBus` facade gives you access to both models:

```csharp
using FluidBus;

// Route an event through a protocol
FBus.Route(myRouteEvent);

// React to an event through channels
FBus.React(myReactEvent);
```

---

## FluidBus.Router

The Router dispatches events through **protocols** (sync or async) to registered **handlers**, matched by event type.

### 1. Create a custom instruction

Instructions carry the logic that handlers execute. Inherit from `FluidInstruction<T>`:

```csharp
using FluidBus.Core.Abstracts;

public class PrintInstruction : FluidInstruction<string>
{
    public PrintInstruction(string? data, params FluidMethod<string>[] methods)
        : base(data, methods) { }
}
```

### 2. Create a custom event

Events carry a protocol and a set of instructions. Inherit from `RouteEvent`:

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

### 3. Create a custom handler

Handlers react to a specific event type. Inherit from `RouteHandler<T>`:

```csharp
using FluidBus.Router.Abstracts;

public class UserCreatedHandler : RouteHandler<UserCreatedEvent>
{
    public UserCreatedHandler(string id)
        : base($"{nameof(UserCreatedEvent)}::{id}") { }

    // Override Handle to add custom logic
    public override bool Handle(IFluidEvent evt)
    {
        Console.WriteLine($"[{Id}] Handling event {evt.Id}");
        return base.Handle(evt);
    }
}
```

### 4. Register and publish

```csharp
using FluidBus;
using FluidBus.Core.Abstracts;
using FluidBus.Core.Protocols;
using FluidBus.Router.Core;

// Register the handler
FRouter.Register(new UserCreatedHandler("user_handler"));

// Create an instruction with a method to execute
var instruction = new PrintInstruction("Hello from FluidBus!", msg => Console.WriteLine(msg));

// Publish the event on the System protocol (sync)
FBus.Route(new UserCreatedEvent("evt_1", BusProtocol.System, instruction));
```

### Custom protocols

You can define your own protocols with sync or async execution:

```csharp
using FluidBus.Core.Protocols;

public class AsyncProtocol : BusProtocol
{
    public override ExecutionStrategy Strategy => ExecutionStrategy.Async;
    public AsyncProtocol() : base("ASYNC") { }
}

// Register the port for the protocol
FRouter.AddPort(new AsyncProtocol());
```

---

## FluidBus.React

React uses **channels** instead of protocols. Handlers auto-subscribe to their event type's channel on creation. Events are dispatched asynchronously to all subscribers.

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

### 3. Instantiate and publish

```csharp
using FluidBus;
using FluidBus.Core.Abstracts;

// Instantiating the handler auto-subscribes it to the ChatMessageEvent channel
var handler = new ChatMessageHandler("chat_listener");

// Publish - all subscribed handlers are notified asynchronously
var instruction = new PrintInstruction("New message!", msg => Console.WriteLine(msg));
FBus.React(new ChatMessageEvent("msg_1", instruction));
```

---

## Router vs React

| | Router | React |
|---|---|---|
| Dispatch | Protocol-based (sync/async) | Channel-based (always async) |
| Handler registration | Manual via `FRouter.Register()` | Auto-subscribe on instantiation |
| Matching | First matching handler | All subscribers on the channel |
| Use case | Command/request patterns | Broadcast/observer patterns |

---

## License

[Business Source License 1.1](LICENSE) - See LICENSE file for details.
