using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Simple bobbing water hazard -- floats in place on a sine wave, damages
    /// on touch. Placeholder circle rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_Jellyfish")]
    [Tracked]
    public class K_Jellyfish : Entity
    {
        private static readonly Color BodyColor = Calc.HexToColor("d98fff");
        private const float BobAmount = 6f;
        private const float BobSpeed = 1.2f;
        private const int TouchDamage = 1;
        private const float HitCooldown = 0.5f;

        private readonly Vector2 origin;
        private float cooldownTimer;

        public K_Jellyfish(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            origin = Position;
            Collider = new Circle(8f);
            Depth = 0;

            Add(new PlayerCollider(OnPlayer));
        }

        public override void Update()
        {
            base.Update();
            Position = origin + new Vector2(0f, (float)System.Math.Sin(Scene.TimeActive * BobSpeed) * BobAmount);

            if (cooldownTimer > 0f)
                cooldownTimer -= Engine.DeltaTime;
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (cooldownTimer > 0f)
                return;
            cooldownTimer = HitCooldown;

            if (player.Get<KirbyPlayerController>() != null)
                K_PlayerHealthManager.TryDamagePlayer(TouchDamage, Center);
            else
                player.Die((player.Center - Center).SafeNormalize());
        }

        public override void Render()
        {
            Draw.Circle(Position, 8f, BodyColor, 10);
            Draw.Circle(Position + new Vector2(0f, -2f), 3f, Color.White * 0.7f, 6);
        }
    }
}
