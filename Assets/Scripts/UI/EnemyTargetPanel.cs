using System;
using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public sealed class EnemyTargetPanel : MonoBehaviour
    {
        private Text nameLabel, intentLabel;
        private Image background;
        private Button button;
        private int targetId;
        public static EnemyTargetPanel Create(RectTransform parent, Font font, int slot, Action<int> select)
        {
            var root = new GameObject($"EnemyTarget{slot + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EnemyTargetPanel));
            var rect = root.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(slot * .5f, 0); rect.anchorMax = new Vector2((slot + 1) * .5f, .34f);
            rect.offsetMin = new Vector2(4, 0); rect.offsetMax = new Vector2(-4, -5);
            var panel = root.GetComponent<EnemyTargetPanel>();
            panel.background = root.GetComponent<Image>();
            panel.button = root.GetComponent<Button>(); panel.button.targetGraphic = panel.background;
            panel.button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = panel.button.colors; colors.disabledColor = Color.white; panel.button.colors = colors;
            panel.button.onClick.AddListener(() => select(panel.targetId));
            panel.nameLabel = EnemyFocusPanel.Label(rect, font, "EnemyName", .48f, 1, 21);
            panel.intentLabel = EnemyFocusPanel.Label(rect, font, "NextMove", 0, .48f, 20);
            return panel;
        }
        public void Render(EnemyViewState enemy, bool available, bool selected, string status)
        {
            targetId = enemy.targetId;
            bool alive = enemy.health > 0 && enemy.maxHealth > 0;
            nameLabel.text = (selected ? "› " : "") + (string.IsNullOrWhiteSpace(enemy.name) ? "Enemy" : enemy.name);
            intentLabel.text = !alive ? "Defeated" : status == "Needs poison" ? status :
                (status == "Acting" ? "Now: " : "Next: ") + (enemy.intention ?? "Waiting") +
                (enemy.incomingDamage > 0 ? $"  {enemy.incomingDamage}" : "");
            intentLabel.color = enemy.incomingDamage > 0 && alive ? new Color(.95f,.76f,.48f) : new Color(.7f,.8f,.76f);
            background.color = selected ? new Color(.09f,.23f,.19f,.94f) : new Color(.025f,.06f,.065f,.86f);
            nameLabel.color = alive ? new Color(.91f,.9f,.8f) : new Color(.5f,.54f,.52f);
            button.interactable = available;
        }
    }
}
