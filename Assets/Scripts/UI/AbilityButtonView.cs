using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visha.UI
{
    [RequireComponent(typeof(Button))]
    public class AbilityButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image accent;
        [SerializeField] private Text description;
        [SerializeField] private string defaultDescription;
        [SerializeField] private AudioSource selectionSource;
        [SerializeField] private AudioClip selectionClip;
        [SerializeField] private BattleHUD hud;
        [SerializeField] private BattleAbility ability;
        private Outline selectionOutline;
        private SkillRing ring;
        private AbilityGlyph glyph;
        private Button button;
        private bool hovered, selected;
        private void Awake()
        {
            button = GetComponent<Button>();
            ring = GetComponentInChildren<SkillRing>(true);
            glyph = GetComponentInChildren<AbilityGlyph>(true);
            button.onClick.AddListener(PlaySelection);
            selectionOutline = gameObject.AddComponent<Outline>();
            selectionOutline.effectColor = new Color(.45f, .91f, .68f, .8f);
            selectionOutline.effectDistance = new Vector2(2, -2);
            selectionOutline.enabled = false;
        }
        private void PlaySelection() { if (selectionSource != null && selectionClip != null) selectionSource.PlayOneShot(selectionClip, .16f); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(PlaySelection); }
        public void SetState(bool isSelected, string reason)
        {
            selected=isSelected;
            if(description) description.text=string.IsNullOrEmpty(reason)?defaultDescription:reason;
        }
        public void OnPointerEnter(PointerEventData e) { hovered=true; if (hud != null) hud.PreviewAbility(ability); }
        public void OnPointerExit(PointerEventData e) { hovered=false; if (hud != null) hud.EndAbilityPreview(); }
        public void OnSelect(BaseEventData e) { hovered = true; if (hud != null) hud.PreviewAbility(ability); }
        public void OnDeselect(BaseEventData e) { hovered = false; if (hud != null) hud.EndAbilityPreview(); }
        private void OnDisable() { hovered=false;transform.localScale=Vector3.one; }
        private void Update()
        {
            if (selectionOutline != null) selectionOutline.enabled = false;
            if (ring != null) ring.color = !button.interactable ? new Color(.46f, .49f, .47f, .65f)
                : selected ? new Color(.53f, 1f, .77f) : hovered ? new Color(.95f, .85f, .6f) : new Color(.69f, .64f, .47f, .8f);
            if (glyph != null) glyph.color = button.interactable ? new Color(.82f, .94f, .83f) : new Color(.52f, .58f, .54f);
            if (description != null) description.color = button.interactable ? new Color(.79f, .86f, .79f) : new Color(.66f, .68f, .65f);
            float scale=button.interactable && (hovered||selected)?1.035f:1;
            transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*scale,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            accent.color=selected?new Color(.45f,.91f,.68f,1):new Color(.70f,.60f,.39f,button.interactable?1:.35f);
        }
    }
}
