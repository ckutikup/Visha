using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Visha.UI
{
    public enum BattleAbility { VenomStrike, SerpentsBite, Fade }
    public enum BattlePhase { Waiting, PlayerTurn, EnemyTurn, Victory, Defeat }

    // A display snapshot supplied by the combat controller. No combat rules run here.
    [Serializable]
    public class BattleViewState
    {
        public int playerHealth = 30;
        public int playerMaxHealth = 30;
        public int enemyHealth = 24;
        public int enemyMaxHealth = 24;
        public int poison;
        public int poisonCap = 5;
        public string enemyName = "Temple Guardian";
        public string intention = "Attack";
        public int incomingDamage = 4;
        public bool fadeAvailable = true;
        public BattlePhase phase = BattlePhase.Waiting;
    }

    public class BattleHUD : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private Text playerHealthLabel;
        [SerializeField] private Text enemyHealthLabel;
        [SerializeField] private Image playerHealthFill;
        [SerializeField] private Image enemyHealthFill;
        [SerializeField] private Text enemyNameLabel;
        [SerializeField] private Text poisonLabel;
        [SerializeField] private Text intentionLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text targetLabel;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Button targetButton;
        [SerializeField] private Button venomButton;
        [SerializeField] private Button biteButton;
        [SerializeField] private Button fadeButton;

        [SerializeField] private HealthBarView playerHealthView;
        [SerializeField] private HealthBarView enemyHealthView;
        [SerializeField] private PoisonIndicatorView poisonView;
        [SerializeField] private IntentView intentView;
        private bool hasRendered;
        [SerializeField] private AbilityButtonView venomView, biteView, fadeView;
        [SerializeField] private TargetIndicator targetIndicator;
        private BattleAbility? pendingAbility;

        public event Action<BattleAbility, int> AbilityRequested;
        public void OnCancel(BaseEventData eventData) => CancelTargeting();
        private BattlePhase phase = BattlePhase.Waiting;
        private bool selected;
        private bool submitted;
        private bool actorsAlive;
        private bool fadeAvailable;
        private int poison;

        private void Awake()
        {
            targetButton.onClick.AddListener(SelectTarget);
            venomButton.onClick.AddListener(ChooseVenomStrike);
            biteButton.onClick.AddListener(ChooseSerpentsBite);
            fadeButton.onClick.AddListener(ChooseFade);
            RefreshControls();
        }

        private void OnDestroy()
        {
            targetButton.onClick.RemoveListener(SelectTarget);
            venomButton.onClick.RemoveListener(ChooseVenomStrike);
            biteButton.onClick.RemoveListener(ChooseSerpentsBite);
            fadeButton.onClick.RemoveListener(ChooseFade);
        }

        /// <summary>Refreshes the view; repeated snapshots cannot unlock an already submitted action.</summary>
        public void Render(BattleViewState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (phase != state.phase) { submitted = false; pendingAbility=null; selected=false; }
            phase = state.phase;
            int playerMax = Mathf.Max(0, state.playerMaxHealth);
            int enemyMax = Mathf.Max(0, state.enemyMaxHealth);
            int playerHP = Mathf.Clamp(state.playerHealth, 0, playerMax);
            int enemyHP = Mathf.Clamp(state.enemyHealth, 0, enemyMax);
            int cap = Mathf.Max(0, state.poisonCap);
            poison = Mathf.Clamp(state.poison, 0, cap);
            actorsAlive = playerHP > 0 && enemyHP > 0;
            fadeAvailable = state.fadeAvailable;
            if (!actorsAlive || phase == BattlePhase.Waiting) selected = false;
            playerHealthLabel.text = $"{playerHP} / {playerMax}";
            enemyHealthLabel.text = $"{enemyHP} / {enemyMax}";
            if(playerHealthView) playerHealthView.SetValue(playerHP,playerMax,!hasRendered);
            else SetFill(playerHealthFill, playerHP, playerMax);
            if(enemyHealthView) enemyHealthView.SetValue(enemyHP,enemyMax,!hasRendered);
            else SetFill(enemyHealthFill, enemyHP, enemyMax);
            if(poisonView) poisonView.SetValue(poison,cap);
            if(intentView) intentView.SetIntent(state.intention,Mathf.Max(0,state.incomingDamage),enemyHP>0);
            hasRendered=true;
            enemyNameLabel.text = state.enemyName;
            poisonLabel.text = $"Poison  {poison} / {cap}";
            if(intentionLabel) intentionLabel.text = enemyHP > 0 ? (state.intention ?? "").ToUpperInvariant() : "";
            phaseLabel.text = phase switch
            {
                BattlePhase.PlayerTurn => "YOUR TURN",
                BattlePhase.EnemyTurn => "ENEMY TURN",
                BattlePhase.Victory => "VICTORY",
                BattlePhase.Defeat => "DEFEAT",
                _ => "PREPARE"
            };
            RefreshControls();
        }

        private static void SetFill(Image fill, int health, int maximum)
        {
            Vector2 anchor = fill.rectTransform.anchorMax;
            anchor.x = maximum > 0 ? (float)health / maximum : 0;
            fill.rectTransform.anchorMax = anchor;
        }

        public void SetFeedback(string message) => feedbackLabel.text = message ?? string.Empty;
        public void SelectTarget()
        {
            if (phase != BattlePhase.PlayerTurn || submitted || !actorsAlive) return;
            selected = true;
            if(pendingAbility.HasValue) Submit(pendingAbility.Value);
            else RefreshControls();
        }
        private void ChooseVenomStrike() => ChooseAbility(BattleAbility.VenomStrike);
        private void ChooseSerpentsBite() => ChooseAbility(BattleAbility.SerpentsBite);
        private void ChooseFade() => ChooseAbility(BattleAbility.Fade);
        public void ChooseAbility(BattleAbility ability)
        {
            if(!CanChoose(ability)) return;
            if(ability==BattleAbility.Fade) { Submit(ability); return; }
            pendingAbility=ability;selected=false;
            if(EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject);
            SetFeedback("Click the guardian to confirm  /  Esc to cancel");RefreshControls();
        }
        public void CancelTargeting()
        {
            if(submitted)return;
            pendingAbility=null;selected=false;SetFeedback("Choose an ability");RefreshControls();
        }
        private bool CanChoose(BattleAbility ability)
        {
            if(phase!=BattlePhase.PlayerTurn || submitted || !actorsAlive || AbilityRequested==null) return false;
            if(ability==BattleAbility.Fade) return fadeAvailable;
            return ability!=BattleAbility.SerpentsBite || poison>0;
        }
        public void RequestVenomStrike() => Submit(BattleAbility.VenomStrike);
        public void RequestSerpentsBite() => Submit(BattleAbility.SerpentsBite);
        public void RequestFade() => Submit(BattleAbility.Fade);

        private bool CanRequest(BattleAbility ability)
        {
            if (phase != BattlePhase.PlayerTurn || submitted || !actorsAlive || AbilityRequested == null)
                return false;
            if (ability == BattleAbility.Fade) return fadeAvailable;
            return selected && (ability != BattleAbility.SerpentsBite || poison > 0);
        }

        private void Submit(BattleAbility ability)
        {
            if (!CanRequest(ability)) return;
            submitted = true;
            pendingAbility=null;
            SetFeedback(ability==BattleAbility.Fade?"Fade":"");
            RefreshControls();
            // Lock before notifying the controller, including against repeated clicks in the same frame.
            AbilityRequested?.Invoke(ability, ability == BattleAbility.Fade ? -1 : 0);
        }

        private void RefreshControls()
        {
            bool targeting=phase==BattlePhase.PlayerTurn && actorsAlive && !submitted && pendingAbility.HasValue;
            targetButton.interactable=targeting;
            targetLabel.text="";
            if(targetIndicator)targetIndicator.SetEligible(targeting);
            venomButton.interactable=CanChoose(BattleAbility.VenomStrike);
            biteButton.interactable=CanChoose(BattleAbility.SerpentsBite);
            fadeButton.interactable=CanChoose(BattleAbility.Fade);
            if(venomView)venomView.SetState(pendingAbility==BattleAbility.VenomStrike, "");
            if(biteView)biteView.SetState(pendingAbility==BattleAbility.SerpentsBite,poison==0?"Requires poison":"");
            if(fadeView)fadeView.SetState(false,fadeAvailable?"":"Unavailable this turn");
        }
    }
}
