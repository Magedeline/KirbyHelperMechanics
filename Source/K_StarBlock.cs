using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Kirby's Star Block -- a physics-driven block that falls and rests under
    /// gravity like a crate, and can be inhaled via
    /// <see cref="InhaleableComponent"/> from a distance. Feeds the same
    /// inhale pipeline as enemies
    /// (KirbyPlayerController.PullAndInhaleEnemies), so swallowing one chains
    /// straight into Star Spit exactly like an inhaled enemy would -- the
    /// block is meant to end up as ammunition. Not solid -- extends Actor,
    /// not Solid, so Kirby currently walks/dashes straight through it rather
    /// than being blocked; only inhale interacts with it. Making it a
    /// physical obstacle too (Solid, or a PlayerCollider while un-inhaled) is
    /// a deliberately separate follow-up, not bundled into this first pass.
    /// <para>
    /// Placed via the "KirbyHelperMechanics/K_StarBlock" Lönn entity, in one
    /// of three sizes matching the art in
    /// Graphics/Atlases/Gameplay/objects/KHM/kirby/starblock/{size}.png
    /// (normal=8px, large=16px, oversized=32px).
    /// </para>
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_StarBlock")]
    [Tracked]
    public class K_StarBlock : Actor
    {
        private const float Gravity = 800f;
        private const float MaxFall = 160f;

        private readonly Image image;
        private float speedY;

        public K_StarBlock(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            int px = SizePixels(data.Attr("size", "normal"));

            Collider = new Hitbox(px, px);
            Depth = -1;

            Add(image = new Image(GFX.Game[TexturePath(px)]));
            image.CenterOrigin();
            image.Position = new Vector2(px / 2f, px / 2f);

            // Marker component only -- KirbyPlayerController.PullAndInhaleEnemies
            // checks for its presence to pull/inhale entities that aren't
            // TakeDamage-able "enemies", this block included.
            Add(new InhaleableComponent());
        }

        private static int SizePixels(string size) => size?.ToLowerInvariant() switch
        {
            "large" => 16,
            "oversized" => 32,
            _ => 8,
        };

        private static string TexturePath(int px) => px switch
        {
            16 => "objects/KHM/kirby/starblock/large",
            32 => "objects/KHM/kirby/starblock/oversized",
            _ => "objects/KHM/kirby/starblock/normal",
        };

        public override void Update()
        {
            base.Update();

            if (OnGround())
            {
                speedY = 0f;
                return;
            }

            speedY = Calc.Approach(speedY, MaxFall, Gravity * Engine.DeltaTime);
            MoveV(speedY * Engine.DeltaTime, OnCollideV);
        }

        private void OnCollideV(CollisionData data)
        {
            speedY = 0f;
        }
    }
}
