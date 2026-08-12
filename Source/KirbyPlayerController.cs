using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod;
using Celeste.Mod.KirbyHelperMechanics;
using Celeste.Mod.KirbyHelperMechanics.Interop;
using Celeste.Projectiles;

namespace Celeste.Entities
{
    /// <summary>
    /// Component that turns the real vanilla <see cref="global::Celeste.Player"/> into
    /// Kirby: sprite/hitbox reskin, Kirby shoes, and the Kirby HP system. Added to /
    /// removed from the live Player entity by <see cref="KirbyPlayerHooks"/> based on
    /// <see cref="PlayerSelectionManager"/>'s current selection -- there is no separate
    /// Kirby Actor and no hidden "shadow" Player anymore, so vanilla's own triggers,
    /// collisions, room transitions, and cutscene systems all apply to Kirby for free.
    /// <para>
    /// Kirby's own abilities (float/hover, inhale, star-spit) are not implemented here
    /// yet -- until they're ported (see the phased rewrite plan), Kirby plays with
    /// unmodified vanilla Player physics, just reskinned.
    /// </para>
    /// </summary>
    public class KirbyPlayerController : Component
    {
        private global::Celeste.Player player;
        private Level level;

        #region Visuals

        /// <summary>Kirby sprite overlay, drawn in place of the vanilla Madeline sprite. See <see cref="KirbyPlayerHooks"/>'s Render hook.</summary>
        internal Sprite KirbySprite { get; private set; }

        /// <summary>Kirby's feet, rendered under the sprite.</summary>
        internal KirbyShoes Shoes { get; private set; }

        public Vector2 CenterOffset { get; private set; }

        #endregion

        #region Health

        /// <summary>Kirby HP pool size. Matches the legacy K_Player default.</summary>
        public int MaxHealth = 6;

        #endregion

        #region Ability state

        /// <summary>
        /// Custom StateMachine state ID for Kirby Float, registered via
        /// <see cref="Monocle.StateMachine.AddState"/> on the real Player's own
        /// StateMachine in <see cref="Added"/> -- see the Phase 2 spike notes there.
        /// -1 until <see cref="Added"/> has run.
        /// </summary>
        public int StKirbyFloat { get; private set; } = -1;

        /// <summary>Custom StateMachine state ID for Kirby Inhale. -1 until <see cref="Added"/> has run.</summary>
        public int StKirbyInhale { get; private set; } = -1;

        /// <summary>Custom StateMachine state ID for Kirby Star Spit. -1 until <see cref="Added"/> has run.</summary>
        public int StKirbyStarSpit { get; private set; } = -1;

        /// <summary>Is the host player currently floating?</summary>
        public bool IsFlying => player != null && StKirbyFloat >= 0 && player.StateMachine.State == StKirbyFloat;

        /// <summary>Is the host player currently inhaling?</summary>
        public bool IsInhaling => player != null && StKirbyInhale >= 0 && player.StateMachine.State == StKirbyInhale;

        public bool IsMouthOpen => IsInhaling;

        public bool CanInhale => player != null && player.StateMachine.State == global::Celeste.Player.StNormal;

        #endregion

        // Module integration
        private static bool _hooksLoaded = false;

        public static void Load()
        {
            if (_hooksLoaded)
                return;

            Logger.Log(LogLevel.Info, "KHM", "[KirbyPlayerController] Loaded");
            _hooksLoaded = true;
        }

        public static void Unload()
        {
            if (!_hooksLoaded)
                return;

            Logger.Log(LogLevel.Info, "KHM", "[KirbyPlayerController] Unloaded");
            _hooksLoaded = false;
        }

        public KirbyPlayerController()
            : base(active: true, visible: true)
        {
        }

