// Kirby Helper Mechanics' own persisted settings -- replaces the
// Stubs/DZModuleStub.cs placeholder that used to fake DZ's
// Celeste.Mod.DZ.DZModule.Settings so this mod could compile standalone.
//
// This mod no longer references anything under Celeste.Mod.DZ: it owns its
// own EverestModule, so it no longer depends on DZ being installed, and it
// no longer risks a Celeste.Mod.DZ.DZModule type collision if DZ happens to
// be loaded in the same Everest process.
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.KirbyHelperMechanics
{
    public class KirbyHelperMechanicsModuleSettings : EverestModuleSettings
    {
        public int KirbyMaxFloatJumps { get; set; } = 5;
        public bool GentleBreezeMode { get; set; } = false;
        public bool KirbyPlayerEnabled { get; set; } = true;
        public bool PlayerAutoSelect { get; set; } = false;

        // Standalone "empty spit" cosmetic -- fires KirbyPuff directly, independent
        // of the Grab/Inhale state machine. Unbound by default (Mod Options ->
        // Keyboard/Controller Config) so it doesn't steal a key from the player.
        [SettingName("modoptions_kirbyhelpermechanics_spitpuffbutton")]
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding KirbySpitButton { get; set; }
    }
}
