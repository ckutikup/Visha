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
            actionLabel.resizeTextForBestFit = true;
            actionLabel.resizeTextMinSize = 14;
            actionLabel.resizeTextMaxSize = actionLabel.fontSize;
            actionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            damageLabel.resizeTextForBestFit = true;
            damageLabel.resizeTextMinSize = 12;
            damageLabel.resizeTextMaxSize = damage > 0 ? 28 : 17;
            damageLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            actionLabel.text=action.ToUpperInvariant();damageLabel.text=damage>0?damage.ToString():"NO ATTACK";
            icon.sprite=damage == 0?defendIcon:attackIcon;
        }
        private void Update()
        {
            pulse=Mathf.MoveTowards(pulse,0,Time.unscaledDeltaTime*3);
            icon.transform.localScale=Vector3.one*(1+.15f*pulse);
        }
    }
}
