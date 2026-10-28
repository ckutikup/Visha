using UnityEngine;

namespace Visha.Combat.Flow
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GuardianAppearance : MonoBehaviour
    {
        private SpriteRenderer actor, aura;
        private Sprite original, vaultSprite;
        private bool awakened;
        private float started;
        private void Awake()
        {
            actor = GetComponent<SpriteRenderer>(); original = actor.sprite;
            var texture = Resources.Load<Texture2D>("VishakanyaVault");
            if (texture != null && original != null)
                vaultSprite = Sprite.Create(texture, new Rect(texture.width * (203f/1234f), texture.height * (95f/1237f), texture.width * (891f/1234f), texture.height * (1039f/1237f)), new Vector2(.5f,0), texture.height * (1039f/1237f) / original.bounds.size.y);
            var go = new GameObject("GuardianAura",typeof(SpriteRenderer));
            go.transform.SetParent(transform,false);
            aura = go.GetComponent<SpriteRenderer>(); aura.sharedMaterial=actor.sharedMaterial;
            aura.sortingLayerID=actor.sortingLayerID; aura.sortingOrder=actor.sortingOrder-1;
            aura.gameObject.SetActive(false);
        }
        public void SetState(bool inVault, bool active)
        {
            if (actor == null) return;
            actor.sprite = inVault && vaultSprite != null ? vaultSprite : original;
            if (active && !awakened) started=Time.time;
            awakened=active;
            aura.sprite=actor.sprite; aura.gameObject.SetActive(active);
        }
        private void LateUpdate()
        {
            if (!awakened) return;
            float pulse=.5f+.5f*Mathf.Sin((Time.time-started)*3f);
            aura.transform.localScale=new Vector3(1.065f+pulse*.015f,1.03f+pulse*.01f,1);
            aura.color=new Color(.28f,1f,.65f,.4f+pulse*.12f);
        }
        private void OnDestroy()
        {
            if (actor != null) actor.sprite=original;
            if (vaultSprite != null) Destroy(vaultSprite);
            if (aura != null) Destroy(aura.gameObject);
        }
    }
}
