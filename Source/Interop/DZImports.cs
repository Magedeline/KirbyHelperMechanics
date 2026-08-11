// Optional soft interop with DZ (https://github.com/Magedeline/DZ), the mod
// Kirby Helper Mechanics's player code was originally extracted from. Ported from the ModInterop
// pattern demonstrated in Picoline (https://github.com/balt-dev/Picoline,
// Source/Interop/PicolineExports.cs): these are plain static delegate
// fields that MonoMod.ModInterop fills in via reflection, at runtime, only
// if a mod named "DZ" is loaded and exports a matching [ModExportName("DZ")]
// API. If DZ isn't installed, every field here stays null, so every call
// site below is a no-op -- Kirby Helper Mechanics needs no compile-time or
// runtime reference to DZ's assembly at all, unlike the old Stubs/*.cs
// placeholders that redeclared DZ's own type names (Celeste.Entities.Boss,
// Celeste.Entities.Bosses.BaseBoss, Celeste.Entities.UniversalHealthUI,
// DZ.K_PlayerHooks) and would collide with DZ's real ones if both mods were
// ever loaded in the same Everest process.
//
// DZ does not currently export a "DZ" ModInterop API matching these fields,
// so today these all just resolve to no-ops (identical in effect to what
// the old stubs did standalone) -- wiring up DZ's export side is a separate,
// DZ-side change.
using System;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.KirbyHelperMechanics.Interop
{
    [ModImportName("DZ")]
    public static class DZImports
    {
        /// <summary>True if `entity` is one of DZ's boss-sized entities (Boss/BaseBoss) -- Kirby's inhale should never pull those in.</summary>
        public static Func<Entity, bool> IsBossSizedEntity;

        /// <summary>True if any DZ boss entity in `level` is within `radius` of `position` -- used for boss-arena detection.</summary>
        public static Func<Level, Vector2, float, bool> IsPositionNearBoss;

        /// <summary>Shows/hides DZ's player-health HUD for the given level.</summary>
        public static Action<Level, bool> SetShowPlayerHealth;

        /// <summary>Shows/hides DZ's boss-health HUD for the given level.</summary>
        public static Action<Level, bool> SetShowBossHealth;
    }
}
