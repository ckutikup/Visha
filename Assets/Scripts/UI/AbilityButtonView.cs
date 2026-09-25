using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visha.UI
{
    [RequireComponent(typeof(Button))]
    public class AbilityButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image accent;
        [SerializeField] private Text description;
        [SerializeField] private string defaultDescription;
        private Button button;
        private bool hovered, selected;
        private void Awake() { button=GetComponent<Button>(); }
        public void SetState(bool isSelected, string reason)
        {
            selected=isSelected;
            if(description) description.text=string.IsNullOrEmpty(reason)?defaultDescription:reason;
        }
        public void OnPointerEnter(PointerEventData e) { hovered=true; }
        public void OnPointerExit(PointerEventData e) { hovered=false; }
        private void OnDisable() { hovered=false;transform.localScale=Vector3.one; }
        private void Update()
        {
            float scale=button.interactable && (hovered||selected)?1.035f:1;
            transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*scale,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            accent.color=selected?new Color(.45f,.91f,.68f,1):new Color(.70f,.60f,.39f,button.interactable?1:.35f);
        }
    }
}
