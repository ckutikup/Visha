using UnityEngine;
using UnityEngine.UI;

namespace Visha.Campaign
{
    /// <summary>
    /// Shows the campaign's overlay screens: the title/tutorial screen, the
    /// victory panel with Next, the defeat panel with Retry, and the end-of-demo
    /// screen with Restart. Buttons only forward to <see cref="CampaignController"/>;
    /// no progression rules live here.
    /// </summary>
    public class CampaignScreens : MonoBehaviour
    {
        [SerializeField] private CampaignController campaign;

        [Header("Screens")]
        [SerializeField] private GameObject titleScreen;
        [SerializeField] private GameObject victoryScreen;
        [SerializeField] private GameObject defeatScreen;
        [SerializeField] private GameObject endScreen;

        [Header("Labels")]
        [SerializeField] private Text encounterLabel;
        [SerializeField] private Text victoryMessage;
        [SerializeField] private Text defeatMessage;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button restartButton;
        [Tooltip("Optional buttons that return to the start screen.")]
        [SerializeField] private Button[] titleButtons = new Button[0];

        private void OnEnable()
        {
            if (campaign == null) return;
            campaign.StateChanged += Show;
            startButton.onClick.AddListener(campaign.StartCampaign);
            nextButton.onClick.AddListener(campaign.NextEncounter);
            retryButton.onClick.AddListener(campaign.RetryEncounter);
            restartButton.onClick.AddListener(campaign.RestartCampaign);
            foreach (Button button in titleButtons) if (button) button.onClick.AddListener(campaign.ReturnToTitle);
        }

        private void OnDisable()
        {
            if (campaign == null) return;
            campaign.StateChanged -= Show;
            startButton.onClick.RemoveListener(campaign.StartCampaign);
            nextButton.onClick.RemoveListener(campaign.NextEncounter);
            retryButton.onClick.RemoveListener(campaign.RetryEncounter);
            restartButton.onClick.RemoveListener(campaign.RestartCampaign);
            foreach (Button button in titleButtons) if (button) button.onClick.RemoveListener(campaign.ReturnToTitle);
        }

        private void Start()
        {
            Show(campaign != null ? campaign.State : CampaignState.Title);
        }

        public void Show(CampaignState state)
        {
            titleScreen.SetActive(state == CampaignState.Title);
            victoryScreen.SetActive(state == CampaignState.Victory);
            defeatScreen.SetActive(state == CampaignState.Defeat);
            endScreen.SetActive(state == CampaignState.DemoComplete);

            if (campaign == null) return;
            int number = campaign.EncounterNumber;
            int count = campaign.EncounterCount;
            string enemy = campaign.CurrentEncounter != null ? campaign.CurrentEncounter.DisplayName : "";

            if (encounterLabel)
            {
                encounterLabel.gameObject.SetActive(state != CampaignState.Title);
                encounterLabel.text = $"ENCOUNTER {number} / {count}";
            }
            if (victoryMessage) victoryMessage.text = $"{enemy} defeated.\nEncounter {number} of {count} cleared.";
            if (defeatMessage) defeatMessage.text = $"Visha has fallen to the {enemy}.\nRetry encounter {number} from the start.";
        }
    }
}
