using UnityEngine;

namespace Visha.Combat.Flow
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GuardianAppearance : MonoBehaviour
    {
        private SpriteRenderer actor;
        private SpriteRenderer aura;

        private Sprite original;
        private Sprite vaultSprite;

        private bool awakened;
        private float started;

        private void Awake()
        {
            actor = GetComponent<SpriteRenderer>();
            original = actor.sprite;

            var texture = Resources.Load<Texture2D>("VishakanyaVault");

            if (texture != null && original != null)
            {
                vaultSprite = Sprite.Create(
                    texture,
                    new Rect(
                        texture.width * (203f / 1234f),
                        texture.height * (95f / 1237f),
                        texture.width * (891f / 1234f),
                        texture.height * (1039f / 1237f)
                    ),
                    new Vector2(.5f, 0),
                    texture.height * (1039f / 1237f) / original.bounds.size.y
                );
            }

            var go = new GameObject(
                "GuardianAura",
                typeof(SpriteRenderer)
            );

            go.transform.SetParent(transform, false);

            aura = go.GetComponent<SpriteRenderer>();
            aura.sharedMaterial = actor.sharedMaterial;
            aura.sortingLayerID = actor.sortingLayerID;
            aura.sortingOrder = actor.sortingOrder - 1;
            aura.gameObject.SetActive(false);
        }

        public void SetState(bool inVault, bool active)
        {
            if (actor == null)
                return;

            actor.sprite =
                inVault && vaultSprite != null
                    ? vaultSprite
                    : original;

            if (active && !awakened)
                started = Time.time;

            awakened = active;

            aura.sprite = actor.sprite;
            aura.gameObject.SetActive(active);

            if (!active)
                actor.color = Color.white;
        }

        private void LateUpdate()
        {
            if (!awakened || aura == null || actor == null)
                return;

            float pulse =
                .5f +
                .5f * Mathf.Sin(
                    (Time.time - started) * 3f
                );

            aura.transform.localScale = new Vector3(
                1.065f + pulse * .015f,
                1.03f + pulse * .01f,
                1f
            );

            // Stronger Sanctum aura while retaining the
            // established green Guardian visual language.
            aura.color = new Color(
                0.32f + pulse * 0.08f,
                1f,
                0.62f + pulse * 0.12f,
                0.38f + pulse * 0.18f
            );

            // Very subtle pulse on the Guardian itself.
            actor.color = Color.Lerp(
                Color.white,
                new Color(
                    0.88f,
                    1f,
                    0.92f,
                    1f
                ),
                pulse * 0.35f
            );
        }

        private void OnDestroy()
        {
            if (actor != null)
            {
                actor.sprite = original;
                actor.color = Color.white;
            }

            if (vaultSprite != null)
                Destroy(vaultSprite);

            if (aura != null)
                Destroy(aura.gameObject);
        }
    }
}