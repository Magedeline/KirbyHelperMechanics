using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Spring -- bounces the player up/left/right on touch,
    /// same shape as vanilla's three Spring orientations. Placeholder circle
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_Spring")]
    [Tracked]
    public class K_Spring : Entity
    {
        private static readonly Color BodyColor = Calc.HexToColor("ffd23f");
        private static readonly Color CoilColor = Calc.HexToColor("d98f00");

        private enum Orientation { Up, Left, Right }

        private const float BounceSpeed = 260f;
        private const float Cooldown = 0.1f;

        private readonly Orientation orientation;
        private float cooldownTimer;

        public K_Spring(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            orientation = ParseOrientation(data.Attr("orientation", "Up"));
            Collider = orientation == Orientation.Up
                ? new Hitbox(16f, 6f, -8f, -6f)
                : new Hitbox(6f, 16f, orientation == Orientation.Left ? 0f : -6f, -8f);
            Depth = -8501;

            Add(new PlayerCollider(OnPlayer));
        }

        private static Orientation ParseOrientation(string value) => value?.ToLowerInvariant() switch
        {
            "left" => Orientation.Left,
            "right" => Orientation.Right,
            _ => Orientation.Up,
        };

        public override void Update()
        {
            base.Update();
            if (cooldownTimer > 0f)
                cooldownTimer -= Engine.DeltaTime;
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (cooldownTimer > 0f)
                return;

            switch (orientation)
            {
                case Orientation.Up:
                    player.Speed.Y = -BounceSpeed;
                    break;
                case Orientation.Left:
                    player.Speed.X = -BounceSpeed;
                    break;
                case Orientation.Right:
                    player.Speed.X = BounceSpeed;
                    break;
            }

            player.varJumpTimer = 0f;
            cooldownTimer = Cooldown;
            Audio.Play("event:/Celestellaris/game/general/spring", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        public override void Render()
        {
            Draw.Circle(Position, 8f, CoilColor, 8);
            Draw.Circle(Position, 6f, BodyColor, 8);
        }
    }
}
