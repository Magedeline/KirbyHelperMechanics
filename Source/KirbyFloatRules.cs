using System;

namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// The float rules currently in force, set by K_KirbyFloatTrigger. Stored in
    /// Session flags/counters (no custom session class needed), so they persist
    /// through death, respawn and save-and-quit. With no override set, Kirby uses
    /// the defaults: float on, 1.5s per airtime, flaps from the mod setting.
    /// </summary>
    internal static class KirbyFloatRules
    {
        public static float DefaultMaxDuration => KirbyTuning.Current.KirbyFloatMaxDuration;

        private const string OverrideFlag = "KirbyHelperMechanics_floatOverride";
        private const string DisabledFlag = "KirbyHelperMechanics_floatDisabled";
        // Counters are ints, so the duration is kept in hundredths of a second.
        private const string DurationCounter = "KirbyHelperMechanics_floatDurationCs";
        private const string FlapsCounter = "KirbyHelperMechanics_floatMaxFlaps";

        public static void Set(Session session, bool enabled, float maxDuration, int maxFlaps)
        {
            session.SetFlag(OverrideFlag, true);
            session.SetFlag(DisabledFlag, !enabled);
            session.SetCounter(DurationCounter, (int)Math.Round(Math.Max(0f, maxDuration) * 100f));
            session.SetCounter(FlapsCounter, Math.Max(0, maxFlaps));
        }

        public static void Clear(Session session)
        {
            session.SetFlag(OverrideFlag, false);
            session.SetFlag(DisabledFlag, false);
        }

        public static bool Enabled(Session session) =>
            session == null || !session.GetFlag(OverrideFlag) || !session.GetFlag(DisabledFlag);

        public static float MaxDuration(Session session) =>
            session != null && session.GetFlag(OverrideFlag)
                ? session.GetCounter(DurationCounter) / 100f
                : DefaultMaxDuration;

        /// <summary>Trigger value if one is set (0 there means "use the mod setting").</summary>
        public static int MaxFlaps(Session session)
        {
            int setting = KirbyHelperMechanicsModule.Settings?.KirbyMaxFloatJumps ?? 5;
            if (session == null || !session.GetFlag(OverrideFlag))
                return setting;
            int flaps = session.GetCounter(FlapsCounter);
            return flaps > 0 ? flaps : setting;
        }
    }
}
