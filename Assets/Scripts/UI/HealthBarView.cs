using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image trail;
        private float goal = 1f;
        public void SetValue(int current, int maximum, bool immediate = false)
        {
            goal = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0;
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
