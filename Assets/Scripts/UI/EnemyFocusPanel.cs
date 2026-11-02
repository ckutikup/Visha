using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public sealed class EnemyFocusPanel : MonoBehaviour
    {
        private Text nameLabel, detailsLabel;
        private Image fill;
        public int DisplayedId { get; private set; } = -1;
        public static EnemyFocusPanel Create(RectTransform parent, Font font)
        {
            var go = new GameObject("EnemyFocus", typeof(RectTransform), typeof(Image), typeof(EnemyFocusPanel));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0,.38f); rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.02f,.055f,.06f,.92f);
            go.GetComponent<Image>().raycastTarget = false;
            var panel = go.GetComponent<EnemyFocusPanel>();
            panel.nameLabel = Label(rect,font,"Name",.54f,1,27);
            panel.detailsLabel = Label(rect,font,"Details",0,.42f,23);
            var track = new GameObject("HealthTrack", typeof(RectTransform), typeof(Image));
            var tr = track.GetComponent<RectTransform>(); tr.SetParent(rect,false);
            tr.anchorMin = new Vector2(.04f,.44f); tr.anchorMax = new Vector2(.96f,.51f); tr.offsetMin = tr.offsetMax = Vector2.zero;
            track.GetComponent<Image>().color = new Color(.17f,.22f,.21f); track.GetComponent<Image>().raycastTarget = false;
            var bar = new GameObject("HealthFill", typeof(RectTransform), typeof(Image));
            var br = bar.GetComponent<RectTransform>(); br.SetParent(tr,false);
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            panel.fill = bar.GetComponent<Image>(); panel.fill.raycastTarget = false;
            return panel;
        }
        internal static Text Label(RectTransform parent, Font font, string name, float bottom, float top, int size)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Text));
            var text = go.GetComponent<Text>(); text.transform.SetParent(parent,false);
            text.rectTransform.anchorMin = new Vector2(0,bottom); text.rectTransform.anchorMax = new Vector2(1,top);
            text.rectTransform.offsetMin = new Vector2(12,0); text.rectTransform.offsetMax = new Vector2(-12,0);
            text.font = font; text.fontSize = size; text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 15; text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.91f,.9f,.8f);
            text.supportRichText = false; text.raycastTarget = false;
            return text;
        }
        public void Render(EnemyViewState enemy, bool acting, bool bite, int bitePerStack)
        {
            DisplayedId = enemy == null ? -1 : enemy.targetId;
            gameObject.SetActive(enemy != null);
            if (enemy == null) return;
            int maximum = Mathf.Max(0,enemy.maxHealth), health = Mathf.Clamp(enemy.health,0,maximum);
            int cap = Mathf.Max(0,enemy.poisonCap), poison = Mathf.Clamp(enemy.poison,0,cap);
            nameLabel.text = (acting ? "ACTING · " : "") + (string.IsNullOrWhiteSpace(enemy.name) ? "Enemy" : enemy.name);
            nameLabel.color = acting ? new Color(1f,.68f,.4f) : new Color(.91f,.9f,.8f);
            detailsLabel.text = $"{health} / {maximum} HP" + (poison > 0 ? $"   ·   Poison {poison}/{cap}" : "") +
                (bite && poison > 0 ? $"   ·   Bite {poison * bitePerStack}" : "");
            fill.rectTransform.anchorMax = new Vector2(maximum > 0 ? (float)health / maximum : 0,1);
            fill.color = health <= maximum * .25f ? new Color(.94f,.39f,.32f) : new Color(.37f,.8f,.65f);
        }
    }
}
