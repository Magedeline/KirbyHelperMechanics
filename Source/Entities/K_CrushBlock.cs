using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Crush Block (Kevin) -- telegraphs, then charges at the
    /// player along one axis when they're in range, damaging on contact, then
    /// retreats back to its start position. Placeholder colored-rect
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_CrushBlock")]
    [Tracked]
    public class K_CrushBlock : Solid
    {
        private static readonly Color BodyColor = Calc.HexToColor("2b2b2b");
        private static readonly Color FaceColor = Calc.HexToColor("ff3b3b");

        private enum ChargeAxis { Horizontal, Vertical }

        private const float ChargeSpeed = 240f;
        private const float ActivationRange = 100f;
        private const float TelegraphTime = 0.4f;
        private const float RetreatDelay = 0.6f;
        private const float RetreatDuration = 0.8f;
        private const int TouchDamage = 1;

        private readonly ChargeAxis axis;
        private readonly Vector2 start;
        private bool busy;

        public K_CrushBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            axis = data.Attr("axis", "Horizontal").Equals("Vertical", StringComparison.OrdinalIgnoreCase)
                ? ChargeAxis.Vertical
                : ChargeAxis.Horizontal;
            start = Position;
            Depth = -9000;
        }

        public override void Update()
        {
            base.Update();

            if (busy || Scene == null)
                return;

            global::Celeste.Player player = Scene.Tracker.GetEntity<global::Celeste.Player>();
            if (player == null)
                return;

            float alongAxisDist = axis == ChargeAxis.Horizontal
                ? Math.Abs(player.Center.Y - Center.Y)
                : Math.Abs(player.Center.X - Center.X);
            float towardOffset = axis == ChargeAxis.Horizontal
                ? player.Center.X - Center.X
                : player.Center.Y - Center.Y;

            if (alongAxisDist < Height && Math.Abs(towardOffset) < ActivationRange)
                Add(new Coroutine(ChargeRoutine(Math.Sign(towardOffset))));
        }

        private IEnumerator ChargeRoutine(int direction)
        {
            busy = true;
            StartShaking(TelegraphTime);
            Audio.Play("event:/Celestellaris/game/08_edge/crushblock_activate", Position);
            yield return TelegraphTime;

            bool hit = false;
            while (!hit)
            {
                hit = axis == ChargeAxis.Horizontal
                    ? MoveHCollideSolids(ChargeSpeed * direction * Engine.DeltaTime, false)
                    : MoveVCollideSolids(ChargeSpeed * direction * Engine.DeltaTime, false);

                DamageOverlappingPlayer();
                yield return null;
            }

            Audio.Play("event:/Celestellaris/game/08_edge/crushblock_impact", Position);
            if (Scene is Level level)
                level.Shake(0.3f);

            yield return RetreatDelay;
            yield return ReturnRoutine();
            busy = false;
        }

        private void DamageOverlappingPlayer()
        {
            global::Celeste.Player player = Scene.Tracker.GetEntity<global::Celeste.Player>();
            if (player == null || !CollideCheck(player))
                return;

            if (player.Get<KirbyPlayerController>() != null)
                K_PlayerHealthManager.TryDamagePlayer(TouchDamage, Center);
            else
                player.Die((player.Center - Center).SafeNormalize());
        }

        private IEnumerator ReturnRoutine()
        {
            Vector2 from = Position;
            for (float t = 0f; t < 1f; t += Engine.DeltaTime / RetreatDuration)
            {
                MoveTo(Vector2.Lerp(from, start, Ease.CubeInOut(t)));
                yield return null;
            }
            MoveTo(start);
        }

        public override void Render()
        {
            Draw.Rect(Collider, BodyColor);
            Draw.Rect(X + Width / 2f - 3f, Y + Height / 2f - 3f, 6f, 6f, FaceColor);
        }
    }
}
