using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Crystal-style touch switch -- lights up permanently (via a Level
    /// Session flag) when the player touches it. K_SwitchGate entities
    /// sharing the same flag open once it's set. Simplified vs vanilla:
    /// activates on touch, not proximity-while-dashing. Placeholder circle
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_TouchSwitch")]
    [Tracked]
    public class K_TouchSwitch : Entity
    {
        private readonly string flag;
        private bool activated;

        public K_TouchSwitch(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            flag = data.Attr("flag", "touch_switch");
            Collider = new Circle(7f);
            Depth = -2000;

            Add(new PlayerCollider(OnPlayer));
        }

        public override void Update()
        {
            base.Update();
            if (!activated && Scene is Level level && level.Session.GetFlag(flag))
                activated = true;
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (activated || Scene is not Level level)
                return;

            activated = true;
            level.Session.SetFlag(flag, true);
            Audio.Play("event:/Celestellaris/game/general/touchswitch_any", Position);
            level.Flash(Color.White * 0.3f);
        }

        public override void Render()
        {
            Color color = activated ? Calc.HexToColor("ffe866") : Calc.HexToColor("6b6b6b");
            Draw.Circle(Position, 7f, color, 8);
        }
    }
}
