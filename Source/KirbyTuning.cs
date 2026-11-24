using System;
using Monocle;

namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// Kirby's gameplay tuning values in one place, read from
    /// <c>Tuning/Pusheen2026/KHM/KirbyTuning.yaml</c> in the mod folder (Sakurai's
    /// "one sortable sheet" tip). Every property defaults to the value the code
    /// shipped with, so a missing file, a missing key or a broken file all fall back
    /// to normal behaviour.
    /// <para>
    /// Edit the YAML and run <c>khm_reload_tuning</c> in the debug console (~) to
    /// apply changes without restarting.
    /// </para>
    /// </summary>
    public class KirbyTuning
    {
        /// <summary>Everest asset path (the .yaml extension is stripped).</summary>
        public const string AssetPath = "Tuning/Pusheen2026/KHM/KirbyTuning";

        public static KirbyTuning Current { get; private set; } = new();

        // ── Float ──────────────────────────────────────────────────────────
        public float KirbyFloatSpeed { get; set; } = -35f;
        public float KirbyFloatFallSpeed { get; set; } = 7f;
        public float KirbyFloatGravity { get; set; } = 45f;
        public float KirbyFloatHSpeed { get; set; } = 80f;
        public float KirbyFloatHAccel { get; set; } = 550f;
        public float KirbyAirPuffSpeed { get; set; } = 120f;
        public int KirbyFloatMaxEntryCost { get; set; } = 3;
        /// <summary>Default float seconds per airtime (K_KirbyFloatTrigger can override it).</summary>
        public float KirbyFloatMaxDuration { get; set; } = 1.5f;

        // ── Wave dash ──────────────────────────────────────────────────────
        public float KirbyWaveDashWindowTime { get; set; } = 0.15f;
        public float KirbyWaveDashSpeedPerChain { get; set; } = 45f;
        public float KirbyWaveDashMaxBonusSpeed { get; set; } = 220f;
        public float KirbyWaveDashChainResetTime { get; set; } = 0.5f;
        public int KirbyWaveDashMaxChain { get; set; } = 5;
        public float KirbyWaveDashJumpWindowTime { get; set; } = 0.25f;

        // ── Inhale ─────────────────────────────────────────────────────────
        public float KirbyInhaleRange { get; set; } = 80f;
        public float KirbyInhalePullSpeed { get; set; } = 200f;
        public float KirbyInhaleTime { get; set; } = 1.5f;
        public float KirbyInhaleMinPullFraction { get; set; } = 0.4f;
        public float KirbyInhaleThreatRadius { get; set; } = 24f;
        public float KirbyInhaleSecondaryPull { get; set; } = 0.6f;
        public float KirbyInhaleAimTilt { get; set; } = 0.6f;

        // ── Star spit ──────────────────────────────────────────────────────
        public float KirbyStarSpitSpeed { get; set; } = 300f;
        public int KirbyStarSpitDamage { get; set; } = 2;
        public float ThrowRecoil { get; set; } = 80f;
        public float StarEnemyHitPadding { get; set; } = 4f;
        public float KirbyHitStreakWindow { get; set; } = 3f;

        // ── Hit-stop and shake ─────────────────────────────────────────────
        public float HitStopDealtBase { get; set; } = 0.03f;
        public float HitStopPerDamage { get; set; } = 0.01f;
        public float HitStopTaken { get; set; } = 0.04f;
        public float HitStopMax { get; set; } = 0.06f;
        public float HitShakeExtra { get; set; } = 0.05f;
        public float HitShakeAmplitude { get; set; } = 2f;

        // ── Taking damage ──────────────────────────────────────────────────
        public float KirbyHurtKnockbackX { get; set; } = 120f;
        public float KirbyHurtKnockbackY { get; set; } = -90f;
        public float KnockbackPerExtraDamage { get; set; } = 0.25f;
        public float KnockbackPerMissingHP { get; set; } = 0.5f;
        public float KnockbackMaxScale { get; set; } = 1.75f;
        public int SpikeDamage { get; set; } = 1;

        /// <summary>(Re)reads the YAML. Keeps the defaults if it's missing or invalid.</summary>
        public static void Load()
        {
            try
            {
                if (Everest.Content.TryGet<AssetTypeYaml>(AssetPath, out ModAsset asset)
                    && asset.TryDeserialize(out KirbyTuning loaded) && loaded != null)
                {
                    Current = loaded;
                    Logger.Log(LogLevel.Info, "KirbyHelperMechanics", "Loaded Kirby tuning from " + AssetPath + ".yaml");
                    return;
                }
                Logger.Log(LogLevel.Warn, "KirbyHelperMechanics", "No Kirby tuning file at " + AssetPath + ".yaml, using defaults");
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "KirbyHelperMechanics", "Couldn't read Kirby tuning, using defaults: " + e.Message);
            }
            Current = new KirbyTuning();
        }

        [Command("khm_reload_tuning", "Reload Kirby Helper Mechanics' KirbyTuning.yaml")]
        public static void ReloadCommand()
        {
            Load();
            Engine.Commands.Log("Kirby tuning reloaded.");
        }
    }
}
