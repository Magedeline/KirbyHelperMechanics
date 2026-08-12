using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Maps Player.Dashes to a fixed color tier, applied to Kirby's body sprite
    /// and shoes (see KirbyPlayerController.RenderKirbyOverlay and
    /// KirbyShoes.Render). Read as "at least N dashes": 10+ = rainbow,
    /// 5-9 = gold, 4 = orange, 3 = green, 2 = purple, 1 = bright red, 0 = light
    /// blue.
    /// </summary>
    internal static class KirbyDashColors
    {
        private static readonly Color ZeroDash = Calc.HexToColor("59C7FF");  // light blue
        private static readonly Color OneDash = Calc.HexToColor("FF3B3B");   // bright red
        private static readonly Color TwoDash = Calc.HexToColor("9C27B0");   // purple
        private static readonly Color ThreeDash = Calc.HexToColor("2ECC71"); // green
        private static readonly Color FourDash = Calc.HexToColor("FF8C00");  // orange
        private static readonly Color FiveDash = Calc.HexToColor("FFD700"); // gold

        private const float RainbowCycleSeconds = 2f;

        public static Color GetColor(int dashes, float timeActive)
        {
            if (dashes >= 10)
                return Calc.HsvToColor((timeActive / RainbowCycleSeconds) % 1f, 1f, 1f);
            if (dashes >= 5)
                return FiveDash;
            if (dashes >= 4)
                return FourDash;
            if (dashes >= 3)
                return ThreeDash;
            if (dashes >= 2)
                return TwoDash;
            if (dashes >= 1)
                return OneDash;
            return ZeroDash;
        }
    }
}
