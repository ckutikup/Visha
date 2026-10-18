using UnityEngine;

namespace Visha.Combat.Flow
{
    public sealed class CharacterIdleMotion : MonoBehaviour
    {
        [SerializeField] private Health health;
        private Vector3 restScale;
        private Quaternion restRotation;
        private Sprite shadowSprite;
        private Texture2D shadowTexture;
        private GameObject shadow;
        private float phase;
        private Vector3 restPosition;
        private int previousHealth;
        private float recoil;
        public Vector3 ActionOffset { get; set; }
        public Vector3 HomePosition => transform.parent == null ? restPosition : transform.parent.TransformPoint(restPosition);

        private void Awake()
        {
            restScale = transform.localScale;
            restPosition = transform.localPosition;
            previousHealth = health != null ? health.CurrentHealth : 0;
            restRotation = transform.localRotation;
            phase = transform.position.x > 0 ? 1.6f : 0f;
            shadowTexture = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            shadowTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[64 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f, dy = (y - 15.5f) / 15.5f;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - dx * dx - dy * dy), 1.5f) * .55f;
                    pixels[y * 64 + x] = new Color(.015f, .02f, .025f, alpha);
                }
            shadowTexture.SetPixels(pixels); shadowTexture.Apply();
            shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, 64, 32), new Vector2(.5f, .5f), 32);
            shadow = new GameObject("GroundShadow", typeof(SpriteRenderer));
            shadow.transform.SetParent(transform.parent, false);
            shadow.transform.position = transform.position + new Vector3(0, .025f, 0);
            shadow.transform.localScale = new Vector3(1.25f, .28f, 1);
            var renderer = shadow.GetComponent<SpriteRenderer>();
            renderer.sprite = shadowSprite;
            renderer.sortingLayerID = GetComponent<SpriteRenderer>().sortingLayerID;
            renderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;
        }

        private void Update()
        {
            bool alive = health == null || !health.IsDead;
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * .8f);
            transform.localPosition = restPosition + ActionOffset + Vector3.right * (restPosition.x > 0 ? recoil : -recoil);
            float breath = alive ? Mathf.Sin(Time.time * 2f + phase) : 0;
            transform.localScale = Vector3.Scale(restScale, new Vector3(1f - breath * .003f, 1f + breath * .006f, 1f));
            if (shadow != null)
            {
                shadow.transform.localScale = new Vector3(1.25f + breath * .008f, .28f, 1);
                shadow.transform.position = transform.position + new Vector3(0, .025f, 0);
            }
            transform.localRotation = restRotation * Quaternion.Euler(0, 0, (alive ? Mathf.Sin(Time.time * 1.3f + phase) * .25f : 0) + recoil * (restPosition.x > 0 ? -14f : 14f));
        }
        private void OnDisable()
        {
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
            transform.localScale = restScale; transform.localRotation = restRotation;
            transform.localPosition = restPosition; recoil = 0; ActionOffset = Vector3.zero;
            if (shadow != null) shadow.SetActive(false);
        }
        private void OnEnable()
        {
            if (shadow != null) shadow.SetActive(true);
            if (health != null) health.OnHealthChanged += HandleHealthChanged;
        }
        private void HandleHealthChanged(int current, int maximum)
        {
            recoil = current < previousHealth ? Mathf.Clamp((previousHealth - current) * .025f, .16f, .3f) : 0;
            previousHealth = current;
        }
        private void OnDestroy()
        {
            if (shadow != null) Destroy(shadow);
            if (shadowSprite != null) Destroy(shadowSprite);
            if (shadowTexture != null) Destroy(shadowTexture);
        }
    }
}
