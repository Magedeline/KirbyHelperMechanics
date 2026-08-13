using Celeste.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Triggers
{
    /// <summary>
    /// Trigger that switches the current player selection to a chosen character
    /// (Kirby or Madeline) on touch, and doubles as a generic "is
    /// &lt;targetPlayer&gt; active here" zone-flag trigger. Lets a map force
    /// whichever character actually suits a given section -- e.g. Madeline for
    /// a vanilla-feeling room inside an otherwise Kirby-default map, or Kirby
    /// for a Kirby-specific gimmick room in an otherwise-vanilla map. Since both
    /// characters now live on the one real vanilla Player (Kirby via
    /// KirbyPlayerController, see KirbyPlayerHooks), switching is just
    /// adding/removing that Component -- no separate Actor/shadow to construct
    /// or swap position/speed/facing onto.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_PlayerTrigger")]
    [Tracked]
    public class K_PlayerTrigger : Trigger
    {
        private readonly string flag;
        private readonly bool clearFlagOnLeave;
        private readonly bool onlyOnce;
        private readonly bool spawnKPlayer;
        private readonly PlayerSelectionManager.PlayerType targetPlayer;
        private readonly bool revertOnLeave;

        private bool triggered;

        /// <summary>Fired when the real Player enters the trigger area already matching (or just switched to) <see cref="targetPlayer"/>.</summary>
        public event System.Action<global::Celeste.Player> OnKPlayerEnter;

        /// <summary>Fired when the Player, still matching <see cref="targetPlayer"/>, leaves the trigger area.</summary>
        public event System.Action<global::Celeste.Player> OnKPlayerLeave;

        public K_PlayerTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            flag = data.Attr("flag", "");
            clearFlagOnLeave = data.Bool("clearFlagOnLeave", true);
            onlyOnce = data.Bool("onlyOnce", false);
            // Kept named "spawnKPlayer" for save/Lönn back-compat with maps built
            // before targetPlayer existed (where it always meant "force-switch to
            // Kirby") -- it now gates force-switching to whichever targetPlayer is
            // set, Kirby or Madeline.
            spawnKPlayer = data.Bool("spawnKPlayer", true);
            // Defaults to Kirby so existing placements (which predate this field)
            // keep behaving exactly as before.
            targetPlayer = data.Enum("targetPlayer", PlayerSelectionManager.PlayerType.Kirby);
            revertOnLeave = data.Bool("revertOnLeave", false);
        }

        public override void OnEnter(global::Celeste.Player player)
        {
            base.OnEnter(player);

            if (onlyOnce && triggered)
                return;

            if (Scene is not Level level)
                return;

            if (!Matches(targetPlayer, player))
            {
                if (!spawnKPlayer)
                    return;

                PlayerSelectionManager.GetOrCreate(level);
                PlayerSelectionManager.SetLevelOverride(targetPlayer);
                KirbyPlayerHooks.SyncKirbyState(player);

                Logger.Log(LogLevel.Info, "K_PlayerTrigger", $"Player switched to {targetPlayer} at {player.Position}");
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

            if (!Matches(targetPlayer, player))
                return;

            if (revertOnLeave)
            {
                // Clears this trigger's own override rather than force-swapping to
                // the "other" character -- falls back to whatever an outer
                // trigger/the mod-settings default already had selected, same as
                // K_PlayerTrigger never having fired here at all.
                PlayerSelectionManager.SetLevelOverride(null);
                KirbyPlayerHooks.SyncKirbyState(player);
            }

            if (clearFlagOnLeave)
                SetFlag(false, level);
            OnKPlayerLeave?.Invoke(player);
        }

        private static bool Matches(PlayerSelectionManager.PlayerType type, global::Celeste.Player player)
        {
            bool isKirby = player.Get<KirbyPlayerController>() != null;
            return isKirby == (type == PlayerSelectionManager.PlayerType.Kirby);
        }

        private void SetFlag(bool value, Level level)
        {
            if (string.IsNullOrEmpty(flag))
                return;

            level.Session.SetFlag(flag, value);
        }
    }
}
