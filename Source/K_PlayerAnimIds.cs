namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// Animation id constants for the "kirby_player_ext" sprite bank entry
    /// (Graphics/Pusheen2026/KHM/k_sprites.xml), mirroring vanilla Celeste's PlayerSprite
    /// pattern of exposing animation ids as fields instead of magic strings.
    /// Keep in sync with Graphics/Pusheen2026/KHM/k_sprites.xml -- every id here must exist
    /// as an Anim/Loop id under kirby_player_ext, and vice versa.
    /// </summary>
    public static class K_PlayerAnimIds
    {
        public const string SpriteBankId = "kirby_player_ext";

        // Basic Animations
        public const string Idle = "kirby_idle";
        public const string Walk = "kirby_walk";
        public const string Run = "kirby_run";
        public const string Jump = "kirby_jump";
        public const string Fall = "kirby_fall";

        // Kirby Abilities
        public const string InhaleStart = "kirby_inhale_start";
        public const string InhaleLoop = "kirby_inhale_loop";
        public const string InhaleEnd = "kirby_inhale_end";
        public const string Hover = "kirby_hover";
        public const string Float = "kirby_float";
        public const string Spit = "kirby_spit";
        public const string Damage = "kirby_damage";

        // Transformation Animations
        public const string TransformIn = "kirby_transform_in";
        public const string TransformOut = "kirby_transform_out";

        // Reactions
        public const string Angry = "kirby_angry";
        public const string LevelUp = "kirby_levelUp";

        // Power States - Fire
        public const string FireIdle = "kirby_fire_idle";
        public const string FireWalk = "kirby_fire_walk";
        public const string FireAttack = "kirby_fire_attack";
        public const string FireBreath = "kirby_fire_breath";
        public const string FireDashball = "kirby_fire_dashball";

        // Power States - Ice
        public const string IceIdle = "kirby_ice_idle";
        public const string IceWalk = "kirby_ice_walk";
        public const string IceAttack = "kirby_ice_attack";
        public const string IceShard = "kirby_ice_shard";
        public const string IceSlide = "kirby_ice_slide";
        public const string IceBreath = "kirby_ice_breath";

        // Power States - Spark
        public const string SparkIdle = "kirby_spark_idle";
        public const string SparkWalk = "kirby_spark_walk";
        public const string SparkAttack = "kirby_spark_attack";
        public const string SparkShield = "kirby_spark_shield";
        public const string SparkCharge = "kirby_sparkDZ_CHarge";
        public const string SparkRelease = "kirby_spark_release";

        // Power States - Stone
        public const string StoneIdle = "kirby_stone_idle";
        public const string StoneWalk = "kirby_stone_walk";
        public const string StoneTransform = "kirby_stone_transform";
        public const string StoneForm = "kirby_stone_form";
        public const string StoneShatter = "kirby_stone_shatter";
        public const string StonePrebigform = "kirby_stone_prebigform";
        public const string StoneBigformgroundpound = "kirby_stone_bigformgroundpound";
        public const string BigformShatter = "kirby_bigform_shatter";

        // Power States - Sword
        public const string SwordIdle = "kirby_sword_idle";
        public const string SwordWalk = "kirby_sword_walk";
        public const string SwordAttack1 = "kirby_sword_attack1";
        public const string SwordAttack2 = "kirby_sword_attack2";
        public const string SwordSpin = "kirby_sword_spin";
        public const string SwordDash = "kirby_sword_dash";
        public const string SwordParry = "kirby_sword_parry";
        public const string SwordSliceup = "kirby_sword_sliceup";
        public const string SwordFinalslice = "kirby_sword_finalslice";

        // Combat Animations - Range and Copy are still placeholders (no
        // dedicated art); Mouthful/Swallow use real dedicated art.
        public const string Range = "kirby_range";
        public const string Mouthful = "kirby_mouthful";
        public const string WalkMouthful = "kirby_walkmouthful";
        public const string Swallow = "kirby_swallow";
        public const string Copy = "kirby_copy";

        // Staged for a future roll-attack/landing-impact ability -- ids exist
        // in Graphics/Pusheen2026/KHM/k_sprites.xml but no KirbyPlayerController state calls
        // them yet.
        public const string Roll = "kirby_roll";
        public const string RollGetup = "kirby_rollGetup";
        public const string RollSit = "kirby_rollSit";
        public const string Land = "kirby_land";

        // Death Animations
        public const string PreDeath = "kirby_pre_death";
        public const string MidDeath = "kirby_mid_death";
        public const string PostDeath = "kirby_post_death";
        public const string Death = "kirby_death";
    }
}
