using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby-flavored Arrow/Move Block -- accelerates in a fixed direction
    /// once a player stands on it, until it hits a solid or leaves the level
    /// bounds, then breaks and respawns at its start position. Simplified vs
    /// vanilla's MoveBlock: no steering, no debris particles. Mirrors the
    /// shape of CEL's own CustomMoveBlock.Controller coroutine (MoveHCollideSolids/
    /// MoveVCollideSolids + HasPlayerRider), just without the steering/debris.
    /// Placeholder colored-rect rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_ArrowBlock")]
    [Tracked]
    public class K_ArrowBlock : Solid
    {
        public enum Directions { Left, Right, Up, Down }

        private static readonly Color IdleColor = Calc.HexToColor("474070");
        private static readonly Color MovingColor = Calc.HexToColor("30b335");
        private static readonly Color BreakingColor = Calc.HexToColor("cc2541");

        private const float Accel = 300f;
        private const float MoveSpeed = 60f;
        private const float RespawnTime = 2.5f;

        private readonly Directions direction;
        private readonly Vector2 startPosition;
        private readonly Vector2 moveDir;

        private float speed;
        private Color fillColor = IdleColor;

        public K_ArrowBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            direction = data.Enum("direction", Directions.Right);
            startPosition = Position;
            Depth = -1;

            moveDir = direction switch
            {
                Directions.Left => -Vector2.UnitX,
                Directions.Right => Vector2.UnitX,
                Directions.Up => -Vector2.UnitY,
                _ => Vector2.UnitY,
            };

            Add(new Coroutine(Controller()));
        }

        private IEnumerator Controller()
        {
            while (true)
            {
                fillColor = IdleColor;
                while (!HasPlayerRider())
                    yield return null;

                Audio.Play("event:/Celestellaris/game/06_stronghold/arrowblock_activate", Position);
                StartShaking(0.2f);
                yield return 0.2f;

                fillColor = MovingColor;
                speed = 0f;

                bool blocked = false;
                while (!blocked)
                {
                    speed = Calc.Approach(speed, MoveSpeed, Accel * Engine.DeltaTime);
                    Vector2 amount = moveDir * speed * Engine.DeltaTime;

                    blocked = direction == Directions.Left || direction == Directions.Right
                        ? MoveHCollideSolids(amount.X, false)
                        : MoveVCollideSolids(amount.Y, false);

                    if (OutOfBounds())
                        blocked = true;

                    yield return null;
                }

                Audio.Play("event:/Celestellaris/game/06_stronghold/arrowblock_break", Position);
                fillColor = BreakingColor;
                StartShaking(0.2f);
                yield return 0.2f;

                Visible = false;
                Collidable = false;
                yield return RespawnTime;

                MoveTo(startPosition);
                Collidable = true;
                Visible = true;
                Audio.Play("event:/Celestellaris/game/06_stronghold/arrowblock_reappear", Position);
            }
        }

        private bool OutOfBounds()
        {
            if (Scene is not Level level)
                return false;

            Rectangle bounds = level.Bounds;
            return Left < bounds.Left || Top < bounds.Top || Right > bounds.Right || Bottom > bounds.Bottom + 32;
        }

        public override void Render()
        {
            Draw.Rect(Collider, fillColor);
            Draw.HollowRect(Collider, Color.Black * 0.6f);
            Draw.Line(Center, Center + moveDir * (Math.Min(Width, Height) / 2f - 2f), Color.White, 2f);
        }
    }
}
