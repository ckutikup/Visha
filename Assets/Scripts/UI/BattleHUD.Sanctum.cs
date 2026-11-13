using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public partial class BattleHUD
    {
        private Text bossPhaseLabel;
        private Text blessingLabel;

        private void RefreshSanctumStatus(BattleViewState state)
        {
            bool inBattle = state.phase != BattlePhase.Waiting &&
                state.phase != BattlePhase.Victory && state.phase != BattlePhase.Defeat;
            bool showBoss = inBattle && state.bossPhase > 0 && state.bossPhaseCount > 0;
            bool showBlessing = inBattle && state.blessing != BlessingStatus.Unavailable;

            if (showBoss && bossPhaseLabel == null)
                bossPhaseLabel = CreateStatusLabel("BossPhase", enemyNameLabel);
            if (bossPhaseLabel != null)
            {
                bossPhaseLabel.gameObject.SetActive(showBoss);
                bossPhaseLabel.text = showBoss
                    ? $"PHASE {Mathf.Clamp(state.bossPhase, 1, state.bossPhaseCount)} / {state.bossPhaseCount}" : "";
            }

            if (showBlessing && blessingLabel == null)
                blessingLabel = CreateStatusLabel("TempleBlessing", playerHealthLabel);
            if (blessingLabel != null)
            {
                blessingLabel.gameObject.SetActive(showBlessing);
                blessingLabel.text = state.blessing == BlessingStatus.Ready
                    ? "BLESSING READY · survive one lethal hit" : "BLESSING SPENT";
                blessingLabel.color = state.blessing == BlessingStatus.Ready
                    ? new Color(.94f, .83f, .57f) : new Color(.70f, .73f, .72f);
            }
        }

        private Text CreateStatusLabel(string objectName, Text owner)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(owner.transform.parent, false);
            var label = go.GetComponent<Text>();
            label.font = owner.font;
            label.fontSize = 20;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.94f, .83f, .57f);
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 8);
            rect.sizeDelta = new Vector2(0, 32);
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .9f);
            shadow.effectDistance = new Vector2(1, -1);
            return label;
        }
    }
}