        public override void Added(Entity entity)
        {
            base.Added(entity);
            if (entity is not global::Celeste.Player p)
            {
                throw new InvalidOperationException("KirbyPlayerController must be added to a vanilla Celeste.Player");
            }
            player = p;

            // Kirby's sprite replaces Madeline's visually; vanilla Hair stays alive
            // (StarFly/DeathEffect/etc still reference it) but hidden.
            player.Hair.Visible = false;
            player.Hair.SimulateMotion = false;

            KirbySprite = GFX.SpriteBank.Create(K_PlayerAnimIds.SpriteBankId);
            KirbySprite.Visible = false; // drawn manually by KirbyPlayerHooks' Render hook, not auto-rendered
            Shoes = new KirbyShoes(player);

            // Monocle.StateMachine.AddState(name, onUpdate, coroutine, begin, end)
            // grows the REAL Player's own StateMachine and returns a fresh state ID
            // -- Everest's patched Monocle already exposes this publicly for exactly
            // this purpose (vanilla itself uses the same mechanism internally for
            // its own DLC-added states, e.g. StFlingBird), so no custom
            // reflection/array-growing hack is needed. Must be called per-Player-
            // instance (each Player constructs its own StateMachine), so this lives
            // here in Added() rather than anywhere static/shared.
            StKirbyFloat = player.StateMachine.AddState("KirbyFloat", KirbyFloatUpdate, null, KirbyFloatBegin, KirbyFloatEnd);
            StKirbyInhale = player.StateMachine.AddState("KirbyInhale", KirbyInhaleUpdate, KirbyInhaleCoroutine, KirbyInhaleBegin, KirbyInhaleEnd);
            StKirbyStarSpit = player.StateMachine.AddState("KirbyStarSpit", KirbyStarSpitUpdate, KirbyStarSpitCoroutine, KirbyStarSpitBegin, KirbyStarSpitEnd);
        }

        #region Kirby Float (hover) -- ported from legacy K_Player.KirbyFloatBegin/Update/End

        private const float KirbyFloatSpeed = -35f;
        private const float KirbyFloatFallSpeed = 18f;
        private const float KirbyFloatGravity = 45f;
        private const float KirbyFloatHSpeed = 80f;
        private const float KirbyFloatHAccel = 550f; // legacy: RunAccel(1000f) * .55f
        private const float KirbyFlapScaleTime = 0.12f;
        private const float KirbyAirPuffSpeed = 120f;

        // Total hold-time cap for a single float session, independent of flap
        // charges -- without this a player can glide almost indefinitely at
        // KirbyFloatFallSpeed once flaps run out, since that terminal speed is so
        // low. Resets to full every time float is (re-)entered from grounded.
        private const float KirbyFloatMaxDuration = 2.5f;

        private int kirbyFlapCount;
        private float kirbyFlapScaleTimer;
        private float kirbyFloatDurationTimer;

        // Rising-edge jump detection: Kirby's air abilities must only trigger on a
        // FRESH jump press, not a buffered one (a press made just before landing or
        // reaching a wall is reserved for vanilla jumps so jump buffering still
        // works exactly like Madeline's). Computed in PreUpdate/PostUpdate, called
        // from KirbyPlayerHooks' On.Celeste.Player.Update hook around orig(self) --
        // NOT from this Component's own Update(), because Monocle updates
        // Components in add-order and StateMachine (added in Player's constructor,
        // long before this component exists) would otherwise run NormalUpdate
        // BEFORE this component's Update() recomputed jumpPressedFresh for the
        // frame, reading last frame's stale value.
        private bool prevJumpCheck;
        private bool jumpPressedFresh;

        /// <summary>Public accessor, e.g. for a future HUD or refill entity.</summary>
        public int KirbyFlapCount => kirbyFlapCount;

        /// <summary>Public accessor, e.g. for a future refill entity.</summary>
        public void SetKirbyFlapCount(int value) => kirbyFlapCount = Math.Max(0, value);

        internal void PreUpdate()
        {
            jumpPressedFresh = Input.Jump.Check && !prevJumpCheck;

            // Flaps fully refill while grounded, mirroring the legacy K_Player's
            // per-frame "onGround -> reset to max" behavior.
            if (player.onGround)
                kirbyFlapCount = KirbyHelperMechanicsModule.Settings?.KirbyMaxFloatJumps ?? 5;
        }

        internal void PostUpdate()
        {
            prevJumpCheck = Input.Jump.Check;
        }

        /// <summary>
        /// Kirby Float's entry condition, checked from KirbyPlayerHooks'
        /// On.Celeste.Player.NormalUpdate hook BEFORE orig(self) runs -- entry has
        /// to preempt vanilla's own jump/wall-jump handling of the same button
        /// press, not react after the fact. Consumes the jump buffer on success so
        /// vanilla's own jump handling in orig(self) never also sees this press.
        /// </summary>
        internal bool CheckFloatEntry()
        {
            if (player.onGround || player.Holding != null)
                return false;
            if (!Input.Jump.Pressed || !jumpPressedFresh || player.jumpGraceTimer > 0f)
                return false;
            if (player.WallJumpCheck(1) || player.WallJumpCheck(-1))
                return false;
            if (kirbyFlapCount <= 0)
                return false;

            Input.Jump.ConsumeBuffer();
            return true;
        }

