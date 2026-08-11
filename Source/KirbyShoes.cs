using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Renders a pair of Kirby-style shoes at the player's feet via the
    /// "kirby_shoes" sprite bank entry (Graphics/k_sprites.xml), replacing
    /// the hat+scarf accent DZ uses. Color mirrors the hair dash-tier color
    /// system so all existing dash, Kirby-pink, and combat colors still apply.
    /// </summary>
    public class KirbyShoes : Component
    {
        /// <summary>Current shoe tint. Set each frame from UpdateHair.</summary>
        public Color Color
        {
            get => sprite?.Color ?? Color.White;
            set
            {
                if (sprite != null)
                    sprite.Color = value;
            }
        }

        private readonly global::Celeste.Player player;
        private readonly Sprite sprite;

        public KirbyShoes(global::Celeste.Player player) : base(active: true, visible: false)
        {
            this.player = player;
            // "kirby_shoes" lives only in Graphics/k_sprites.xml (no vanilla-Sprites.xml
            // fallback like kirby_player_ext has), so if this component gets built before
            // KirbyHelperMechanicsModule.LoadContent has merged that bank in, GFX.SpriteBank
            // won't have the id yet. Guard instead of crashing the whole room load; the
            // shoes just don't render for that instance (same pattern as BossActor.cs).
            if (GFX.SpriteBank != null && GFX.SpriteBank.Has("kirby_shoes"))
            {
                sprite = GFX.SpriteBank.Create("kirby_shoes");
                sprite.Color = Calc.HexToColor("ff99cc");
            }
        }

        public override void Render()
        {
            if (sprite == null)
                return;

            sprite.RenderPosition = player.Sprite.RenderPosition;
            sprite.FlipX = player.Facing == Facings.Left;
            sprite.Render();
        }
    }
}
