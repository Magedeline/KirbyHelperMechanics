# Kirby Helper Mechanics

An [Everest](https://everestapi.github.io/) mod for Celeste that adds a fully
playable Kirby character (`K_Player`) as an alternative to Madeline, with his
own movement kit, health system, and map-editor entities.

## What it does

- **`K_Player`** — a standalone `Actor` (not a `Celeste.Player` subclass) that
  reimplements Madeline's full move set (dash, climb, wall-jump, dream-dash,
  swimming, room transitions, ...) plus Kirby-specific mechanics: multi-flap
  hover flight, inhale/spit, and a star-projectile attack (`PlayerStarBullet`).
  A hidden vanilla `Celeste.Player` "shadow" is kept in sync behind the scenes
  so vanilla systems that only know how to collide with `Player`
  (springs, boosters, dream blocks, triggers, ...) keep working unmodified.
- **Player switching** — `PlayerSelectionManager` lets a map (or the mod's own
  settings) choose Kirby or Madeline as the active character, with per-level
  overrides and a `K_PlayerTrigger` map entity to swap mid-level.
- **Kirby health system** — `K_PlayerHealthManager` gives Kirby an HP pool
  instead of one-hit death, toggled and configured per-room with
  `K_PlayerHealthTrigger`.
- **Refills** — `K_PlayerRefill` is a Kirby-aware reimplementation of vanilla
  Refill (vanilla's only recognizes `Celeste.Player`, so it never fires for
  `K_Player`).
- **Cosmetics** — `KirbyShoes` (placeholder shoe rendering matching the dash
  color system) and `KirbyPuff` (spit-poof particle).
- **Lönn integration** — entity/trigger plugins and forms under `Loenn/` so
  mappers can place Kirby-specific entities from the Celeste map editor.

## Requirements

- [Everest](https://everestapi.github.io/) `1.6314.0` or newer.

Optional soft dependencies (detected at runtime if installed; the mod runs
fine without any of them): ExtendedVariantMode, MaxHelpingHand, FrostHelper,
GravityHelper, EeveeHelper, FactoryHelper, JackalHelper.

## Installing

Drop the mod folder into your Celeste `Mods/` directory (or install the
packaged `.zip` through Olympus / your usual mod manager) so that
`Mods/KirbyHelperMechanics/everest.yaml` sits alongside your other mods.

## Building from source

The mod is built from `Source/`:

```
cd Source
dotnet build KirbyHelperMechanics.csproj
```

Targets `net8.0`. If a Celeste install isn't found three directories up
(`../../../Celeste.dll`, i.e. this repo living inside `Mods/<name>/Source`),
the build falls back to the stripped reference assemblies in
`Source/lib-stripped/`. A post-build step copies the built DLL/PDB into
`bin/`, which is what `everest.yaml` points Everest at — building without
that copy step running will make Everest report the mod assembly as missing.

## Packaging a release zip

```
pwsh ./publish.ps1
```

Builds in `Release` and writes `dist/KirbyHelperMechanics.zip` with
`everest.yaml` at the zip root (not wrapped in an extra folder) alongside
`bin/`, `Graphics/`, `Loenn/`, and `Audio/` — the exact layout Everest/Olympus
expect for a drag-and-drop install. `Source/`, `Source/lib-stripped/`, and
other dev-only files are left out.

## Project layout

```
Audio/           FMOD banks/events
Graphics/        Sprites and atlases (incl. Graphics/k_sprites.xml, a custom
                 sprite bank loaded explicitly by the mod at startup -- it
                 must NOT be named Sprites.xml, since DZ ships its own
                 "kirby_player_ext" element that would silently win the
                 auto-merge otherwise)
Loenn/           Map-editor (Lönn) entity/trigger plugins and forms
Source/          C# mod source (see below)
everest.yaml     Mod metadata and dependencies
```

Inside `Source/`:

```
K_Player.cs                    Kirby's player Actor - the bulk of the mod
KirbyPlayerController.cs       Flight/inhale/glomp component (K_Player or vanilla Player)
KirbyHelperMechanicsModule.cs  Everest module entry point
KirbyHelperMechanicsModuleSettings.cs  Persisted mod options
PlayerSelectionManager.cs      Kirby vs. Madeline selection state
K_PlayerHealthManager.cs       Kirby's HP system
K_PlayerRefill.cs              Kirby-aware Refill entity
KirbyPuff.cs / KirbyShoes.cs   Cosmetic effects
PlayerStarBullet.cs            Spit-attack projectile
ShadowPlayerHooks.cs           Suppresses K_Player's hidden vanilla-Player shadow
Extensions/KirbyMode.cs        Global Kirby mode / power-state tracking
Triggers/                      K_PlayerTrigger, K_PlayerHealthTrigger
Interop/DZImports.cs           Optional soft interop with the DZ mod
Stubs/                         Compile-time stubs for types this mod doesn't hard-depend on
```

## Credits

Several mechanics and entities are ported from
[DZ](https://github.com/Magedeline/DZ), the mod `K_Player` and related code
were originally extracted from; Kirby's move set is adapted from a PICO-8
Kirby fangame's Lua source. See in-file doc comments for specifics.
