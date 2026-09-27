using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public class IntentView : MonoBehaviour
    {
        [SerializeField] private Text actionLabel;
        [SerializeField] private Text damageLabel;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite defendIcon;
        private string lastAction;
        private int lastDamage=-1;
        private float pulse;
        public void SetIntent(string action, int damage, bool alive)
        {
            gameObject.SetActive(alive);
            action=action??string.Empty;
            if(action!=lastAction || damage!=lastDamage) pulse=1;
            lastAction=action;lastDamage=damage;
            actionLabel.text=action.ToUpperInvariant();damageLabel.text=damage>0?damage.ToString():string.Empty;
            icon.sprite=action.ToLowerInvariant().Contains("defend")?defendIcon:attackIcon;
        }
        private void Update()
        {
            pulse=Mathf.MoveTowards(pulse,0,Time.unscaledDeltaTime*3);
            icon.transform.localScale=Vector3.one*(1+.15f*pulse);
        }
    }
}
