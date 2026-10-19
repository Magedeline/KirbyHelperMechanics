using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities;

/// <summary>
/// Ported from DZ's PlayerHealthManager, renamed with the K_ prefix to match
/// K_Player's own naming convention -- both mods declare a same-named entity
/// in Celeste.Entities, and giving each its own tracked type keeps a combined
/// DZ + Kirby Helper Mechanics install from running two independent health
/// systems under the identical name. See EXTRACTION_PLAN.md's BossStubs.cs
/// note for the same concern applied to boss types.
/// <para>
/// Owns the whole "Kirby takes a hit" contract so the individual hazards
/// don't each have to: a shared post-hit invulnerability window (so standing
/// between two hazards isn't double damage and a hazard can't re-hit every
/// frame), the actual death when HP runs out (nothing else ever called
/// Player.Die -- Kirby used to become immortal at 0 HP, since Damage() just
/// returned false from then on), the hit feedback on the controller, and the
/// HP pips drawn in HUD space (this entity carries <see cref="Tags.HUD"/>, so
/// its Render runs in HUD coordinates).
/// </para>
/// </summary>
[Tracked]
public class K_PlayerHealthManager : Entity
{
    public static K_PlayerHealthManager Instance { get; private set; }

    // A manager that has been handed to level.OnEndOfFrame but not yet Added,
    // so a second GetOrCreate in the same frame reuses it instead of queueing
    // a duplicate.
    private static K_PlayerHealthManager pending;

    /// <summary>Post-hit invulnerability window, shared by every damage source.</summary>
    public const float InvulnerabilityTime = 1.0f;
    private const float BlinkInterval = 0.08f;

    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamageTaken;

    public int MaxHP { get; private set; } = 6;
    public int CurrentHP { get; private set; } = 6;
    public bool IsKirbyMode { get; private set; }
    public bool IsDead => CurrentHP <= 0;
    public bool IsLowHealth => CurrentHP > 0 && CurrentHP <= Math.Max(1, MaxHP / 3);
    public float HealthPercent => MaxHP <= 0 ? 0f : (float)CurrentHP / MaxHP;

    public float InvulnerabilityTimer { get; private set; }
    public bool IsInvulnerable => InvulnerabilityTimer > 0f;

    /// <summary>
    /// True on the frames the Kirby sprite should be skipped for the classic
    /// post-hit blink. Read by KirbyPlayerController.RenderKirbyOverlay.
    /// </summary>
    public bool BlinkHidden => IsInvulnerable && ((int)(InvulnerabilityTimer / BlinkInterval) % 2) == 1;

    public K_PlayerHealthManager() : base(Vector2.Zero)
    {
        Tag = Tags.Persistent | Tags.TransitionUpdate | Tags.HUD;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        Instance = this;
        if (pending == this)
            pending = null;
    }

    public override void Removed(Scene scene)
    {
        if (Instance == this)
            Instance = null;

        base.Removed(scene);
    }

    /// <summary>
    /// Forgets the singleton (and any still-pending add) when a level is left
    /// -- Monocle never calls Removed on a scene that is simply dropped, so
    /// without this the next map would keep talking to a manager that lives
    /// in the dead scene (no HUD, stale event subscribers). Called from
    /// KirbyPlayerHooks' Everest.Events.Level.OnExit handler.
    /// </summary>
    internal static void ResetStatics()
    {
        Instance = null;
        pending = null;
    }

    public static K_PlayerHealthManager GetOrCreate(Level level, int maxHP = 6)
    {
        if (level == null)
            return Instance;

        var manager = Instance;
        // Left over from a previous level (see ResetStatics) -- never reuse it.
        if (manager != null && manager.Scene != null && manager.Scene != level)
        {
            Instance = null;
            manager = null;
        }

        manager ??= pending ?? level.Tracker.GetEntity<K_PlayerHealthManager>();

        bool created = false;
        if (manager == null)
        {
            manager = new K_PlayerHealthManager();
            pending = manager;
            // Deferred: GetOrCreate can be called from Entity.Added()/Awake() while
            // Monocle's EntityList is still enumerating the "adding" list. Adding
            // here synchronously would mutate that same list mid-enumeration,
            // crashing with "Collection was modified; enumeration operation may
            // not execute."
            level.OnEndOfFrame += () => level.Add(manager);
            created = true;
        }

        if (created)
        {
            manager.MaxHP = Math.Max(1, maxHP);
            manager.CurrentHP = manager.MaxHP;
            manager.OnHealthChanged?.Invoke(manager.CurrentHP, manager.MaxHP);
        }
        else
        {
            manager.SetMaxHP(maxHP);
        }

        manager.SyncLegacyKirbyMode(level);
        return manager;
    }

    public void EnableKirbyMode(int maxHP = 6)
    {
        IsKirbyMode = true;
        SetMaxHP(maxHP);
        FullHeal();
    }

    public void DisableKirbyMode()
    {
        IsKirbyMode = false;
        SyncLegacyKirbyMode();
    }

