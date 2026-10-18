using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public sealed class DemoSoundControl : MonoBehaviour
    {
        private AudioSource ambience;
        private AudioClip ambienceClip;
        private Text label;
        private float previousVolume;
        private bool muted;

        private void Start()
        {
            previousVolume = AudioListener.volume;
            var canvasObject = new GameObject("SoundControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var control = new GameObject("Sound", typeof(RectTransform), typeof(Image), typeof(Button));
            control.transform.SetParent(canvasObject.transform, false);
            var rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-20, -16);
            rect.sizeDelta = new Vector2(130, 40);
            control.GetComponent<Image>().color = new Color(.06f, .1f, .11f, .94f);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(control.transform, false);
            label = textObject.GetComponent<Text>();
            var existing = FindAnyObjectByType<BattleHUD>();
            label.font = existing != null ? existing.GetComponentInChildren<Text>(true).font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.color = new Color(.85f, .78f, .6f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.text = "SOUND ON";
            control.GetComponent<Button>().onClick.AddListener(ToggleMute);
            // A quiet, seamless temple drone; integer frequencies close the loop.
            const int rate = 22050;
            var samples = new float[rate * 4];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / rate;
                samples[i] = (.6f * Mathf.Sin(2 * Mathf.PI * 110 * time) + .25f * Mathf.Sin(2 * Mathf.PI * 165 * time))
                    * (.8f + .2f * Mathf.Cos(2 * Mathf.PI * .25f * time));
            }
            ambienceClip = AudioClip.Create("TempleDrone", samples.Length, 1, rate, false);
            ambienceClip.SetData(samples, 0);
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = ambienceClip;
            ambience.loop = true;
            ambience.volume = .025f;
            ambience.Play();
        }
        public void ToggleMute()
        {
            muted = !muted;
            AudioListener.volume = muted ? 0 : previousVolume;
            if (label != null) label.text = muted ? "SOUND OFF" : "SOUND ON";
        }
        private void OnDestroy()
        {
            AudioListener.volume = previousVolume;
            if (ambience != null) ambience.Stop();
            if (ambienceClip != null) Destroy(ambienceClip);
        }
    }
}
