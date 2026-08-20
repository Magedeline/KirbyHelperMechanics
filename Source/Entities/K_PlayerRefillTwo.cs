using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Two-charge dash refill -- same shape as K_PlayerRefill but grants 2
    /// dashes instead of 1, mirroring vanilla's double-dash Refill pickup
    /// (introduced in Core/Farewell). Reuses vanilla's own "objects/refillTwo"
    /// sprite so it needs no new art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_PlayerRefillTwo")]
    [Tracked]
    public class K_PlayerRefillTwo : Entity
    {
        private const string SfxTouch = "event:/Celestellaris/game/general/diamond_touch";
        private const string SfxReturn = "event:/Celestellaris/game/general/diamond_return";

        private readonly bool oneUse;
        private readonly bool refillHealth;
        private readonly float respawnTime;

        private readonly Sprite sprite;
        private readonly Wiggler wiggler;
        private readonly SineWave sine;
        private readonly BloomPoint bloom;
        private readonly VertexLight light;

        private float respawnTimer;
        private bool Collected => respawnTimer > 0f;

        public K_PlayerRefillTwo(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            oneUse = data.Bool("oneUse", false);
            refillHealth = data.Bool("refillHealth", true);
            respawnTime = System.Math.Max(0f, data.Float("respawnTime", 2.5f));

            Collider = new Circle(10f);
            Depth = -100;
            Tag = Tags.TransitionUpdate;

            Add(sprite = new Sprite(GFX.Game, "objects/refillTwo/idle"));
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();

            Add(wiggler = Wiggler.Create(1f, 4f, v => sprite.Scale = Vector2.One * (1f + v * 0.2f)));
            Add(sine = new SineWave(0.6f, 0f));
            Add(bloom = new BloomPoint(0.5f, 12f));
            Add(light = new VertexLight(Color.White, 1f, 16, 24));
            Add(new PlayerCollider(OnPlayer));
        }

        public override void Update()
        {
            base.Update();

            if (Collected)
            {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f)
                    Respawn();
                return;
            }

            float offset = sine.Value * 2f;
            sprite.Position = new Vector2(0f, offset);
            bloom.Position = new Vector2(0f, offset);
        }

        private void OnPlayer(global::Celeste.Player player)
        {
            if (Collected)
                return;

            bool isKirby = player.Get<KirbyPlayerController>() != null;
            var healthManager = K_PlayerHealthManager.Instance;

            bool needsDash = player.Dashes < 2;
            bool needsHealth = refillHealth && isKirby && healthManager != null && healthManager.CurrentHP < healthManager.MaxHP;

            if (!needsDash && !needsHealth)
                return;

            Audio.Play(SfxTouch, Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

            player.Dashes = 2;
            player.RefillStamina();
            if (refillHealth && isKirby)
                healthManager?.FullHeal();

            wiggler.Start();
            Collidable = false;

            if (Scene is Level level)
            {
                level.Displacement.AddBurst(Position, 0.5f, 8f, 32f, 0.8f, Ease.QuadOut, Ease.QuadOut);
                level.ParticlesFG.Emit(ParticleTypes.Dust, 16, Position, Vector2.One * 8f);
            }

            Add(new Coroutine(RefillRoutine()));
        }

        private IEnumerator RefillRoutine()
        {
            sprite.Visible = false;
            light.Visible = false;
            bloom.Visible = false;

            if (oneUse)
            {
                RemoveSelf();
                yield break;
            }

            yield return respawnTime > 0.4f ? respawnTime - 0.4f : 0f;

            respawnTimer = respawnTime;
        }

        private void Respawn()
        {
            if (Collidable)
                return;

            Collidable = true;
            sprite.Visible = true;
            light.Visible = true;
            bloom.Visible = true;
            respawnTimer = 0f;

            Audio.Play(SfxReturn, Position);
            wiggler.Start();
        }
    }
}