    public void SetMaxHP(int maxHP)
    {
        MaxHP = Math.Max(1, maxHP);
        CurrentHP = Calc.Clamp(CurrentHP, 0, MaxHP);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        SyncLegacyKirbyMode();
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        int next = Calc.Clamp(CurrentHP + amount, 0, MaxHP);
        if (next == CurrentHP)
            return;

        CurrentHP = next;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        SyncLegacyKirbyMode();
    }

    public void FullHeal()
    {
        // A fresh Player (respawn) always starts clean, including any blink
        // left over from the hit that killed the previous one.
        InvulnerabilityTimer = 0f;

        if (CurrentHP == MaxHP)
            return;

        CurrentHP = MaxHP;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        SyncLegacyKirbyMode();
    }

    /// <summary>
    /// Applies damage, honoring the shared invulnerability window. Returns
    /// true only when HP actually changed. Prefer <see cref="TryDamagePlayer"/>
    /// from hazards -- that one also handles the knockback/feedback and the
    /// death at 0 HP.
    /// </summary>
    public bool Damage(int amount)
    {
        if (amount <= 0 || IsDead || IsInvulnerable)
            return false;

        CurrentHP = Math.Max(0, CurrentHP - amount);
        InvulnerabilityTimer = InvulnerabilityTime;
        OnDamageTaken?.Invoke(amount);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        SyncLegacyKirbyMode();
        return true;
    }

    /// <summary>
    /// The one entry point hazards should use. Damages the Kirby HP pool
    /// (respecting i-frames), then either kills the player if that emptied it
    /// or plays the hit reaction (knockback away from <paramref name="source"/>,
    /// hurt animation, blink) on the live KirbyPlayerController. Returns true
    /// when the hit landed.
    /// </summary>
    public static bool TryDamagePlayer(int damage, Vector2 source)
    {
        var manager = Instance;
        if (manager == null)
        {
            Level level = Engine.Scene as Level;
            manager = level?.Tracker?.GetEntity<K_PlayerHealthManager>();
        }

        if (manager == null || !manager.Damage(damage))
            return false;

        Level scene = manager.Scene as Level ?? Engine.Scene as Level;
        var player = scene?.Tracker.GetEntity<global::Celeste.Player>();
        if (player == null)
            return true;

        Vector2 away = (player.Center - source).SafeNormalize(-Vector2.UnitY);
        if (manager.IsDead)
        {
            player.Die(away);
            return true;
        }

        player.Get<KirbyPlayerController>()?.OnDamaged(away);
        return true;
    }

    public override void Update()
    {
        base.Update();

        if (InvulnerabilityTimer > 0f)
            InvulnerabilityTimer = Math.Max(0f, InvulnerabilityTimer - Engine.DeltaTime);
    }

    // HUD layout (1920x1080 HUD space): a row of pips in the bottom-left corner,
    // out of the way of vanilla's own top-left strawberry/timer displays.
    private const float HudX = 64f;
    private const float HudY = 1080f - 64f - 32f;
    private const float PipSize = 32f;
    private const float PipGap = 10f;

    private static readonly Color PipFull = Calc.HexToColor("ff4f78");
    private static readonly Color PipLow = Calc.HexToColor("ffd36b");
    private static readonly Color PipEmpty = Color.Black * 0.45f;
    private static readonly Color PipOutline = Color.White * 0.85f;

    /// <summary>
    /// HP pips. Runs in HUD coordinates thanks to <see cref="Tags.HUD"/>.
    /// Hidden whenever the health system is off, so Madeline sees nothing.
    /// </summary>
    public override void Render()
    {
        base.Render();

        if (!IsKirbyMode || MaxHP <= 0)
            return;
        if (Scene is not Level level || level.Paused)
            return;

        Color full = PipFull;
        if (IsLowHealth)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(level.TimeActive * 10f);
            full = Color.Lerp(PipFull, PipLow, pulse);
        }

        for (int i = 0; i < MaxHP; i++)
        {
            float x = HudX + i * (PipSize + PipGap);
            Draw.Rect(x, HudY, PipSize, PipSize, i < CurrentHP ? full : PipEmpty);
            Draw.HollowRect(x, HudY, PipSize, PipSize, PipOutline);
        }
    }

    private void SyncLegacyKirbyMode(Level level = null)
    {
        level ??= Scene as Level ?? Engine.Scene as Level;
        if (level == null)
            return;

        var kirbyMode = level.Tracker.GetEntity<global::Celeste.Extensions.KirbyMode>();
        if (kirbyMode == null && IsKirbyMode)
        {
            kirbyMode = new global::Celeste.Extensions.KirbyMode();
            level.Add(kirbyMode);
        }

        if (kirbyMode == null)
            return;

        kirbyMode.IsActive = IsKirbyMode;
        kirbyMode.MaxHealth = MaxHP;
        kirbyMode.CurrentHealth = CurrentHP;
        kirbyMode.IsDead = IsDead;
    }
}