        private void KirbyFloatBegin()
        {
            // Consume one flap on entry.
            kirbyFlapCount = Math.Max(0, kirbyFlapCount - 1);
            kirbyFlapScaleTimer = KirbyFlapScaleTime;
            kirbyFloatDurationTimer = KirbyFloatMaxDuration;

            // Initial upward kick -- preserves stronger upward momentum so
            // jump/dash height carries into the float instead of being clipped.
            player.Speed.Y = Math.Min(player.Speed.Y, KirbyFloatSpeed);
            player.varJumpTimer = 0;

            // Puffed-up squash on entry.
            player.Sprite.Scale = new Vector2(1.3f, 0.75f);
            KirbySprite?.Play(K_PlayerAnimIds.Float);

            // Reuses vanilla's own jump sound as the "puff" cue -- the legacy
            // K_Player played a DZ-only "event:/DZ/char/kirby/jump" FMOD event that
            // this standalone mod doesn't ship a bank for.
            player.Play("event:/char/madeline/jump");

            level?.Particles.Emit(global::Celeste.Player.P_DashA, 3, player.BottomCenter, Vector2.UnitX * 4, Calc.Down);
        }

        private void KirbyFloatEnd()
        {
            player.Sprite.Scale = Vector2.One;
            kirbyFlapScaleTimer = 0f;

            if (!player.onGround)
                level?.Particles.Emit(global::Celeste.Player.P_DashA, 4, player.Center, Vector2.One * 4, Calc.Down);
        }

        private int KirbyFloatUpdate()
        {
            // Animate scale back to puffed-up (wide/short) while floating.
            kirbyFlapScaleTimer -= Engine.DeltaTime;
            player.Sprite.Scale = kirbyFlapScaleTimer > 0f
                ? Vector2.Lerp(new Vector2(1.2f, 0.85f), new Vector2(1.3f, 0.75f), kirbyFlapScaleTimer / KirbyFlapScaleTime)
                : new Vector2(1.2f, 0.85f);

            // Horizontal movement -- slightly floatier than normal.
            player.Speed.X = Calc.Approach(player.Speed.X, KirbyFloatHSpeed * player.moveX, KirbyFloatHAccel * Engine.DeltaTime);

            // Gentle float gravity -- drifts slowly downward.
            player.Speed.Y = Calc.Approach(player.Speed.Y, KirbyFloatFallSpeed, KirbyFloatGravity * Engine.DeltaTime);

            // Hold-time cap: forces an exit back to normal falling once expired,
            // regardless of remaining flaps or held input -- landing or dashing
            // from there on is just the normal next move, same as every other
            // float exit below.
            kirbyFloatDurationTimer -= Engine.DeltaTime;
            if (kirbyFloatDurationTimer <= 0f)
                return global::Celeste.Player.StNormal;

            // Fast-fall: hold down to puff out the air and drop out of float.
            if (Input.MoveY.Value > 0)
            {
                player.Speed.Y = Math.Max(player.Speed.Y, KirbyAirPuffSpeed);
                return global::Celeste.Player.StNormal;
            }

            if (Input.Jump.Pressed)
            {
                // Wall jump escape: floating against a wall jumps off it, exactly
                // like Madeline would.
                if (player.WallJumpCheck(1))
                {
                    player.WallJump(-1);
                    return global::Celeste.Player.StNormal;
                }
                if (player.WallJumpCheck(-1))
                {
                    player.WallJump(1);
                    return global::Celeste.Player.StNormal;
                }

                // Additional flap: press jump again to bounce upward (costs a flap).
                if (kirbyFlapCount > 0)
                {
                    Input.Jump.ConsumeBuffer();
                    kirbyFlapCount = Math.Max(0, kirbyFlapCount - 1);
                    kirbyFlapScaleTimer = KirbyFlapScaleTime;

                    player.Speed.Y = KirbyFloatSpeed;
                    player.Sprite.Scale = new Vector2(1.35f, 0.7f);

                    player.Play("event:/char/madeline/jump");
                    level?.Particles.Emit(global::Celeste.Player.P_DashA, 2, player.BottomCenter, Vector2.UnitX * 4, Calc.Down);
                }
            }

            // Out of flaps and falling faster than the float's terminal speed.
            if (kirbyFlapCount <= 0 && player.Speed.Y > KirbyFloatFallSpeed)
                return global::Celeste.Player.StNormal;

            // Land -- exit float (flap count refills via PreUpdate while grounded).
            if (player.onGround && player.Speed.Y >= 0)
                return global::Celeste.Player.StNormal;

            // Cancel with dash.
            if (Input.Dash.Pressed && player.CanDash)
                return player.StartDash();

            // Cancel with grab to cling to walls.
            if (Input.Grab.Check && player.ClimbCheck((int)player.Facing))
                return global::Celeste.Player.StClimb;

            if (player.moveX != 0)
                player.Facing = (Facings)player.moveX;

            return StKirbyFloat;
        }

