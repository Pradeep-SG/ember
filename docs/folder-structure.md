# Folder structure convention

`Assets/` is Unity's canonical import root. Everything the game loads lives under it. Organize by **what the thing is**, not which feature it belongs to, until we have enough content that feature bundles become obviously cleaner.

## Top-level layout

```
Assets/
  Scripts/      C# gameplay, editor, runtime utilities. One subfolder per system.
  Scenes/       .unity scene files. One per environment or harness.
  Art/          2D art source and imported assets.
  Prefabs/      Prefab assets.
  Audio/        Music and SFX.
  Settings/     URP, input actions, quality. Leave as Unity generated.
  Tests/        Play Mode and Edit Mode test assemblies.
```

`Library/`, `Logs/`, `Temp/`, `UserSettings/`, `obj/`, `Build/` are all Unity-generated and are ignored via `.gitignore`. Do not check them in.

## Scripts

```
Assets/Scripts/
  Runtime/
    Breathing/           4-7-8 timing primitives
    ShadowBattle/        State machines and phase controllers
    Dialogue/            Demon dialogue runtime
    Data/                Oath, Chain, Class, Demon, Resonance structs
    Persistence/         Local save (Phase 1), cloud adapter later
    UI/
    Core/                Kernel utilities: time, random, events
  Editor/                Editor-only tools and inspectors
```

Namespace each subsystem: `Kindrith.Breathing`, `Kindrith.ShadowBattle`, etc. Assembly definitions (.asmdef) per subsystem once compile time becomes a nuisance — not before.

## Art

```
Assets/Art/
  Characters/
    Warden/
    Demons/
  Realm/
    Structures/
    Biomes/
  UI/
    Icons/
    Frames/
  VFX/
  Fonts/
```

Source `.psd`/`.ai` live alongside exports. Git LFS already tracks these via `.gitattributes`.

## Prefabs

Mirror the `Art/` subdivision: `Prefabs/Characters/`, `Prefabs/Realm/`, `Prefabs/UI/`. A prefab lives wherever its visual root lives.

## Audio

```
Assets/Audio/
  Music/
  SFX/
    ShadowBattle/
    UI/
    Realm/
  Voice/       (if we ever add it — unlikely Phase 1)
```

## Scenes

- `Bootstrap.unity` — entry scene, loads systems then hands off.
- `ShadowBattle.unity` — the Phase 1 prototype scene. This is the only gameplay scene in Phase 1.
- `Harness_*.unity` — dev-only test harnesses. Not shipped.

## Tests

```
Assets/Tests/
  EditMode/   Pure unit tests. Fast. No scene required.
  PlayMode/   Scene-integrated tests. Slower.
```

Each with its own `.asmdef` referencing `UnityEngine.TestRunner` and `UnityEditor.TestRunner`.

## When to deviate

If a feature grows past ~15 files across Scripts/Art/Prefabs and is cross-cutting, it earns a feature folder:

```
Assets/Features/ShadowBattle/
  Scripts/
  Prefabs/
  Art/
  Scenes/
```

Don't do this preemptively. Cross-cutting pain is a signal, not a guess.
