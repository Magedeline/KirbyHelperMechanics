using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Zip Mover -- a Solid that rides a track to a node and
    /// back once a player stands on it. Placeholder colored-rect rendering
    /// (rope line + body) stands in for real art until that exists. Built on
    /// Solid.MoveTo rather than vanilla's own sealed ZipMover (see the CEL
    /// mod's PuzzleEntities.TeleportCrate for the same MoveTo pattern this
    /// mirrors).
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_ZipMover")]
    [Tracked]
    public class K_ZipMover : Solid
    {
        private static readonly Color BodyColor = Calc.HexToColor("b23cff");
        private static readonly Color RopeColor = Calc.HexToColor("6b1fa3");

        private const float TravelTime = 1.2f;
        private const float PauseAtTarget = 0.8f;
        private const float PauseAtStart = 0.4f;

        private readonly Vector2 start;
        private readonly Vector2 target;

        public K_ZipMover(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            start = Position;
            target = data.Nodes.Length > 0 ? data.NodesOffset(offset)[0] : start;
            Depth = -9000; // read above most terrain, like vanilla's ZipMover

            Add(new Coroutine(Sequence()));
        }

        public override void Render()
        {
            Vector2 halfSize = new Vector2(Width, Height) / 2f;
            Draw.Line(start + halfSize, target + halfSize, RopeColor, 3f);
            Draw.Rect(Collider, BodyColor);
            Draw.HollowRect(Collider, Color.White * 0.6f);
        }

        private IEnumerator Sequence()
        {
            while (true)
            {
                while (GetPlayerRider() == null)
                    yield return null;

                yield return PauseAtStart;
                yield return TravelRoutine(start, target);

                yield return PauseAtTarget;
                yield return TravelRoutine(target, start);
            }
        }

        private IEnumerator TravelRoutine(Vector2 from, Vector2 to)
        {
            Audio.Play("event:/Celestellaris/new_content/game/19_spaces/zip_mover", Position);
            for (float t = 0f; t < 1f; t += Engine.DeltaTime / TravelTime)
            {
                MoveTo(Vector2.Lerp(from, to, Ease.SineInOut(t)));
                yield return null;
            }
            MoveTo(to);
        }
    }
}
