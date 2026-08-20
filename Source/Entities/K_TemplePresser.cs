using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Momentary pressure-plate switch -- its paired flag is true only while
    /// the player is standing on it (unlike K_TouchSwitch, which latches
    /// permanently). Drives K_TempleGateDoor. Placeholder colored-rect
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_TemplePresser")]
    [Tracked]
    public class K_TemplePresser : Entity
    {
        private readonly string flag;
        private bool pressed;

        public K_TemplePresser(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            flag = data.Attr("flag", "temple_presser");
            Collider = new Hitbox(16f, 4f, -8f, -4f);
            Depth = -50;
        }

        public override void Update()
        {
            base.Update();

            bool nowPressed = CollideCheck<global::Celeste.Player>(Position - Vector2.UnitY);
            if (nowPressed != pressed)
            {
                pressed = nowPressed;
                if (Scene is Level level)
                {
                    level.Session.SetFlag(flag, pressed);
                    Audio.Play(pressed ? "event:/Celestellaris/game/general/touchswitch_any" : "event:/Celestellaris/game/general/touchswitch_gate_finish", Position);
                }
            }
        }

        public override void Render()
        {
            Draw.Rect(Position - new Vector2(8f, 4f), 16f, 4f, pressed ? Calc.HexToColor("ffe866") : Calc.HexToColor("6b6b6b"));
        }
    }
}
