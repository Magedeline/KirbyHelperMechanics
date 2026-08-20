using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Pinball-style bumper -- launches the player away from its center at
    /// high speed on contact. The "hot" variant (data.Bool("hot")) also deals
    /// touch damage instead of just bouncing, matching the "evil" reskin.
    /// Placeholder circle rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_Bumper")]
    [Tracked]
    public class K_Bumper : Entity
    {
        private const float LaunchSpeed = 280f;
        private const float Cooldown = 0.2f;
        private const int TouchDamage = 1;

        private readonly bool hot;
        private float cooldownTimer;
        private float wobble;

        public K_Bumper(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            hot = data.Bool("hot", false);
            Collider = new Circle(12f);
            Depth = -8500;

            Add(new PlayerCollider(OnPlayer));
        }

        public override void Update()
        {
            base.Update();
            if (cooldownTimer > 0f)
                cooldownTimer -= Engine.DeltaTime;
            if (wobble > 0f)
                wobble -= Engine.DeltaTime * 3f;
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (cooldownTimer > 0f)
                return;

            cooldownTimer = Cooldown;
            wobble = 1f;

            Vector2 dir = (player.Center - Center).SafeNormalize(-Vector2.UnitY);
            player.Speed = dir * LaunchSpeed;
            player.varJumpTimer = 0f;

            Audio.Play("event:/Celestellaris/game/08_edge/pinballbumper_hit", Position);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);

            if (hot)
            {
                if (player.Get<KirbyPlayerController>() != null)
                    K_PlayerHealthManager.TryDamagePlayer(TouchDamage, Center);
                else
                    player.Die(-dir);
            }
        }

        public override void Render()
        {
            Color body = hot ? Calc.HexToColor("ff3b3b") : Calc.HexToColor("ffb23c");
            float scale = 1f + wobble * 0.3f;
            Draw.Circle(Position, 12f * scale, body, 14);
            Draw.Circle(Position, 6f, Color.White * 0.7f, 8);
        }
    }
}
