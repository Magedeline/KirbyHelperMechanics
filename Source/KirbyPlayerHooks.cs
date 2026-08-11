// Replaces ShadowPlayerHooks.cs + K_PlayerRespawnHooks.cs + K_Player.cs's own
// Added()-time takeover logic. There is no separate Kirby Actor and no hidden
// shadow Player anymore -- Kirby is just a KirbyPlayerController Component
// living on whichever real vanilla Celeste.Player the level currently has, so
// the only job left is: keep that component attached/detached to match
// PlayerSelectionManager's current selection.
//
// On.Celeste.Player.Added fires for every *new* Player instance a level ever
// gets -- initial entry, death/respawn, checkpoint reload, teleport, Save &
// Quit resume (all of which route through Level.LoadLevel, which always
// constructs a fresh Player) -- but deliberately does NOT re-fire on an
// ordinary room-to-room transition, since vanilla keeps the same Player
// instance alive across those. That's exactly the set of moments
// K_PlayerRespawnHooks used to have to re-create K_Player for, so no separate
// Everest.Events.Level.OnLoadLevel hook is needed here.
using Celeste.Entities;
using Monocle;

namespace Celeste.Mod.KirbyHelperMechanics
{
    internal static class KirbyPlayerHooks
    {
        private static bool loaded;

        internal static void Load()
        {
            if (loaded)
                return;

            On.Celeste.Player.Added += OnPlayerAdded;
            On.Celeste.Player.Render += OnPlayerRender;
            On.Celeste.Player.Update += OnPlayerUpdate;
            On.Celeste.Player.NormalUpdate += OnNormalUpdate;
            PlayerSelectionManager.OnPlayerSelectionChanged += OnPlayerSelectionChanged;

            KirbyPlayerController.Load();
            loaded = true;
        }

        internal static void Unload()
        {
            if (!loaded)
                return;

            On.Celeste.Player.Added -= OnPlayerAdded;
            On.Celeste.Player.Render -= OnPlayerRender;
            On.Celeste.Player.Update -= OnPlayerUpdate;
            On.Celeste.Player.NormalUpdate -= OnNormalUpdate;
            PlayerSelectionManager.OnPlayerSelectionChanged -= OnPlayerSelectionChanged;

            KirbyPlayerController.Unload();
            loaded = false;
        }

        private static void OnPlayerAdded(On.Celeste.Player.orig_Added orig, global::Celeste.Player self, Scene scene)
        {
            orig(self, scene);

            if (scene is Level level)
            {
                PlayerSelectionManager.GetOrCreate(level);
            }

            SyncKirbyState(self);
        }

        private static void OnPlayerSelectionChanged(PlayerSelectionManager.PlayerType _)
        {
            // Lets a mid-level selection change (e.g. K_PlayerTrigger, or an
            // in-game settings toggle) take effect immediately on whatever
            // Player instance is already live, without waiting for a respawn.
            if (Engine.Scene is Level level)
            {
                var player = level.Tracker.GetEntity<global::Celeste.Player>();
                if (player != null)
                    SyncKirbyState(player);
            }
        }

        /// <summary>
        /// Adds or removes <see cref="KirbyPlayerController"/> on <paramref name="player"/>
        /// so it matches <see cref="PlayerSelectionManager"/>'s current selection.
        /// Safe to call redundantly -- it no-ops if already in the desired state.
        /// </summary>
        internal static void SyncKirbyState(global::Celeste.Player player)
        {
            if (player == null)
                return;

            bool shouldBeKirby = PlayerSelectionManager.IsPlayerSelected(PlayerSelectionManager.PlayerType.Kirby);
            var existing = player.Get<KirbyPlayerController>();

            if (shouldBeKirby && existing == null)
            {
                player.Add(new KirbyPlayerController());
            }
            else if (!shouldBeKirby && existing != null)
            {
                player.Remove(existing);
            }
        }

        /// <summary>
        /// Draws the Kirby sprite overlay in place of vanilla Madeline. Hides the
        /// real Sprite for the duration of vanilla's own Render() (so DeathEffect,
        /// StarFly transform draws, silhouette handling, etc. all still run
        /// unmodified) and draws KirbySprite/Shoes on top afterwards.
        /// </summary>
        private static void OnPlayerRender(On.Celeste.Player.orig_Render orig, global::Celeste.Player self)
        {
            var kpc = self.Get<KirbyPlayerController>();
            if (kpc == null)
            {
                orig(self);
                return;
            }

            bool wasVisible = self.Sprite.Visible;
            self.Sprite.Visible = false;
            orig(self);
            self.Sprite.Visible = wasVisible;

            kpc.RenderKirbyOverlay();
            if (kpc.IsInhaling)
                kpc.RenderInhaleTendrils();
        }

        /// <summary>
        /// Wraps every Player.Update() call so KirbyPlayerController can compute
        /// its rising-edge jump-press tracking (see the field comments in
        /// KirbyPlayerController's Kirby Float region) at frame-accurate times
        /// relative to orig(self) -- which is what actually runs
        /// StateMachine.Update() / NormalUpdate this frame -- rather than relying
        /// on Monocle's Component-update ordering, which is not guaranteed to run
        /// this component before or after StateMachine's own Update().
        /// </summary>
        private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, global::Celeste.Player self)
        {
            var kpc = self.Get<KirbyPlayerController>();
            kpc?.PreUpdate();
            orig(self);
            kpc?.PostUpdate();
        }

        /// <summary>
        /// Checks Kirby's ability entry conditions BEFORE vanilla's own
        /// NormalUpdate runs -- entry must preempt vanilla's own handling of the
        /// same button press (jump/wall-jump, grab/climb), not react after the
        /// fact once vanilla has already consumed it. Mirrors the legacy priority
        /// ladder's ordering (Grab-button abilities before Jump-button abilities).
        /// Falls through to unmodified vanilla NormalUpdate (dash, climb, jump,
        /// wall jump, ducking, pickup, ...) whenever no Kirby ability should take
        /// over this frame.
        /// </summary>
        private static int OnNormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, global::Celeste.Player self)
        {
            var kpc = self.Get<KirbyPlayerController>();
            if (kpc != null)
            {
                if (kpc.CheckInhaleEntry())
                    return kpc.StKirbyInhale;
                if (kpc.CheckFloatEntry())
                    return kpc.StKirbyFloat;
            }

            return orig(self);
        }
    }
}
