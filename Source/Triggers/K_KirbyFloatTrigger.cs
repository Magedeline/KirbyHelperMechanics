using Celeste.Mod.KirbyHelperMechanics;
using Microsoft.Xna.Framework;

namespace Celeste.Triggers
{
    /// <summary>
    /// Lets a mapper decide how much Kirby can float in a part of the map: turn
    /// Float off, or change its time budget and flap count. Free flight trivialises
    /// precision platforming, so this keeps the mapper in control of it room by
    /// room instead of one global value.
    /// <para>
    /// The rule is stored in the Session (see <see cref="KirbyFloatRules"/>), so it
    /// survives death, respawn and save-and-quit until another trigger changes it.
    /// </para>
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_KirbyFloatTrigger")]
    public class K_KirbyFloatTrigger : Trigger
    {
        private readonly bool floatEnabled;
        private readonly float maxDuration;
        private readonly int maxFlaps;
        private readonly bool resetOnLeave;

        public K_KirbyFloatTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            floatEnabled = data.Bool("floatEnabled", true);
            maxDuration = data.Float("maxDuration", KirbyFloatRules.DefaultMaxDuration);
            maxFlaps = data.Int("maxFlaps", 0);
            resetOnLeave = data.Bool("resetOnLeave", false);
        }

        public override void OnEnter(global::Celeste.Player player)
        {
            base.OnEnter(player);
            if (Scene is Level level)
                KirbyFloatRules.Set(level.Session, floatEnabled, maxDuration, maxFlaps);
        }

        public override void OnLeave(global::Celeste.Player player)
        {
            base.OnLeave(player);
            if (resetOnLeave && Scene is Level level)
                KirbyFloatRules.Clear(level.Session);
        }
    }
}
