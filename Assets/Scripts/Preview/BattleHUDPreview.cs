using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Visha.UI;

namespace Visha.Preview
{
    // Inspector and development samples for the HUD. This does not resolve combat.
    public class BattleHUDPreview : MonoBehaviour
    {
        [SerializeField] private BattleHUD hud;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Button playerTurnButton, enemyTurnButton, victoryButton, defeatButton, resetButton;
        private BattleViewState sample;
        private void OnEnable()
        {
            hud.AbilityRequested += OnAbilityRequested;
            playerTurnButton.onClick.AddListener(ShowPlayerTurn); enemyTurnButton.onClick.AddListener(ShowEnemyTurn);
            victoryButton.onClick.AddListener(ShowVictory); defeatButton.onClick.AddListener(ShowDefeat); resetButton.onClick.AddListener(ResetPreview);
        }
        private void Start() { debugPanel.SetActive(false); ResetPreview(); }
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) debugPanel.SetActive(!debugPanel.activeSelf);
#endif
        }
        private void OnDisable()
        {
            hud.AbilityRequested -= OnAbilityRequested;
            playerTurnButton.onClick.RemoveListener(ShowPlayerTurn); enemyTurnButton.onClick.RemoveListener(ShowEnemyTurn);
            victoryButton.onClick.RemoveListener(ShowVictory); defeatButton.onClick.RemoveListener(ShowDefeat); resetButton.onClick.RemoveListener(ResetPreview);
        }
        private void Show(BattleViewState state)
        {
            hud.Render(new BattleViewState { phase = BattlePhase.Waiting });
            sample = state; hud.Render(sample); hud.SetFeedback(state.phase == BattlePhase.PlayerTurn ? "Choose an ability" : "");
        }
        public void ResetPreview() => Show(new BattleViewState { phase = BattlePhase.PlayerTurn });
        public void ShowPlayerTurn() => Show(new BattleViewState { phase = BattlePhase.PlayerTurn, playerHealth = 22, enemyHealth = 16, poison = 3, fadeAvailable = false });
        public void ShowEnemyTurn() => Show(new BattleViewState { phase = BattlePhase.EnemyTurn, playerHealth = 22, enemyHealth = 16, poison = 3 });
        public void ShowVictory() => Show(new BattleViewState { phase = BattlePhase.Victory, enemyHealth = 0 });
        public void ShowDefeat() => Show(new BattleViewState { phase = BattlePhase.Defeat, playerHealth = 0 });
        private void OnAbilityRequested(BattleAbility ability, int target)
        {
            // Demonstrate input locking without applying damage, poison, or enemy actions.
            sample.phase = BattlePhase.EnemyTurn; hud.Render(sample);
            hud.SetFeedback("Action request recorded. F1 opens the sample controls.");
        }
    }
}
