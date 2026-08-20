using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Solid door that stays open (non-collidable) only while its paired
    /// K_TemplePresser flag is true, and re-blocks the instant it isn't --
    /// unlike K_SwitchGate, which opens permanently. Placeholder colored-rect
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_TempleGateDoor")]
    [Tracked]
    public class K_TempleGateDoor : Solid
    {
        private static readonly Color BodyColor = Calc.HexToColor("5a4632");
        private static readonly Color OpenColor = Calc.HexToColor("caa46b");

        private readonly string flag;

        public K_TempleGateDoor(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            flag = data.Attr("flag", "temple_presser");
            Depth = -9998;
        }

        public override void Update()
        {
            base.Update();
            bool open = Scene is Level level && level.Session.GetFlag(flag);
            Collidable = !open;
            Visible = !open;
        }

        public override void Render()
        {
            Draw.Rect(Collider, BodyColor);
            Draw.HollowRect(Collider, OpenColor);
        }
    }
}
