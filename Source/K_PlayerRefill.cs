using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.KirbyHelperMechanics;

namespace Celeste.Entities
{
    /// <summary>
    /// Refill pickup that also tops up Kirby's HP (a concept vanilla's own Refill
    /// doesn't know about) via <see cref="KirbyPlayerController"/>/<see cref="K_PlayerHealthManager"/>.
    /// Dash/stamina refills work on the real vanilla Player like vanilla's own
    /// Refill entity now that Kirby has no separate Actor/shadow -- this exists
    /// purely for the combined dash+stamina+HP mapper convenience, reusing
    /// vanilla's "objects/refill" sprite/sound so it needs no new art.
    /// </summary>
    [CustomEntity(ids: "KirbyHelperMechanics/K_PlayerRefill")]
    [Tracked]
    public class K_PlayerRefill : Entity
    {
        private const string SfxTouch = "event:/game/general/diamond_touch";
        private const string SfxReturn = "event:/game/general/diamond_return";

        private readonly bool oneUse;
        private readonly bool refillDash;
        private readonly bool refillStamina;
        private readonly bool refillHealth;
        private readonly float respawnTime;

        private readonly Sprite sprite;
        private readonly Wiggler wiggler;
        private readonly SineWave sine;
        private readonly BloomPoint bloom;
        private readonly VertexLight light;

        private float respawnTimer;
        private bool Collected => respawnTimer > 0f;

        public K_PlayerRefill(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            oneUse = data.Bool("oneUse", false);
            refillDash = data.Bool("refillDash", true);
            refillStamina = data.Bool("refillStamina", true);
            refillHealth = data.Bool("refillHealth", true);
            respawnTime = System.Math.Max(0f, data.Float("respawnTime", 2.5f));

            Collider = new Circle(10f);
            Depth = -100;
            Tag = Tags.TransitionUpdate;

            Add(sprite = new Sprite(GFX.Game, "objects/refill/idle"));
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

            var healthManager = K_PlayerHealthManager.Instance;
            bool isKirby = player.Get<KirbyPlayerController>() != null;

            bool needsDash = refillDash && player.Dashes < player.MaxDashes;
            bool needsStamina = refillStamina && player.Stamina < global::Celeste.Player.ClimbTiredThreshold;
            bool needsHealth = refillHealth && isKirby && healthManager != null && healthManager.CurrentHP < healthManager.MaxHP;

            if (!needsDash && !needsStamina && !needsHealth)
                return;

            Audio.Play(SfxTouch, Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

            if (refillDash)
                player.RefillDash();
            if (refillStamina)
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
