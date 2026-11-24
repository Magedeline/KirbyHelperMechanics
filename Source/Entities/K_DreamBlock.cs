using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Dream Block -- a lightweight reinterpretation of
    /// vanilla's Dream Dash, not a port of it: fully solid until the player
    /// dashes into it, at which point it briefly turns non-collidable and
    /// gives a speed boost through, then re-solidifies. No dream-dash camera
    /// sequence/curve steering, deliberately -- this is a first pass.
    /// Placeholder colored-rect rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_DreamBlock")]
    [Tracked]
    public class K_DreamBlock : Solid
    {
        private static readonly Color BodyColor = Calc.HexToColor("091a3d");
        private static readonly Color StarColor = Calc.HexToColor("6ce0ff");

        private const float PassSpeed = 240f;
        // How long the block waits for the player to actually move inside
        // after the dash touches it, and the hard cap on a single pass.
        private const float EnterGrace = 0.15f;
        private const float MaxPassTime = 2f;

        private bool passing;

        public K_DreamBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            Depth = -11000;

            // A Solid never overlaps the player (the player's own movement
            // stops flush against it), so "dashed into me" can't be an
            // overlap test -- vanilla reports exactly that contact through
            // OnDashCollide, from Player.OnCollideH/V, with the side hit.
            OnDashCollide = OnDashed;
        }

        private DashCollisionResults OnDashed(global::Celeste.Player player, Vector2 direction)
        {
            if (passing)
                return DashCollisionResults.NormalCollision;

            // Open up right now, not when the coroutine first runs next
            // Update -- a diagonal dash can report a second contact (other
            // axis) within this same frame.
            passing = true;
            Collidable = false;
            player.Speed = direction * PassSpeed;

            Add(new Coroutine(PassThrough(player, direction)));
            // Ignore = vanilla skips its own stop-against-a-wall handling.
            return DashCollisionResults.Ignore;
        }

        private IEnumerator PassThrough(global::Celeste.Player player, Vector2 dir)
        {
            passing = true;
            Collidable = false;

            player.Speed = dir * PassSpeed;

            bool isKirby = player.Get<KirbyPlayerController>() != null;
            Audio.Play(isKirby ? "event:/Celestellaris/char/kirby/dreamblock_enter" : "event:/Celestellaris/char/madeline/dreamblock_enter", Position);

            // Stay open until the player has come out the other side, however
            // wide the block is, holding the pass speed the whole way so the
            // dash ending (or gravity) can't strand them inside.
            Rectangle bounds = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
            bool entered = false;
            bool inside = false;
            for (float t = 0f; t < MaxPassTime; t += Engine.DeltaTime)
            {
                inside = player.Scene != null && !player.Dead && player.CollideRect(bounds);
                if (inside)
                {
                    entered = true;
                    player.Speed = dir * PassSpeed;
                }
                else if (entered || t >= EnterGrace)
                {
                    break;
                }

                yield return null;
            }

            Collidable = true;
            passing = false;

            // Exit blocked for the whole pass: same outcome as a vanilla dream
            // block with no way out.
            if (inside && player.Scene != null && !player.Dead && player.CollideRect(bounds))
                player.Die(-dir);
        }

        public override void Render()
        {
            Draw.Rect(Collider, BodyColor);
            Draw.HollowRect(Collider, StarColor * 0.8f);
        }
    }
}
