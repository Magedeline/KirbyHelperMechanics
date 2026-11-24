using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// Shakes whoever got hit during Kirby's hit-stop (Sakurai: the victim shakes, the
    /// attacker doesn't; sideways on the ground, up-down in the air; the shake decays).
    /// <para>
    /// Works on any entity without touching its code: the offset is added to the
    /// entity's Position only around <c>GameplayRenderer.Render</c> and removed again
    /// right after, so collision and game logic never see it. It has to live in a
    /// render hook because during <c>Celeste.Freeze</c> the scene does not update at
    /// all -- only rendering keeps running.
    /// </para>
    /// </summary>
    internal static class KirbyHitShake
    {
        private static float Amplitude => KirbyTuning.Current.HitShakeAmplitude; // pixels at the start of the shake

        private sealed class Shake
        {
            public Entity Target;
            public Vector2 Axis;
            public float Duration;
            public float Elapsed;
        }

        private static readonly List<Shake> shakes = new();

        internal static void Load() => On.Celeste.GameplayRenderer.Render += OnGameplayRender;

        internal static void Unload()
        {
            On.Celeste.GameplayRenderer.Render -= OnGameplayRender;
            shakes.Clear();
        }

        /// <summary>Forget every shake (level exit: those entities are gone).</summary>
        internal static void Clear() => shakes.Clear();

        /// <summary>Shake <paramref name="target"/> for <paramref name="duration"/> seconds of real time.</summary>
        internal static void Start(Entity target, float duration)
        {
            if (target == null || duration <= 0f)
                return;

            // On the ground the shake is sideways, in the air it's vertical.
            bool grounded = target is Actor actor && actor.Scene != null && actor.OnGround();
            Vector2 axis = grounded ? Vector2.UnitX : Vector2.UnitY;

            foreach (var s in shakes)
            {
                if (s.Target == target)
                {
                    // Re-hit while still shaking: restart instead of stacking.
                    s.Axis = axis;
                    s.Duration = duration;
                    s.Elapsed = 0f;
                    return;
                }
            }

            shakes.Add(new Shake { Target = target, Axis = axis, Duration = duration });
        }

        private static void OnGameplayRender(On.Celeste.GameplayRenderer.orig_Render orig, GameplayRenderer self, Scene scene)
        {
            if (shakes.Count == 0)
            {
                orig(self, scene);
                return;
            }

            // Whole-pixel offsets, so "+= offset" then "-= offset" restores the exact
            // original float position.
            var offsets = new Vector2[shakes.Count];
            for (int i = 0; i < shakes.Count; i++)
            {
                var s = shakes[i];
                if (s.Target.Scene != scene || s.Elapsed >= s.Duration)
                    continue;

                float decay = 1f - s.Elapsed / s.Duration;
                float px = (float)Math.Ceiling(Amplitude * decay);
                // Flip side every rendered frame.
                float sign = ((int)(s.Elapsed * 60f) % 2 == 0) ? 1f : -1f;
                offsets[i] = s.Axis * px * sign;
                s.Target.Position += offsets[i];
            }

            try
            {
                orig(self, scene);
            }
            finally
            {
                for (int i = 0; i < offsets.Length; i++)
                    shakes[i].Target.Position -= offsets[i];

                // Real time, not DeltaTime: the scene clock is stopped during the freeze.
                for (int i = shakes.Count - 1; i >= 0; i--)
                {
                    var s = shakes[i];
                    s.Elapsed += Engine.RawDeltaTime;
                    if (s.Elapsed >= s.Duration || s.Target.Scene == null)
                        shakes.RemoveAt(i);
                }
            }
        }
    }
}
