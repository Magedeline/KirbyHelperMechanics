using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Helpers;

namespace Celeste.Entities
{
    /// <summary>
    /// Badeline boss encounter -- placeholder only, per request: no attack
    /// logic, just a drawable stand-in on BossActor's scaffolding so the real
    /// fight can be built on top of it later. AutoStartEncounter is off so
    /// dropping this in a room doesn't trigger BossActor's health-bar/
    /// encounter-start side effects before there's an actual fight to show.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_BadelineBossPlaceholder")]
    [Tracked]
    public class K_BadelineBossPlaceholder : BossActor
    {
        private static readonly Color BodyColor = Calc.HexToColor("2b1a3d");
        private static readonly Color FlameColor = Calc.HexToColor("6ce0ff");

        public K_BadelineBossPlaceholder(EntityData data, Vector2 offset)
            : base(
                data.Position + offset,
                spriteName: "kirby_badeline_boss_placeholder_no_such_sprite",
                spriteScale: Vector2.One,
                maxFall: 0f,
                collidable: false,
                solidCollidable: false,
                gravityMult: 0f,
                collider: new Hitbox(16f, 20f, -8f, -20f))
        {
            MaxHealth = Health = data.Int("health", 10);
            ConfigureEncounter(autoStart: false, hideUntilStart: false);
        }

        public override void Render()
        {
            base.Render();
            Draw.Rect(X - 8f, Y - 20f, 16f, 20f, BodyColor);
            Draw.HollowRect(X - 8f, Y - 20f, 16f, 20f, FlameColor);
        }
    }
}
