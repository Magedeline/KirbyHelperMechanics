using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Entities;

namespace Celeste.Projectiles
{
    /// <summary>
    /// Ported from DZ's Source/Mechanics/Combat/KirbyProjectiles.cs -- just this one
    /// class, not the whole file (the others there are boss/enemy projectiles that
    /// stay in DZ). Star projectile fired by the player when spitting out an
    /// inhaled enemy. Travels in a fixed direction, damages the first enemy it
    /// hits, then disappears.
    /// </summary>
    [Tracked]
    public class PlayerStarBullet : Entity
    {
        private readonly Vector2 velocity;
        private readonly KirbyPlayerController owner;
        private readonly int damage;
        private float rotation;

        public PlayerStarBullet(Vector2 position, Vector2 velocity, KirbyPlayerController owner, int damage)
            : base(position)
        {
            this.velocity = velocity;
            this.owner = owner;
            this.damage = damage;
            Depth = -50;
            Collider = new Circle(6f);
        }

        public override void Update()
        {
            base.Update();

            Position += velocity * Engine.DeltaTime;
            rotation += Engine.DeltaTime * 8f;

            // Trail
            if (Scene.OnInterval(0.04f))
                (Scene as Level)?.ParticlesFG.Emit(ParticleTypes.SparkyDust, Position);

            // Travels indefinitely until it physically hits something -- no timeout.
            if (CollideCheck<Solid>())
            {
                Burst();
                RemoveSelf();
                return;
            }

            // Enemy hit — first valid target wins, projectile dies on contact
            foreach (Entity entity in Scene.Entities)
            {
                if (!owner.IsValidTarget(entity))
                    continue;
                if (!CollideCheck(entity))
                    continue;

                owner.DealProjectileDamage(entity, damage, velocity.SafeNormalize());
                Burst();
                RemoveSelf();
                return;
            }
        }

        public override void Render()
        {
            base.Render();
            // Draw a yellow spinning star using the same particle sprite as BossStarProjectile.
            // Falls back gracefully if no atlas texture is set — the trail particles still show.
            GFX.Game.GetAtlasSubtexturesAt("projectiles/kirby/star/idle", 0)
                ?.DrawCentered(Position, Color.White, 1f, rotation);
        }

        private void Burst()
        {
            var level = Scene as Level;
            level?.ParticlesFG.Emit(ParticleTypes.SparkyDust, 6, Position, Vector2.One * 5f);
            level?.Displacement.AddBurst(Position, 0.2f, 4f, 20f, 0.3f, Ease.QuadOut, Ease.QuadOut);
            Audio.Play("event:/char/badeline/boss_bullet_impact", Position);
        }
    }
}
