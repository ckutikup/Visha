using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visha.UI
{
    public class TargetIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject markers;
        private bool eligible,hovered;
        public void SetEligible(bool value) { eligible=value;markers.SetActive(value);if(!value)hovered=false; }
        public void OnPointerEnter(PointerEventData e) { hovered=true; }
        public void OnPointerExit(PointerEventData e) { hovered=false; }
        private void Update()
        {
            if(!eligible)return;
            float scale=hovered?.97f:1+Mathf.Sin(Time.unscaledTime*3)*.012f;
            markers.transform.localScale=Vector3.Lerp(markers.transform.localScale,Vector3.one*scale,Time.unscaledDeltaTime*16);
        }
    }
}
