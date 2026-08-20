using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Solid gate that opens (slides to its node) permanently once its paired
    /// K_TouchSwitch flag is set. Placeholder colored-rect rendering stands
    /// in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_SwitchGate")]
    [Tracked]
    public class K_SwitchGate : Solid
    {
        private static readonly Color BodyColor = Calc.HexToColor("8f8f8f");
        private static readonly Color LitColor = Calc.HexToColor("ffe866");

        private const float OpenDuration = 1.2f;

        private readonly string flag;
        private readonly Vector2 start;
        private readonly Vector2 open;
        private bool opening;

        public K_SwitchGate(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            flag = data.Attr("flag", "touch_switch");
            start = Position;
            open = data.Nodes.Length > 0 ? data.NodesOffset(offset)[0] : start;
            Depth = -9999;
        }

        public override void Update()
        {
            base.Update();

            if (!opening && Scene is Level level && level.Session.GetFlag(flag))
                Add(new Coroutine(OpenRoutine()));
        }

        private IEnumerator OpenRoutine()
        {
            opening = true;
            Audio.Play("event:/Celestellaris/game/general/touchswitch_gate_open", Position);

            for (float t = 0f; t < 1f; t += Engine.DeltaTime / OpenDuration)
            {
                MoveTo(Vector2.Lerp(start, open, Ease.SineInOut(t)));
                yield return null;
            }
            MoveTo(open);
        }

        public override void Render()
        {
            Draw.Rect(Collider, opening ? LitColor : BodyColor);
            Draw.HollowRect(Collider, Color.Black * 0.5f);
        }
    }
}
