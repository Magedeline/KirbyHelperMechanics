using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Ported from the Lua `puff` object (PICO-8 Kirby fangame) -- the small
    /// cosmetic poof Kirby spits out. Drifts sideways from the spawn facing,
    /// decelerating, and disappears after ~1 visual frame (~0.12s @60fps).
    ///
    /// Not currently spawned anywhere in K_Player -- wire it up wherever the
    /// Lua triggered it (e.g. an empty Star Spit) with
    /// `Scene.Add(new KirbyPuff(Center, (int)Facing));`.
    ///
    /// No sprite is shipped for this yet: Render() looks for atlas frames at
    /// "particles/kirby/puff" and falls back to a simple fading dot if none
    /// exist, so the effect is never invisible.
    /// </summary>
    [Tracked]
    public class KirbyPuff : Entity
    {
        // Lua: this.spr=13 in init, this.spr+=0.15 per frame, destroyed once
        // spr>=14 -- i.e. it lives for exactly one visual frame, roughly
        // 7 frames / ~0.117s @60fps. Reproduced as a 0..1 progress value that
        // advances at the equivalent per-second rate so it's frame-rate independent.
        private const float FrameAdvancePerSecond = 0.15f * 60f;
        private const float FrameSpan = 14f - 13f;

        private float frame;
        private Vector2 speed;

        public KirbyPuff(Vector2 position, int facing)
            : base(position)
        {
            int flip = facing >= 0 ? 1 : -1;

            // Lua: this.spd.x = 2*p_flipx (px/frame @60fps -> px/sec)
            speed = new Vector2(2f * 60f * flip, 0f);
            // Lua: this.x += 3*p_flipx
            Position += new Vector2(3f * flip, 0f);

            Collidable = false; // Lua: this.solids = false
            Collider = new Hitbox(8f, 10f, 0f, 0f);
            Depth = -50;
        }

        public override void Update()
        {
            base.Update();

            // Lua: this.spd.x *= 0.95 per frame @60fps -- reproduced as an
            // equivalent per-second decay so it matches regardless of frame rate.
            speed.X *= (float)Math.Pow(0.95, 60f * Engine.DeltaTime);
            Position += speed * Engine.DeltaTime;

            frame += FrameAdvancePerSecond * Engine.DeltaTime;
            if (frame >= FrameSpan)
            {
                RemoveSelf();
            }
        }

        public override void Render()
        {
            base.Render();

            MTexture texture = GFX.Game.GetAtlasSubtexturesAt("particles/kirby/puff", 0);
            if (texture != null)
            {
                texture.DrawCentered(Position, Color.White);
                return;
            }

            float t = Calc.ClampedMap(frame, 0f, FrameSpan, 0f, 1f);
            Draw.Circle(Position, MathHelper.Lerp(1f, 4f, t), Color.White * (1f - t), 8);
        }
    }
}
