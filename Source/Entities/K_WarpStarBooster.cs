using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby's Warp Star as a booster -- touching it flies the player along a
    /// curve to a linked node at high speed (a simplified stand-in for a full
    /// cutscene-style warp sequence), then hands control back. Placeholder
    /// star-shaped rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_WarpStarBooster")]
    [Tracked]
    public class K_WarpStarBooster : Entity
    {
        private static readonly Color BodyColor = Calc.HexToColor("ffe866");
        private static readonly Color TrailColor = Calc.HexToColor("ff9a3c");

        private const float FlightDuration = 0.8f;

        private readonly Vector2 target;
        private bool used;

        public K_WarpStarBooster(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            target = data.Nodes.Length > 0 ? data.NodesOffset(offset)[0] : Position;
            Collider = new Circle(9f);
            Depth = -8500;

            Add(new PlayerCollider(OnPlayer));
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (used)
                return;

            Add(new Coroutine(WarpRoutine(player)));
        }

        private IEnumerator WarpRoutine(global::Celeste.Player player)
        {
            used = true;
            Collidable = false;

            player.StateMachine.State = global::Celeste.Player.StDummy;
            Vector2 from = player.Position;
            Audio.Play("event:/Celestellaris/game/general/cassette_bubblereturn", Position);

            for (float t = 0f; t < 1f; t += Engine.DeltaTime / FlightDuration)
            {
                player.Position = Vector2.Lerp(from, target, Ease.CubeOut(t));
                player.Speed = Vector2.Zero;
                if (Scene.OnInterval(0.02f))
                    (Scene as Level)?.ParticlesFG.Emit(global::Celeste.Player.P_DashA, 2, player.Center, Vector2.One * 3f);
                yield return null;
            }

            player.Position = target;
            player.StateMachine.State = global::Celeste.Player.StNormal;
        }

        public override void Render()
        {
            Draw.Circle(Position, 9f, BodyColor, 5);
            Draw.Circle(Position, 4f, TrailColor, 5);
        }
    }
}
