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
            get => sprite.Color;
            set => sprite.Color = value;
        }

        private readonly global::Celeste.Player player;
        private readonly Sprite sprite;

        public KirbyShoes(global::Celeste.Player player) : base(active: true, visible: false)
        {
            this.player = player;
            sprite = GFX.SpriteBank.Create("kirby_shoes");
            sprite.Color = Calc.HexToColor("ff99cc");
        }

        public override void Render()
        {
            sprite.RenderPosition = player.Sprite.RenderPosition;
            sprite.FlipX = player.Facing == Facings.Left;
            sprite.Render();
        }
    }
}
