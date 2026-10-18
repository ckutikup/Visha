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
        [SerializeField] private bool fixedPosition;
        private Vector3 homePosition;
        private void Awake() { if (target != null) homePosition = target.position; rect=(RectTransform)transform; canvas=GetComponentInParent<Canvas>(); }
        private void LateUpdate()
        {
            if(!target || !worldCamera) return;
            Camera uiCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            Vector2 point;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,
                worldCamera.WorldToScreenPoint((fixedPosition ? homePosition : target.position)+worldOffset),uiCamera,out point)) rect.anchoredPosition=point;
        }
    }
}
