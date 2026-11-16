using System;
using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    public partial class BattleScreenView
    {
        private Action openMap, newGame;
        private Button titleMap, victoryMap, newGameButton;
        private Text checkpointLabel;
        private bool confirmNewGame, hasCheckpoint;

        public void ConfigureCampaignOptions(Action map, Action restartGame)
        {
            openMap = map;
            newGame = restartGame;
            if (restartGame != null && newGameButton == null)
            {
                titleMap = CopyButton(startButton, "TempleMap", "Temple Map", new Vector2(-240, -395), () => Submit(openMap));
                newGameButton = CopyButton(startButton, "NewGame", "New Game", new Vector2(240, -395), RequestNewGame);
                victoryMap = CopyButton(nextButton, "TempleMap", "Temple Map", new Vector2(-220, -190), () => Submit(openMap));
                var returnButton = nextButton.transform.parent.Find("ReturnButton").GetComponent<Button>();
                var returnRect = (RectTransform)returnButton.transform;
                returnRect.anchoredPosition = new Vector2(220, -190);
                returnRect.sizeDelta = new Vector2(400, 64);
                ((RectTransform)victoryMap.transform).sizeDelta = new Vector2(400, 64);
                returnButton.GetComponentInChildren<Text>().text = "Return to Title";
                var go = new GameObject("Checkpoint", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(titlePanel.transform, false);
                checkpointLabel = go.GetComponent<Text>();
                checkpointLabel.font = startButton.GetComponentInChildren<Text>().font;
                checkpointLabel.fontSize = 22;
                checkpointLabel.alignment = TextAnchor.MiddleCenter;
                checkpointLabel.color = new Color(.78f, .85f, .78f);
                checkpointLabel.raycastTarget = false;
                var rect = checkpointLabel.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0, -470);
                rect.sizeDelta = new Vector2(1300, 50);
            }
            Refresh();
        }

        public void SetCheckpoint(bool exists, int nextEncounter, int cleared, int total, bool failed)
        {
            hasCheckpoint = exists;
            if (newGame == null) return;
            startButton.GetComponentInChildren<Text>().text = exists ? "Continue" : "Start Game";
            nextButton.GetComponentInChildren<Text>().text = "Next Encounter";
            if (checkpointLabel != null)
                checkpointLabel.text = failed ? "Progress could not be saved. Keep the game open and check storage access."
                    : exists ? $"{cleared} / {total} encounters cleared · continue at encounter {nextEncounter} · saved locally"
                    : "Progress saves at encounter checkpoints. Continuing restarts the fight at full health.";
        }

        private Button CopyButton(Button source, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var copy = Instantiate(source, source.transform.parent);
            copy.name = name;
            copy.onClick = new Button.ButtonClickedEvent();
            copy.onClick.AddListener(action);
            copy.GetComponentInChildren<Text>().text = label;
            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(430, 64);
            return copy;
        }

        private void RequestNewGame()
        {
            if (pending || newGame == null) return;
            if (hasCheckpoint && !confirmNewGame)
            {
                confirmNewGame = true;
                newGameButton.GetComponentInChildren<Text>().text = "Confirm New Game";
                checkpointLabel.text = "This replaces your saved progress. Choose Continue to keep it.";
                return;
            }
            Submit(newGame);
        }

        private void ResetNewGameConfirmation()
        {
            confirmNewGame = false;
            if (newGameButton != null) newGameButton.GetComponentInChildren<Text>().text = "New Game";
        }

        private void RefreshCampaignOptions()
        {
            if (newGameButton == null) return;
            titleMap.gameObject.SetActive(openMap != null);
            victoryMap.gameObject.SetActive(openMap != null);
            newGameButton.gameObject.SetActive(newGame != null);
            titleMap.interactable = victoryMap.interactable = !pending && openMap != null;
            newGameButton.interactable = !pending && newGame != null;
        }
    }
}
