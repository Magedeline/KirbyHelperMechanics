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
        private readonly MTexture texture;

        // Hidden hit assistance (HAL's Forgotten Land talk: if it looks like it
        // should have hit, it hits). Walls use the real circle; enemies are
        // tested against one a few pixels bigger, so a star that visibly grazes
        // an enemy counts.
        private const float EnemyHitPadding = 4f;
        private readonly Collider wallCollider;
        private readonly Collider enemyCollider;

        // When the star was spat. The hit streak is judged from this moment, not
        // from impact, so a long-travelling star still continues the combo
        // (Return to Dream Land Deluxe counts hits before they connect).
        private float spawnTime = -1f;

        /// <param name="size">0 = small (16px), 1 = medium (24px), 2 = large (32px) -- see projectiles/KHM/kirby/star/.</param>
        public PlayerStarBullet(Vector2 position, Vector2 velocity, KirbyPlayerController owner, int damage, int size = 1)
            : base(position)
        {
            this.velocity = velocity;
            this.owner = owner;
            this.damage = damage;
            Depth = -50;

            size = Calc.Clamp(size, 0, 2);
            float radius = 6f + size * 4f;
            Collider = wallCollider = new Circle(radius);
            enemyCollider = new Circle(radius + EnemyHitPadding);
            texture = GFX.Game["projectiles/KHM/kirby/star/" + (size == 0 ? "small" : size == 1 ? "medium" : "large")];
        }

        public override void Update()
        {
            base.Update();

            if (spawnTime < 0f)
                spawnTime = (Scene as Level)?.TimeActive ?? 0f;

            Position += velocity * Engine.DeltaTime;
            // Spins in its direction of travel (clockwise when flying right).
            rotation += Engine.DeltaTime * 14f * (velocity.X < 0f ? -1f : 1f);

            // Trail
            if (Scene.OnInterval(0.04f))
                (Scene as Level)?.ParticlesFG.Emit(ParticleTypes.SparkyDust, Position);

            // No timeout, but a star that leaves the room is gone for good.
            if (Scene is Level level && !level.IsInBounds(Position, 32f))
            {
                RemoveSelf();
                return;
            }

            if (CollideCheck<Solid>())
            {
                Burst();
                RemoveSelf();
                return;
            }

            // Enemy hit — first valid target wins, projectile dies on contact.
            // Checked with the padded enemy collider (see EnemyHitPadding).
            Entity hit = null;
            Collider = enemyCollider;
            try
            {
                foreach (Entity entity in Scene.Entities)
                {
                    if (owner.IsValidTarget(entity) && CollideCheck(entity))
                    {
                        hit = entity;
                        break;
                    }
                }
            }
            finally
            {
                Collider = wallCollider;
            }

            if (hit != null)
            {
                owner.DealProjectileDamage(hit, damage, velocity.SafeNormalize(), spawnTime);
                Burst();
                RemoveSelf();
            }
        }

        public override void Render()
        {
            base.Render();
            // Draw a yellow spinning star using the same particle sprite as BossStarProjectile.
            // Falls back gracefully if no atlas texture is set — the trail particles still show.
            texture.DrawCentered(Position, Color.White, 1f, rotation);
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
