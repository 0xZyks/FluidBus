# 🌀 FluidBus
> A lightweight and extensible event bus for C# applications.

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Language](https://img.shields.io/badge/language-C%23-blue.svg)]()

---

## ✨ Overview

**FluidBus** is a modular and dynamic event system built around the idea of associating each event type with an OpCode and a strongly-typed handler.  
It’s designed to be fast, flexible, and easy to extend — whether you’re building a small app, a plugin system, or a full framework.

---

## ⚙️ Features

- ⚡ **Dynamic Event Dispatching** — Send events with automatic handler resolution.  
- 🧩 **Strong Typing** — Type-safe `FluidEvent` / `FluidHandler<T>` structure.  
- 🔢 **OpCode-based Routing** — Each handler is mapped to an event type via an opcode.  
- 🔄 **Centralized Registration** — Manage handlers and opcodes from a single registry (`HandlerLinq`).  
- 🧠 **Self-Logging** — Built-in `BusLogEvent` and `BusLogHandler` for system-level feedback.  
- 🔧 **Extensible API** — Simple to extend with your own events and handlers.  
- 🧵 *(Optional)* Thread-safe ready and async-friendly design (future-proof).

---

## 📦 Installation

Clone or include the source project directly into your solution:

```bash
git clone https://github.com/0xZyks/FluidBus.git
```

Then add a reference to the FluidBus project in your application.
🚀 Quick Start
1️⃣ Initialize the Bus

Before publishing or dispatching any events:

Bus.Init();

This registers the internal BusLogEvent and its handler automatically.
2️⃣ Create Your Own Event

You define events by inheriting from FluidEvent:
```csharp
public class PlayerJoinEvent : FluidEvent
{
    public string PlayerName { get; }

    public PlayerJoinEvent(string name)
        : base(IdGenerator.GetNewId(IdGenerator.IdType.Event))
    {
        PlayerName = name;
    }
}
```
3️⃣ Create a Matching Handler

Every event needs a handler derived from FluidHandler<T>:

```csharp
public class PlayerJoinHandler : FluidHandler<PlayerJoinEvent>
{
    public PlayerJoinHandler() 
        : base(IdGenerator.GetNewId(IdGenerator.IdType.Handler)) 
    { }

    public override void Handle(PlayerJoinEvent evt)
    {
        Console.WriteLine($"[JOIN] Player connected: {evt.PlayerName}");
    }
}
```

4️⃣ Register the Handler

Handlers are registered by associating them with an opcode and their event type:

```csharp
Bus.RegisterByOpCode(0x10); // Optional registration via opcode only

HandlerLinq.RegisterNewOpCode(
    opcode: 0x10,
    handler: new PlayerJoinHandler(),
    evt: typeof(PlayerJoinEvent)
);

Bus.RegisterByHandler(new PlayerJoinHandler());
```

5️⃣ Publish an Event

Finally, trigger your event anywhere in your app:

```csharp
var joinEvent = new PlayerJoinEvent("Alice");
Bus.Publish(joinEvent);
```

The bus will automatically:

    Log the dispatch (BusLogEvent)

    Find the right handler(s)

    Execute their Handle() methods

Output:
```bash
[SYS] - Event Dispatched: $Fluid-EVT-XXXXXXX
[JOIN] Player connected: Alice
[SYS] - Handler Triggered: $Fluid-HDL-XXXXXXX
```

💡 Advanced Example

Multiple handlers, dynamic events, and logging:

```csharp
// Another event
public class ScoreUpdatedEvent : FluidEvent
{
    public string Player { get; }
    public int NewScore { get; }

    public ScoreUpdatedEvent(string player, int score)
        : base(IdGenerator.GetNewId(IdGenerator.IdType.Event))
    {
        Player = player;
        NewScore = score;
    }
}

// Corresponding handler
public class ScoreUpdatedHandler : FluidHandler<ScoreUpdatedEvent>
{
    public ScoreUpdatedHandler() 
        : base(IdGenerator.GetNewId(IdGenerator.IdType.Handler)) { }

    public override void Handle(ScoreUpdatedEvent evt)
    {
        Console.WriteLine($"[SCORE] {evt.Player} now has {evt.NewScore} points!");
    }
}

// Register them
HandlerLinq.RegisterNewOpCode(0x11, new ScoreUpdatedHandler(), typeof(ScoreUpdatedEvent));
Bus.RegisterByHandler(new ScoreUpdatedHandler());

// Trigger the event
Bus.Publish(new ScoreUpdatedEvent("Alice", 42));
```
```bash
🧠 Architecture Overview

+----------------+
|     Bus        |  --> Public API (Init, Publish, Register)
+----------------+
          |
          v
+----------------+
|  HandlerLinq   |  --> Central registry (handlers + opcodes)
+----------------+
          |
          v
+----------------+
| HandlerOpCode  |  --> Binding: (byte OpCode, Type Event, IFluidHandler Handler)
+----------------+
          ^
          |
+----------------+
|   FluidEvent   |  --> Base for all events, handles dispatch & logs
|   FluidHandler |  --> Generic handler implementation
+----------------+
```

🔮 Roadmap

Add DispatchAsync() for async handlers

Add thread-safe collections (ConcurrentDictionary)

Add dependency injection integration (Autofac / Microsoft.Extensions.DependencyInjection)

Publish NuGet package

    Write full test suite

🧑‍💻 Author

FluidBus — developed by Zyks/ZKS
🕒 Initial version built in ~10 hours over 2 days
💬 “A fluid event system for developers who like clean architecture.”
📜 License

MIT License — feel free to fork, modify, and build upon FluidBus.
