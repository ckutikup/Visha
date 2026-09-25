using UnityEngine;

namespace Visha.UI
{
    public class WorldHUDAnchor : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset;
        [SerializeField] private Camera worldCamera;
        private RectTransform rect;
        private void Awake() { rect=(RectTransform)transform; }
        private void LateUpdate()
        {
            if(!target || !worldCamera) return;
            Vector2 point;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,
                worldCamera.WorldToScreenPoint(target.position+worldOffset),null,out point)) rect.anchoredPosition=point;
        }
    }
}
