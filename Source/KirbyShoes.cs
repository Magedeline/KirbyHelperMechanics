using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby's replacement for Madeline's hair-color dash indicator: the shoes
    /// on Kirby's own body art are recolored every frame to
    /// <see cref="global::Celeste.PlayerHair.Color"/>, the exact color vanilla
    /// would be painting Madeline's hair right now (red/pink/blue by dash
    /// count, the white refill flash, plus whatever OverrideHairColor another
    /// mod or variant has applied). Vanilla keeps updating that color in
    /// Player.UpdateHair every frame even though the hair itself is hidden
    /// while Kirby is active, so nothing here has to re-derive it.
    /// <para>
    /// The art ships with the shoes painted crimson directly into every body
    /// frame (there is no separate shoes layer), so this works as a per-frame
    /// recolor overlay: the first time a given atlas frame is drawn, its pixels
    /// are read back once and a small mask texture is built holding just the
    /// shoe pixels (crimson-family pixels in the bottom <see cref="FeetRows"/>
    /// rows of the frame's opaque bounding box -- the cheeks and mouth sit
    /// higher up, the pink body is nowhere near crimson). That mask is cached
    /// forever and drawn over the body with the same position/origin/scale/
    /// flip as the body sprite, tinted with the hair color, preserving the
    /// original shading as a brightness ramp.
    /// </para>
    /// </summary>
    public class KirbyShoes : Component
    {
        /// <summary>
        /// How many rows up from the bottom of a frame's opaque bounding box
        /// count as "shoes". Two covers the sole plus its highlight row in the
        /// idle/walk/run/jump/float frames; three would start catching the
        /// cheeks in the standing frames.
        /// </summary>
        private const int FeetRows = 2;

        // Luma of AE0449, the main shoe fill color in the idle frame -- pixels
        // are shaded relative to it so darker outline pixels stay darker.
        private const float ReferenceLuma = 0.30f * 0xAE + 0.59f * 0x04 + 0.11f * 0x49;
        private const float MinShade = 0.45f;

        private static readonly Dictionary<MTexture, Texture2D> maskCache = new();

        private readonly global::Celeste.Player player;
        private readonly Sprite body;

        /// <summary>
        /// <paramref name="body"/> is the Kirby body sprite the shoes belong
        /// to -- the mask is derived from whichever frame it is currently
        /// showing, and drawn with that sprite's own transform.
        /// </summary>
        public KirbyShoes(global::Celeste.Player player, Sprite body) : base(active: false, visible: false)
        {
            this.player = player;
            this.body = body;
        }

        /// <summary>Current shoe tint: vanilla's live hair color.</summary>
        public Color Color => player.Hair.Color;

        /// <summary>
        /// Draws the recolored shoes over the body sprite. Must be called right
        /// after the body sprite itself has been drawn, from inside the same
        /// SpriteBatch.
        /// </summary>
        public override void Render()
        {
            MTexture frame = body?.Texture;
            if (frame == null)
                return;

            Texture2D mask = GetMask(frame);
            if (mask == null)
                return;

            Color tint = player.Hair.Color;
            // Mirror vanilla's own flash tinting on the body (tired red flash etc).
            if (body.Color != Color.White)
                tint = new Color(tint.ToVector4() * body.Color.ToVector4());

            float scaleFix = frame.ScaleFix;
            Draw.SpriteBatch.Draw(
                mask,
                body.RenderPosition,
                null,
                tint,
                body.Rotation,
                (body.Origin - frame.DrawOffset) / scaleFix,
                body.Scale * scaleFix,
                body.Effects,
                0f);
        }

        /// <summary>
        /// True for the crimson family the shoes are painted in (strong red,
        /// weak green, blue no stronger than red). The pink body (green ~0xA0)
        /// and the black outline never match.
        /// </summary>
        private static bool IsShoeColor(Color c) =>
            c.R >= 0x60 && c.G <= 0x50 && c.B <= c.R && c.R - c.G >= 0x60;

        private static Texture2D GetMask(MTexture frame)
        {
            if (maskCache.TryGetValue(frame, out Texture2D cached))
                return cached;

            // Not loaded yet (VirtualTexture lazy-loads) -- try again next frame
            // rather than caching a permanent miss.
            Texture2D source = frame.Texture?.Texture_Safe;
            if (source == null || source.IsDisposed)
                return null;

            Texture2D mask = null;
            try
            {
                mask = BuildMask(frame, source);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warn, "KHM", $"[KirbyShoes] Failed to build shoe mask for {frame.AtlasPath}: {e.Message}");
            }

            // Cache null too: a frame with no shoe pixels (or a failed readback)
            // shouldn't be re-scanned every single frame.
            maskCache[frame] = mask;
            return mask;
        }

        private static Texture2D BuildMask(MTexture frame, Texture2D source)
        {
            Rectangle clip = frame.ClipRect;
            int w = clip.Width;
            int h = clip.Height;
            if (w <= 0 || h <= 0)
                return null;

            Color[] pixels = new Color[w * h];
            source.GetData(0, clip, pixels, 0, pixels.Length);

            int bottom = -1;
            for (int y = h - 1; y >= 0 && bottom < 0; y--)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].A > 0)
                    {
                        bottom = y;
                        break;
                    }
                }
            }
            if (bottom < 0)
                return null;

            Color[] maskPixels = new Color[w * h];
            bool any = false;
            int top = Math.Max(0, bottom - FeetRows + 1);
            for (int y = top; y <= bottom; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = pixels[y * w + x];
                    if (c.A == 0 || !IsShoeColor(c))
                        continue;

                    float luma = 0.30f * c.R + 0.59f * c.G + 0.11f * c.B;
                    float shade = Calc.Clamp(luma / ReferenceLuma, MinShade, 1f);
                    byte v = (byte)Math.Round(shade * 255f);
                    maskPixels[y * w + x] = new Color(v, v, v, (byte)255);
                    any = true;
                }
            }
            if (!any)
                return null;

            var mask = new Texture2D(Engine.Instance.GraphicsDevice, w, h);
            mask.SetData(maskPixels);
            return mask;
        }

        /// <summary>Disposes every cached mask. Called from KirbyPlayerHooks.Unload.</summary>
        internal static void ClearMaskCache()
        {
            foreach (Texture2D mask in maskCache.Values)
                mask?.Dispose();
            maskCache.Clear();
        }
    }
}
