using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Solid door that opens permanently once a player carrying the matching
    /// K_Key id touches it, consuming the key. Placeholder colored-rect
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_LockedDoor")]
    [Tracked]
    public class K_LockedDoor : Solid
    {
        private static readonly Color BodyColor = Calc.HexToColor("3a2f22");
        private static readonly Color LockColor = Calc.HexToColor("ffe866");

        private readonly string requiredKeyId;
        private bool unlocking;

        public K_LockedDoor(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            requiredKeyId = data.Attr("keyId", "");
            Depth = -9997;
        }

        public override void Update()
        {
            base.Update();

            if (unlocking || Scene == null)
                return;

            global::Celeste.Player player = Scene.Tracker.GetEntity<global::Celeste.Player>();
            K_KeyCarrier carrier = player?.Get<K_KeyCarrier>();
            if (player != null && carrier != null && carrier.Has(requiredKeyId) && CollideCheck(player))
            {
                carrier.Remove(requiredKeyId);
                Add(new Coroutine(UnlockRoutine()));
            }
        }

        private IEnumerator UnlockRoutine()
        {
            unlocking = true;
            Audio.Play("event:/Celestellaris/game/05_fractured/key_unlock", Position);
            StartShaking(0.3f);
            yield return 0.3f;

            Collidable = false;
            Visible = false;
        }

        public override void Render()
        {
            Draw.Rect(Collider, BodyColor);
            Draw.Rect(X + Width / 2f - 2f, Y + Height / 2f - 2f, 4f, 4f, LockColor);
        }
    }
}
