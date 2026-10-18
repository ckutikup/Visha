using UnityEngine;
using Visha.UI;

namespace Visha.Campaign
{
    public sealed class CampaignEnvironment : MonoBehaviour
    {
        [SerializeField] private CampaignController campaign;
        [SerializeField] private BattleScreenView screens;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Sprite courtyard;
        [SerializeField] private Sprite vault;
        [SerializeField] private int vaultFirstEncounter = 3;

        private Color targetColor = Color.white;
        private Vector3 backgroundScale;
        private int previousEncounter = -1;
        private readonly Transform[] dust = new Transform[16];
        private Sprite dustSprite;
        private void OnDestroy() { if (dustSprite != null) Destroy(dustSprite); }
        private void Awake()
        {
            backgroundScale = background != null ? background.transform.localScale : Vector3.one;
            dustSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            for (int i = 0; i < dust.Length; i++)
            {
                var mote = new GameObject("TempleDust", typeof(SpriteRenderer));
                mote.transform.SetParent(transform, false);
                mote.transform.position = new Vector3(-7f + i * .91f, -1f + (i % 5) * .8f, 0);
                mote.transform.localScale = Vector3.one * .018f;
                var renderer = mote.GetComponent<SpriteRenderer>();
                renderer.sprite = dustSprite;
                renderer.color = new Color(.8f, .72f, .5f, .2f);
                renderer.sortingOrder = 0;
                dust[i] = mote.transform;
            }
            if (GetComponent<DemoSoundControl>() == null) gameObject.AddComponent<DemoSoundControl>();
        }
        private void Update()
        {
            for (int i = 0; i < dust.Length; i++)
                if (dust[i] != null)
                    dust[i].position = new Vector3(-7f + i * .91f + Mathf.Sin(Time.time * .17f + i) * .12f,
                        -1.5f + Mathf.Repeat(i * .83f + Time.time * .035f, 4.5f), 0);
            if (background != null) background.color = Color.Lerp(background.color, targetColor, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
        }
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (background == null || background.sprite == null || camera == null) return;
            Vector2 size = background.sprite.bounds.size;
            float height = camera.orthographicSize * 2;
            float width = height * camera.pixelWidth / Mathf.Max(1, camera.pixelHeight);
            float scale = Mathf.Max(1, Mathf.Max(width / (size.x * backgroundScale.x), height / (size.y * backgroundScale.y)));
            float lowerHalf = size.y * backgroundScale.y * .5f - 3f;
            if (lowerHalf > .1f) scale = Mathf.Max(scale, (camera.orthographicSize - 3f) / lowerHalf);
            background.transform.localScale = backgroundScale * scale;
            background.transform.position = new Vector3(0, 3f * (scale - 1f), background.transform.position.z);
        }
        private void OnEnable() { if (campaign != null) campaign.StateChanged += Refresh; }
        private void Start() { if (campaign != null) Refresh(campaign.State); }
        private void OnDisable() { if (campaign != null) campaign.StateChanged -= Refresh; }
        private void Refresh(CampaignState state)
        {
            bool inVault = state != CampaignState.Title && campaign.EncounterNumber >= vaultFirstEncounter;
            if (background != null)
            {
                background.sprite = inVault ? vault : courtyard;
                if (state == CampaignState.InBattle && previousEncounter != campaign.EncounterNumber)
                    background.color = new Color(.3f, .34f, .36f);
            }
            previousEncounter = campaign.EncounterNumber;
            bool won = state == CampaignState.Victory || state == CampaignState.DemoComplete;
            targetColor = won ? new Color(1f, .95f, .82f) : inVault ? new Color(.83f, .88f, .93f) : new Color(.94f, .91f, .83f);
            if (screens != null) screens.SetArea(inVault ? "Vault" : "Courtyard");
        }
    }
}
