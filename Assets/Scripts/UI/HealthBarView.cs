using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image trail;
        private float goal = 1f;
        private Color baseColor;
        private void Awake() { if (fill != null) baseColor = fill.color; }
        public void SetValue(int current, int maximum, bool immediate = false)
        {
            float next = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0;
            if (next > goal) immediate = true;
            goal = next;
            if (fill != null) fill.color = next <= .25f ? new Color(.94f, .39f, .32f) : baseColor;
            if (immediate || !Application.isPlaying) { SetWidth(fill, goal); SetWidth(trail, goal); }
        }
        private void Update()
        {
            SetWidth(fill, Mathf.MoveTowards(fill.rectTransform.anchorMax.x, goal, Time.unscaledDeltaTime * 2.7f));
            if(trail) SetWidth(trail, Mathf.MoveTowards(trail.rectTransform.anchorMax.x, goal, Time.unscaledDeltaTime * .8f));
        }
        private static void SetWidth(Image image, float value)
        {
            if(!image) return;
            var max=image.rectTransform.anchorMax; max.x=value; image.rectTransform.anchorMax=max;
        }
    }
}
