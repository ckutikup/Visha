using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    [Serializable]
    public sealed class EnemyViewState
    {
        public int targetId;
        public string name = "Enemy";
        public int health, maxHealth, poison;
        public int poisonCap = 5;
        public string intention;
        public int incomingDamage;
    }

    public partial class BattleHUD
    {
        private EnemyViewState[] enemyRoster;
        private EnemyTargetPanel[] enemyPanels;
        private RectTransform rosterRoot;
        private EnemyFocusPanel focusPanel;
        private int actingEnemyId = -1;
        public int DisplayedEnemyId => phase == BattlePhase.EnemyTurn && Alive(FindEnemy(actingEnemyId)) ? actingEnemyId : selectedEnemyId;
        public bool IsEnemyActing(int id) => phase == BattlePhase.EnemyTurn && actingEnemyId == id;
        public bool CanSelectEnemy => HasEnemyRoster && inputEnabled && phase == BattlePhase.PlayerTurn && !submitted && actorsAlive;
        public bool IsEnemyAlive(int id) => Alive(FindEnemy(id));
        private int selectedEnemyId = -1;
        public int SelectedEnemyId => selectedEnemyId;
        private bool HasEnemyRoster => enemyRoster != null;

        private void UpdateEnemyRoster(BattleViewState state)
        {
            if (state.enemies != null && (state.enemies.Length > 2 ||
                state.enemies.Any(e => e == null || e.targetId < 0) ||
                state.enemies.Select(e => e.targetId).Distinct().Count() != state.enemies.Length))
                throw new ArgumentException("Supply up to two enemies with unique non-negative target IDs.", nameof(state));
            actingEnemyId = state.actingEnemyId;
            enemyRoster = state.enemies == null ? null : state.enemies.Select(e => new EnemyViewState
            {
                targetId = e.targetId, name = e.name, health = e.health, maxHealth = e.maxHealth,
                poison = e.poison, poisonCap = e.poisonCap, intention = e.intention,
                incomingDamage = e.incomingDamage
            }).ToArray();
            if (!HasEnemyRoster) { selectedEnemyId = -1; return; }
            if (FindEnemy(selectedEnemyId) == null || !Alive(FindEnemy(selectedEnemyId)))
            {
                selectedEnemyId = -1;
                selected = false;
                foreach (var enemy in enemyRoster)
                    if (Alive(enemy)) { selectedEnemyId = enemy.targetId; break; }
            }
        }

        private static bool Alive(EnemyViewState enemy) => enemy != null && enemy.health > 0 && enemy.maxHealth > 0;
        private EnemyViewState FindEnemy(int id) => enemyRoster?.FirstOrDefault(e => e.targetId == id);
        private bool HasLivingTarget() => enemyRoster.Any(Alive);
        private bool HasPoisonedTarget() => enemyRoster.Any(e => Alive(e) && Mathf.Clamp(e.poison, 0, Mathf.Max(0, e.poisonCap)) > 0);
        private bool IsEligibleTarget(int id, BattleAbility ability)
        {
            var enemy = FindEnemy(id);
            return Alive(enemy) && (ability != BattleAbility.SerpentsBite || Mathf.Clamp(enemy.poison, 0, Mathf.Max(0, enemy.poisonCap)) > 0);
        }
        private void RefreshSelectedEnemy()
        {
            var enemy = FindEnemy(selectedEnemyId);
            poison = enemy == null ? 0 : Mathf.Clamp(enemy.poison, 0, Mathf.Max(0, enemy.poisonCap));
            enemyName = string.IsNullOrWhiteSpace(enemy?.name) ? "enemy" : enemy.name;
        }
        private void PrepareEnemyTarget(BattleAbility ability)
        {
            if (IsEligibleTarget(selectedEnemyId, ability)) return;
            var eligible = enemyRoster.FirstOrDefault(e => IsEligibleTarget(e.targetId, ability));
            if (eligible == null) return;
            selectedEnemyId = eligible.targetId;
            selected = false;
            RefreshSelectedEnemy();
        }
        public void SelectEnemy(int targetId)
        {
            if (!HasEnemyRoster || !inputEnabled || phase != BattlePhase.PlayerTurn || submitted || !actorsAlive) return;
            var enemy = FindEnemy(targetId);
            if (!Alive(enemy)) { SetFeedback("Choose a living enemy."); return; }
            selectedEnemyId = targetId;
            RefreshSelectedEnemy();
            selected = true;
            if (pendingAbility.HasValue)
            {
                if (IsEligibleTarget(targetId, pendingAbility.Value)) Submit(pendingAbility.Value);
                else { selected = false; SetFeedback("This enemy needs poison before Serpent’s Bite."); }
            }
            else SetFeedback($"Target: {enemyName} · choose an ability");
            RefreshControls();
        }
        public void CycleEnemyTarget()
        {
            if (!HasEnemyRoster || !inputEnabled || phase != BattlePhase.PlayerTurn || submitted || !actorsAlive) return;
            var eligible = enemyRoster.Where(e => Alive(e) && (!pendingAbility.HasValue || IsEligibleTarget(e.targetId, pendingAbility.Value))).ToArray();
            if (eligible.Length == 0) return;
            int index = Array.FindIndex(eligible, e => e.targetId == selectedEnemyId);
            selectedEnemyId = eligible[(index + 1) % eligible.Length].targetId;
            selected = false;
            RefreshSelectedEnemy();
            SetFeedback($"Target: {enemyName} · Enter confirms · Esc cancels");
            RefreshControls();
        }
        private void RefreshEnemyPanels(bool targeting)
        {
            if (HasEnemyRoster && rosterRoot == null)
            {
                var root = new GameObject("EnemyRoster", typeof(RectTransform));
                rosterRoot = root.GetComponent<RectTransform>();
                rosterRoot.SetParent(transform, false);
                rosterRoot.anchorMin = new Vector2(.50f, .73f);
                rosterRoot.anchorMax = new Vector2(.97f, .91f);
                rosterRoot.offsetMin = rosterRoot.offsetMax = Vector2.zero;
                focusPanel = EnemyFocusPanel.Create(rosterRoot, phaseLabel.font);
                enemyPanels = new EnemyTargetPanel[2];
                for (int i = 0; i < enemyPanels.Length; i++)
                    enemyPanels[i] = EnemyTargetPanel.Create(rosterRoot, phaseLabel.font, i, SelectEnemy);
            }
            if (rosterRoot != null) rosterRoot.gameObject.SetActive(HasEnemyRoster);
            // Legacy scene references stay intact for single-enemy combat.
            enemyNameLabel.transform.parent.gameObject.SetActive(!HasEnemyRoster);
            if (intentView) intentView.gameObject.SetActive(!HasEnemyRoster && actorsAlive);
            if (!HasEnemyRoster) return;
            var focused = FindEnemy(DisplayedEnemyId);
            focusPanel.Render(focused, phase == BattlePhase.EnemyTurn && DisplayedEnemyId == actingEnemyId,
                pendingAbility == BattleAbility.SerpentsBite, biteDamagePerStack);
            for (int i = 0; i < enemyPanels.Length; i++)
            {
                enemyPanels[i].gameObject.SetActive(i < enemyRoster.Length);
                if (i >= enemyRoster.Length) continue;
                var enemy = enemyRoster[i];
                bool available = inputEnabled && phase == BattlePhase.PlayerTurn && actorsAlive && !submitted && Alive(enemy);
                bool eligible = !pendingAbility.HasValue || IsEligibleTarget(enemy.targetId, pendingAbility.Value);
                enemyPanels[i].Render(enemy, available, enemy.targetId == DisplayedEnemyId,
                    !Alive(enemy) ? "Defeated" : targeting && !eligible ? "Needs poison" : phase == BattlePhase.EnemyTurn && enemy.targetId == actingEnemyId ? "Acting" : enemy.targetId == selectedEnemyId ? "Selected" : "");
            }
        }
    }
}
