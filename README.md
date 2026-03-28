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

Events carry a protocol and a set of instructions. They implement `IRouteEvent`, which extends `IFluidEvent` with:
- `Protocol` — the `BusProtocol` used for dispatch (sync/async)
- `Dispatch(IFluidHandler)` — executes the event's instructions through the given handler

Inherit from `RouteEvent`:

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

### 3. Register and manage handlers

```csharp
using FluidBus.React.Core;

var handler = new ChatMessageHandler("chat_listener");

// Register a handler
FReact.RegisterHandler(handler);

// Remove a handler
FReact.DropHandler(handler);
```

### 4. Publish

```csharp
using FluidBus;
using FluidBus.Core.Abstracts;
using FluidBus.React.Core;

// Instantiating the handler auto-subscribes it to the ChatMessageEvent channel
var handler = new ChatMessageHandler("chat_listener");

// Publish - all subscribed handlers are notified asynchronously
var instruction = new PrintInstruction("New message!", msg => Console.WriteLine(msg));
FBus.React(new ChatMessageEvent("msg_1", instruction));

// Wait for all pending react tasks to complete before exiting
FReact.Flush();
```

### Flush

React events are dispatched in parallel (fire-and-forget). Call `FReact.Flush()` to wait for all pending tasks to complete. Typically placed at the end of your `Main`:

```csharp
FBus.React(event1);
FBus.React(event2);
FBus.React(event3);

// All three events run in parallel — Flush blocks until every task is done
FReact.Flush();
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

## Error handling

All exceptions inherit from `FluidBusError` and provide explicit messages. No silent failures.

| Exception | Thrown when |
|---|---|
| `DispatchException` | An async dispatch fails, or an unknown `ExecutionStrategy` is encountered on a protocol |
| `ProtocolNotFoundException` | `FRouter.Publish()` is called with an event whose protocol has no registered port |
| `HandlerNotFoundException` | No handler is registered for a given event type, or `HandlerLinq.Drop()` targets a handler that doesn't exist |
| `DuplicateHandlerException` | `HandlerLinq.Register()` is called with a handler whose ID is already registered for that event type |
| `InstructionException` | `Execute()` or `ExecuteAndGet()` is called on an instruction with no methods/funcs, or with null data |
| `ChannelException` | A `ReactChannel.Write()` is called with no subscribers, or a subscriber callback fails during async dispatch |
| `HandlerLinqException` | General error in the handler query layer |

All exceptions expose `.DisplayMessage()` (inherited from `FluidBusError`) to print the error to the console.

---

## License

[Business Source License 1.1](LICENSE) - See LICENSE file for details.
