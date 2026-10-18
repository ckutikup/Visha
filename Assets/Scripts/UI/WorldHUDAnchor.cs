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
        [SerializeField] private bool aboveTarget;
        [SerializeField] private Vector2 screenOffset;
        private float headOffset;
        private void Awake()
        {
            if (target != null)
            {
                homePosition = target.position;
                var sprite = target.GetComponent<SpriteRenderer>();
                if (sprite != null) headOffset = sprite.bounds.max.y - target.position.y;
            }
            rect = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
        }
        private void LateUpdate()
        {
            if(!target || !worldCamera) return;
            Camera uiCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            Vector2 point;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,
                worldCamera.WorldToScreenPoint((fixedPosition ? homePosition : target.position)+worldOffset + (aboveTarget ? Vector3.up * headOffset : Vector3.zero)),uiCamera,out point))
            {
                point += screenOffset;
                var parent = (RectTransform)rect.parent;
                Vector2 half = rect.rect.size * .5f;
                Rect bounds = parent.rect;
                point.x = Mathf.Clamp(point.x, bounds.xMin + half.x + 24, bounds.xMax - half.x - 24);
                point.y = Mathf.Clamp(point.y, bounds.yMin + half.y + 24, bounds.yMax - half.y - 24);
                rect.anchoredPosition = point;
            }
        }
    }
}
