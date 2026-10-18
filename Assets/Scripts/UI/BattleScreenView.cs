using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Visha.UI
{
    public enum BattleScreen { Battle, Title, Victory, Defeat, Complete }

    public class BattleScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject titlePanel, victoryPanel, defeatPanel, completePanel;
        [SerializeField] private Text encounterLabel, victoryMessage, defeatMessage, feedbackLabel;
        [SerializeField] private Button startButton, nextButton, victoryRetryButton, retryButton, restartButton;
        [SerializeField] private Button[] returnButtons;
        private BattleHUD hud;
        private Action start, next, retry, restart, returnToStart;
        private bool pending;
        private BattleScreen screen = BattleScreen.Battle;
        private int encounter, count;
        private string area;
        public void SetArea(string value) { area = value; UpdateEncounterLabel(); }
        private void UpdateEncounterLabel()
        {
            encounterLabel.text = encounter > 0 ? (string.IsNullOrEmpty(area) ? $"ENCOUNTER {encounter} / {count}" : $"{area.ToUpperInvariant()}  /  {encounter} OF {count}") : string.Empty;
        }
        public BattleScreen Screen => screen;

        private void Awake()
        {
            startButton.onClick.AddListener(RequestStart);
            nextButton.onClick.AddListener(RequestNext);
            victoryRetryButton.onClick.AddListener(RequestRetry);
            retryButton.onClick.AddListener(RequestRetry);
            restartButton.onClick.AddListener(RequestRestart);
            foreach (var button in returnButtons) button.onClick.AddListener(RequestReturn);
            Refresh();
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(RequestStart);
            nextButton.onClick.RemoveListener(RequestNext);
            victoryRetryButton.onClick.RemoveListener(RequestRetry);
            retryButton.onClick.RemoveListener(RequestRetry);
            restartButton.onClick.RemoveListener(RequestRestart);
            foreach (var button in returnButtons) button.onClick.RemoveListener(RequestReturn);
        }

        public void SetHUD(BattleHUD value) { hud = value; Refresh(); }

        // Controllers own navigation; the view only requests it.
        public void BindNavigation(Action start, Action next, Action retry, Action restart, Action returnToStart)
        {
            pending = false;
            this.start = start; this.next = next; this.retry = retry;
            this.restart = restart; this.returnToStart = returnToStart;
            Refresh();
        }

        public void SetEncounter(int number, int total)
        {
            count = Mathf.Max(0, total);
            encounter = Mathf.Clamp(number, 0, count);
            UpdateEncounterLabel();
            Refresh();
        }

        public void ShowTitle() => Show(BattleScreen.Title);
        public void ShowBattle() => Show(BattleScreen.Battle);
        public void ShowVictory(string enemyName)
        {
            victoryMessage.text = $"{DisplayName(enemyName)} defeated." + EncounterMessage();
            Show(BattleScreen.Victory);
        }
        public void ShowDefeat(string enemyName)
        {
            defeatMessage.text = $"Vishakanya has fallen to {DisplayName(enemyName)}." +
                (encounter > 0 ? $"\nRetry encounter {encounter} from the start." : "\nTry this battle again.");
            Show(BattleScreen.Defeat);
        }
        public void ShowComplete() => Show(BattleScreen.Complete);
        public void SetVictoryDetail(string message)
        {
            if (screen == BattleScreen.Victory) victoryMessage.text += "\n" + message;
        }
        private string EncounterMessage() => encounter > 0 ? $"\nEncounter {encounter} of {count} cleared." : string.Empty;
        private static string DisplayName(string name) => string.IsNullOrWhiteSpace(name) ? "the enemy" : name;

        private void Show(BattleScreen value)
        {
            // A new screen is the controller's acknowledgement of navigation.
            if (screen != value) pending = false;
            screen = value;
            feedbackLabel.text = string.Empty;
            Refresh();
            if (EventSystem.current != null)
            {
                Button focus = screen switch
                {
                    BattleScreen.Title => startButton,
                    BattleScreen.Victory => next != null ? nextButton : victoryRetryButton,
                    BattleScreen.Defeat => retryButton,
                    BattleScreen.Complete => restartButton,
                    _ => null
                };
                EventSystem.current.SetSelectedGameObject(focus != null && focus.interactable ? focus.gameObject : null);
            }
        }

        public void RejectNavigation(string reason)
        {
            pending = false;
            feedbackLabel.text = reason ?? string.Empty;
            Refresh();
        }

        public void RequestStart() { if (screen == BattleScreen.Title) Submit(start); }
        public void RequestNext() { if (screen == BattleScreen.Victory) Submit(next); }
        public void RequestRetry() { if (screen == BattleScreen.Defeat || screen == BattleScreen.Victory) Submit(retry); }
        public void RequestRestart() { if (screen == BattleScreen.Complete) Submit(restart); }
        public void RequestReturn() { if (screen == BattleScreen.Victory || screen == BattleScreen.Defeat || screen == BattleScreen.Complete) Submit(returnToStart); }
        private void Submit(Action callback)
        {
            if (pending || callback == null || !isActiveAndEnabled) return;
            pending = true;
            Refresh();
            callback();
        }

        private void Refresh()
        {
            titlePanel.SetActive(screen == BattleScreen.Title);
            victoryPanel.SetActive(screen == BattleScreen.Victory);
            defeatPanel.SetActive(screen == BattleScreen.Defeat);
            completePanel.SetActive(screen == BattleScreen.Complete);
            encounterLabel.gameObject.SetActive(screen == BattleScreen.Battle && encounter > 0);
            if (hud != null) hud.SetInputEnabled(screen == BattleScreen.Battle);
            startButton.interactable = !pending && start != null;
            nextButton.gameObject.SetActive(next != null);
            nextButton.interactable = !pending && next != null;
            victoryRetryButton.gameObject.SetActive(next == null && retry != null);
            victoryRetryButton.interactable = !pending && retry != null;
            retryButton.interactable = !pending && retry != null;
            restartButton.interactable = !pending && restart != null;
            foreach (var button in returnButtons)
            {
                button.gameObject.SetActive(returnToStart != null);
                button.interactable = !pending && returnToStart != null;
            }
        }
    }
}
