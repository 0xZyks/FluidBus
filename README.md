# FluidBus

Bus d'evenements modulaire en C# (.NET 10) concu dans le cadre du projet **Fluid.Guard**.

## Principe

FluidBus implemente un pattern **publish/subscribe** ou :

- Des **Events** transportent des **Instructions** executables
- Des **Handlers** enregistres traitent les events selon leur type
- Des **Protocols** definissent la strategie d'execution (synchrone ou asynchrone)
- Une **BluePrintFactory** permet de creer events et handlers dynamiquement par reflexion

## Architecture

```
FluidBus/
├── Core/
│   ├── FBus.cs                   # Point d'entree statique du bus (Publish, Register, AddPort)
│   ├── BusProtocols/
│   │   ├── BusProtocol.cs        # Classe abstraite + enum ExecutionStrategy (Sync/Async)
│   │   └── SystemProtocol.cs     # Protocole systeme synchrone par defaut
│   ├── Herits/
│   │   ├── IFluidEvent.cs        # Interface evenement (Id, Protocol, Instructions, Dispatch)
│   │   ├── FluidEvent.cs         # Classe abstraite avec dispatch + logging auto
│   │   ├── IFluidHandler.cs      # Interface handler (Id, CallCount, EventType, Handle)
│   │   └── FluidHandler<T>.cs    # Classe abstraite generique typee par event
│   ├── Instructions/
│   │   ├── IFluidInstruction.cs   # Interface instruction (Execute, ExecuteAndGet)
│   │   ├── FluidInstruction<T>.cs # Abstraite avec delegates (FluidMethod/FluidFunc) + event OnResult
│   │   └── System/
│   │       └── LogInstruction.cs  # Instruction de logging (FluidInstruction<string>)
│   ├── HLinq/
│   │   └── HandlerLinq.cs        # Registre statique de handlers avec lookup par type
│   └── Tasks/
│       └── FluidTask.cs          # Wrapper async avec etats (Running, Completed, Failed, Cancelled)
├── BluePrint/
│   └── BluePrintFactory.cs       # Factory par reflexion pour creer events et handlers
├── Event/
│   └── BusLogEvent.cs            # Event de log systeme interne
├── Handler/
│   └── BusLogHandler.cs          # Handler pour les logs systeme
└── Errors/
    ├── FluidBusError.cs          # Exception abstraite de base
    ├── BluePrintException.cs     # Erreur de factory
    ├── DispatchException.cs      # Erreur de dispatch
    └── HandlerLinqException.cs   # Erreur de registre
```

## Flux d'execution

1. **Register** — Un handler est enregistre via `FBus.Register()`, stocke dans `HandlerLinq` par type d'event
2. **Publish** — Un event est publie via `FBus.Publish()`, le bus cherche un port compatible (meme protocol)
3. **Lookup** — `HandlerLinq.TryGetHandlers()` trouve les handlers correspondant au type de l'event
4. **Dispatch** — Le `BusPort` dispatch selon la strategie du protocol (sync direct ou async via `FluidTask`)
5. **Handle** — Le handler execute les instructions de l'event (`Execute()` / `ExecuteAndGet()`)
6. **BluePrint** — Si aucun handler n'est disponible, `BluePrintFactory` en clone un par reflexion

## Utilisation rapide

```csharp
using FluidBus.Core;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using FluidBus.Core.Instructions.System;
using FluidBus.BluePrint;

// Definir un event custom
public class MyEvent : FluidEvent
{
    public MyEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs)
        : base($"{nameof(MyEvent)}::{id}", protocol, instrs) { }
}

// Definir un handler type sur MyEvent
public class MyHandler : FluidHandler<MyEvent>
{
    public MyHandler(string id) : base($"{nameof(MyHandler)}::{id}") { }

    public override bool Handle(IFluidEvent evt)
    {
        foreach (var instr in evt.Instructions)
        {
            instr.Execute();
            instr.ExecuteAndGet();
        }
        CallCount++;
        return true;
    }
}

// Utilisation
FBus.Register(new MyHandler("mon_handler"));

var instr = new LogInstruction("Hello FluidBus", msg => Console.WriteLine(msg));

var (evt, success) = BluePrintFactory.NewEvent(
    typeof(MyEvent),
    "mon_event",
    BusProtocol.System,
    instr);

FBus.Publish(evt);
```

## Build

Prerequis : .NET 10 SDK

```bash
dotnet build
dotnet run --project LoggerTestBusCSharpPython
```

## Licence

MIT — Copyright (c) 2026 ZKS