        #endregion

        #region Kirby Inhale -- ported from legacy K_Player.KirbyInhaleBegin/Update/End/Coroutine

        private const float KirbyInhaleRange = 80f;
        private const float KirbyInhalePullSpeed = 200f;
        private const float KirbyInhaleTime = 1.5f;
        // Pull strength at max range used to taper all the way to zero (dist ==
        // KirbyInhaleRange -> 0 pull), so a target caught right at the edge of
        // range barely moved at first and often couldn't close the gap before
        // KirbyInhaleTime ran out -- the practical usable range was much smaller
        // than KirbyInhaleRange implied. This floors the falloff instead of
        // letting it hit zero.
        private const float KirbyInhaleMinPullFraction = 0.4f;
        private const int KirbyInhaleTendrilCount = 5;

        private float kirbyInhaleTimer;
        private bool kirbyHasInhaledEnemy;
        private Vector2[] kirbyInhaleTendrils;

        /// <summary>
        /// Kirby Inhale's entry condition, checked from KirbyPlayerHooks'
        /// On.Celeste.Player.NormalUpdate hook BEFORE orig(self) runs, same reason
        /// as <see cref="CheckFloatEntry"/>. Ducking and up+grab are excluded to
        /// mirror the legacy priority ladder, which reserved those combos for
        /// Slide Tackle / Counter Stance -- both out of scope for this port, but
        /// excluding them here keeps Inhale's own trigger condition unchanged if
        /// they're ever added back.
        /// </summary>
        internal bool CheckInhaleEntry()
        {
            if (!Input.Grab.Pressed || player.Holding != null)
                return false;
            if (!player.onGround || player.Ducking)
                return false;
            if (Input.MoveY.Value == -1)
                return false;

            Input.Grab.ConsumeBuffer();
            return true;
        }

        private void KirbyInhaleBegin()
        {
            kirbyInhaleTimer = KirbyInhaleTime;
            player.Speed = Vector2.Zero;

            player.Sprite.Play("idle");
            KirbySprite?.Play(K_PlayerAnimIds.InhaleStart); // chains to InhaleLoop via its own goto
            Input.Rumble(RumbleStrength.Light, RumbleLength.Short);

            CreateKirbyInhaleTendrils();

            // This is the only path into StKirbyInhale (a neutral grab press with
            // nothing already swallowed), so a puff here plays exactly once per entry.
            level?.Add(new KirbyPuff(player.Center, (int)player.Facing));
        }

        private void KirbyInhaleEnd()
        {
            kirbyInhaleTendrils = null;
            // Harmless even when chaining straight into StKirbyStarSpit -- that
            // state's own Begin() plays over this in the same StateMachine.Update().
            KirbySprite?.Play(K_PlayerAnimIds.InhaleEnd);
        }

        private int KirbyInhaleUpdate()
        {
            kirbyInhaleTimer -= Engine.DeltaTime;
            player.Speed.X = Calc.Approach(player.Speed.X, 0, global::Celeste.Player.RunReduce * Engine.DeltaTime);

            if (!player.onGround)
                player.Speed.Y = Calc.Approach(player.Speed.Y, global::Celeste.Player.MaxFall, global::Celeste.Player.Gravity * .3f * Engine.DeltaTime);
            else
                player.Speed.Y = 0;

            // Pull enemies toward the player.
            Vector2 inhaleDir = Vector2.UnitX * (int)player.Facing;
            Vector2 inhaleOrigin = player.Center + inhaleDir * 16f;
            PullAndInhaleEnemies(inhaleOrigin, inhaleDir);
            UpdateKirbyInhaleTendrils(inhaleDir);

            // A successful capture chains straight into Star Spit -- no need to
            // release and re-press Grab, so holding it through the whole
            // inhale-then-spit motion works as one continuous action.
            if (kirbyHasInhaledEnemy)
                return StKirbyStarSpit;

            if (level != null && level.OnInterval(.04f))
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 particlePos = inhaleOrigin + inhaleDir * Calc.Random.Range(8f, KirbyInhaleRange);
                    particlePos.Y += Calc.Random.Range(-8f, 8f);
                    level.Particles.Emit(global::Celeste.Player.P_DashA, particlePos, (-inhaleDir).Angle());
                }
            }

