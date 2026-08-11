namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// Which sprite bank <see cref="K_PlayerSprite"/> should build from --
    /// mirrors vanilla's own Celeste.PlayerSpriteMode (Madeline/MadelineNoBackpack/
    /// Badeline/MadelineAsBadeline/Playback), swapped for this mod's Kirby skins.
    /// Deliberately declared in the Celeste.Mod.KirbyHelperMechanics namespace
    /// (not Celeste) and named with a K_ prefix so it never collides with the
    /// vanilla Celeste.PlayerSpriteMode type shipped in Celeste.dll.
    /// </summary>
    public enum K_PlayerSpriteMode
    {
        Kirby,
        KirbyNoBackpack,
        KirbyKnght,
        Playback,
    }
}
