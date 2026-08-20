using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Booster -- red (data.Bool("red", false) == true) refills
    /// the player's dash on entry like vanilla's classic pink Booster; green
    /// doesn't, mirroring vanilla's Core/Farewell no-dash-refill variant.
    /// Simplified vs vanilla: no aim-charge animation, launches immediately
    /// along the player's current aim (or facing) after a brief dummy-state
    /// hold. Placeholder circle rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_Booster")]
    [Tracked]
    public class K_Booster : Entity
    {
        private const float HoldTime = 0.25f;
        private const float BoostSpeed = 240f;
        private const float Cooldown = 1.5f;

        private readonly bool isRed;
        private readonly Color bodyColor;
        private bool used;

        public K_Booster(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            isRed = data.Bool("red", false);
            bodyColor = isRed ? Calc.HexToColor("ff3b3b") : Calc.HexToColor("3bff6b");

            Collider = new Circle(10f);
            Depth = -8500;

            Add(new PlayerCollider(OnPlayer));
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (used || player.StateMachine.State == global::Celeste.Player.StDummy)
                return;

            Add(new Coroutine(BoostRoutine(player)));
        }

        private IEnumerator BoostRoutine(global::Celeste.Player player)
        {
            used = true;
            Collidable = false;

            player.StateMachine.State = global::Celeste.Player.StDummy;
            player.Speed = Vector2.Zero;
            Audio.Play(isRed ? "event:/Celestellaris/game/07_inferno/redbooster_enter" : "event:/Celestellaris/game/06_stronghold/greenbooster_enter", Position);

            yield return HoldTime;

            Vector2 dir = player.lastAim;
            if (dir == Vector2.Zero)
                dir = Vector2.UnitX * (int)player.Facing;

            player.Speed = dir.SafeNormalize() * BoostSpeed;
            if (isRed)
                player.RefillDash();

            player.StateMachine.State = global::Celeste.Player.StNormal;
            Audio.Play(isRed ? "event:/Celestellaris/game/07_inferno/redbooster_reappear" : "event:/Celestellaris/game/06_stronghold/greenbooster_reappear", Position);

            yield return Cooldown;
            used = false;
            Collidable = true;
        }

        public override void Render()
        {
            Draw.Circle(Position, 10f, bodyColor, 12);
            Draw.Circle(Position, 6f, Color.White * 0.6f, 8);
        }
    }
}
