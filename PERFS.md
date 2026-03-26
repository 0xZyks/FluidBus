# FluidBus — Analyse de Performance

---

## P0 — Critiques (bugs / crashes potentiels)

### 1. Aucune thread-safety sur les dictionnaires statiques

**`HandlerLinq.cs:10`** et **`FBus.cs:15`**

Les `Dictionary` et `HashSet` statiques sont partagés entre threads sans aucune synchronisation. Un `FBus.Publish` concurrent corrompra les structures internes.

**Fix :** `ConcurrentDictionary` ou `ReaderWriterLockSlim`.

---

### 2. `NewHandler` via reflection sans arguments sur un constructeur paramétré

**`BluePrintFactory.cs:21`**

`Activator.CreateInstance(typeof(BusLogHandler))` sans passer d'args, mais `BusLogHandler` exige un `string id`. → Echoue **à chaque fois**, capture un stack trace d'exception pour rien.

---

## P1 — Gros impacts perf

### 4. Amplification 3x du publish par le logging

**`FluidEvent.cs:29-36`**

Chaque `Dispatch` non-log déclenche **2 `FBus.Publish` supplémentaires** pour le logging. Le throughput est divisé par 3.

---

### 5. `Activator.CreateInstance` (reflection) sur chaque log dispatch

**`BluePrintFactory.cs:41`** + **`FluidEvent.cs:41-48`**

Reflection pour créer un `BusLogEvent` 2x par dispatch normal. La reflection est ~100x plus lente qu'un `new` direct.

**Fix :** Construire `BusLogEvent` directement :

```csharp
return new BusLogEvent(name, BusProtocol.System, new LogInstruction(message, _consoleWriter));
```

---

### 6. Exceptions utilisées pour le control flow

**`BluePrintFactory.cs:19-27`** et **`FBus.cs:73`**

`throw` + `catch` dans le même bloc, juste pour afficher un message. La capture de stack trace est une des opérations les plus coûteuses en .NET.

**Fix :** Simple `if/else` + `Console.WriteLine`.

---

### 7. Scan linéaire O(n) au lieu de lookup O(1)

**`HandlerLinq.cs:29-31`**

`TryGetHandlers` itère toutes les clés du dictionnaire puis refait un lookup par indexer. Remplaçable par un seul `TryGetValue` :

```csharp
public static bool TryGetHandlers(IFluidEvent evt, out Dictionary<IFluidHandler, bool> hdls)
    => handlers.TryGetValue(evt.GetType(), out hdls!);
```

**`FBus.cs:35-51`**

Scan linéaire de `_ports` pour trouver le bon port. Un `Dictionary<string, BusPort>` donne du O(1).

---

## P2 — Moyen impact

### 8. Double `CallCount++`

**`FBus.cs:47`** + **`FluidHandler.cs:28`**

Le compteur est incrémenté dans le bus ET dans le handler. Bug logique + cycles gaspillés.

---

### 9. `HashSet` pour les instructions = mauvaise structure

**`FluidEvent.cs:24`**

`new HashSet<IFluidInstruction>(instructions)` alors qu'un simple tableau suffit. Le HashSet alloue des buckets, calcule des hash, et déduplique sur `Data` seulement (bug d'égalité dans `FluidInstruction.cs:44-48` — 2 instructions différentes avec même data = la 2e est silencieusement ignorée).

---

### 10. LINQ `.Last()` sur un `Dictionary`

**`FBus.cs:48`**

`handlers.Last().Key` est O(n) et l'ordre d'un `Dictionary` n'est pas garanti. Résultat non-déterministe.

---

### 11. Double lookup dans Register/Drop

**`HandlerLinq.cs:14-16`**

`ContainsKey` + indexer = 2 lookups.

**Fix :** Utiliser `TryGetValue` :

```csharp
if (!handlers.TryGetValue(handler.EventType, out var dict))
{
    dict = new Dictionary<IFluidHandler, bool>();
    handlers[handler.EventType] = dict;
}
dict.Add(handler, false);
```

---

### 12. `Execute()` ET `ExecuteAndGet()` appelés systématiquement

**`FluidHandler.cs:24-25`**

Les deux méthodes sont appelées sur chaque instruction, mais une instruction n'a que des `_methods` OU des `_funcs`. Une des deux boucles tourne toujours à vide.

---

## P3 — Micro-optimisations / GC pressure

### 13. Allocations de closures et strings par dispatch

- **`FluidEvent.cs:31,36`** — 2 interpolations de string par dispatch pour le logging
- **`FluidEvent.cs:47`** — Lambda `msg => Console.WriteLine(msg)` recréée à chaque fois (cache en `static readonly`)
- **`FBus.cs:69-74`** — `FluidTask` fire-and-forget, l'objet est GC-eligible immédiatement
- **`FluidHandler.cs:16`**, **`FluidEvent.cs:22`** — Interpolation de string dans les constructeurs

---

### 14. `ContinueWith` sans `TaskScheduler`

**`FluidTask.cs:30`**

Utilise le scheduler ambient. Risque de deadlock sur un contexte UI.

**Fix :**

```csharp
_task.ContinueWith(t => callback(GetState()), TaskScheduler.Default);
```

---

### 15. Le bool de busy-state n'est jamais mis à `true`

**`FBus.cs:41-45`**

Le mécanisme "handler occupé" est du dead code. Tous les handlers apparaissent toujours disponibles.

---

## Résumé

Le plus gros poste de perte de perf est la **combinaison logging x reflection** : chaque publish normal génère 2 publishes de log supplémentaires, chacun créé par `Activator.CreateInstance`. Il y a donc **3 publishes x reflection x scan linéaire x allocations de strings/closures** par événement réel.

Corriger les points **4, 5, 6 et 7** donnera le plus gros gain immédiat.
