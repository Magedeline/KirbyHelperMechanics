using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Crumble Block -- a Solid that falls apart a short beat
    /// after a player stands on it, then respawns. Placeholder colored-rect
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_CrumbleBlock")]
    [Tracked]
    public class K_CrumbleBlock : Solid
    {
        private static readonly Color BodyColor = Calc.HexToColor("c97a3d");
        private static readonly Color CrackColor = Calc.HexToColor("6b3f1d");

        private const float FallDelay = 0.4f;
        private const float RespawnTime = 2.5f;

        private bool crumbling;

        public K_CrumbleBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            Depth = 0;
        }

        public override void Update()
        {
            base.Update();

            if (!crumbling && Collidable && HasPlayerRider())
                Add(new Coroutine(CrumbleRoutine()));
        }

        public override void Render()
        {
            if (!Collidable)
                return;

            Draw.Rect(Collider, BodyColor);
            Draw.HollowRect(Collider, CrackColor);
        }

        private IEnumerator CrumbleRoutine()
        {
            crumbling = true;
            StartShaking(FallDelay);
            Audio.Play("event:/Celestellaris/game/general/fallblock_shake", Position);

            yield return FallDelay;

            Collidable = false;
            Visible = false;
            Audio.Play("event:/Celestellaris/game/general/fallblock_impact", Position);
            if (Scene is Level level)
                level.Shake(0.2f);

            yield return RespawnTime;

            Collidable = true;
            Visible = true;
            crumbling = false;
        }
    }
}
