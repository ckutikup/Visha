using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Visha.UI
{
    public enum BattleAbility { VenomStrike, SerpentsBite, Fade }
    public enum BattlePhase { Waiting, PlayerTurn, EnemyTurn, Victory, Defeat, Resolving, Poison }

    // A display snapshot supplied by the combat controller. No combat rules run here.
    [Serializable]
    public class BattleViewState
    {
        public bool awakeningUnlocked, awakeningUsed, twinFangReady;
        public int awakeningCharges, awakeningActions;
        public EnemyViewState[] enemies;
        public int actingEnemyId = -1;
        public int playerHealth = 30;
        public int playerMaxHealth = 30;
        public int enemyHealth = 24;
        public int enemyMaxHealth = 24;
        public int poison;
        public int poisonCap = 5;
        public int poisonDamagePerStack = 2;
        public string enemyName = "Temple Guardian";
        public string intention = "Attack";
        public int incomingDamage = 4;
        public int venomStacks = 2;
        public int biteDamagePerStack = 4;
        public int fadeReductionPercent = 50;
        public bool fadeAvailable = true;
        public bool fadeActive;
        public bool routineOpportunity, routineBoon;
        public BattlePhase phase = BattlePhase.Waiting;
    }

    public partial class BattleHUD : MonoBehaviour, ICancelHandler
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
        [SerializeField] private bool compactAbilities;
        private bool hasRendered;
        private int previousPlayerHealth = -1;
        private Text playerDamage;
        private Text routineLabel;
        private Coroutine damageRoutine;
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
        private int poisonDamagePerStack = 2;
        private int venomStacks = 2, biteDamagePerStack = 4, fadeReductionPercent = 50;
        private bool inputEnabled = true;
        private string enemyName = "enemy";
        public string EnemyName => enemyName;

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
            UpdateEnemyRoster(state);
            awakeningState = state;
            bool phaseChanged = phase != state.phase;
            if (phaseChanged) { submitted = false; pendingAbility=null; selected=false; }
            phase = state.phase;
            int playerMax = Mathf.Max(0, state.playerMaxHealth);
            int enemyMax = Mathf.Max(0, state.enemyMaxHealth);
            int playerHP = Mathf.Clamp(state.playerHealth, 0, playerMax);
            int enemyHP = Mathf.Clamp(state.enemyHealth, 0, enemyMax);
            int cap = Mathf.Max(0, state.poisonCap);
            poison = Mathf.Clamp(state.poison, 0, cap);
            poisonDamagePerStack = state.poisonDamagePerStack;
            actorsAlive = playerHP > 0 && (HasEnemyRoster ? HasLivingTarget() : enemyHP > 0);
            fadeAvailable = state.fadeAvailable;
            venomStacks = Mathf.Max(0, state.venomStacks);
            biteDamagePerStack = Mathf.Max(0, state.biteDamagePerStack);
            fadeReductionPercent = Mathf.Clamp(state.fadeReductionPercent, 0, 100);
            enemyName = string.IsNullOrWhiteSpace(state.enemyName) ? "enemy" : state.enemyName;
            if (HasEnemyRoster) RefreshSelectedEnemy();
            if (!actorsAlive || (pendingAbility.HasValue && !CanChoose(pendingAbility.Value)))
            {
                pendingAbility = null;
                selected = false;
            }
            if (previousPlayerHealth >= 0 && playerHP < previousPlayerHealth)
                ShowPlayerDamage(previousPlayerHealth - playerHP, state.fadeActive);
            else if (playerHP > previousPlayerHealth || phase == BattlePhase.Waiting)
                ClearPlayerDamage();
            previousPlayerHealth = playerHP;
            playerHealthLabel.text = $"{playerHP} / {playerMax}";
            enemyHealthLabel.text = $"{enemyHP} / {enemyMax}";
            if(playerHealthView) playerHealthView.SetValue(playerHP,playerMax,!hasRendered);
            else SetFill(playerHealthFill, playerHP, playerMax);
            if(enemyHealthView) enemyHealthView.SetValue(enemyHP,enemyMax,!hasRendered);
            else SetFill(enemyHealthFill, enemyHP, enemyMax);
            if(poisonView) poisonView.SetValue(poison,cap);
            if(intentView) intentView.SetIntent(state.intention,Mathf.Max(0,state.incomingDamage),enemyHP>0);
            hasRendered=true;
            if (routineLabel == null)
            {
                var go = new GameObject("RoutineHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(transform, false);
                routineLabel = go.GetComponent<Text>();
                routineLabel.font = phaseLabel.font;
                routineLabel.fontSize = 22;
                routineLabel.color = new Color(.63f, .91f, .76f);
                routineLabel.alignment = TextAnchor.MiddleCenter;
                routineLabel.raycastTarget = false;
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(HasEnemyRoster ? .25f : .5f, 1f);
                rect.anchoredPosition = new Vector2(0, -110);
                rect.sizeDelta = new Vector2(HasEnemyRoster ? 800 : 880, 40);
            }
            var routineRect = routineLabel.rectTransform;
            routineRect.anchorMin = routineRect.anchorMax = new Vector2(HasEnemyRoster ? .25f : .5f, 1f);
            routineRect.sizeDelta = new Vector2(HasEnemyRoster ? 800 : 880, 40);
            routineLabel.text = state.routineBoon ? "CURSE FRAGMENT · next Venom +1 poison (cap 5)"
                : state.routineOpportunity ? "BREAK THE ROUTINE · Fade through this heavy attack" : "";
            routineLabel.gameObject.SetActive(actorsAlive && phase != BattlePhase.Waiting);
            enemyNameLabel.text = enemyName;
            poisonLabel.text = $"Poison  {poison} / {cap}";
            if(intentionLabel) intentionLabel.text = enemyHP > 0 ? (state.intention ?? "").ToUpperInvariant() : "";
            phaseLabel.text = phase switch
            {
                BattlePhase.PlayerTurn => "YOUR TURN",
                BattlePhase.Resolving => "YOUR ACTION",
                BattlePhase.Poison => "POISON",
                BattlePhase.EnemyTurn => "ENEMY TURN",
                BattlePhase.Victory => "VICTORY",
                BattlePhase.Defeat => "DEFEAT",
                _ => "PREPARE"
            };
            if (phaseChanged)
                SetFeedback(phase == BattlePhase.PlayerTurn ? "Choose an ability" : string.Empty);
            RefreshControls();
        }

        private void ShowPlayerDamage(int damage, bool guarded)
        {
            if (playerDamage == null)
            {
                var popup = new GameObject("PlayerDamage", typeof(RectTransform), typeof(Text), typeof(Shadow));
                popup.transform.SetParent(playerHealthLabel.transform.parent, false);
                playerDamage = popup.GetComponent<Text>();
                playerDamage.font = playerHealthLabel.font;
                playerDamage.fontSize = 34;
                playerDamage.alignment = TextAnchor.MiddleCenter;
                playerDamage.raycastTarget = false;
                playerDamage.rectTransform.anchorMin = playerDamage.rectTransform.anchorMax = new Vector2(.5f, .5f);
                playerDamage.rectTransform.sizeDelta = new Vector2(340, 56);
                var shadow = popup.GetComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, .95f);
                shadow.effectDistance = new Vector2(2, -2);
            }
            ClearPlayerDamage();
            playerDamage.text = guarded ? $"−{damage}  GUARDED" : $"−{damage} HP";
            playerDamage.gameObject.SetActive(true);
            damageRoutine = StartCoroutine(AnimatePlayerDamage(guarded));
        }

        private IEnumerator AnimatePlayerDamage(bool guarded)
        {
            float elapsed = 0;
            Color color = guarded ? new Color(.6f, .95f, .8f) : new Color(1f, .53f, .4f);
            while (elapsed < .9f)
            {
                elapsed += Time.unscaledDeltaTime;
                playerDamage.rectTransform.anchoredPosition = new Vector2(0, 170 + elapsed * 38);
                color.a = 1f - Mathf.InverseLerp(.5f, .9f, elapsed);
                playerDamage.color = color;
                yield return null;
            }
            playerDamage.gameObject.SetActive(false);
            damageRoutine = null;
        }

        private void ClearPlayerDamage()
        {
            if (damageRoutine != null) StopCoroutine(damageRoutine);
            damageRoutine = null;
            if (playerDamage != null) playerDamage.gameObject.SetActive(false);
        }

        private void OnDisable() => ClearPlayerDamage();

        /// <summary>Campaign overlays disable combat input without changing the battle state.</summary>
        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
            if (!value) { pendingAbility = null; selected = false; }
            RefreshControls();
        }

        /// <summary>The controller calls this when it rejects a request without consuming a turn.</summary>
        public void RejectAction(string reason)
        {
            if (phase != BattlePhase.PlayerTurn) return;
            submitted = false;
            pendingAbility = null;
            selected = false;
            SetFeedback(reason);
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
            if (HasEnemyRoster) { SelectEnemy(selectedEnemyId); return; }
            if (!inputEnabled || phase != BattlePhase.PlayerTurn || submitted || !actorsAlive) return;
            selected = true;
            if(pendingAbility.HasValue) Submit(pendingAbility.Value);
            else RefreshControls();
        }
        private void ChooseVenomStrike() => ChooseAbility(BattleAbility.VenomStrike);
        private void ChooseSerpentsBite() => ChooseAbility(BattleAbility.SerpentsBite);
        private void ChooseFade() => ChooseAbility(BattleAbility.Fade);
        public void ChooseAbility(BattleAbility ability)
        {
            if (!CanChoose(ability))
            {
                PreviewAbility(ability);
                return;
            }
            if(ability==BattleAbility.Fade) { Submit(ability); return; }
            pendingAbility=ability;selected=false;
            if(EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject);
            string preview = ability == BattleAbility.SerpentsBite
                ? $"{poison * biteDamagePerStack} damage; consumes {poison} poison"
                : $"Adds up to {venomStacks} poison; each stack deals {poisonDamagePerStack}/turn and stays";
            if (compactAbilities)
                preview = ability == BattleAbility.SerpentsBite ? $"{poison * biteDamagePerStack} damage · consumes poison" : $"+{venomStacks} poison · {poisonDamagePerStack}/stack each turn";
            if (HasEnemyRoster)
            {
                PrepareEnemyTarget(ability);
                SetFeedback($"{enemyName} · click enemy or Enter to confirm · Esc cancels");
                RefreshControls();
                return;
            }
            SetFeedback(compactAbilities ? $"{preview} · click enemy or Enter · Esc cancels" : $"{preview} · click {enemyName} to confirm / Esc to cancel");
            RefreshControls();
        }
        public void PreviewAbility(BattleAbility ability)
        {
            if (!inputEnabled || !compactAbilities || phase != BattlePhase.PlayerTurn || submitted || pendingAbility.HasValue) return;
            string message = ability == BattleAbility.VenomStrike
                ? $"Venom Strike · +{venomStacks} poison; each stack deals {poisonDamagePerStack} damage per turn"
                : ability == BattleAbility.SerpentsBite
                    ? poison > 0 ? $"Serpent's Bite · {poison * biteDamagePerStack} damage; consumes all poison" : "Serpent's Bite · use Venom Strike to add poison first"
                    : fadeAvailable ? $"Fade · reduce the next enemy phase by {fadeReductionPercent}%" : "Fade is recovering · use Venom Strike or Bite first";
            if (HasEnemyRoster && ability == BattleAbility.SerpentsBite && poison == 0 && HasPoisonedTarget())
                message = "Serpent’s Bite · choose an enemy with poison";
            SetFeedback(message);
        }
        public void EndAbilityPreview()
        {
            if (inputEnabled && compactAbilities && phase == BattlePhase.PlayerTurn && !submitted && !pendingAbility.HasValue)
                SetFeedback("Choose an ability");
        }

        public void CancelTargeting()
        {
            if(submitted || !inputEnabled || phase != BattlePhase.PlayerTurn)return;
            pendingAbility=null;selected=false;SetFeedback("Choose an ability");RefreshControls();
        }
        private bool CanChoose(BattleAbility ability)
        {
            if(!inputEnabled || phase!=BattlePhase.PlayerTurn || submitted || !actorsAlive || AbilityRequested==null) return false;
            if(ability==BattleAbility.Fade) return fadeAvailable;
            return ability!=BattleAbility.SerpentsBite || (HasEnemyRoster ? HasPoisonedTarget() : poison>0);
        }
        public void RequestVenomStrike() => Submit(BattleAbility.VenomStrike);
        public void RequestSerpentsBite() => Submit(BattleAbility.SerpentsBite);
        public void RequestFade() => Submit(BattleAbility.Fade);

        private bool CanRequest(BattleAbility ability)
        {
            if (!inputEnabled || phase != BattlePhase.PlayerTurn || submitted || !actorsAlive || AbilityRequested == null)
                return false;
            if (ability == BattleAbility.Fade) return fadeAvailable;
            return selected && (!HasEnemyRoster || IsEligibleTarget(selectedEnemyId, ability)) && (ability != BattleAbility.SerpentsBite || poison > 0);
        }

        private void Submit(BattleAbility ability)
        {
            if (!CanRequest(ability)) return;
            submitted = true;
            pendingAbility=null;
            SetFeedback(ability==BattleAbility.Fade?"Fade":"");
            RefreshControls();
            // Lock before notifying the controller, including against repeated clicks in the same frame.
            AbilityRequested?.Invoke(ability, ability == BattleAbility.Fade ? -1 : HasEnemyRoster ? selectedEnemyId : 0);
        }

        private void RefreshControls()
        {
            RefreshAwakening();
            bool targeting=inputEnabled && phase==BattlePhase.PlayerTurn && actorsAlive && !submitted && pendingAbility.HasValue;
            targetButton.interactable=targeting && !HasEnemyRoster;
            RefreshEnemyPanels(targeting);
            targetLabel.text=targeting && !HasEnemyRoster ? "SELECT TARGET" : string.Empty;
            if(targetIndicator)targetIndicator.SetEligible(targeting && !HasEnemyRoster);
            venomButton.interactable=CanChoose(BattleAbility.VenomStrike);
            biteButton.interactable=CanChoose(BattleAbility.SerpentsBite);
            fadeButton.interactable=CanChoose(BattleAbility.Fade);
            if(venomView)venomView.SetState(pendingAbility==BattleAbility.VenomStrike, compactAbilities ? $"+{venomStacks} poison" : $"+{venomStacks} poison · persists each turn");
            if(biteView)biteView.SetState(pendingAbility==BattleAbility.SerpentsBite,poison==0?(HasEnemyRoster && HasPoisonedTarget() ? "Choose poisoned target" : "Needs poison"):compactAbilities ? $"{poison * biteDamagePerStack} DMG" : $"{poison * biteDamagePerStack} damage · consumes poison");
            if (awakeningState != null && awakeningState.twinFangReady && biteView && poison > 0)
                biteView.SetState(pendingAbility==BattleAbility.SerpentsBite,$"{poison*biteDamagePerStack} DMG · leaves 2 poison");
            if(fadeView)fadeView.SetState(false,fadeAvailable ? (compactAbilities ? $"{fadeReductionPercent}% guard" : $"Reduce enemy phase by {fadeReductionPercent}%") : (compactAbilities ? "Recover" : "Recovering · use another ability"));
        }
    }
}
