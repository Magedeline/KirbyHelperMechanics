// Kirby Helper Mechanics' own persisted settings -- replaces the
// Stubs/DZModuleStub.cs placeholder that used to fake DZ's
// Celeste.Mod.DZ.DZModule.Settings so this mod could compile standalone.
//
// This mod no longer references anything under Celeste.Mod.DZ: it owns its
// own EverestModule, so it no longer depends on DZ being installed, and it
// no longer risks a Celeste.Mod.DZ.DZModule type collision if DZ happens to
// be loaded in the same Everest process.
using Celeste.Entities;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.KirbyHelperMechanics
{
    public class KirbyHelperMechanicsModuleSettings : EverestModuleSettings
    {
        public int KirbyMaxFloatJumps { get; set; } = 5;
        public bool GentleBreezeMode { get; set; } = false;
        public bool KirbyPlayerEnabled { get; set; } = true;
        public bool PlayerAutoSelect { get; set; } = false;

        /// <summary>
        /// Overrides KirbyPlayerEnabled's auto-generated OnOff menu item (Everest
        /// looks for a Create{PropertyName}Entry(TextMenu, bool) method on this
        /// class before falling back to its own reflection-based item -- see
        /// EverestModule.CreateModMenuSection). The default item only calls the
        /// property setter, so toggling "Kirby Player Enabled" from the pause
        /// menu had no visible effect: PlayerSelectionManager.LoadDefaultFromSettings
        /// (which reads this setting) only ever runs once, the first time its
        /// singleton is created for a fresh game process. Driving
        /// PlayerSelectionManager.SetDefaultPlayer directly here re-derives the
        /// active selection immediately and (via its OnPlayerSelectionChanged
        /// event, which KirbyPlayerHooks already subscribes to) syncs the live
        /// Player's KirbyPlayerController the moment the toggle is flipped.
        /// </summary>
        public void CreateKirbyPlayerEnabledEntry(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.OnOff("Kirby Player Enabled", KirbyPlayerEnabled).Change(value =>
            {
                KirbyPlayerEnabled = value;
                PlayerSelectionManager.SetDefaultPlayer(value
                    ? PlayerSelectionManager.PlayerType.Kirby
                    : PlayerSelectionManager.PlayerType.Madeline);
            }));
        }

        // Standalone "empty spit" cosmetic -- fires KirbyPuff directly, independent
        // of the Grab/Inhale state machine. Unbound by default (Mod Options ->
        // Keyboard/Controller Config) so it doesn't steal a key from the player.
        [SettingName("modoptions_kirbyhelpermechanics_spitpuffbutton")]
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding KirbySpitButton { get; set; }

        // Manual Kirby/Madeline swap, checked every Player.Update in
        // KirbyPlayerHooks regardless of which character is currently active
        // (unlike KirbyInhaleButton, this has to work even while playing as
        // Madeline). Unbound by default, same reasoning as KirbySpitButton --
        // players opt in via Mod Options -> Keyboard/Controller Config.
        [SettingName("modoptions_kirbyhelpermechanics_toggleplayerbutton")]
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding ToggleKirbyMadelineButton { get; set; }

        // Kirby's Inhale entry/hold button. Deliberately its own binding rather
        // than reusing Input.Grab -- Grab is already overloaded with vanilla
        // climb/hold-object behavior, so sharing it made Inhale fight climbing
        // against walls. Unbound by default, same reasoning as KirbySpitButton.
        [SettingName("modoptions_kirbyhelpermechanics_inhalebutton")]
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding KirbyInhaleButton { get; set; }
    }
}
