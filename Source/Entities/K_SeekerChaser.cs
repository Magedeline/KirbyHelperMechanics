using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Seeker-style chaser -- a free-flying enemy that homes in
    /// on the player within range, damaging on contact. Exposes TakeDamage(int),
    /// so it's already a valid target for KirbyPlayerController's inhale/Star
    /// Spit pipeline (see IsDamageableTarget) without needing an
    /// InhaleableComponent of its own. Placeholder circle rendering stands in
    /// for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_SeekerChaser")]
    [Tracked]
    public class K_SeekerChaser : Actor
    {
        private static readonly Color BodyColor = Calc.HexToColor("2fb8a6");
        private static readonly Color SpikeColor = Calc.HexToColor("125e54");

        private const float HomingSpeed = 90f;
        private const float TurnRate = 4f; // fraction of HomingSpeed approached per second
        private const float DetectRange = 140f;
        private const int TouchDamage = 1;

        private readonly int maxHealth;
        private int health;
        private Vector2 velocity;
        private float hurtFlash;
        private bool aggro;

        public K_SeekerChaser(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            maxHealth = Math.Max(1, data.Int("health", 2));
            health = maxHealth;

            Collider = new Circle(8f);
            Depth = 0;

            Add(new PlayerCollider(OnPlayer));
        }

        public override void Update()
        {
            base.Update();

            if (hurtFlash > 0f)
                hurtFlash -= Engine.DeltaTime;

            global::Celeste.Player player = Scene?.Tracker.GetEntity<global::Celeste.Player>();
            bool inRange = player != null && Vector2.Distance(player.Center, Center) < DetectRange;

            if (inRange && !aggro)
                Audio.Play("event:/Celestellaris/game/07_inferno/seeker_aggro", Position);
            aggro = inRange;

            if (inRange)
            {
                Vector2 desired = (player.Center - Center).SafeNormalize() * HomingSpeed;
                velocity = Calc.Approach(velocity, desired, HomingSpeed * TurnRate * Engine.DeltaTime);
            }
            else
            {
                velocity = Calc.Approach(velocity, Vector2.Zero, HomingSpeed * Engine.DeltaTime);
            }

            MoveH(velocity.X * Engine.DeltaTime);
            MoveV(velocity.Y * Engine.DeltaTime);
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            Audio.Play("event:/Celestellaris/game/07_inferno/seeker_booped", Position);

            if (player.Get<KirbyPlayerController>() != null)
                K_PlayerHealthManager.TryDamagePlayer(TouchDamage, Center);
            else
                player.Die((player.Center - Center).SafeNormalize());
        }

        public void TakeDamage(int amount)
        {
            health -= amount;
            hurtFlash = 0.15f;

            if (Scene is Level level)
                level.Particles.Emit(global::Celeste.Player.P_DashA, 6, Center, Vector2.One * 4f);

            if (health <= 0)
            {
                Audio.Play("event:/Celestellaris/game/07_inferno/seeker_death", Position);
                RemoveSelf();
            }
            else
            {
                Audio.Play("event:/Celestellaris/game/07_inferno/seeker_hit_normal", Position);
            }
        }

        public override void Render()
        {
            Color color = hurtFlash > 0f ? Color.White : BodyColor;
            Draw.Circle(Center, 8f, color, 10);
            Draw.Circle(Center, 4f, SpikeColor, 6);
        }
    }
}
