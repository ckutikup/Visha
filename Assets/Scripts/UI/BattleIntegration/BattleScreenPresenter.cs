using UnityEngine;
using Visha.Battle;

namespace Visha.UI
{
    // Connects the existing single-battle outcome to the shared screens.
    public class BattleScreenPresenter : MonoBehaviour
    {
        [SerializeField] private BattleScreenView view;
        [SerializeField] private BattleHUD hud;
        [SerializeField] private BattleOutcomeController outcome;

        private void OnEnable()
        {
            if (view == null || hud == null || outcome == null) return;
            view.SetHUD(hud);
            view.BindNavigation(null, null, outcome.Retry, null, null);
            outcome.OnBattleEnded += OnEnded;
            outcome.OnRetry += OnRetry;
            if (outcome.IsOver) OnEnded(outcome.Outcome);
            else view.ShowBattle();
        }
        private void OnDisable()
        {
            if (outcome != null)
            {
                outcome.OnBattleEnded -= OnEnded;
                outcome.OnRetry -= OnRetry;
            }
            if (view != null) view.BindNavigation(null, null, null, null, null);
        }
        private void OnEnded(BattleOutcome result)
        {
            if (result == BattleOutcome.Victory) view.ShowVictory(hud.EnemyName);
            else view.ShowDefeat(hud.EnemyName);
        }
        private void OnRetry() => view.ShowBattle();
    }
}
