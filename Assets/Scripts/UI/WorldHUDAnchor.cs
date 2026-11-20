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
            var actor = target.GetComponent<SpriteRenderer>();
            if (aboveTarget && actor != null && actor.sprite != null)
                headOffset = actor.sprite.bounds.max.y * target.lossyScale.y;
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
                // Use the visible camera area, including when rendering at another aspect ratio.
                Rect pixels = worldCamera.pixelRect;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pixels.min, uiCamera, out var lower)
                    && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pixels.max, uiCamera, out var upper))
                    bounds = Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
                if (headOffset > 4f && GetComponent<IntentView>() != null)
                    point = new Vector2(bounds.center.x, bounds.yMax - 190);
                point.x = Mathf.Clamp(point.x, bounds.xMin + half.x + 24, bounds.xMax - half.x - 24);
                point.y = Mathf.Clamp(point.y, bounds.yMin + half.y + 24, bounds.yMax - half.y - 24);
                rect.localPosition = new Vector3(point.x, point.y, rect.localPosition.z);
            }
        }
    }
}
