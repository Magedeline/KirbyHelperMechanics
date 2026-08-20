# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Building

```
cd Source
dotnet build KirbyHelperMechanics.csproj
```

Targets `net8.0`. A post-build step copies `KirbyHelperMechanics.dll` and `.pdb` from `Source/bin/<config>/net8.0/` into the mod root's `bin/` — this copy must succeed for Everest to load the mod. If the Celeste install isn't three directories up (`../../../Celeste.dll`), the build falls back to `Source/lib-stripped/`.

There is no test suite; validation is done by running the mod inside Celeste via Everest.

## Architecture

### Kirby is a Component, not a separate Actor

`KirbyPlayerController` is a `Monocle.Component` attached to (and removed from) the **real vanilla `Celeste.Player`** at runtime. There is no separate Kirby Actor and no hidden "shadow" Player. This means all of vanilla's collision, room transitions, triggers, and cutscene systems apply to Kirby automatically.

`KirbyPlayerHooks` (loaded via `KirbyHelperMechanicsModule.Load`) owns the MonoMod `On.*` hooks on `Celeste.Player.Added/Render/Update/NormalUpdate` and manages attaching/detaching `KirbyPlayerController` to match `PlayerSelectionManager`'s current selection.

### Player selection flow

`PlayerSelectionManager` is a `Tags.Persistent` singleton Entity. It resolves the active character (Kirby or Madeline) from three layers: mod settings default → current selection → per-level override. Mid-level swaps (via `K_PlayerTrigger` or in-game settings toggle) fire `OnPlayerSelectionChanged`, which `KirbyPlayerHooks` handles by calling `SyncKirbyState` on the live Player immediately.

`PlayerSelectionManager.GetOrCreate` defers `level.Add(manager)` to `level.OnEndOfFrame` to avoid mutating Monocle's entity list mid-enumeration.

### Custom StateMachine states

`KirbyPlayerController.Added` registers `StKirbyFloat`, `StKirbyInhale`, and `StKirbyStarSpit` on the vanilla `Player.StateMachine` via `StateMachine.AddState`. These IDs are `-1` until `Added` has run.

### JIT safety rule for optional dependencies

The `.csproj` references `lib-stripped/*.dll` for optional mods (ExtendedVariantMode, FactoryHelper, etc.) at compile time only (`Reference Private="false"` keeps them out of `bin/`). **Never reference an optional mod's types directly inside a method that runs unconditionally** (constructors, `Update`, etc.) — the .NET JIT resolves all types in a method's IL on first invocation, regardless of which branch is taken. Gate every such reference in its own dedicated method called only when the matching `*Loaded` flag in `KirbyHelperMechanicsModule` is `true`.

### Soft interop with DZ

`DZImports` uses `MonoMod.ModInterop` (`[ModImportName("DZ")]`) for zero-reference soft interop with the DZ mod. Fields are populated at runtime by `typeof(DZImports).ModInterop()` in `KirbyHelperMechanicsModule.Initialize()` (after all mods' `Load()` have run). If DZ isn't installed, every field remains `null` — all call sites are no-ops.

### Lönn map editor integration

`Loenn/` contains Lua entity/trigger plugin files and UI forms used by the Celeste map editor Lönn. These run in Lua inside the editor and are independent of the C# runtime.

### Sprite loading

`Graphics/Pusheen2026/KHM/k_sprites.xml` is a custom-named sprite bank — Everest only auto-merges files literally named `Graphics/Sprites.xml`, so this one is loaded explicitly via `new Monocle.SpriteBank(GFX.Game, "Graphics/Pusheen2026/KHM/k_sprites.xml")` in `KirbyHelperMechanicsModule.LoadContent`. **Do not rename it to `Graphics/Sprites.xml`**: DZ's own `Mods/DZ/Graphics/Sprites.xml` already defines an element also named `kirby_player_ext`, with a different, `kirby_`-prefixed animation scheme (`start="kirby_idle"`, ids like `kirby_idle`/`kirby_walk`/`kirby_run` instead of this mod's vanilla-mirrored `idle`/`walk`/`runFast`). If auto-merged, DZ's copy (processed after this mod's) silently wins, and `KirbyPlayerController`'s vanilla-id mirroring (`RenderKirbyOverlay`) finds no matching animations — Kirby renders but never animates. Loading explicitly from `LoadContent` (which runs after every mod's `Load()`, i.e. after Everest's own auto-merge pass) guarantees this mod's `kirby_player_ext` always overwrites DZ's, regardless of mod load order.

For the same reason, this mod's Kirby atlas folders live under `characters/KHM/`, `cutscenes/KHM/`, and `objects/KHM/` — **not** `characters/DZ/` etc. DZ ships its own separately-authored Kirby skin at those exact `DZ/`-namespaced paths, and Everest's asset resolution is per-file: with both mods installed, any PNG that exists at the same relative path in both mods' `Graphics/Atlases/` trees resolves to whichever mod's copy loads last (DZ, in practice), silently discarding this mod's copy for that one frame, while frames only this mod has are unaffected. The result used to be individual animations rendering as an inconsistent mix of DZ's art and this mod's art. **Never add new Kirby assets under a `DZ/`-namespaced path** — always use `KHM/`.

### Namespaces

Map entities live in `Celeste.Entities`, triggers in `Celeste.Triggers`, helpers in `Celeste.Helpers`, module/settings/hooks in `Celeste.Mod.KirbyHelperMechanics`, and extensions in `Celeste.Extensions`. This mirrors the convention used by DZ (the upstream source) to avoid type-name collisions.
