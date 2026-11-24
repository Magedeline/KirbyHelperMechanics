using Celeste.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// Makes vanilla <see cref="Spikes"/> take Kirby HP instead of killing him outright.
    /// <para>
    /// The hook sits on <c>Player.Die</c>, but only while a <c>Spikes.OnCollide</c> call is
    /// on the stack. That way vanilla still decides whether the spikes hit at all (they
    /// only hurt when you move into them, e.g. falling onto upward spikes), and every
    /// other cause of death (pits, crushing, spinners) is left alone.
    /// </para>
    /// </summary>
    internal static class KirbySpikeHooks
    {
        // The Spikes whose OnCollide is currently running, or null.
        private static Spikes collidingSpikes;

        internal static void Load()
        {
            On.Celeste.Spikes.OnCollide += OnSpikesCollide;
            On.Celeste.Player.Die += OnPlayerDie;
        }

        internal static void Unload()
        {
            On.Celeste.Spikes.OnCollide -= OnSpikesCollide;
            On.Celeste.Player.Die -= OnPlayerDie;
            collidingSpikes = null;
        }

        private static void OnSpikesCollide(On.Celeste.Spikes.orig_OnCollide orig, Spikes self, global::Celeste.Player player)
        {
            collidingSpikes = self;
            try
            {
                orig(self, player);
            }
            finally
            {
                collidingSpikes = null;
            }
        }

        private static PlayerDeadBody OnPlayerDie(On.Celeste.Player.orig_Die orig, global::Celeste.Player self,
            Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            Spikes spikes = collidingSpikes;
            if (spikes == null || self.Dead || evenIfInvincible || SaveData.Instance.Assists.Invincible
                || self.Get<KirbyPlayerController>() == null)
                return orig(self, direction, evenIfInvincible, registerDeathInStats);

            var manager = K_PlayerHealthManager.Instance;
            // No HP pool, or the pool is already empty: this is the lethal call that
            // TryDamagePlayer makes itself (it re-enters here while the flag is set).
            if (manager == null || manager.IsDead)
                return orig(self, direction, evenIfInvincible, registerDeathInStats);

            // Still blinking from an earlier hit: the spikes do nothing.
            if (manager.IsInvulnerable)
                return null;

            // Put the "source" on the spike side of Kirby so the knockback pushes him
            // out along the spikes' facing (up off floor spikes, away from wall spikes).
            Vector2 source = self.Center - SpikeOutward(spikes.Direction) * 8f;
            K_PlayerHealthManager.TryDamagePlayer(1, source);
            return null;
        }

        private static Vector2 SpikeOutward(Spikes.Directions dir) => dir switch
        {
            Spikes.Directions.Up => -Vector2.UnitY,
            Spikes.Directions.Down => Vector2.UnitY,
            Spikes.Directions.Left => -Vector2.UnitX,
            _ => Vector2.UnitX,
        };
    }
}
