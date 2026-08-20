using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Entities
{
    /// <summary>
    /// Carries collected K_Key ids for the current Player instance -- added on
    /// first pickup, naturally goes away on death/respawn since Player itself
    /// is recreated then (see KirbyPlayerHooks' class comment), which is what
    /// makes keys "lost on death" without any extra bookkeeping.
    /// </summary>
    public class K_KeyCarrier : Component
    {
        private readonly HashSet<string> keys = new();

        public K_KeyCarrier() : base(active: false, visible: false) { }

        public void Add(string id) => keys.Add(id);
        public bool Has(string id) => keys.Contains(id);
        public void Remove(string id) => keys.Remove(id);
        public int Count => keys.Count;
    }

    /// <summary>
    /// Vanilla-style Key pickup -- adds its id to the player's K_KeyCarrier on
    /// touch and removes itself. No visual tether to a carried-key icon yet
    /// (that's a follow-up once real art exists). Placeholder diamond
    /// rendering stands in for real art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_Key")]
    [Tracked]
    public class K_Key : Entity
    {
        private static readonly Color BodyColor = Calc.HexToColor("ffe866");

        private readonly string id;
        private float bob;

        public K_Key(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            id = data.Attr("id", data.ID.ToString());
            Collider = new Hitbox(8f, 8f, -4f, -4f);
            Depth = -100;

            Add(new PlayerCollider(OnPlayer));
        }

        public override void Update()
        {
            base.Update();
            bob += Engine.DeltaTime;
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            var carrier = player.Get<K_KeyCarrier>();
            if (carrier == null)
                player.Add(carrier = new K_KeyCarrier());

            carrier.Add(id);
            Audio.Play("event:/Celestellaris/game/general/key_get", Position);
            RemoveSelf();
        }

        public override void Render()
        {
            Vector2 pos = Position + new Vector2(0f, (float)System.Math.Sin(bob * 2f) * 2f);
            Draw.Rect(pos - new Vector2(4f, 4f), 8f, 8f, BodyColor);
            Draw.HollowRect(pos - new Vector2(4f, 4f), 8f, 8f, Color.Black * 0.6f);
        }
    }
}
