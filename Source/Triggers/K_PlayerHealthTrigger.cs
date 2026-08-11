using Celeste.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Triggers
{
    /// <summary>
    /// Level-placed on/off switch for K_PlayerHealthManager. Ported from DZ's
    /// KirbyHealthTrigger -- without one of these in the room, the health
    /// system stays whatever KirbyPlayerController.EnableKirbyMode left it as;
    /// this lets a mapper explicitly enable, disable, or heal it on entry.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_PlayerHealthTrigger")]
    public class K_PlayerHealthTrigger : Trigger
    {
        private readonly bool enableHealth;
        private readonly int maxHealth;
        private readonly int healAmount;
        private readonly bool fullHeal;
        private readonly bool setRespawnPoint;
        private readonly bool onlyOnce;
        private bool triggered;

        public K_PlayerHealthTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            enableHealth = data.Bool("enableHealth", true);
            maxHealth = data.Int("maxHealth", 6);
            healAmount = data.Int("healAmount", 0);
            fullHeal = data.Bool("fullHeal", false);
            setRespawnPoint = data.Bool("setRespawnPoint", false);
            onlyOnce = data.Bool("onlyOnce", true);
        }

        public override void OnEnter(global::Celeste.Player player)
        {
            base.OnEnter(player);

            if (onlyOnce && triggered)
                return;

            triggered = true;

            var level = Scene as Level;
            var healthManager = K_PlayerHealthManager.Instance ?? K_PlayerHealthManager.GetOrCreate(level, maxHealth);

            if (enableHealth)
            {
                healthManager?.EnableKirbyMode(maxHealth);

                if (fullHeal)
                    healthManager?.FullHeal();
                else if (healAmount > 0)
                    healthManager?.Heal(healAmount);

                Logger.Log(LogLevel.Info, "K_PlayerHealthTrigger", $"Health system enabled with max HP: {maxHealth}");
            }
            else
            {
                healthManager?.DisableKirbyMode();
                Logger.Log(LogLevel.Info, "K_PlayerHealthTrigger", "Health system disabled");
            }

            if (setRespawnPoint && level != null)
            {
                level.Session.RespawnPoint = player.Position;
                Logger.Log(LogLevel.Info, "K_PlayerHealthTrigger", $"Respawn point set to: {player.Position} in room {level.Session.Level}");
            }
        }
    }
}
