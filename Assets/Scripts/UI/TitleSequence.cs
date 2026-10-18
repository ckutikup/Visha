using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public sealed class TitleSequence : MonoBehaviour
    {
        [SerializeField] private Text story;
        [SerializeField] private RectTransform backdrop;
        [SerializeField] private CanvasGroup storyGroup;
        private float elapsed;
        private readonly string[] lines =
        {
            "The temple has fallen silent. Its guardians no longer recognise their own.",
            "Vishakanya returns to the courtyard, carrying the venom that may break the curse.",
            "Read their intentions. Let poison work. Strike when the opening comes."
        };

        private void OnEnable() { elapsed = 0f; Refresh(); }
        private void Update() { elapsed += Time.unscaledDeltaTime; Refresh(); }
        private void Refresh()
        {
            int index = Mathf.Min((int)(elapsed / 4.5f), lines.Length - 1);
            if (story != null) story.text = lines[index];
            float localTime = elapsed - index * 4.5f;
            if (storyGroup != null)
                storyGroup.alpha = Mathf.Clamp01(localTime / .6f) *
                    (index < lines.Length - 1 ? Mathf.Clamp01((4.5f - localTime) / .6f) : 1f);
            if (backdrop != null)
                backdrop.localScale = Vector3.one * Mathf.Lerp(1f, 1.045f, Mathf.Clamp01(elapsed / 14f));
        }
        private void OnDisable()
        {
            if (backdrop != null) backdrop.localScale = Vector3.one;
        }
    }
}
