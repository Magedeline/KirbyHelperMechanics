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
/// </summary>
[Tracked]
public class K_PlayerHealthManager : Entity
{
    public static K_PlayerHealthManager Instance { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamageTaken;

    public int MaxHP { get; private set; } = 6;
    public int CurrentHP { get; private set; } = 6;
    public bool IsKirbyMode { get; private set; }
    public bool IsDead => CurrentHP <= 0;
    public bool IsLowHealth => CurrentHP > 0 && CurrentHP <= Math.Max(1, MaxHP / 3);
    public float HealthPercent => MaxHP <= 0 ? 0f : (float)CurrentHP / MaxHP;

    public K_PlayerHealthManager() : base(Vector2.Zero)
    {
        Tag = Tags.Persistent | Tags.TransitionUpdate;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        Instance = this;
    }

    public override void Removed(Scene scene)
    {
        if (Instance == this)
            Instance = null;

        base.Removed(scene);
    }

    public static K_PlayerHealthManager GetOrCreate(Level level, int maxHP = 6)
    {
        if (level == null)
            return Instance;

        var manager = Instance ?? level.Tracker.GetEntity<K_PlayerHealthManager>();
        bool created = false;
        if (manager == null)
        {
            manager = new K_PlayerHealthManager();
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
        if (CurrentHP == MaxHP)
            return;

        CurrentHP = MaxHP;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        SyncLegacyKirbyMode();
    }

    public bool Damage(int amount)
    {
        if (amount <= 0 || IsDead)
            return false;

        CurrentHP = Math.Max(0, CurrentHP - amount);
        OnDamageTaken?.Invoke(amount);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        SyncLegacyKirbyMode();
        return true;
    }

    public static bool TryDamagePlayer(int damage, Vector2 source)
    {
        var manager = Instance;
        if (manager == null)
        {
            Level level = Engine.Scene as Level;
            manager = level?.Tracker?.GetEntity<K_PlayerHealthManager>();
        }

        if (manager == null)
            return false;

        return manager.Damage(damage);
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
