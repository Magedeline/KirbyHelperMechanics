using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Floating "damage dealt" popup spawned by KirbyPlayerController whenever
    /// one of Kirby's attacks damages an enemy. Shows the damage amount and,
    /// from the second consecutive hit on, the current hit streak. Drawn on
    /// the HUD layer (text is unreadable at gameplay resolution) but anchored
    /// to a world position, so it stays over the enemy as the camera moves.
    /// </summary>
    public class KirbyDamageNumber : Entity
    {
        private const float Lifetime = 0.8f;
        private const float RiseDistance = 14f;

        private readonly string damageText;
        private readonly string streakText;
        private float timer;

        public KirbyDamageNumber(Vector2 worldPosition, int damage, int streak)
            : base(worldPosition)
        {
            Tag = Tags.HUD;
            damageText = "-" + damage;
            streakText = streak > 1 ? "x" + streak + " streak" : null;
        }

        public override void Update()
        {
            base.Update();
            timer += Engine.DeltaTime;
            if (timer >= Lifetime)
                RemoveSelf();
        }

        public override void Render()
        {
            if (Scene is not Level level)
                return;

            float t = timer / Lifetime;
            float alpha = 1f - Ease.CubeIn(t);
            // Pop in slightly oversized, then settle.
            float scale = 1f + 0.4f * (1f - Ease.CubeOut(Calc.Clamp(timer / 0.15f, 0f, 1f)));

            Vector2 world = Position - Vector2.UnitY * (8f + RiseDistance * Ease.CubeOut(t));
            // Gameplay is 320x180, the HUD is 1920x1080.
            Vector2 screen = (world - level.Camera.Position) * 6f;

            ActiveFont.DrawOutline(damageText, screen, new Vector2(0.5f, 1f), Vector2.One * 0.9f * scale,
                Color.Gold * alpha, 2f, Color.Black * alpha);

            if (streakText != null)
            {
                ActiveFont.DrawOutline(streakText, screen, new Vector2(0.5f, 0f), Vector2.One * 0.5f * scale,
                    Color.White * alpha, 2f, Color.Black * alpha);
            }
        }
    }
}
