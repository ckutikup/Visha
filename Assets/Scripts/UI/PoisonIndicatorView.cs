using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public class PoisonIndicatorView : MonoBehaviour
    {
        [SerializeField] private Image[] segments;
        private int stacks;
        private float displayed;
        private int capacity = 5;
        private float emphasis;
        private static readonly Color Empty = new Color(.16f,.23f,.24f,1);
        private static readonly Color Venom = new Color(.37f,.87f,.65f,1);
        public void SetValue(int value, int maximum)
        {
            int next=Mathf.Clamp(value,0,Mathf.Max(0,maximum));
            if(next!=stacks) emphasis=1;
            if(next>=stacks || !Application.isPlaying) displayed=next;
            stacks=next;capacity=Mathf.Max(0,maximum);Paint();
        }
        private void Update() { emphasis=Mathf.MoveTowards(emphasis,0,Time.unscaledDeltaTime*3);displayed=Mathf.MoveTowards(displayed,stacks,Time.unscaledDeltaTime*18);Paint(); }
        private void Paint()
        {
            for(int i=0;i<segments.Length;i++)
            {
                bool active = i<capacity;segments[i].gameObject.SetActive(active);
                bool filled = capacity<=segments.Length ? i<displayed : (i+1f)/segments.Length<= displayed/capacity;
                segments[i].color=filled?Venom:Empty;
                float pulse=filled && stacks==capacity && capacity>0?Mathf.Sin(Time.unscaledTime*3)*.055f:0;
                segments[i].transform.localScale=Vector3.one*(1+emphasis*.13f+pulse);
            }
        }
    }
}
