using UnityEngine;

namespace Visha.UI
{
    public class WorldHUDAnchor : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset;
        [SerializeField] private Camera worldCamera;
        private RectTransform rect;
        private Canvas canvas;
        private void Awake() { rect=(RectTransform)transform; canvas=GetComponentInParent<Canvas>(); }
        private void LateUpdate()
        {
            if(!target || !worldCamera) return;
            Camera uiCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            Vector2 point;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,
                worldCamera.WorldToScreenPoint(target.position+worldOffset),uiCamera,out point)) rect.anchoredPosition=point;
        }
    }
}
