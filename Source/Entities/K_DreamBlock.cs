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
        private const float PassDuration = 0.35f;

        private bool passing;

        public K_DreamBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            Depth = -11000;
        }

        public override void Update()
        {
            base.Update();

            if (passing || Scene == null)
                return;

            global::Celeste.Player player = Scene.Tracker.GetEntity<global::Celeste.Player>();
            if (player == null || player.StateMachine.State != global::Celeste.Player.StDash)
                return;

            if (CollideCheck(player))
                Add(new Coroutine(PassThrough(player)));
        }

        private IEnumerator PassThrough(global::Celeste.Player player)
        {
            passing = true;
            Collidable = false;

            Vector2 dir = player.Speed.SafeNormalize(Vector2.UnitX * (int)player.Facing);
            player.Speed = dir * PassSpeed;

            bool isKirby = player.Get<KirbyPlayerController>() != null;
            Audio.Play(isKirby ? "event:/Celestellaris/char/kirby/dreamblock_enter" : "event:/Celestellaris/char/madeline/dreamblock_enter", Position);

            yield return PassDuration;

            Collidable = true;
            passing = false;
        }

        public override void Render()
        {
            Draw.Rect(Collider, BodyColor);
            Draw.HollowRect(Collider, StarColor * 0.8f);
        }
    }
}