            if (!Input.Grab.Check || kirbyInhaleTimer <= 0)
                return global::Celeste.Player.StNormal;

            if (player.CanDash)
                return player.StartDash();

            return StKirbyInhale;
        }

        private IEnumerator KirbyInhaleCoroutine()
        {
            while (kirbyInhaleTimer > 0 && Input.Grab.Check)
                yield return null;

            player.StateMachine.State = global::Celeste.Player.StNormal;
        }

        private void PullAndInhaleEnemies(Vector2 origin, Vector2 direction)
        {
            // Snapshot: RemoveSelf() below mutates Scene.Entities, which would
            // crash a live foreach over the same collection mid-enumeration.
            foreach (Entity entity in player.Scene.Entities.ToArray())
            {
                // only pull things that can actually be fought -- never Theo,
                // holdables, or other neutral actors
                if (entity is not Actor || !IsDamageableTarget(entity))
                    continue;

                // bosses are too big to inhale
                if (DZImports.IsBossSizedEntity?.Invoke(entity) == true)
                    continue;

                float dist = Vector2.Distance(entity.Center, origin);
                if (dist > KirbyInhaleRange)
                    continue;

                // check the entity is roughly in front
                Vector2 toEntity = entity.Center - player.Center;
                if (toEntity.Length() > 0 && Vector2.Dot(toEntity.SafeNormalize(), direction) < 0.3f)
                    continue;

                // pull it toward the mouth
                Vector2 pullDir = (player.Center - entity.Center).SafeNormalize();
                float pullFraction = 1f - (1f - KirbyInhaleMinPullFraction) * (dist / KirbyInhaleRange);
                float pullStrength = KirbyInhalePullSpeed * pullFraction;

                if (entity is Actor actor)
                {
                    actor.MoveH(pullDir.X * pullStrength * Engine.DeltaTime);
                    actor.MoveV(pullDir.Y * pullStrength * Engine.DeltaTime);
                }

                // if close enough, inhale it
                if (dist < 16f)
                {
                    kirbyHasInhaledEnemy = true;
                    entity.RemoveSelf();
                    // Best-effort vanilla substitute for the missing DZ audio bank -- see KirbyFloatBegin.
                    player.Play("event:/char/madeline/grab");
                    player.Sprite.Scale = new Vector2(1.4f, .6f);
                    break;
                }
            }
        }

        // ── Inhale tendril visual (ported from ingeste.lua create_inhales/update_inhales/draw_inhales) ──

        private void CreateKirbyInhaleTendrils()
        {
            kirbyInhaleTendrils = new Vector2[KirbyInhaleTendrilCount];
            for (int i = 0; i < KirbyInhaleTendrilCount; i++)
                kirbyInhaleTendrils[i] = player.Center;
        }

        private void UpdateKirbyInhaleTendrils(Vector2 facingDir)
        {
            if (kirbyInhaleTendrils == null)
                return;

            for (int i = 0; i < kirbyInhaleTendrils.Length; i++)
            {
                Vector2 tendril = kirbyInhaleTendrils[i];

                float dist = Vector2.Distance(tendril, player.Center);
                if (dist <= 2f || dist >= 18f)
                {
                    tendril = player.Center + facingDir * (10f + Calc.Random.Range(0f, 5f));
                    tendril.Y += 6f - Calc.Random.Range(0f, 12f);
                }

                Vector2 toCenter = player.Center - tendril;
                float length = toCenter.Length();
                Vector2 step = length > 0f ? toCenter / length : Vector2.Zero;

                tendril.X -= Math.Abs(step.X) * facingDir.X;
                tendril.Y += (tendril.Y > player.Center.Y ? -1f : 1f) * Math.Abs(step.Y);

                kirbyInhaleTendrils[i] = tendril;
            }
        }

        /// <summary>Inhale tendril wisps, drawn while inhaling. See KirbyPlayerHooks' Render hook.</summary>
        internal void RenderInhaleTendrils()
        {
            if (kirbyInhaleTendrils == null)
                return;

            foreach (Vector2 tendril in kirbyInhaleTendrils)
                Draw.Point(tendril, Color.White);
        }

        #endregion

        #region Damage helpers -- used by Inhale (target filtering) and Star Spit's projectile

        // Cached per-type lookup of TakeDamage(int) so combat checks don't pay
        // reflection costs every frame. A null value means the type has no such method.
        private static readonly Dictionary<Type, System.Reflection.MethodInfo> takeDamageMethodCache = new();

        private static System.Reflection.MethodInfo GetTakeDamageMethod(Type type)
        {
            if (!takeDamageMethodCache.TryGetValue(type, out var method))
            {
                method = type.GetMethod("TakeDamage", new Type[] { typeof(int) });
                takeDamageMethodCache[type] = method;
            }
            return method;
        }

        private bool IsDamageableTarget(Entity entity)
        {
            return entity != player && entity is not global::Celeste.Player
                && GetTakeDamageMethod(entity.GetType()) != null;
        }

        private void ApplyCombatDamage(Entity target, int damage, Vector2 knockbackDir)
        {
            var method = GetTakeDamageMethod(target.GetType());
            if (method == null)
                return;

            method.Invoke(target, new object[] { damage });

            level?.Displacement.AddBurst(target.Center, .2f, 4, 24, .2f, Ease.QuadOut, Ease.QuadOut);
            Dust.Burst(target.Center, knockbackDir.Angle(), 4);
        }

        /// <summary>Public wrapper so <see cref="PlayerStarBullet"/> can reuse the same damage checks.</summary>
        public bool IsValidTarget(Entity entity) => IsDamageableTarget(entity);

        /// <summary>Public wrapper so <see cref="PlayerStarBullet"/> can reuse the same damage checks.</summary>
        public void DealProjectileDamage(Entity target, int damage, Vector2 knockbackDir)
            => ApplyCombatDamage(target, damage, knockbackDir);

        #endregion

        #region Kirby Star Spit -- ported from legacy K_Player.KirbyStarSpitBegin/Update/End/Coroutine

        private const float KirbyStarSpitSpeed = 300f;
        private const int KirbyStarSpitDamage = 2;
        private const float ThrowRecoil = 80f;

        private Vector2 kirbyStarSpitDir;

        private void KirbyStarSpitBegin()
        {
            kirbyHasInhaledEnemy = false;
            kirbyStarSpitDir = player.lastAim;
            if (kirbyStarSpitDir == Vector2.Zero)
                kirbyStarSpitDir = Vector2.UnitX * (int)player.Facing;

            player.Speed = Vector2.Zero;
            player.Sprite.Play(PlayerSprite.Dash);
            player.Sprite.Scale = new Vector2(1.4f, .6f);
            KirbySprite?.Play(K_PlayerAnimIds.Spit);

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        private void KirbyStarSpitEnd()
        {
        }

        private int KirbyStarSpitUpdate()
        {
            if (!player.onGround)
                player.Speed.Y = Calc.Approach(player.Speed.Y, global::Celeste.Player.MaxFall, global::Celeste.Player.Gravity * Engine.DeltaTime);

            if (player.CanDash)
                return player.StartDash();

            return StKirbyStarSpit;
        }

        private IEnumerator KirbyStarSpitCoroutine()
        {
            yield return null;

            SpawnKirbyStarProjectile(player.Center, kirbyStarSpitDir);

            // recoil
            player.Speed.X = -kirbyStarSpitDir.X * ThrowRecoil;

            SlashFx.Burst(player.Center, kirbyStarSpitDir.Angle());
            // Best-effort vanilla substitute for the missing DZ audio bank -- see KirbyFloatBegin.
            player.Play("event:/char/madeline/dash_red_right");

            yield return .15f;

            player.StateMachine.State = global::Celeste.Player.StNormal;
        }

        private void SpawnKirbyStarProjectile(Vector2 from, Vector2 direction)
        {
            direction = direction.SafeNormalize();
            level?.Add(new PlayerStarBullet(
                from + direction * 8f,
                direction * KirbyStarSpitSpeed,
                this,
                KirbyStarSpitDamage));

            level?.ParticlesFG.Emit(global::Celeste.Player.P_DashA, 4, from + direction * 8f, Vector2.One * 4, direction.Angle());
            level?.Displacement.AddBurst(from + direction * 8f, .3f, 4, 24, .3f, Ease.QuadOut, Ease.QuadOut);
        }

        #endregion

        public override void Removed(Entity entity)
        {
            if (player != null)
            {
                player.Hair.Visible = true;
                player.Hair.SimulateMotion = true;
            }
            base.Removed(entity);
        }

        public override void EntityAdded(Scene scene)
        {
            base.EntityAdded(scene);
            level = scene as Level;

            if (level != null)
            {
                var healthManager = K_PlayerHealthManager.GetOrCreate(level, MaxHealth);
                healthManager?.EnableKirbyMode(MaxHealth);
            }
        }

        public override void EntityRemoved(Scene scene)
        {
            K_PlayerHealthManager.Instance?.DisableKirbyMode();

            base.EntityRemoved(scene);
        }

        public override void Update()
        {
            base.Update();
            // KirbySprite/Shoes are never added to player.Components (they're drawn
            // manually by RenderKirbyOverlay/KirbyShoes.Render, see the Visible=false
            // comment in Added()), so Monocle never advances KirbySprite's animation
            // timer on its own -- without this call every Kirby animation freezes on
            // its first frame instead of playing like Madeline's own sprite does.
            KirbySprite?.Update();
            // All other per-frame physics/ability logic runs via KirbyPlayerHooks'
            // On.Celeste.Player.Update/NormalUpdate hooks (PreUpdate/PostUpdate/
            // CheckFloatEntry/CheckInhaleEntry) and the StateMachine callbacks
            // registered in Added() -- nothing else to do here.
        }

        /// <summary>
        /// True while a Kirby-only ability state (Float/Inhale/StarSpit) is
        /// active -- these states play their own KirbySprite animation directly
        /// (kirby_float/kirby_inhale_*/kirby_spit) rather than mirroring vanilla's
        /// Sprite.CurrentAnimationID, since none of those ids exist in vanilla's
        /// own "player"/"badeline" bank.
        /// </summary>
        private bool InKirbyAbilityState =>
            (StKirbyFloat >= 0 && player.StateMachine.State == StKirbyFloat) ||
            (StKirbyInhale >= 0 && player.StateMachine.State == StKirbyInhale) ||
            (StKirbyStarSpit >= 0 && player.StateMachine.State == StKirbyStarSpit);

        /// <summary>
        /// Draws <see cref="KirbySprite"/> and <see cref="Shoes"/> in place of the
        /// vanilla sprite. Called from <see cref="KirbyPlayerHooks"/>' Player.Render
        /// hook, after vanilla's own Render() has run with the real sprite hidden.
        /// Outside a Kirby-only ability state, KirbySprite mirrors whatever
        /// animation id vanilla's own UpdateSprite (running unmodified via
        /// orig(self)) picked for the real Sprite this frame -- idle/run/climb/
        /// dash/duck/etc all "just work" this way since kirby_player_ext's ids in
        /// Graphics/k_sprites.xml deliberately match vanilla's "player" entry.
        /// </summary>
        internal void RenderKirbyOverlay()
        {
            Shoes?.Render();

            if (KirbySprite == null)
                return;

            if (!InKirbyAbilityState)
            {
                string vanillaId = player.Sprite.CurrentAnimationID;
                if (!string.IsNullOrEmpty(vanillaId) && KirbySprite.CurrentAnimationID != vanillaId && KirbySprite.Has(vanillaId))
                    KirbySprite.Play(vanillaId);
            }

            KirbySprite.RenderPosition = player.Sprite.RenderPosition;
            KirbySprite.Scale = player.Sprite.Scale;
            KirbySprite.Rotation = player.Sprite.Rotation;
            // idle00.png etc are fully painted art (pink body, red cheeks/feet,
            // black eyes), not a white silhouette like vanilla hair or the shoes
            // sprite -- multiplying the whole body by a flat dash-tier color turns
            // it into a solid-color blob. Only mirror vanilla's own flash-effect
            // tinting (hurt/bounce/etc), never apply KirbyDashColors here.
            KirbySprite.Color = player.Sprite.Color;
            KirbySprite.Render();
        }
    }

    #region Helper Classes

    /// <summary>
    /// Ported from the Lua `mouthvoid` object (PICO-8 Kirby fangame). A cosmetic
    /// hitbox that tracks in front of Kirby's mouth while inhaling and removes
    /// itself once the player stops inhaling.
    ///
    /// This is a literal translation and is not currently instantiated anywhere
    /// -- this mod's real swallow/pull logic will live in KirbyPlayerController's
    /// ported inhale state (see the phased rewrite plan) and doesn't need a
    /// separate tracked hitbox. Spawn one with `player.Scene.Add(new
    /// MouthVoidCollider(player, facing))` if some other content wants a physical
    /// void to collide against.
    /// </summary>
    public class MouthVoidCollider : Entity
    {
        private readonly global::Celeste.Player player;

        // Lua: this.p_flipx = this.flip.x and -1 or 1 -- computed once in init
        // and never re-read, so the void does NOT follow a mid-inhale facing
        // flip; it stays locked to whichever way Kirby faced when it spawned.
        private readonly int flipx;

        private const float Ox = 10f; // Lua: this.ox = 10
        private const float Oy = -2f; // Lua: this.y = player.y - 2

        public MouthVoidCollider(global::Celeste.Player player, int facingDir)
            : base(player.Position)
        {
            this.player = player;
            flipx = facingDir >= 0 ? 1 : -1;

            // Lua: this.x += this.ox * this.p_flipx
            Position += new Vector2(Ox * flipx, 0f);

            Collidable = false; // Lua: this.solids = false
            Collider = new Hitbox(10f, 12f, 0f, Oy); // Lua: hitbox={x=0,y=-2,w=10,h=12}
            Visible = false;
        }

        public override void Update()
        {
            base.Update();

            // Lua only ever updates/destroys itself inside `if this.player ~= nil`
            // and never reassigns that reference, so a removed player would leave
            // the void frozen in place forever. Removing here instead is a small,
            // deliberate deviation to avoid leaking an entity.
            if (player?.Scene == null)
            {
                RemoveSelf();
                return;
            }

            // Lua: this.y = this.player.y-2; this.x = this.player.x+(this.ox*this.p_flipx)
            Position = new Vector2(player.X + Ox * flipx, player.Y + Oy);

            // Lua: this.x += (this.flip.x and -1 or -2) -- the source never sets
            // mouthvoid's own flip.x, so this branch always took the "-2" arm in
            // the original PICO-8 code. Reproduced here as a small per-facing
            // alignment fudge instead of a dead constant.
            Position.X += flipx > 0 ? -1f : -2f;

            // Lua: if not this.player.inhaling then destroy_object(this) end
            if (!(player.Get<KirbyPlayerController>()?.IsInhaling ?? false))
            {
                RemoveSelf();
            }
        }
    }

    /// <summary>
    /// Particle system for inhale effect. Accepts the real vanilla Player as owner.
    /// </summary>
    public class InhaleParticleSystem : Component
    {
        private Entity player;
        private Particle[] particles;
        private bool isInhaling;

        private struct Particle
        {
            public Vector2 Position;
        }

        public InhaleParticleSystem(Entity player)
            : base(active: true, visible: true)
        {
            this.player = player;
            this.particles = new Particle[5];
        }

        public void StartInhaling()
        {
            isInhaling = true;
            // Initialize particles
            for (int i = 0; i < particles.Length; i++)
            {
                ResetParticle(ref particles[i], 1);
            }
        }

        public void StopInhaling()
        {
            isInhaling = false;
        }

        public void UpdateInhale(int facingDir)
        {
            if (!isInhaling) return;

            Vector2 center = player.Position + new Vector2(4, 4);

            for (int i = 0; i < particles.Length; i++)
            {
                float dist = Vector2.Distance(particles[i].Position, center);

                if (dist <= 2 || dist >= 18)
                {
                    ResetParticle(ref particles[i], facingDir);
                }

                // Move toward center
                Vector2 dir = (center - particles[i].Position).SafeNormalize();
                particles[i].Position += dir * 40f * Engine.DeltaTime;
            }
        }

        private void ResetParticle(ref Particle p, int facingDir)
        {
            Vector2 center = player.Position + new Vector2(4, 4);
            float distance = 10 + Calc.Random.Range(0f, 5f);
            float angle = Calc.Random.Range(-0.5f, 0.5f) * (float)Math.PI;

            p.Position = center + new Vector2(
                (float)Math.Cos(angle) * distance * facingDir,
                (float)Math.Sin(angle) * distance
            );
        }

        public override void Render()
        {
            if (!isInhaling) return;

            foreach (var p in particles)
            {
                Draw.Pixel.Draw(p.Position, Vector2.Zero, Color.White);
            }
        }
    }

    #endregion

    #region Inhaleable Component

    /// <summary>
    /// Component that can be added to entities to make them inhaleable by Kirby.
    /// When the entity's collider overlaps with Kirby's mouth void, OnInhaled is called.
    /// </summary>
    public class InhaleableComponent : Component
    {
        public InhaleableComponent()
            : base(active: true, visible: false)
        {
        }

        /// <summary>
        /// Called when Kirby inhales this entity.
        /// Override to implement custom behavior (e.g., being swallowed, dropping loot, etc.)
        /// </summary>
        public virtual void OnInhaled(global::Celeste.Player player)
        {
            // Default behavior: remove the entity
            Entity?.RemoveSelf();
        }
    }

    #endregion
}
