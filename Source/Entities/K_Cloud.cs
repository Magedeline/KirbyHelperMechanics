using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Cloud platform -- sinks while a player stands on it and
    /// rises back when they leave; the "fragile" variant (data.Bool("fragile"))
    /// breaks apart instead of just sinking, then respawns at its start
    /// height. Placeholder colored-rect rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_Cloud")]
    [Tracked]
    public class K_Cloud : JumpThru
    {
        private static readonly Color BodyColor = Calc.HexToColor("e8e8ff");
        private static readonly Color FragileColor = Calc.HexToColor("ffb3e6");

        private const float SinkAmount = 4f;
        private const float SinkSpeed = 30f;
        private const float RiseSpeed = 20f;
        private const float RespawnTime = 2f;

        private readonly bool fragile;
        private readonly float startY;
        private float sink;
        private bool broken;

        public K_Cloud(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, safe: true)
        {
            fragile = data.Bool("fragile", false);
            startY = Y;
            Depth = -9990;
        }

        public override void Update()
        {
            base.Update();

            if (broken)
                return;

            bool ridden = HasPlayerRider();

            if (ridden)
            {
                sink = Calc.Approach(sink, SinkAmount, SinkSpeed * Engine.DeltaTime);

                if (fragile && sink >= SinkAmount - 0.1f)
                    Add(new Coroutine(BreakRoutine()));
            }
            else
            {
                sink = Calc.Approach(sink, 0f, RiseSpeed * Engine.DeltaTime);
            }

            Y = startY + sink;
        }

        private IEnumerator BreakRoutine()
        {
            broken = true;
            Audio.Play("event:/Celestellaris/game/06_stronghold/cloud_pink_boost", Position);
            Collidable = false;
            Visible = false;

            yield return RespawnTime;

            Y = startY;
            sink = 0f;
            Collidable = true;
            Visible = true;
            broken = false;
            Audio.Play("event:/Celestellaris/game/06_stronghold/cloud_pink_reappear", Position);
        }

        public override void Render()
        {
            Draw.Rect(X, Y, Width, 6f, fragile ? FragileColor : BodyColor);
            Draw.HollowRect(X, Y, Width, 6f, Color.Black * 0.4f);
        }
    }
}
