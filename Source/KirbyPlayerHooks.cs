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
using Microsoft.Xna.Framework;
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
            On.Celeste.Player.DashUpdate += OnDashUpdate;
            On.Celeste.Player.StartDash += OnStartDash;
            On.Celeste.Player.Die += OnPlayerDie;
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool += OnTrailManagerAdd;
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
            On.Celeste.Player.DashUpdate -= OnDashUpdate;
            On.Celeste.Player.StartDash -= OnStartDash;
            On.Celeste.Player.Die -= OnPlayerDie;
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool -= OnTrailManagerAdd;
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

            CheckTogglePlayerButton(self);
        }

        /// <summary>
        /// Manual Kirby/Madeline swap bound to
        /// KirbyHelperMechanicsModuleSettings.ToggleKirbyMadelineButton. Checked
        /// unconditionally on every Player.Update (unlike the Kirby-only ability
        /// checks above) since it has to work while playing as Madeline too, not
        /// just while a KirbyPlayerController is already attached. Mirrors
        /// K_PlayerTrigger's own swap: SetLevelOverride so the swap is scoped to
        /// the current level like a manual trigger touch, followed by a direct
        /// SyncKirbyState call so it takes effect immediately rather than
        /// waiting on the OnPlayerSelectionChanged event's own tracker lookup.
        /// </summary>
        private static void CheckTogglePlayerButton(global::Celeste.Player self)
        {
            if (KirbyHelperMechanicsModule.Settings?.ToggleKirbyMadelineButton?.Pressed != true)
                return;

            var next = PlayerSelectionManager.GetSelectedPlayer() == PlayerSelectionManager.PlayerType.Kirby
                ? PlayerSelectionManager.PlayerType.Madeline
                : PlayerSelectionManager.PlayerType.Kirby;

            PlayerSelectionManager.SetLevelOverride(next);
            SyncKirbyState(self);
        }

        /// <summary>
        /// Retints the death shatter effect from Madeline's hair color to
        /// Kirby's own dash-tier color. PlayerDeadBody captures self.Hair.Color
        /// into its own field at construction time (inside orig()) and passes
        /// that captured value to DeathEffect -- it does not keep a live
        /// reference to Hair -- so swapping the color immediately before orig()
        /// runs and restoring it right after changes only the death effect's
        /// tint, without touching any of vanilla's own death/respawn timing or
        /// logic (which still runs completely unmodified via orig()).
        /// </summary>
        private static global::Celeste.PlayerDeadBody OnPlayerDie(On.Celeste.Player.orig_Die orig, global::Celeste.Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            var kpc = self.Get<KirbyPlayerController>();
            if (kpc == null)
                return orig(self, direction, evenIfInvincible, registerDeathInStats);

            Color originalHairColor = self.Hair.Color;
            self.Hair.Color = KirbyDashColors.GetColor(self.Dashes, (self.Scene as Level)?.TimeActive ?? 0f);
            global::Celeste.PlayerDeadBody result = orig(self, direction, evenIfInvincible, registerDeathInStats);
            self.Hair.Color = originalHairColor;
            return result;
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
                if (kpc.CheckDoubleJumpEntry())
                    return global::Celeste.Player.StNormal;
                if (kpc.CheckFloatEntry())
                    return kpc.StKirbyFloat;
            }

            return orig(self);
        }

        /// <summary>
        /// Same idea as <see cref="OnNormalUpdate"/>, but for the Dash state --
        /// vanilla's own DashUpdate only lets Input.Jump cancel a dash early
        /// while grounded (SuperJump) or against a wall (SuperWallJump), never
        /// in open air, so without this a jump press mid-air-dash is just
        /// silently ignored until the dash times out on its own. Checked BEFORE
        /// orig(self) for the same preempt-the-same-button-press reason as
        /// OnNormalUpdate; falls through to unmodified vanilla DashUpdate
        /// (ground/wall jump-cancel, curve-dash assist, dash particles, ...)
        /// whenever neither check fires.
        /// </summary>
        private static int OnDashUpdate(On.Celeste.Player.orig_DashUpdate orig, global::Celeste.Player self)
        {
            var kpc = self.Get<KirbyPlayerController>();
            if (kpc != null)
            {
                if (kpc.CheckDoubleJumpEntry())
                    return global::Celeste.Player.StNormal;
                if (kpc.CheckFloatEntry())
                    return kpc.StKirbyFloat;
            }

            return orig(self);
        }

        /// <summary>
        /// Refills one Kirby flap after every dash actually starts (ground or
        /// air, including Float's own dash-cancel) -- see
        /// KirbyPlayerController.GrantDashFlapRefill for why this only matters
        /// in the air. Also arms the wave dash buff watch (see
        /// KirbyPlayerController.NotifyKirbyDashStarted) whenever the dash that
        /// just started has a downward component. Runs after orig() so
        /// DashDir/Speed are already set up by the time either call happens,
        /// though ordering doesn't actually matter here since they touch
        /// unrelated state.
        /// </summary>
        private static int OnStartDash(On.Celeste.Player.orig_StartDash orig, global::Celeste.Player self)
        {
            int result = orig(self);
            var kpc = self.Get<KirbyPlayerController>();
            kpc?.GrantDashFlapRefill();
            kpc?.NotifyKirbyDashStarted(self.DashDir);
            return result;
        }

        /// <summary>
        /// Swaps the Image a dash/wall-jump/dream-dash after-image snapshot is
        /// built from when it belongs to a Kirby-controlled Player. Vanilla's own
        /// TrailManager.Add overloads resolve the source image via
        /// entity.Get&lt;PlayerSprite&gt;(), which only ever finds the real Sprite
        /// -- see KirbyPlayerController.PrepareKirbySpriteForTrail for why that's
        /// wrong for Kirby. Also drops the hair snapshot: TrailManager renders it
        /// unconditionally regardless of PlayerHair.Visible, and Kirby's sprite
        /// already includes its own "hair" as part of the body art.
        /// </summary>
        private static global::Celeste.TrailManager.Snapshot OnTrailManagerAdd(On.Celeste.TrailManager.orig_Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool orig, Vector2 position, Image sprite, global::Celeste.PlayerHair hair, Vector2 scale, Color color, int depth, float duration, bool frozenUpdate, bool useRawDeltaTime)
        {
            if (sprite?.Entity is global::Celeste.Player player)
            {
                Sprite kirbySprite = player.Get<KirbyPlayerController>()?.PrepareKirbySpriteForTrail();
                if (kirbySprite != null)
                {
                    sprite = kirbySprite;
                    hair = null;
                }
            }

            return orig(position, sprite, hair, scale, color, depth, duration, frozenUpdate, useRawDeltaTime);
        }
    }
}
