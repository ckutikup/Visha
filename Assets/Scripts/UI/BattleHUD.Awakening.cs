using System;
using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public partial class BattleHUD
    {
        public event Action AwakeningRequested;
        private BattleViewState awakeningState;
        private Button awakeningButton;
        private Text awakeningText;
        private Text biteTitle;
        public void RequestAwakening()
        {
            if (awakeningState == null || !awakeningState.awakeningUnlocked || awakeningState.awakeningUsed ||
                awakeningState.awakeningCharges < 3 || !inputEnabled || submitted || !actorsAlive || phase != BattlePhase.PlayerTurn) return;
            CancelTargeting();
            AwakeningRequested?.Invoke();
        }
        private void RefreshAwakening()
        {
            if (awakeningState == null) return;
            if (biteTitle == null && biteButton != null)
                foreach (var label in biteButton.GetComponentsInChildren<Text>(true))
                    if (label.text == "Serpent’s Bite" || label.text == "Serpent's Bite" || label.text == "SERPENT'S BITE") { biteTitle = label; break; }
            if (biteTitle != null) biteTitle.text = awakeningState.twinFangReady ? "Twin Fang" : "Serpent's Bite";
            if (!awakeningState.awakeningUnlocked)
            {
                if (awakeningButton != null) awakeningButton.gameObject.SetActive(false);
                return;
            }
            if (awakeningButton == null)
            {
                var go = new GameObject("Awakening",typeof(RectTransform),typeof(Image),typeof(Button));
                go.transform.SetParent(transform,false);
                var rect=(RectTransform)go.transform;
                rect.anchorMin=new Vector2(.025f,.15f); rect.anchorMax=new Vector2(.34f,.23f);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
                var image=go.GetComponent<Image>(); image.color=new Color(.07f,.19f,.16f,.95f);
                awakeningButton=go.GetComponent<Button>(); awakeningButton.targetGraphic=image;
                awakeningButton.onClick.AddListener(RequestAwakening);
                var label=new GameObject("Label",typeof(RectTransform),typeof(Text)); label.transform.SetParent(go.transform,false);
                awakeningText=label.GetComponent<Text>(); awakeningText.font=phaseLabel.font;
                awakeningText.fontSize=26; awakeningText.resizeTextForBestFit=true; awakeningText.resizeTextMinSize=16; awakeningText.resizeTextMaxSize=26;
                awakeningText.alignment=TextAnchor.MiddleCenter; awakeningText.color=new Color(.78f,.94f,.81f); awakeningText.raycastTarget=false;
                awakeningText.rectTransform.anchorMin=Vector2.zero; awakeningText.rectTransform.anchorMax=Vector2.one;
                awakeningText.rectTransform.offsetMin=new Vector2(12,4); awakeningText.rectTransform.offsetMax=new Vector2(-12,-4);
            }
            awakeningButton.gameObject.SetActive(true);
            bool active=awakeningState.awakeningActions>0;
            awakeningText.text=active ? $"AWAKENED · {awakeningState.awakeningActions} actions left"
                : awakeningState.awakeningUsed ? "Awakening spent"
                : awakeningState.awakeningCharges>=3 ? "AWAKEN  [4] · two actions"
                : $"AWAKENING  {awakeningState.awakeningCharges}/3 · charge with Bite / heavy guard";
            awakeningButton.interactable=!active && !awakeningState.awakeningUsed && awakeningState.awakeningCharges>=3 && inputEnabled && !submitted && actorsAlive && phase==BattlePhase.PlayerTurn;
        }
    }
}
