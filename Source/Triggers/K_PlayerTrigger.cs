using Celeste.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Triggers
{
    /// <summary>
    /// Trigger that switches the current selection to Kirby on touch (optionally),
    /// and doubles as a generic "is Kirby active here" zone-flag trigger. Since
    /// Kirby is just a KirbyPlayerController Component on the one real vanilla
    /// Player now, "spawning" Kirby is just adding the component -- no separate
    /// Actor/shadow to construct or swap position/speed/facing onto.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_PlayerTrigger")]
    [Tracked]
    public class K_PlayerTrigger : Trigger
    {
        private readonly string flag;
        private readonly bool clearFlagOnLeave;
        private readonly bool onlyOnce;
        private readonly bool spawnKPlayer;

        private bool triggered;

        /// <summary>Fired when Kirby (the real Player with KirbyPlayerController attached) enters the trigger area.</summary>
        public event System.Action<global::Celeste.Player> OnKPlayerEnter;

        /// <summary>Fired when Kirby leaves the trigger area.</summary>
        public event System.Action<global::Celeste.Player> OnKPlayerLeave;

        public K_PlayerTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            flag = data.Attr("flag", "");
            clearFlagOnLeave = data.Bool("clearFlagOnLeave", true);
            onlyOnce = data.Bool("onlyOnce", false);
            spawnKPlayer = data.Bool("spawnKPlayer", true);
        }

        public override void OnEnter(global::Celeste.Player player)
        {
            base.OnEnter(player);

            if (onlyOnce && triggered)
                return;

            if (Scene is not Level level)
                return;

            bool isKirby = player.Get<KirbyPlayerController>() != null;
            if (!isKirby)
            {
                if (!spawnKPlayer)
                    return;

                PlayerSelectionManager.GetOrCreate(level);
                PlayerSelectionManager.SetLevelOverride(PlayerSelectionManager.PlayerType.Kirby);
                KirbyPlayerHooks.SyncKirbyState(player);

                Logger.Log(LogLevel.Info, "K_PlayerTrigger", $"Player switched to Kirby at {player.Position}");
            }

            triggered = true;
            SetFlag(true, level);
            OnKPlayerEnter?.Invoke(player);
        }

        public override void OnLeave(global::Celeste.Player player)
        {
            base.OnLeave(player);

            if (Scene is not Level level)
                return;

            if (player.Get<KirbyPlayerController>() == null)
                return;

            if (clearFlagOnLeave)
                SetFlag(false, level);
            OnKPlayerLeave?.Invoke(player);
        }

        private void SetFlag(bool value, Level level)
        {
            if (string.IsNullOrEmpty(flag))
                return;

            level.Session.SetFlag(flag, value);
        }
    }
}
