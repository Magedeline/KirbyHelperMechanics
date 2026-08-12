using System;
using Celeste.Mod;
using Celeste.Mod.KirbyHelperMechanics.Interop;
using MonoMod.ModInterop;

namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// Entry point for the Kirby Helper Mechanics mod.
    /// <para>
    /// Graphics/k_sprites.xml is a custom-named sprite bank -- Everest only
    /// auto-merges files literally named Graphics/Sprites.xml across mods, so
    /// this file must be loaded explicitly. This is deliberate, not just an
    /// Everest quirk: DZ's own Mods/DZ/Graphics/Sprites.xml already defines a
    /// "kirby_player_ext" element for the same atlas folder, using a
    /// different, "kirby_"-prefixed animation scheme. If this mod's sprite
    /// bank were also auto-merged, DZ's copy (processed after this mod's)
    /// would silently win and KirbyPlayerController's vanilla-id mirroring
    /// would find no matching animations -- Kirby would render but never
    /// animate. Loading explicitly here in LoadContent, which runs after
    /// every mod's Load() (i.e. after Everest's own auto-merge pass), makes
    /// this mod's kirby_player_ext always overwrite DZ's, regardless of mod
    /// load order.
    /// </para>
    /// </summary>
    public class KirbyHelperMechanicsModule : EverestModule
    {
        public static KirbyHelperMechanicsModule Instance { get; private set; }

        public override Type SettingsType => typeof(KirbyHelperMechanicsModuleSettings);
        public static KirbyHelperMechanicsModuleSettings Settings => (KirbyHelperMechanicsModuleSettings)Instance._Settings;

        // Optional third-party mods this repo has a compile-time (Reference
        // Private="false") lib-stripped/*.dll for, so KirbyPlayerController.cs can
        // call their real types directly instead of via reflection. IMPORTANT: the .NET JIT
        // resolves every type referenced anywhere in a method's IL the first time
        // that method is invoked, not just the types on the branch actually taken
        // -- so it is never safe to reference one of these mods' types directly
        // inside a method that runs unconditionally (the constructor, Update, ...).
        // Every such reference must live in its own dedicated method, called only
        // when the matching *Loaded flag below is true.
        public static bool FactoryHelperLoaded { get; private set; }

        public KirbyHelperMechanicsModule()
        {
            Instance = this;
        }

        public override void Load()
        {
            KirbyPlayerHooks.Load();

            FactoryHelperLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "FactoryHelper", Version = new Version(1, 4, 1) });
        }

        public override void Unload()
        {
            KirbyPlayerHooks.Unload();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Bind DZImports' delegate fields here, not in Load() -- Initialize()
            // runs after every mod's Load() has already executed, so if DZ is
            // loaded (in any order relative to this mod) its ModExportName("DZ")
            // API, if any, has already been registered by the time this runs.
            typeof(DZImports).ModInterop();
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);

            // Monocle.SpriteBank.LoadSpriteBank(path) (the static helper) only
            // merges multiple mods' overrides of the SAME vanilla content path --
            // for a mod-only path like "Graphics/k_sprites.xml" (never a vanilla
            // file) it just parses and returns an XmlDocument with no side
            // effects. Discarding that return value, as this used to do, never
            // registered kirby_player_ext/kirby_shoes/etc into GFX.SpriteBank at
            // all; on any setup where DZ (which defines its own colliding
            // kirby_player_ext) isn't installed, GFX.SpriteBank.Create throws
            // "Missing animation name in SpriteData" the first time Kirby is
            // selected. Building our own SpriteBank from the file and merging its
            // entries into the real GFX.SpriteBank is what actually registers
            // them -- and per the file's own header comment, doing this in
            // LoadContent (after every mod's Load(), i.e. after Everest's own
            // Graphics/Sprites.xml auto-merge pass) makes this mod's entries win
            // over DZ's regardless of mod load order.
            var kirbySpriteBank = new Monocle.SpriteBank(GFX.Game, "Graphics/k_sprites.xml");
            foreach (var entry in kirbySpriteBank.SpriteData)
                GFX.SpriteBank.SpriteData[entry.Key] = entry.Value;
            // KirbyPlayerController rides the real vanilla Player, so it reads
            // global::Celeste.Player's own P_DashA/P_DashB/etc particle-type
            // statics directly -- no separate copy step needed here anymore.
        }
    }
}
