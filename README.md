# Fluid.Guard

**Fluid.Guard** est un **packer C#** qui transforme du code IL (.NET) en un binaire hybride **Rust + C#** via P/Invoke, pilote par un **bus event-driven**. L'objectif : rendre le reverse engineering extremement difficile en remplacant le control flow original par un systeme d'evenements opaques, des tokens rotatifs, et du bytecode custom genere par Rust.

Le code original est transforme par **dnlib** au moment du packing : chaque appel de methode, chaque usage de resultat devient un evenement publie sur le bus, avec des IDs derives d'une seed de build unique.

---

## Table des matieres

1. [Concept du packer](#concept-du-packer)
2. [Architecture globale](#architecture-globale)
3. [Structure du projet](#structure-du-projet)
4. [C# - FluidBus Library](#c---fluidbus-library)
   - [FBus (Bus principal)](#fbus---bus-principal)
   - [Systeme d'evenements](#systeme-devenements)
   - [Systeme de handlers](#systeme-de-handlers)
   - [HandlerLinq (Registre)](#handlerlinq---registre)
   - [Systeme d'instructions](#systeme-dinstructions)
   - [Protocoles et strategies d'execution](#protocoles-et-strategies-dexecution)
   - [FluidTask (Async)](#fluidtask---async)
   - [BluePrintFactory (Factory dynamique)](#blueprintfactory---factory-dynamique)
   - [FluidCoreAPI (Interop Rust)](#fluidcoreapi---interop-rust)
   - [FluidVM (Machine virtuelle)](#fluidvm---machine-virtuelle)
   - [Systeme d'exceptions](#systeme-dexceptions)
5. [Rust - fluid_core](#rust---fluid_core)
   - [FFI Exports (lib.rs)](#ffi-exports-librs)
   - [Seed (seed.rs)](#seed-seedrs)
   - [Token (token.rs)](#token-tokenrs)
   - [Bytecode (bytecode.rs)](#bytecode-bytecoders)
6. [Programme de test](#programme-de-test)
7. [Flux d'execution complet](#flux-dexecution-complet)
8. [Format du bytecode](#format-du-bytecode)
9. [Securite et proprietes cryptographiques](#securite-et-proprietes-cryptographiques)
10. [Concept de packing dnlib](#concept-de-packing-dnlib)
11. [Design cible complet](#design-cible-complet)
12. [Diagrammes d'architecture](#diagrammes-darchitecture)
13. [Build et configuration](#build-et-configuration)
14. [Roadmap et etat actuel](#roadmap-et-etat-actuel)
15. [Code source complet](#code-source-complet)

---

## Concept du packer

### Vue d'ensemble

Fluid.Guard prend un assembly .NET classique et le transforme en un binaire ou :
1. **Le control flow original disparait** — remplace par des evenements sur un bus pub/sub
2. **Les appels de methode deviennent du bytecode** — genere par Rust avec des signatures seed-dependantes
3. **Les donnees transitent via des tokens rotatifs** — chaque appel consomme et rotate un token
4. **Le code natif Rust** sert de couche d'indirection — le C# n'a jamais acces direct aux bytes

### Pipeline de packing (cible)

```
Code C# original (.dll/.exe)
         |
         | dnlib analyse l'IL
         v
Identification des appels de methode et usages de resultats
         |
         | Transformation IL
         v
Chaque appel -> FBus.Publish(new CoreEvent(hash(nom+seed_build), ...))
Chaque resultat -> OnResult callback -> nouvel event chaine
         |
         | Generation bytecode
         v
Rust genere le bytecode pour chaque methode (type, method, args)
Signatures derivees de seed_build -> uniques par build
         |
         | Assemblage final
         v
Binaire hybride : C# (bus + handlers) + Rust (libfluid_core.so/dll)
```

### Ce que voit un reverse engineer

**Avant packing :**
```csharp
var result = Console.ReadLine();
Console.WriteLine(result);
```

**Apres packing :**
```csharp
FBus.Publish(new CoreEvent("a3f7b2c1", BusProtocol.System,
    new RustInstruction(0x02, (data) => FluidCoreAPI.Send(data))));
// a3f7b2c1 = hash("ReadLine" + seed_build) — opaque, change a chaque build
```

Les IDs d'events sont :
- **Uniques** — pas de collision entre events
- **Opaques** — aucun lien visible avec la logique originale
- **Seed-dependants** — `hash(nom_fonction + seed_build)`, changent a chaque build

---

## Architecture globale

```
+--------------------------------------------------+
|              Programme de test (C#)               |
|         LoggerTestBusCSharpPython/Program.cs       |
+--------------------------------------------------+
                        |
                        | utilise
                        v
+--------------------------------------------------+
|              FluidBus Library (C#)                |
|                                                    |
|  FBus (dispatcher) <---> HandlerLinq (registre)   |
|       |                       |                    |
|  BusPort/Protocol        Handlers                  |
|       |                       |                    |
|  Events <-----------> Instructions                 |
|       |                       |                    |
|  BluePrintFactory        FluidTask                 |
|       |                                            |
|  FluidCoreAPI (P/Invoke)                           |
|       |                                            |
|  FluidVM (stub)                                    |
+--------------------------------------------------+
                        |
                        | P/Invoke (DllImport)
                        v
+--------------------------------------------------+
|           fluid_core Library (Rust)               |
|              libfluid_core.so/dll                  |
|                                                    |
|  init()          -> seed generation                |
|  process_bytes() -> echo/ack                       |
|  get_token()     -> token generation               |
|  rotate_token()  -> token rotation                 |
|  get_bytecode()  -> bytecode generation            |
|  free_bytes()    -> deallocation memoire            |
+--------------------------------------------------+
```

---

## Structure du projet

```
FluidBus/
├── LoggerTestBusCSharpPython.slnx    # Solution .NET (regroupe les 2 projets C#)
├── README.md
├── LICENSE
├── .gitignore
│
├── FluidBus/                          # Librairie C# (le framework)
│   ├── FluidBus.csproj               # .NET 10.0, pas de deps externes
│   ├── Core/
│   │   ├── FBus.cs                   # Bus principal statique
│   │   ├── FluidCoreAPI.cs           # P/Invoke vers libfluid_core (Rust)
│   │   ├── VM/
│   │   │   └── FluidVM.cs            # Machine virtuelle (stub)
│   │   ├── Tasks/
│   │   │   └── FluidTask.cs          # Wrapper async avec etats
│   │   ├── HLinq/
│   │   │   └── HandlerLinq.cs        # Registre de handlers (type-based lookup)
│   │   ├── Herits/
│   │   │   ├── IFluidEvent.cs        # Interface evenement
│   │   │   ├── FluidEvent.cs         # Classe abstraite evenement
│   │   │   ├── IFluidHandler.cs      # Interface handler
│   │   │   └── FluidHandler.cs       # Classe abstraite handler generique
│   │   ├── Instructions/
│   │   │   ├── IFluidInstruction.cs   # Interface instruction
│   │   │   ├── FluidInstruction.cs    # Classe abstraite instruction generique
│   │   │   ├── System/
│   │   │   │   └── LogInstruction.cs  # Instruction de logging
│   │   │   └── Core/
│   │   │       └── RustInstruction.cs # Instruction avec token Rust rotatif
│   │   └── BusProtocols/
│   │       ├── BusProtocol.cs         # Classe abstraite protocole + enum ExecutionStrategy
│   │       └── SystemProtocol.cs      # Protocole systeme (Sync)
│   ├── Event/
│   │   ├── BusLogEvent.cs             # Evenement de log interne
│   │   └── CoreEvent.cs               # Evenement metier principal
│   ├── Handler/
│   │   ├── BusLogHandler.cs           # Handler pour BusLogEvent
│   │   └── CoreHandler.cs             # Handler pour CoreEvent
│   ├── BluePrint/
│   │   └── BluePrintFactory.cs        # Factory par reflexion (Activator.CreateInstance)
│   └── Errors/
│       ├── FluidBusError.cs           # Exception de base abstraite
│       ├── BluePrintException.cs      # Erreur factory
│       ├── DispatchException.cs       # Erreur dispatch
│       └── HandlerLinqException.cs    # Erreur registre
│
├── fluid_core/                        # Librairie Rust (core natif)
│   ├── Cargo.toml                     # cdylib, edition 2024, 0 deps externes
│   └── src/
│       ├── lib.rs                     # FFI exports (6 fonctions extern "C")
│       └── core/
│           ├── mod.rs                 # Declarations de modules
│           ├── seed.rs                # Generation de seed (time XOR pid)
│           ├── token.rs               # Generation et rotation de tokens
│           └── bytecode.rs            # Generation de bytecode structure
│
└── LoggerTestBusCSharpPython/         # Programme de test
    ├── LoggerTestBusCSharpPython.csproj  # .NET 10.0, reference FluidBus.csproj
    └── Program.cs                     # Main de test
```

---

## C# - FluidBus Library

**Target:** .NET 10.0
**Dependencies externes:** Aucune (standard library uniquement)
**Nullable:** Active
**ImplicitUsings:** Active

### FBus - Bus principal

**Fichier:** `FluidBus/Core/FBus.cs`
**Type:** Classe statique

Le point d'entree central du framework. C'est le dispatcher principal.

**Etat interne:**
- `HashSet<BusPort> _ports` : Collection de ports de routage (un port par protocole)

**Constructeur statique:**
1. Appelle `FluidCoreAPI.Initialize()` pour initialiser la lib Rust
2. Cree le HashSet de ports
3. Enregistre un `BusLogHandler("bus_logger")` pour le logging interne
4. Ajoute un port pour `BusProtocol.System` (SystemProtocol)

**Methodes publiques:**

| Methode | Signature | Description |
|---------|-----------|-------------|
| `AddPort` | `bool AddPort(BusProtocol protocol)` | Cree un BusPort et l'ajoute aux ports |
| `Register` | `bool Register(IFluidHandler hdl)` | Delegue a `HandlerLinq.Register(hdl)` |
| `TryGetHandlers` | `bool TryGetHandlers(IFluidEvent evt, out Dictionary<IFluidHandler, bool> handlers)` | Delegue a `HandlerLinq.TryGetHandlers` |
| `Publish` | `bool Publish(IFluidEvent evt)` | Dispatche l'evenement (voir flux ci-dessous) |

**Logique de `Publish(IFluidEvent evt)` :**
1. Itere sur `_ports` pour trouver le port dont le protocole matche `evt.Protocol`
2. Cherche un handler disponible (valeur `false` = pas occupe) via `TryGetHandlers`
3. Si aucun handler dispo : `BluePrintFactory.NewHandler()` en cree un dynamiquement
4. Incremente `handler.CallCount`
5. Appelle `port.Dispatch(evt, handler)`

**Classe interne `BusPort` :**
- Propriete `BusProtocol Protocol`
- Methode `Dispatch(IFluidEvent evt, IFluidHandler hdl)` :
  - Si `Strategy == Sync` : appelle `evt.Dispatch(hdl)` directement
  - Si `Strategy == Async` : wrap dans un `FluidTask` avec callback `OnComplete`
- `Equals/GetHashCode` : compare par `Protocol.Name`

#### Exemples d'usage FBus

**Enregistrement + publication basique :**
```csharp
// 1. Enregistrer un handler pour les CoreEvent
FBus.Register(new CoreHandler("mon_handler"));

// 2. Creer une instruction (ici une simple lambda void)
var logInstr = new LogInstruction("Hello FluidBus", (msg) => {
    Console.WriteLine(msg);
});

// 3. Publier un evenement — le handler execute l'instruction
FBus.Publish(new CoreEvent("mon_event", BusProtocol.System, logInstr));
// Output: "Hello FluidBus"
```

**Ajout d'un protocole custom :**
```csharp
// Creer un protocole async custom
public class MyAsyncProtocol : BusProtocol
{
    public override ExecutionStrategy Strategy => ExecutionStrategy.Async;
    public MyAsyncProtocol() : base("MY_ASYNC") { }
}

// L'ajouter au bus
FBus.AddPort(new MyAsyncProtocol());

// Les events publies avec ce protocole seront dispatches dans un FluidTask
FBus.Publish(new CoreEvent("async_evt", myAsyncProtocol, instruction));
```

**Plusieurs instructions dans un seul event :**
```csharp
var instr1 = new LogInstruction("Step 1", (msg) => Console.WriteLine(msg));
var instr2 = new LogInstruction("Step 2", (msg) => Console.WriteLine(msg));
var instr3 = new RustInstruction(0x01, (data) => FluidCoreAPI.Send(data));

// Toutes les instructions sont executees par le handler
FBus.Publish(new CoreEvent("multi", BusProtocol.System, instr1, instr2, instr3));
```

---

### Systeme d'evenements

#### IFluidEvent (Interface)

**Fichier:** `FluidBus/Core/Herits/IFluidEvent.cs`

```csharp
interface IFluidEvent
{
    string Id { get; }
    BusProtocol Protocol { get; }
    HashSet<IFluidInstruction> Instructions { get; }
    bool Dispatch(IFluidHandler hdl);
}
```

#### FluidEvent (Classe abstraite)

**Fichier:** `FluidBus/Core/Herits/FluidEvent.cs`
**Implemente:** `IFluidEvent`

**Constructeur:**
```csharp
FluidEvent(string id, BusProtocol protocol, params IFluidInstruction[] instructions)
```
- Stocke id, protocol
- Convertit le tableau d'instructions en `HashSet<IFluidInstruction>`

**Methode `Dispatch(IFluidHandler handler)` :**
1. Cree un `BusLogEvent` via `BluePrintFactory.NewEvent()` avec message `"Event Dispatched: {Id}"`
2. Publie ce log event sur le bus via `FBus.Publish()`
3. Appelle `handler.Handle(this)`
4. Cree un second `BusLogEvent` avec message `"Handler Triggered: {handler.Id}"`
5. Publie ce second log event
6. Retourne `true`

**Pattern:** Chaque dispatch d'evenement genere automatiquement 2 evenements de log internes.

#### Implementations concretes

| Classe | Fichier | Format Id | Usage |
|--------|---------|-----------|-------|
| `BusLogEvent` | `FluidBus/Event/BusLogEvent.cs` | `[EVT::{BusLogEvent}::{id}]` | Evenements de log systeme |
| `CoreEvent` | `FluidBus/Event/CoreEvent.cs` | `[EVT::{CoreEvent}::{id}]` | Evenements metier |

Les deux heritent de `FluidEvent` et ajoutent simplement un formatage d'Id specifique.

#### Exemple : creer un type d'evenement custom

```csharp
// Definir un nouvel evenement
public class SecurityEvent : FluidEvent
{
    public SecurityEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs)
        : base($"[EVT::{{SecurityEvent}}::{id}]", protocol, instrs) { }
}

// Definir le handler correspondant
public class SecurityHandler : FluidHandler<SecurityEvent>
{
    public SecurityHandler(string id) : base($"[HDL::{{SecurityHandler}}::{id}]") { }

    public override bool Handle(IFluidEvent evt)
    {
        foreach (var instruction in evt.Instructions)
        {
            instruction.Execute();
            instruction.ExecuteAndGet();
        }
        CallCount++;
        return true;
    }
}

// Utilisation
FBus.Register(new SecurityHandler("sec_hdl"));
FBus.Publish(new SecurityEvent("alert", BusProtocol.System, someInstruction));
```

**Format des IDs generes :**
```
CoreEvent("test")     → Id = "[EVT::{CoreEvent}::test]"
BusLogEvent("log_1")  → Id = "[EVT::{BusLogEvent}::log_1]"
SecurityEvent("alert") → Id = "[EVT::{SecurityEvent}::alert]"
```

---

### Systeme de handlers

#### IFluidHandler (Interface)

**Fichier:** `FluidBus/Core/Herits/IFluidHandler.cs`

```csharp
interface IFluidHandler
{
    string Id { get; }
    int CallCount { get; set; }
    Type EventType { get; }
    abstract bool Handle(IFluidEvent evt);
}
```

#### FluidHandler\<T\> (Classe abstraite generique)

**Fichier:** `FluidBus/Core/Herits/FluidHandler.cs`
**Implemente:** `IFluidHandler`

**Constructeur:**
```csharp
FluidHandler(string id)
```
- Stocke l'id
- Definit `EventType = typeof(T)`

`Handle` est abstrait et doit etre implemente par les sous-classes.

#### Implementations concretes

| Classe | Fichier | Type generique | Format Id | Comportement Handle |
|--------|---------|---------------|-----------|---------------------|
| `BusLogHandler` | `FluidBus/Handler/BusLogHandler.cs` | `BusLogEvent` | `[HDL::{BusLogEvent}::{id}]` | Execute toutes les instructions (Execute + ExecuteAndGet) |
| `CoreHandler` | `FluidBus/Handler/CoreHandler.cs` | `CoreEvent` | `HDL::{CoreHandler}::{id}` | Execute toutes les instructions (Execute + ExecuteAndGet) |

Les deux handlers ont la meme logique dans `Handle()` :
```csharp
foreach (var instruction in evt.Instructions)
{
    instruction.Execute();
    instruction.ExecuteAndGet();
}
CallCount++;
return true;
```

---

### HandlerLinq - Registre

**Fichier:** `FluidBus/Core/HLinq/HandlerLinq.cs`
**Type:** Classe statique

Registre central des handlers. Utilise un lookup par type d'evenement.

**Etat interne:**
```csharp
Dictionary<Type, Dictionary<IFluidHandler, bool>> handlers
```
- Cle externe : `Type` de l'evenement (ex: `typeof(CoreEvent)`)
- Cle interne : instance du handler
- Valeur interne : `bool` = handler occupe ou non

**Methodes:**

| Methode | Description |
|---------|-------------|
| `Register(IFluidHandler handler)` | Ajoute le handler dans `handlers[handler.EventType]`. Cree l'entree si elle n'existe pas. |
| `Drop(IFluidHandler handler)` | Retire le handler de son type d'evenement. Retourne `false` si le type n'existe pas. |
| `TryGetHandlers(IFluidEvent evt, out Dictionary<IFluidHandler, bool> hdls)` | Cherche les handlers enregistres pour le type de `evt`. Retourne `true` si trouve. |

#### Exemple : etat interne du registre

```
Apres les enregistrements suivants :
  FBus.Register(new CoreHandler("core_1"));
  FBus.Register(new CoreHandler("core_2"));
  FBus.Register(new BusLogHandler("logger"));

Le dictionnaire interne ressemble a :
handlers = {
    typeof(CoreEvent) => {
        CoreHandler("core_1") => false,   // false = disponible
        CoreHandler("core_2") => false,
    },
    typeof(BusLogEvent) => {
        BusLogHandler("logger") => false,
    }
}

Quand FBus.Publish() utilise un handler, sa valeur passe a true (occupe).
Si aucun handler n'est disponible (tous a true), BluePrintFactory.NewHandler()
en cree un nouveau et l'enregistre automatiquement.
```

---

### Systeme d'instructions

#### IFluidInstruction (Interface)

**Fichier:** `FluidBus/Core/Instructions/IFluidInstruction.cs`

```csharp
interface IFluidInstruction
{
    Type DataType { get; }
    void Execute();
    object? ExecuteAndGet();
}
```

#### FluidInstruction\<T\> (Classe abstraite generique)

**Fichier:** `FluidBus/Core/Instructions/FluidInstruction.cs`

**Delegates definis:**
```csharp
delegate void FluidMethod<T>(T data);        // Callback void
delegate TResult FluidFunc<T, TResult>(T data); // Callback avec retour
delegate void FluidResult(object? result);    // Notification de resultat
```

**Proprietes:**
- `Type DataType` : toujours `typeof(T)`
- `T? Data` : payload de l'instruction (protected set)

**Evenement:**
- `event FluidResult? OnResult` : fire quand `ExecuteAndGet()` complete

**Constructeurs:**
```csharp
FluidInstruction(T? data, params FluidMethod<T>[] methods)  // Pour Execute()
FluidInstruction(T? data, params FluidFunc<T, object>[] funcs) // Pour ExecuteAndGet()
```

**Methodes:**

| Methode | Comportement |
|---------|-------------|
| `Execute()` | Invoque chaque `FluidMethod<T>` dans `_methods` avec `Data` |
| `ExecuteAndGet()` | Invoque chaque `FluidFunc<T, object>` dans `_funcs`, garde le dernier resultat, fire `OnResult`, retourne le resultat |

**Equals/GetHashCode:** Compare par `Data` et `typeof(T)`.

#### Exemples d'usage des instructions

**LogInstruction — execution void :**
```csharp
// Constructeur avec FluidMethod<string> (void)
var log = new LogInstruction("message de log", (msg) => {
    Console.WriteLine($"[LOG] {msg}");
});

log.Execute();
// Output: "[LOG] message de log"
```

**LogInstruction — execution avec retour :**
```csharp
// Constructeur avec FluidFunc<string, object> (retour)
var log = new LogInstruction("data", (msg) => {
    return msg.ToUpper();
});

log.OnResult += (result) => {
    Console.WriteLine($"Resultat: {result}");
};

var result = log.ExecuteAndGet();
// result = "DATA"
// OnResult fire avec "DATA"
```

**Chaine de fonctions dans une seule instruction :**
```csharp
// Plusieurs FluidFunc executees en sequence — seul le DERNIER resultat est retourne
var instr = new LogInstruction("input",
    (msg) => msg.ToUpper(),           // "INPUT"
    (msg) => msg + "_processed",      // "input_processed" (recoit le Data original)
    (msg) => msg.Length               // 5 (recoit le Data original)
);
// Attention : chaque func recoit Data (pas le resultat de la precedente)
// ExecuteAndGet() retourne le resultat de la DERNIERE func : 5
```

**RustInstruction — lifecycle complet avec rotation de token :**
```csharp
// Creation : demande un token a Rust pour l'opcode 0x01
var rustInstr = new RustInstruction(0x01, (data) => {
    // 'data' = le token courant (8 bytes)
    return FluidCoreAPI.Send(data);
});

// A ce stade :
//   rustInstr._token = token_v1 (8 bytes depuis hash(0x01 + seed))
//   rustInstr.Data   = token_v1

// Ecouter les resultats
rustInstr.OnResult += (result) => {
    byte[] response = (byte[])result;
    Console.WriteLine(Encoding.UTF8.GetString(response));
};

// Premier appel
rustInstr.Execute();
// → base.Execute() avec token_v1 (mais pas de _methods, donc no-op)
// → _token = FluidCoreAPI.Rotate(token_v1) → token_v2

rustInstr.ExecuteAndGet();
// → base.ExecuteAndGet() : appelle la func avec token_v2
// → FluidCoreAPI.Send(token_v2) → Rust recoit token_v2
// → OnResult fire avec la reponse de Rust
// → _token = FluidCoreAPI.Rotate(token_v2) → token_v3
// → Data = token_v3

// Prochain appel utilisera token_v3, etc.
```

**Structure en memoire d'un token au fil des rotations :**
```
Construction:  _token = [0xA3, 0x7F, 0x2B, 0x91, 0xC4, 0x58, 0xE0, 0x1D]  (token_v1)
Apres Execute: _token = [0x15, 0xD8, 0x4A, 0x63, 0xB7, 0x0C, 0x9E, 0xF2]  (token_v2)
Apres ExecGet: _token = [0x82, 0x6C, 0x3F, 0xA5, 0x09, 0xDB, 0x71, 0x44]  (token_v3)
(les valeurs changent a chaque run car le seed est different)
```

#### Implementations concretes

##### LogInstruction

**Fichier:** `FluidBus/Core/Instructions/System/LogInstruction.cs`
**Herite de:** `FluidInstruction<string>`

Instruction pour les operations de logging avec payload string. Memes constructeurs que la classe parent.

##### RustInstruction

**Fichier:** `FluidBus/Core/Instructions/Core/RustInstruction.cs`
**Herite de:** `FluidInstruction<byte[]>`

**Champ prive:** `byte[] _token`

**Constructeur:**
```csharp
RustInstruction(byte opcode, params FluidFunc<byte[], object>[] funcs)
```
1. Demande un token a Rust : `_token = FluidCoreAPI.RequestToken(opcode)`
2. Definit `Data = _token`
3. Stocke les `funcs`

**Comportement override:**

| Methode | Comportement additionnel |
|---------|------------------------|
| `Execute()` | Appelle `base.Execute()` puis `_token = FluidCoreAPI.Rotate(_token)` |
| `ExecuteAndGet()` | Appelle `base.ExecuteAndGet()` puis rotation du token + `Data = _token` |

**Pattern:** Chaque execution rotate le token automatiquement pour securite/gestion d'etat.

---

### Protocoles et strategies d'execution

#### ExecutionStrategy (Enum)

**Fichier:** `FluidBus/Core/BusProtocols/BusProtocol.cs`

```csharp
enum ExecutionStrategy
{
    Sync = 0,   // Execution synchrone
    Async = 1   // Execution asynchrone
}
```

#### BusProtocol (Classe abstraite)

**Fichier:** `FluidBus/Core/BusProtocols/BusProtocol.cs`

| Propriete | Type | Description |
|-----------|------|-------------|
| `Name` | `string` | Identifiant du protocole |
| `Strategy` | `ExecutionStrategy` (abstract) | Mode d'execution |

**Champ statique:**
```csharp
static readonly BusProtocol System = new SystemProtocol();
```

#### SystemProtocol

**Fichier:** `FluidBus/Core/BusProtocols/SystemProtocol.cs`
**Herite de:** `BusProtocol`

- `Name = "SYSTEM"`
- `Strategy = ExecutionStrategy.Sync`

Protocole par defaut, utilise pour les evenements systeme et les logs internes.

#### Exemple : dispatch Sync vs Async

```csharp
// === SYNC (SystemProtocol) ===
// L'appel est bloquant — Publish() retourne apres l'execution complete
FBus.Publish(new CoreEvent("sync_evt", BusProtocol.System, instruction));
Console.WriteLine("Ceci s'affiche APRES l'execution du handler");

// === ASYNC (protocole custom) ===
// L'appel est non-bloquant — Publish() retourne immediatement
// Le handler s'execute dans un FluidTask (Task.Run)
FBus.Publish(new CoreEvent("async_evt", myAsyncProtocol, instruction));
Console.WriteLine("Ceci s'affiche PENDANT l'execution du handler");
```

**Comment BusPort.Dispatch gere l'async :**
```csharp
// Internement, quand Strategy == Async :
new FluidTask(() => evt.Dispatch(hdl))
    .OnComplete((state) => {
        if (state == FluidTaskState.Failed)
            throw new DispatchException("Dispatch failed");
    });
```

---

### FluidTask - Async

**Fichier:** `FluidBus/Core/Tasks/FluidTask.cs`

Wrapper au-dessus de `System.Threading.Tasks.Task` avec gestion d'etats custom.

#### FluidTaskState (Enum)

```csharp
enum FluidTaskState
{
    Running,
    Completed,
    Failed,
    Cancelled
}
```

#### FluidTask (Classe)

**Constructeurs:**
```csharp
FluidTask(Action action)        // Lance Task.Run(action)
FluidTask(Func<bool> action)    // Lance Task.Run(action)
```

**Proprietes:**
- `bool IsCompleted` : `_task.IsCompleted`
- `bool IsFailed` : `_task.IsFaulted`

**Methodes:**

| Methode | Description |
|---------|-------------|
| `GetState()` | Retourne l'etat courant (Cancelled > Failed > Completed > Running) |
| `OnComplete(Action<FluidTaskState> callback)` | Attache un callback de fin, retourne `this` (fluent API) |

#### Exemple : FluidTask avec chainage fluent

```csharp
// Lancer une action async et reagir a la fin
new FluidTask(() => {
    // Code execute dans Task.Run()
    Thread.Sleep(1000);
    Console.WriteLine("Travail fini");
})
.OnComplete((state) => {
    // Callback quand la task se termine
    switch (state)
    {
        case FluidTaskState.Completed:
            Console.WriteLine("Succes !");
            break;
        case FluidTaskState.Failed:
            Console.WriteLine("Erreur !");
            break;
        case FluidTaskState.Cancelled:
            Console.WriteLine("Annule !");
            break;
    }
});
```

---

### BluePrintFactory - Factory dynamique

**Fichier:** `FluidBus/BluePrint/BluePrintFactory.cs`
**Type:** Classe statique

Factory utilisant la reflexion (`Activator.CreateInstance`) pour creer des handlers et evenements dynamiquement.

**Methodes publiques:**

| Methode | Signature | Description |
|---------|-----------|-------------|
| `NewHandler` | `(IFluidHandler, bool) NewHandler(IFluidHandler baseHdl)` | Cree un nouveau handler du meme type que `baseHdl` via reflexion, l'enregistre dans HandlerLinq |
| `NewEvent` | `(IFluidEvent, bool) NewEvent(Type baseEvt, string name, BusProtocol protocol, params IFluidInstruction[] instrs)` | Cree un evenement du type `baseEvt` avec les args fournis |

**Methode privee:**
```csharp
T CreateInstance<T>(Type concreteType, params object[] args) where T : class
```
- Utilise `Activator.CreateInstance(concreteType, args)`
- Leve `BluePrintException` si constructeur manquant ou erreur d'invocation

**Usage dans FBus :**
- Quand `Publish()` ne trouve aucun handler disponible, il appelle `NewHandler()` pour en creer un nouveau dynamiquement
- `FluidEvent.Dispatch()` utilise `NewEvent()` pour creer les `BusLogEvent` internes

#### Exemples d'usage

**Creation dynamique d'un handler :**
```csharp
// A partir d'un handler existant, creer un clone du meme type
var original = new CoreHandler("template");
var (newHandler, success) = BluePrintFactory.NewHandler(original);
// newHandler est un nouveau CoreHandler, enregistre dans HandlerLinq
// success = true si tout s'est bien passe
```

**Creation dynamique d'un event :**
```csharp
// Creer un CoreEvent sans utiliser 'new CoreEvent(...)' directement
var (evt, success) = BluePrintFactory.NewEvent(
    typeof(CoreEvent),           // Type concret a instancier
    "dynamic_event",             // Id
    BusProtocol.System,          // Protocole
    new RustInstruction(0x01, (data) => FluidCoreAPI.Send(data))
);

// Equivalent a : new CoreEvent("dynamic_event", BusProtocol.System, ...)
// Mais sans reference directe au type dans le code — utile pour le packing
FBus.Publish(evt);
```

**Pourquoi la factory est essentielle pour le packer :**
```csharp
// Code packe — aucune reference directe aux types concrets :
var (evt, _) = BluePrintFactory.NewEvent(
    Type.GetType("FluidBus.CoreEvent"),  // Resolu a runtime par reflexion
    "a3f7b2c1",                           // Hash opaque
    BusProtocol.System,
    new RustInstruction(0x04, (data) => FluidCoreAPI.Send(data))
);
FBus.Publish(evt);
// Un RE ne voit que des strings opaques et de la reflexion
```

---

### FluidCoreAPI - Interop Rust

**Fichier:** `FluidBus/Core/FluidCoreAPI.cs`
**Type:** Classe statique

Couche d'interop C# <-> Rust via P/Invoke (`DllImport("libfluid_core")`).

#### Declarations P/Invoke privees

| EntryPoint Rust | Signature C# |
|----------------|--------------|
| `process_bytes` | `IntPtr ProcessBytes(byte[] data, nuint len, out nuint outLen)` |
| `free_bytes` | `void FreeBytes(IntPtr ptr, nuint len)` |
| `init` | `ulong Init()` |
| `get_token` | `IntPtr GetToken(byte opcode, out nuint outLen)` |
| `rotate_token` | `IntPtr RotateToken(byte[] data, nuint len, out nuint outLen)` |
| `get_bytecode` | `IntPtr GetBytecode(byte opcode, byte[] typeName, nuint typeNameLen, byte[] methodName, nuint methodNameLen, byte[] argType, nuint argTypeLen, byte[] arg, nuint argLen, out nuint outLen)` |

#### Methodes publiques

| Methode | Signature | Description |
|---------|-----------|-------------|
| `Send` | `byte[] Send(byte[] data)` | Envoie des bytes a Rust via `ProcessBytes`, copie le resultat, libere la memoire Rust |
| `Free` | `void Free(IntPtr ptr, nuint outLen)` | Libere la memoire allouee cote Rust |
| `Initialize` | `ulong Initialize()` | Appelle `Init()` Rust, genere et stocke le seed |
| `RequestToken` | `byte[] RequestToken(byte opcode)` | Demande un token pour un opcode donne |
| `Rotate` | `byte[] Rotate(byte[] token)` | Rotate un token existant (hash du token courant + seed) |
| `GetBytecode` | `byte[] GetBytecode(byte opcode, string typeName, string methodName, string argType, byte[] arg)` | Genere du bytecode structure. Convertit les strings en UTF-8 bytes avant l'appel |
| `GetMethod` | `byte[] GetMethod(byte[] token)` | **STUB** - retourne `byte[0]` |
| `Execute` | `object? Execute(byte[] bytecode)` | **STUB** - retourne `null` |

**Pattern memoire :** Marshal -> copie vers managed array -> Free memoire Rust. Chaque appel FFI suit ce cycle.

#### Exemple : cycle complet d'un appel P/Invoke

```csharp
// === Send() — le pattern Marshal/Copy/Free ===
byte[] input = Encoding.UTF8.GetBytes("hello");
byte[] response = FluidCoreAPI.Send(input);
// Internement :
//   1. C# appelle ProcessBytes(input, 5, out outLen)
//   2. Rust alloue un Vec<u8>, retourne un IntPtr
//   3. C# copie les bytes : Marshal.Copy(ptr, managed, 0, outLen)
//   4. C# libere la memoire Rust : FreeBytes(ptr, outLen)
//   5. Retourne le managed array
string result = Encoding.UTF8.GetString(response);
// result = "Received OpCode, sending Token"
```

```csharp
// === Token lifecycle complet ===
// 1. Initialisation (fait automatiquement dans FBus static ctor)
ulong seed = FluidCoreAPI.Initialize();
// seed = timestamp_nanos XOR pid (ex: 0x1A2B3C4D5E6F7890)

// 2. Demander un token pour l'opcode 0x01
byte[] token = FluidCoreAPI.RequestToken(0x01);
// token = 8 bytes : hash(0x01, seed).to_le_bytes()
// ex: [0xA3, 0x7F, 0x2B, 0x91, 0xC4, 0x58, 0xE0, 0x1D]

// 3. Rotation
byte[] token2 = FluidCoreAPI.Rotate(token);
// token2 = 8 bytes : hash(token, seed).to_le_bytes()
// ex: [0x15, 0xD8, 0x4A, 0x63, 0xB7, 0x0C, 0x9E, 0xF2]

// 4. Encore
byte[] token3 = FluidCoreAPI.Rotate(token2);
// token3 = 8 bytes : hash(token2, seed).to_le_bytes()
// Chaque rotation produit un token completement different
```

```csharp
// === Generation de bytecode ===
byte[] bytecode = FluidCoreAPI.GetBytecode(
    0x04,                                    // opcode
    "Console",                               // type name
    "WriteLine",                             // method name
    "String",                                // arg type
    Encoding.UTF8.GetBytes("Hello World")    // arg data
);
// bytecode = ~50 bytes contenant le format structure
// (voir section "Format du bytecode" pour le detail octet par octet)
```

---

### FluidVM - Machine virtuelle

**Fichier:** `FluidBus/Core/VM/FluidVM.cs`

**Etat:** Stub/work-in-progress

```csharp
object? Run(byte[] token)
{
    var bytecode = FluidCoreAPI.GetMethod(token);  // retourne byte[0]
    return FluidCoreAPI.Execute();                  // retourne null
}
```

Sera la VM pour executer le bytecode genere par Rust.

#### Cible d'implementation de la MiniVM

```csharp
// Ce que FluidVM.Run() devra faire une fois implemente :
object? Run(byte[] token)
{
    // 1. Envoyer le token a Rust pour obtenir le bytecode decode
    byte[] decoded = FluidCoreAPI.GetMethod(token);
    // decoded contiendra: type="Console", method="WriteLine", args=["Hello"]

    // 2. Executer via reflexion
    // Pseudo-code cible :
    Type type = Type.GetType(decoded.TypeName);           // typeof(Console)
    MethodInfo method = type.GetMethod(decoded.MethodName); // Console.WriteLine
    return method.Invoke(null, decoded.Args);               // Console.WriteLine("Hello")
}
```

---

### Systeme d'exceptions

**Fichier base:** `FluidBus/Errors/FluidBusError.cs`

```
FluidBusError (abstract, herite de Exception)
├── BluePrintException    - Erreurs de factory/reflexion
│   Format: "[BluePrintException]: {msg}"
│   Cas: constructeur manquant, erreur d'invocation
│
├── DispatchException     - Erreurs de dispatch d'evenements
│   Cas: echec dispatch async
│
└── HandlerLinqException  - Erreurs de registre de handlers
    Format: "[HandlerLinqException]: {msg}"
    Cas: echec d'enregistrement
```

Toutes ont une methode `DisplayMessage()` qui ecrit sur `Console.WriteLine()`.

---

## Rust - fluid_core

**Target:** cdylib (shared library: .so Linux, .dll Windows, .dylib macOS)
**Edition:** 2024
**Dependencies externes:** Aucune (standard library uniquement)
**Version:** 0.1.0

### FFI Exports (lib.rs)

**Fichier:** `fluid_core/src/lib.rs`

**Etat global:**
```rust
static SEED: AtomicU64 = AtomicU64::new(0);
```
- Seed global thread-safe (AtomicU64, ordering SeqCst)
- Initialise par `init()`, utilise par toutes les autres fonctions

#### Fonctions exportees

##### `init() -> u64`
- Genere un seed via `generate_seed()`
- Stocke dans `SEED` atomique
- Retourne la valeur du seed

##### `process_bytes(data, len, out_len) -> *mut u8`
- Recoit des bytes depuis C#
- Affiche l'input en stdout
- Retourne le message : `"Received OpCode, sending Token"`
- Le caller doit liberer avec `free_bytes`

##### `get_token(opcode, out_len) -> *mut u8`
- Charge le seed courant
- Appelle `generate_token(opcode, seed)`
- Retourne 8 bytes (token) via pointeur
- Utilise `mem::forget` pour eviter la deallocation

##### `rotate_token(data, len, out_len) -> *mut u8`
- Charge le seed courant
- Appelle `next_token(current_token, seed)`
- Retourne le nouveau token (8 bytes)

##### `get_bytecode(opcode, type_name, method_name, arg_type, arg, out_len) -> *mut u8`
- Convertit les C strings en `&str` (UTF-8)
- Appelle `generate_bytecode(seed, opcode, type_name, method_name, arg_type, arg)`
- Retourne le bytecode genere

##### `free_bytes(ptr, len)`
- Reconstruit un `Vec<u8>` depuis le pointeur brut
- Le drop implicite libere la memoire

**Pattern memoire :** `Vec::into_raw_parts()` + `mem::forget()` pour transferer l'ownership au caller C#. Le caller doit appeler `free_bytes()` pour liberer.

#### Exemple : pattern memoire FFI detaille

```
=== get_token(0x01, &out_len) ===

Cote Rust :
  1. let token: Vec<u8> = generate_token(0x01, seed);
     // token = [0x1D, 0xE0, 0x58, 0xC4, 0x91, 0x2B, 0x7F, 0xA3]
     // token.len() = 8

  2. let ptr = token.as_mut_ptr();     // pointeur vers le buffer
     let len = token.len();            // 8
     *out_len = len;                   // ecrit 8 dans out_len pour le caller
     std::mem::forget(token);          // empeche Rust de drop le Vec
     return ptr;                       // retourne le pointeur brut

Cote C# (FluidCoreAPI.RequestToken) :
  3. IntPtr ptr = GetToken(0x01, out nuint outLen);
     // ptr = adresse memoire Rust, outLen = 8

  4. byte[] managed = new byte[outLen];
     Marshal.Copy(ptr, managed, 0, (int)outLen);
     // Copie les 8 bytes dans un tableau managed C#

  5. FreeBytes(ptr, outLen);
     // Rust reconstruit le Vec et le drop → memoire liberee

  6. return managed;
     // Le tableau C# vit dans le GC, independant de Rust
```

---

### Seed (seed.rs)

**Fichier:** `fluid_core/src/core/seed.rs`

```rust
pub fn generate_seed() -> u64
```

**Algorithme :**
1. Recupere le temps systeme en nanosecondes depuis UNIX_EPOCH
2. Recupere le PID du process
3. XOR les deux valeurs
4. Retourne le resultat comme u64

**Sources d'entropie :** temps haute resolution + PID.

#### Exemple de generation

```
Supposons :
  timestamp_nanos = 1710000000000000000  (0x17C3A7A5E3B58000)
  pid             = 12345               (0x0000000000003039)

  seed = 0x17C3A7A5E3B58000 XOR 0x0000000000003039
       = 0x17C3A7A5E3B5B039

Chaque run produit un seed different (le timestamp change).
Chaque process produit un seed different (le PID change).
```

---

### Token (token.rs)

**Fichier:** `fluid_core/src/core/token.rs`

#### `generate_token(opcode: u8, seed: u64) -> Vec<u8>`

**Algorithme :**
1. Cree un `DefaultHasher`
2. Hash l'opcode (u8)
3. Hash le seed (u64)
4. Finit le hash -> u64
5. Convertit en 8 bytes little-endian
6. Retourne `Vec<u8>` (8 bytes)

**Propriete :** Deterministe. Meme opcode + meme seed = meme token.

#### `next_token(current_token: &[u8], seed: u64) -> Vec<u8>`

**Algorithme :**
1. Cree un `DefaultHasher`
2. Hash le token courant (tous les bytes)
3. Hash le seed (u64)
4. Finit le hash -> u64
5. Convertit en 8 bytes little-endian
6. Retourne `Vec<u8>` (8 bytes)

**Propriete :** Chaine deterministe. Token N+1 depend de Token N + seed. Forme une sequence de rotation.

#### Exemple : chaine de rotation complete

```
Seed = 0x17C3A7A5E3B5B039

generate_token(opcode=0x01, seed):
  hasher = DefaultHasher::new()
  hasher.write_u8(0x01)
  hasher.write_u64(0x17C3A7A5E3B5B039)
  hash = hasher.finish()  → ex: 0xA37F2B91C458E01D
  token_v1 = hash.to_le_bytes() → [0x1D, 0xE0, 0x58, 0xC4, 0x91, 0x2B, 0x7F, 0xA3]

next_token(token_v1, seed):
  hasher = DefaultHasher::new()
  hasher.write(&[0x1D, 0xE0, 0x58, 0xC4, 0x91, 0x2B, 0x7F, 0xA3])
  hasher.write_u64(0x17C3A7A5E3B5B039)
  hash = hasher.finish()  → ex: 0x15D84A63B70C9EF2
  token_v2 = [0xF2, 0x9E, 0x0C, 0xB7, 0x63, 0x4A, 0xD8, 0x15]

next_token(token_v2, seed):
  token_v3 = [0x44, 0x71, 0xDB, 0x09, 0xA5, 0x3F, 0x6C, 0x82]

Chaine : token_v1 → token_v2 → token_v3 → ...
Connaitre token_v3 ne permet PAS de retrouver token_v2 (forward secrecy).
Le meme opcode + le meme seed produira TOUJOURS la meme chaine.
```

---

### Bytecode (bytecode.rs)

**Fichier:** `fluid_core/src/core/bytecode.rs`

#### Fonctions de signature privees

```rust
fn make_sig(seed: u64, section_id: u8) -> [u8; 2]
```
Hash(seed + section_id) -> prend les 2 premiers bytes du hash en little-endian.

| Fonction | Section ID | Usage |
|----------|-----------|-------|
| `sig_header(seed)` | `0x01` | Signature du header |
| `sig_opcode(seed)` | `0x02` | Signature de la section opcode |
| `sig_type(seed)` | `0x03` | Signature de la section type |
| `sig_method(seed)` | `0x04` | Signature de la section methode |
| `sig_args(seed)` | `0x05` | Signature de la section arguments |

#### `generate_bytecode(seed, opcode, type_name, method_name, arg_type, arg) -> Vec<u8>`

Genere un bytecode structure auto-descriptif.

#### Exemple : hex dump d'un bytecode reel

Pour l'appel `Console.WriteLine("Fluid.Guard")` avec opcode `0x04` :

```
generate_bytecode(seed, 0x04, "Console", "WriteLine", "String", b"Fluid.Guard")
```

**Sortie (hex dump annote) :**
```
Offset  Hex                                      ASCII         Section
------  ---                                      -----         -------
0x00    [XX XX]                                                sig_header (2 bytes, hash(seed+0x01))
0x02    [04]                                                   nb_sections = 4

0x03    [YY YY]                                                sig_opcode (2 bytes, hash(seed+0x02))
0x05    [01]                                                   len = 1
0x06    [04]                                     .             opcode = 0x04

0x07    [ZZ ZZ]                                                sig_type (2 bytes, hash(seed+0x03))
0x09    [07]                                                   len = 7
0x0A    [43 6F 6E 73 6F 6C 65]                   Console       type_name

0x11    [WW WW]                                                sig_method (2 bytes, hash(seed+0x04))
0x13    [09]                                                   len = 9
0x14    [57 72 69 74 65 4C 69 6E 65]             WriteLine     method_name

0x1D    [VV VV]                                                sig_args (2 bytes, hash(seed+0x05))
0x1F    [06]                                                   arg_type_len = 6
0x20    [53 74 72 69 6E 67]                       String        arg_type
0x26    [0B]                                                   arg_len = 11
0x27    [46 6C 75 69 64 2E 47 75 61 72 64]       Fluid.Guard   arg_data

Total : 0x32 = 50 bytes
```

**Notes :**
- `XX XX`, `YY YY`, etc. = signatures qui changent a chaque run (dependantes du seed)
- Les champs texte sont du pur UTF-8
- Un meme appel packag deux fois avec des seeds differentes produira des bytecodes differents (signatures differentes)
- Taille totale : `3 (header) + 4 (opcode) + 10 (type) + 12 (method) + 21 (args) = 50 bytes`

---

## Programme de test

**Fichier:** `LoggerTestBusCSharpPython/Program.cs`
**Projet:** .NET 10.0 executable, reference `FluidBus.csproj`

### Ce que fait le test

1. **Enregistrement handler :**
   ```csharp
   FBus.Register(new CoreHandler("core"));
   ```

2. **Creation d'une RustInstruction :**
   ```csharp
   var rustInstruction = new RustInstruction(0x01, (data) => {
       return FluidCoreAPI.Send(data);
   });
   ```
   - Opcode `0x01`
   - La func envoie le token a Rust et recoit la reponse

3. **Callback sur resultat :**
   ```csharp
   rustInstruction.OnResult += (result) => {
       byte[] bytes = (byte[])result;
       Console.WriteLine($"C# Received: {Encoding.UTF8.GetString(bytes)}");
   };
   ```

4. **Publication d'evenement :**
   ```csharp
   FBus.Publish(new CoreEvent("test", BusProtocol.System, rustInstruction));
   ```

5. **Generation de bytecode :**
   ```csharp
   var bytecode = FluidCoreAPI.GetBytecode(0x04, "Console", "WriteLine", "Int",
       Encoding.UTF8.GetBytes("Fluid.Guard"));
   ```
   Affiche les bytes et la longueur du bytecode genere.

### Methode Test() commentee

Demontre la rotation de token :
```csharp
var token = FluidCoreAPI.RequestToken(0x01);
// Affiche token initial
token = FluidCoreAPI.Rotate(token);
// Affiche token apres 1ere rotation
token = FluidCoreAPI.Rotate(token);
// Affiche token apres 2eme rotation
```

---

## Flux d'execution complet

Voici le flux complet quand on appelle `FBus.Publish(new CoreEvent(..., rustInstruction))` :

```
1. FBus.Publish(coreEvent)
   |
   2. Itere sur _ports, trouve le port avec Protocol.Name == "SYSTEM"
   |
   3. HandlerLinq.TryGetHandlers(coreEvent)
   |   -> Cherche handlers pour typeof(CoreEvent)
   |   -> Trouve CoreHandler("core")
   |
   4. Verifie si le handler est disponible (bool == false)
   |   Si aucun dispo -> BluePrintFactory.NewHandler() cree un clone
   |
   5. handler.CallCount++
   |
   6. port.Dispatch(coreEvent, coreHandler)
   |   |
   |   7. Strategy == Sync, donc appel direct:
   |      coreEvent.Dispatch(coreHandler)
   |      |
   |      8. Cree BusLogEvent("bus_log_dispatch", "Event Dispatched: [EVT::{CoreEvent}::test]")
   |      |   -> FBus.Publish(busLogEvent)
   |      |      -> Meme cycle, mais pour BusLogEvent
   |      |      -> BusLogHandler.Handle() execute les LogInstructions
   |      |
   |      9. coreHandler.Handle(coreEvent)
   |      |   |
   |      |   10. Pour chaque instruction dans coreEvent.Instructions:
   |      |       |
   |      |       11. rustInstruction.Execute()
   |      |       |   -> base.Execute() : invoque FluidMethod<byte[]> avec _token
   |      |       |   -> FluidCoreAPI.Rotate(_token) : rotation du token
   |      |       |
   |      |       12. rustInstruction.ExecuteAndGet()
   |      |           -> base.ExecuteAndGet() : invoque FluidFunc<byte[], object>
   |      |           |   -> FluidCoreAPI.Send(_token) dans la func
   |      |           |   -> Rust recoit les bytes, repond "Received OpCode, sending Token"
   |      |           |   -> OnResult event fire avec le resultat
   |      |           -> FluidCoreAPI.Rotate(_token) : rotation du token
   |      |
   |      13. Cree BusLogEvent("bus_log_dispatch", "Handler Triggered: [HDL::...]")
   |          -> FBus.Publish(busLogEvent)
   |
   14. Retourne true
```

---

## Format du bytecode

Le bytecode genere par `generate_bytecode()` en Rust a cette structure :

```
Offset  Taille   Contenu                  Description
------  ------   -------                  -----------
0x00    2 bytes  sig_header(seed)         Signature du header (hash seed+0x01)
0x02    1 byte   0x04                     Nombre de sections (fixe a 4)

--- Section Opcode ---
0x03    2 bytes  sig_opcode(seed)         Signature section opcode (hash seed+0x02)
0x05    1 byte   0x01                     Longueur de la donnee (toujours 1)
0x06    1 byte   opcode                   L'opcode (ex: 0x04)

--- Section Type ---
0x07    2 bytes  sig_type(seed)           Signature section type (hash seed+0x03)
0x09    1 byte   type_name.len() as u8    Longueur du nom de type
0x0A    N bytes  type_name.as_bytes()     Nom du type en UTF-8 (ex: "Console")

--- Section Method ---
0x0A+N  2 bytes  sig_method(seed)         Signature section methode (hash seed+0x04)
+2      1 byte   method_name.len() as u8  Longueur du nom de methode
+3      M bytes  method_name.as_bytes()   Nom de la methode en UTF-8 (ex: "WriteLine")

--- Section Args ---
+3+M    2 bytes  sig_args(seed)           Signature section args (hash seed+0x05)
+2      1 byte   arg_type.len() as u8     Longueur du type d'argument
+3      K bytes  arg_type.as_bytes()      Type d'argument en UTF-8 (ex: "Int")
+3+K    1 byte   arg.len() as u8          Longueur des donnees d'argument
+4+K    L bytes  arg                      Donnees brutes de l'argument
```

**Proprietes :**
- Les signatures dependent du seed -> change a chaque initialisation de la lib
- Format auto-descriptif (longueurs prefixees)
- Supporte n'importe quel type/methode/argument
- Les longueurs sont sur 1 byte -> max 255 bytes par champ

---

## Securite et proprietes cryptographiques

### Seed

- Generee via `timestamp_nanos XOR PID` — unique par run
- Stockee dans `AtomicU64` global — thread-safe (SeqCst)
- **A terme :** sera droppee apres init (la seed ne persiste pas en memoire)
- **Future : systeme dual-seed**
  - `seed_build` : utilisee au packing par dnlib pour generer les IDs d'events et les signatures bytecode
  - `seed_runtime` : generee a chaque execution pour les tokens
  - `seed_build` droppee apres init du runtime

### Forward Secrecy des tokens

La chaine de tokens a une propriete de **forward secrecy** :
```
token_v1 = hash(opcode + seed)
token_v2 = hash(token_v1 + seed)
token_v3 = hash(token_v2 + seed)
...
```

- Un token compromis **ne compromet pas les tokens precedents**
- Chaque token est le hash du precedent + seed
- Impossible de remonter la chaine sans connaitre le seed
- Le C# ne manipule jamais les bytes directement — tout passe par P/Invoke

#### Exemple de forward secrecy

```
Scenario : un attaquant dumpe la memoire et capture token_v3

  token_v1 = hash(opcode + seed)          ← inconnu
  token_v2 = hash(token_v1 + seed)        ← inconnu
  token_v3 = hash(token_v2 + seed)        ← capture !
  token_v4 = hash(token_v3 + seed)        ← calculable SI seed connu

L'attaquant a token_v3 mais :
  - Ne peut PAS remonter a token_v2 (hash one-way)
  - Ne peut PAS remonter a token_v1
  - Ne peut PAS calculer token_v4 sans le seed
  - Le seed est dans un AtomicU64 cote Rust (pas dans le heap C#)

Meme avec un debugger C#, les tokens ne sont que des byte[] opaques
qui transitent par P/Invoke — jamais construits ni decodes cote managed.
```

### Signatures bytecode

**Etat actuel :** Chaque section est prefixee par 2 bytes = `hash(seed + section_id)[0..2]`. Simple mais suffisant pour le PoC.

**Design cible :** Les signatures passeront a un systeme de **double validation** (voir "Systeme de validation des signatures") avec generation explicite de `[a, b]` satisfaisant deux conditions independantes, et des chunks junk avec de fausses signatures pour noyer les vraies sections.

### XOR bytecode/token (design cible)

Le bytecode n'est **jamais en clair en memoire**. Il est toujours chiffre avec le token courant :

```
Stockage:
  bytecode_chiffre = bytecode_clair XOR token_courant

Utilisation:
  bytecode_clair = bytecode_chiffre XOR token_courant
  → parse → execute → token rotate → bytecode re-chiffre avec nouveau token

Cycle :
  token_v1 : bytecode XOR token_v1 → stocke
  appel    : bytecode XOR token_v1 → clair → execute
  rotation : token_v1 → token_v2
  re-stock : bytecode XOR token_v2 → stocke
  ...
```

Le meme bytecode est chiffre differemment a chaque rotation. Un dump memoire a l'instant T donne un bytecode chiffre avec un token qui n'existe plus.

### Ordre aleatoire des sections (design cible)

L'ordre actuel est fixe (`opcode → type → method → args`), mais le design cible **randomise l'ordre des sections** a chaque generation :

```
Build 1 : [header][method][args][opcode][type]
Build 2 : [header][type][opcode][args][method]
Build 3 : [header][args][type][method][opcode]
```

Le parser utilise les signatures (2 bytes) pour identifier chaque section, pas sa position. Cela rend l'analyse de pattern impossible.

### Chunks junk (design cible)

Des chunks de donnees aleatoires sont inseres **entre les vraies sections** a des positions aleatoires :

```
[header][JUNK_1][opcode][JUNK_2][JUNK_3][type][method][JUNK_4][args]
```

**Les chunks junk ont le meme format que les vrais chunks** : `[sig_2bytes][len_1byte][data_random]`. La seule difference est que leur signature `[a, b]` est generee pour echouer la double validation (voir section "Systeme de validation des signatures"). Visuellement, un junk est indistinguable d'une vraie section sans connaitre le seed.

### Systeme de validation des signatures (design cible)

Pour distinguer un chunk valide d'un chunk junk, le parser utilise une **double condition independante** sur les 2 bytes de la signature `[a, b]` :

```
Condition 1 (XOR) :  a XOR b == (seed_low_byte + section_id) % 256
Condition 2 (somme): (a + b) % 256 == (seed_high_byte ^ section_id)

est_valide = condition_1 ET condition_2
```

Les deux conditions sont independantes → probabilite de faux positif = `(1/256) * (1/256)` = **1/65536**.

Avec un bytecode contenant 4 vraies sections + 10 chunks junk, la probabilite qu'un junk passe les deux tests est negligeable.

#### Generation des signatures valides (au build)

Au moment de la generation du bytecode, on doit produire un `[a, b]` qui satisfait les deux conditions **simultanement** :

```
Pour section_id et seed donnes :
  target_xor  = (seed_low_byte + section_id) % 256
  target_sum  = (seed_high_byte ^ section_id) % 256

  Trouver a, b tels que :
    a XOR b == target_xor
    (a + b) % 256 == target_sum

  Resolution :
    a XOR b = target_xor  →  b = a XOR target_xor
    (a + (a XOR target_xor)) % 256 = target_sum

    → Iterer a de 0 a 255, calculer b = a XOR target_xor
    → Verifier si (a + b) % 256 == target_sum
    → Premier match → sig = [a, b]
```

Il existe toujours au moins une solution (systeme a 2 equations, 256 candidats pour `a`).

#### Generation des signatures junk (au build)

Les chunks junk ont aussi des signatures `[a, b]`, mais generees pour **echouer volontairement** au moins une des deux conditions :

```
Pour generer un faux [a, b] :
  1. Generer a aleatoire
  2. Generer b aleatoire
  3. Verifier que la paire NE satisfait PAS les deux conditions
     → Si par malchance elle les satisfait (1/65536), regenerer
  4. Stocker [a, b] + donnees aleatoires comme chunk junk

Le parser voit :
  [A3 7F] → teste conditions → ECHOUE → junk, skip
  [1D E0] → teste conditions → ECHOUE → junk, skip
  [sig valide] → teste conditions → OK → section opcode !
  [B2 44] → teste conditions → ECHOUE → junk, skip
  [sig valide] → teste conditions → OK → section type !
  ...
```

#### Exemple complet de parsing

```
Bytecode recu (apres dechiffrement XOR token) :
  [header][JUNK][method][JUNK][JUNK][args][opcode][JUNK][type]

Parser :
  1. Lire header (position fixe 0) → nb_sections = 4
  2. Position = 3 (apres header)
  3. Boucle :
     a. Lire sig = [bytes[pos], bytes[pos+1]]
     b. Pour chaque section_id possible (0x02..0x05) :
        - Calculer target_xor et target_sum
        - Tester les deux conditions
        - Si match → section trouvee, lire len + data, passer a la suite
     c. Si aucun section_id ne matche → chunk junk
        - Lire len + skip les data
     d. Repeter jusqu'a avoir trouve les 4 sections

  Resultat : opcode, type, method, args extraits dans n'importe quel ordre
```

### HashMap de stockage (design cible)

Cote Rust, les bytecodes et tokens sont stockes dans une `HashMap<u8, Vec<u8>>` :

```rust
// Stockage interne cible
static BYTECODES: Mutex<HashMap<u8, Vec<u8>>> = ...;  // opcode → bytecode chiffre
static TOKENS: Mutex<HashMap<u8, Vec<u8>>> = ...;     // opcode → token courant

// Flow d'init
init():
  seed_runtime = generate_seed()
  pour chaque opcode dans la table:
    token = generate_token(opcode, seed_runtime)
    TOKENS[opcode] = token
    bytecode = dechiffrer(BYTECODES_CHIFFRES[opcode], seed_build)
    BYTECODES[opcode] = bytecode XOR token  // re-chiffre avec token runtime
  drop(seed_build)  // overwrite + forget
```

### Flow d'init revise (design cible)

```
1. Chargement lib Rust
   → METHODS[] et ARGS[] sont en memoire, chiffres avec seed_build

2. init() appele par C#
   → Genere seed_runtime (rdtsc + PID + ASLR a terme)
   → Pour chaque opcode :
     a. Dechiffre le nom de methode : method = METHODS[opcode] XOR seed_build
     b. Genere token initial : token = hash(opcode, seed_runtime)
     c. Genere bytecode clair : bytecode = generate_bytecode(...)
     d. Chiffre avec token : BYTECODES[opcode] = bytecode XOR token
     e. Stocke dans HashMap
   → Drop seed_build (overwrite avec garbage, puis forget)
   → seed_build n'existe plus en memoire

3. Runtime
   → Chaque appel : dechiffre bytecode avec token courant
   → Execute
   → Rotate token
   → Re-chiffre bytecode avec nouveau token
```

---

## Concept de packing dnlib

### Le probleme central

dnlib doit transformer du code IL classique en code event-driven. C'est la partie la plus complexe du packer.

### Transformation des appels de methode

**Code original :**
```csharp
var result = SomeMethod(arg);
Console.WriteLine(result);
```

**Code transforme :**
```csharp
// SomeMethod(arg) devient un event avec RustInstruction
FBus.Publish(new CoreEvent(
    hash("SomeMethod" + seed_build),  // ID opaque
    BusProtocol.System,
    new RustInstruction(opcode, (data) => FluidCoreAPI.Send(data))
));
```

### Transformation des usages de resultat

C'est le point le plus complexe. Chaque usage d'un resultat de fonction devient un callback `OnResult` qui dispatch un nouvel event.

**Code original :**
```csharp
var result = token;
Console.WriteLine(result);
```

**Code transforme :**
```csharp
instr.OnResult += (result) => {
    FBus.Publish(BluePrintFactory.NewEvent(
        typeof(CoreEvent),
        "on_result_xyz",           // hash derive de seed_build
        BusProtocol.System,
        new RustInstruction(0x02, (data) => FluidCoreAPI.Send(data))
    ).evt);
};
```

### Chainages d'events

Les events sont chaines — un event peut en declencher d'autres via `OnResult` :
```
Event A (appel methode)
  └─ OnResult → Event B (usage du resultat)
       └─ OnResult → Event C (prochain appel)
            └─ ...
```

Cela transforme un control flow lineaire en un graphe d'evenements asynchrones, extremement difficile a suivre en analyse statique.

### Generation des IDs d'events au packing

```
ID event = hash(nom_fonction + seed_build)
```

- **Uniques** — pas de collision entre events
- **Opaques** — un RE voit `"a3f7b2c1"` sans aucun lien avec `"Console.WriteLine"`
- **Derives de seed_build** — changent a chaque repackaging
- **Non reversibles** — hash one-way

### Integration cible avec BluePrintFactory

BluePrintFactory sera utilise dans le flow de packing pour :
1. Creer dynamiquement les events au runtime via `NewEvent()`
2. Creer dynamiquement les handlers via `NewHandler()`
3. Tout est instancie par reflexion → pas de references directes dans l'IL

---

## Diagrammes d'architecture

### Hierarchie des interfaces et classes

```
IFluidEvent
└── FluidEvent (abstract)
    ├── BusLogEvent
    └── CoreEvent

IFluidHandler
└── FluidHandler<T> (abstract)
    ├── BusLogHandler       (T = BusLogEvent)
    └── CoreHandler          (T = CoreEvent)

IFluidInstruction
└── FluidInstruction<T> (abstract)
    ├── LogInstruction       (T = string)
    └── RustInstruction      (T = byte[])

Exception
└── FluidBusError (abstract)
    ├── BluePrintException
    ├── DispatchException
    └── HandlerLinqException

BusProtocol (abstract)
└── SystemProtocol          (Sync, Name="SYSTEM")
```

### Relations entre composants

```
FBus (statique)
├── possede HashSet<BusPort>
├── utilise HandlerLinq (statique) pour le registre
├── utilise BluePrintFactory (statique) pour la creation dynamique
└── initialise FluidCoreAPI (statique) au demarrage

BusPort
├── contient un BusProtocol
└── dispatche via FluidTask (async) ou direct (sync)

FluidEvent
├── contient HashSet<IFluidInstruction>
├── dispatch via handler.Handle()
└── publie des BusLogEvent (auto-logging)

RustInstruction
├── demande un token a FluidCoreAPI au constructeur
└── rotate le token a chaque Execute/ExecuteAndGet

FluidCoreAPI
└── P/Invoke vers libfluid_core (Rust)
    ├── init -> seed.rs
    ├── get_token -> token.rs
    ├── rotate_token -> token.rs
    ├── get_bytecode -> bytecode.rs
    ├── process_bytes -> lib.rs (echo)
    └── free_bytes -> lib.rs (dealloc)
```

---

## Build et configuration

### Pre-requis

- .NET 10.0 SDK
- Rust toolchain (edition 2024)

### Build Rust

```bash
cd fluid_core
cargo build --release
# Produit: target/release/libfluid_core.so (Linux)
#          target/release/fluid_core.dll (Windows)
#          target/release/libfluid_core.dylib (macOS)
```

### Build C#

```bash
# Depuis la racine du projet
dotnet build LoggerTestBusCSharpPython.slnx
```

**Important :** La lib Rust (libfluid_core.so/dll) doit etre accessible par le runtime .NET (dans le PATH, le repertoire de l'executable, ou configure dans runtimeconfig).

### Fichier solution

`LoggerTestBusCSharpPython.slnx` contient :
- `FluidBus/FluidBus.csproj` (librairie)
- `LoggerTestBusCSharpPython/LoggerTestBusCSharpPython.csproj` (executable, reference FluidBus)

---

## Design cible complet

### Ce que c'est

Un packer commercial anti-reverse engineering ciblant les applications .NET/C#. Pas un obfuscateur qui cache le code — un systeme qui le **deplace** entierement dans un core Rust natif, rendant le binaire C# une facade vide sans logique.

### Experience utilisateur finale

```
Dev drop son .exe sur la plateforme web
→ Fluid.Guard analyse et packe automatiquement
→ Dev telecharge le binaire protege + lib Rust
→ Zero integration manuelle dans son code
```

### Architecture runtime cible

```
Binaire packe
├── C# — Facade aveugle
│   ├── FBus — orchestre les events
│   ├── BluePrintFactory — cree les events dynamiquement
│   ├── RustInstruction — transporte les tokens
│   ├── MiniVM — execute via Reflection sans comprendre
│   └── FluidCoreAPI — P/Invoke vers Rust (futur: NativeLibrary)
│
└── Rust — Cerveau natif
    ├── seed_runtime — unique par run (rdtsc + PID + ASLR)
    ├── Hash chain tokens — forward secrecy, ephemeres
    ├── Bytecode byte[4][n] — signatures seed-dependantes
    ├── Parser bytecode — valide + extrait methode + args
    ├── Table methodes — strings chiffrees avec seed_build
    └── VM native — execute la logique originale
```

### Pipeline de packing detaille

```
Etape 1 — Analyse (dnlib)
→ Lire IL original
→ Extraire toutes les methodes, signatures, args
→ Identifier le control flow

Etape 2 — Generation Rust
→ Creer table METHODS[] + ARGS[] separes et chiffres
→ seed_build chiffre les strings
→ Compiler Rust avec la table generee

Etape 3 — Reecriture IL (dnlib)
→ Supprimer tout le corps des methodes
→ Chaque appel → FBus.Publish(CoreEvent(hash(nom+seed_build)))
→ Chaque usage de resultat → OnResult → nouvel event chaine
→ Injecter MiniVM + FluidCoreAPI + handlers

Etape 4 — Packer natif Rust
→ Chiffrer + compresser libfluid_core
→ Dechiffrement en memoire au runtime uniquement
→ Jamais ecrit sur disque en clair
```

### Flow d'execution runtime cible

```
Init
→ FBus static ctor
→ FluidCoreAPI.Initialize()
→ Rust genere seed_runtime
→ seed_build droppee + overwrite garbage

Appel de methode (ex: Console.WriteLine("Hello"))
→ FBus.Publish(CoreEvent("a3f7b2c1", RustInstruction(0x04)))
→ RustInstruction demande token_v1 a Rust
→ CoreHandler.Handle() → ExecuteAndGet()
→ FluidCoreAPI.Send(token_v1 + bytecode)
→ Rust valide token → parse bytecode → extrait methode + args
→ Retourne "Console", "WriteLine", "Hello" a MiniVM
→ MiniVM : MethodInfo.Invoke(Console, "WriteLine", ["Hello"])
→ OnResult → dispatch event suivant
→ token_v1 rotate → token_v2
```

### Couches de protection

```
Couche 1 — Bytecode ephemere
  Signatures seed-dependantes + byte[n] + chunks junk
  → Illisible sans seed_runtime
  → XOR avec token courant → jamais en clair en memoire

Couche 2 — Hash chain tokens
  Forward secrecy + auto-invalidation
  → Token capture = inutilisable au prochain run

Couche 3 — JumpEvents (tick system)
  Events factices pseudo-reguliers
  → RE ne distingue pas vrais events des faux

Couche 4 — TTRL (Time To Run Limit)
  Mesure duree de vie des events
  → Breakpoint detecte → flag

Couche 5 — AntiDebugEvent
  Reponse graduee aux flags :
  1. Loop main
  2. Inverser handlers
  3. Supprimer lib Rust
  4. Stopper dispatch

Couche 6 — AntiTamper
  Checksum par event
  → Tamper detecte → crash immediat

Couche 7 — Packer natif Rust
  Lib Rust chiffree sur disque
  → Analyse statique impossible
```

### Ce que voit un RE

```
dnSpy sur le C#
→ FBus.Publish(CoreEvent("a3f7b2c1"...))
→ MethodInfo.Invoke(???)
→ Zero logique visible

Ghidra sur la lib Rust
→ Binaire chiffre
→ Zero surface d'attaque statique

Memory dump au runtime
→ Tokens ephemeres deja rotes
→ Bytecode XOR avec token deja invalide
→ Etat transitoire inutilisable

Debugger
→ TTRL detecte la pause
→ AntiDebugEvent change le comportement
```

### Modele commercial

```
Indie/SME  → 9€/mois, 100€/an, 20€ one-time
Enterprise → 200€/mois, 2000€/an, 5000€ one-time

Plateforme web :
  → Panel utilisateur
  → Historique versions protegees
  → Launch counts + crack attempt flags
  → Crack success % estime
  → Signature legale unique par produit
```

---

## Roadmap et etat actuel

### Implemente et fonctionnel

| Composant | Description | Statut |
|-----------|-------------|--------|
| FBus | Bus d'evenements complet (Publish/Subscribe) | OK |
| HandlerLinq | Registre de handlers avec lookup par type | OK |
| BusProtocol | Protocoles Sync/Async (seul Sync implemente) | OK |
| BluePrintFactory | Factory dynamique par reflexion | OK |
| FluidCoreAPI | Interop C# <-> Rust (P/Invoke complet) | OK |
| Seed | Generation via time XOR PID, stockage AtomicU64 | OK |
| Token | Generation initiale + rotation (hash chain) | OK |
| Forward secrecy | Token N+1 ne compromet pas Token N | OK |
| Bytecode | Generation structuree avec signatures seed-dependantes | OK |
| RustInstruction | Token request au constructeur + rotation auto a chaque appel | OK |
| Instructions | Systeme avec FluidMethod/FluidFunc + OnResult callbacks | OK |
| Logging interne | BusLogEvent auto-generes a chaque dispatch | OK |
| Memory management | Marshal/Free pattern pour interop Rust | OK |

### Stubs (code present mais non fonctionnel)

| Composant | Fichier | Etat actuel |
|-----------|---------|-------------|
| `FluidCoreAPI.GetMethod()` | `FluidBus/Core/FluidCoreAPI.cs` | Retourne `byte[0]` |
| `FluidCoreAPI.Execute()` | `FluidBus/Core/FluidCoreAPI.cs` | Retourne `null` |
| `FluidVM.Run()` | `FluidBus/Core/VM/FluidVM.cs` | Appelle les 2 stubs ci-dessus |

### Roadmap

```
PoC (court terme) ← ON EST LA
  → Parser bytecode Rust
  → MiniVM C# fonctionnelle
  → Dual seeds (build + runtime)
  → Chunks junk + ordre aleatoire sections
  → XOR bytecode/token
  → HashMap<u8, Vec<u8>> stockage
  → Validation signatures (XOR + somme)
  → Demo : Console.WriteLine via MiniVM

MVP (moyen terme)
  → JumpEvents + tick system
  → TTRL
  → AntiDebugEvent
  → NativeLibrary (remplace DllImport)
  → dnlib packer minimal
  → Jeu de demo protege

Produit (long terme)
  → Packer natif Rust (lib chiffree sur disque)
  → Plateforme web
  → Telemetry
  → Support Unity/Godot
  → Concurrence AAA
```

---
