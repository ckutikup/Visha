using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visha.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip click;
        private Button button;
        private bool hovered, focused;
        private void Awake() { button = GetComponent<Button>(); button.onClick.AddListener(PlayClick); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(PlayClick); }
        private void PlayClick() { if (source != null && click != null) source.PlayOneShot(click, .22f); }
        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = false; }
        public void OnSelect(BaseEventData e) { focused = true; }
        public void OnDeselect(BaseEventData e) { focused = false; }
        private void Update()
        {
            float goal = button.interactable && (hovered || focused) ? 1.025f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * goal,
                1f - Mathf.Exp(-15f * Time.unscaledDeltaTime));
        }
        private void OnDisable() { hovered = focused = false; transform.localScale = Vector3.one; }
    }
}
