using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Bouncing hazard -- fire or ice flavored via data.Attr("kind"), bounces
    /// off solids, damages on touch. Placeholder circle rendering stands in
    /// for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_BouncingHazard")]
    [Tracked]
    public class K_BouncingHazard : Actor
    {
        private const float Speed = 90f;
        private const int TouchDamage = 1;

        private readonly Color bodyColor;
        private Vector2 velocity;

        public K_BouncingHazard(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            bool ice = data.Attr("kind", "fire").ToLowerInvariant() == "ice";
            bodyColor = ice ? Calc.HexToColor("6ce0ff") : Calc.HexToColor("ff6b1f");

            float angle = data.Float("angle", 45f) * (float)System.Math.PI / 180f;
            velocity = Calc.AngleToVector(angle, Speed);

            Collider = new Circle(6f);
            Depth = 0;

            Add(new PlayerCollider(OnPlayer));
        }

        public override void Update()
        {
            base.Update();

            if (MoveH(velocity.X * Engine.DeltaTime))
                velocity.X *= -1f;
            if (MoveV(velocity.Y * Engine.DeltaTime))
                velocity.Y *= -1f;
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (player.Get<KirbyPlayerController>() != null)
                K_PlayerHealthManager.TryDamagePlayer(TouchDamage, Center);
            else
                player.Die((player.Center - Center).SafeNormalize());
        }

        public override void Render()
        {
            Draw.Circle(Center, 6f, bodyColor, 10);
        }
    }
}
