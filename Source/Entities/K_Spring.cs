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

            // Force out of Kirby's own ability states (Float above all --
            // KirbyFloatUpdate drives Speed.Y toward its own gentle terminal
            // fall speed every frame) before applying the bounce. Left
            // active, Float keeps fighting/stacking with the impulse below
            // frame after frame -- what reads as the bounce "forcing Kirby
            // upward" far harder than a normal spring, or oscillating
            // instead of a single clean launch. A plain StNormal state
            // doesn't touch Speed.Y on its own, so this makes the spring
            // behave the same for Kirby as it already does for Madeline.
            if (player.Get<KirbyPlayerController>()?.InKirbyAbilityState is true)
                player.StateMachine.State = global::Celeste.Player.StNormal;

            // Clear onGround before the impulse -- without this, a spring
            // sitting flush on a floor tile lets vanilla's own landed
            // handling see the player as still grounded for a frame after
            // the bounce and immediately re-clamp/re-settle them, which is
            // what makes the bounce look like it "goes down instantly" with
            // no cooldown even though cooldownTimer is armed correctly below.
            player.onGround = false;

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

            // Match vanilla Spring's own SuperBounce/SideBounce contract: arm
            // AutoJump so a jump press right after this bounce becomes a
            // normal bounce-jump combo instead of nothing (plain Madeline) or
            // getting stolen into a Float hover (Kirby -- see
            // KirbyPlayerController.KirbyJustBounced).
            player.AutoJump = true;
            player.AutoJumpTimer = 0f;

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
