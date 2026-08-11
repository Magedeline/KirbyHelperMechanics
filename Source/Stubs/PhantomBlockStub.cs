// STUB -- placeholder for Celeste.Entities.PhantomBlock.
// "PhantomBlock" does not exist anywhere in this Celeste.dll's metadata (verified
// by grepping the DLL directly) -- it isn't vanilla content. It must come from one
// of DZ's helper-mod dependencies (FrostHelper/VivHelper/CommunalHelper/etc., see
// DZ/everest.yaml), none of which this repo depends on. K_Player.cs's dream-dash
// corner-correction fallback checks CollideCheck<PhantomBlock>/CollideFirst<PhantomBlock>
// purely to treat a dream-dashable-but-not-DreamBlock solid the same as a DreamBlock.
// This stub is intentionally empty and derives from Solid (matching real usage sites
// that treat it as a kind of Solid); nothing will ever actually be one of these types
// while this repo runs standalone, so the fallback silently never triggers instead
// of crashing -- functionally "phantom-block dream-dash compat" is off until a real
// dependency is added.
//
// [Tracked] is required, not optional, for that "silently never triggers" claim to
// actually hold: Monocle.Tracker only allocates a (possibly empty) list for types
// registered with [Tracked] at startup. K_Player.cs's CollideFirst<PhantomBlock>/
// CollideCheck<PhantomBlock> calls index Tracker.Entities[typeof(PhantomBlock)]
// directly -- without [Tracked] that key was never added at all, so the very first
// dream-dash corner-correction check threw KeyNotFoundException (confirmed via Live
// Watch), regardless of there being zero PhantomBlock instances.
//
// IMPORTANT -- do not ship this file as-is: if the mod that actually defines
// PhantomBlock is ever added as a real dependency, delete this stub and use theirs.
namespace Celeste.Entities
{
    [Monocle.Tracked]
    public class PhantomBlock : Solid
    {
        public PhantomBlock() : base(Microsoft.Xna.Framework.Vector2.Zero, 8f, 8f, safe: false)
        {
        }

        // No-ops matching DreamBlock's interface -- K_Player.cs treats PhantomBlock
        // as a dream-dashable-but-not-DreamBlock solid and calls these the same way
        // it calls DreamBlock.FootstepRipple/OnPlayerExit. Since nothing will ever
        // actually be a PhantomBlock while this repo runs standalone (see the file
        // header), these bodies never run.
        public void FootstepRipple(Microsoft.Xna.Framework.Vector2 position) { }
        public void OnPlayerExit(global::Celeste.Player player) { }
    }
}
