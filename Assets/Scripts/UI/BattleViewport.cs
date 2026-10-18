using UnityEngine;

namespace Visha.UI
{
    [RequireComponent(typeof(Camera))]
    public sealed class BattleViewport : MonoBehaviour
    {
        private Camera view;
        private const float HalfHeight = 5.0625f;
        private const float HalfWidth = 9f;
        private void Awake() { view = GetComponent<Camera>(); view.backgroundColor = new Color(.025f, .045f, .05f); }
        private void LateUpdate()
        {
            float aspect = (float)view.pixelWidth / Mathf.Max(1, view.pixelHeight);
            view.orthographicSize = Mathf.Max(HalfHeight, HalfWidth / aspect);
        }
    }
}
