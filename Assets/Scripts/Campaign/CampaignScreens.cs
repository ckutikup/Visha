using UnityEngine;
using UnityEngine.UI;
using Visha.UI;

namespace Visha.Campaign
{
    /// <summary>
    /// Drives Charith's shared <see cref="BattleScreenView"/> from the campaign:
    /// binds its Start, Next, Retry, Restart and Return to Start buttons to
    /// <see cref="CampaignController"/>, and shows the screen that matches each
    /// <see cref="CampaignState"/>. It replaces BattleScreenPresenter, which only
    /// knows a single battle, so the scene must not run both.
    ///
    /// No progression rules or screen layout live here.
    /// </summary>
    public class CampaignScreens : MonoBehaviour
    {
        [SerializeField] private CampaignController campaign;
        [SerializeField] private BattleScreenView view;
        [Tooltip("Lets the screens block combat input while an overlay is shown.")]
        [SerializeField] private BattleHUD hud;

        [Header("Ending")]
        [Tooltip("Heading on the complete screen once the final encounter is won.")]
        [SerializeField] private string endingHeading = "THE CURSE IS BROKEN";
        [Tooltip("Message on the complete screen once the final encounter is won.")]
        [TextArea]
        [SerializeField] private string endingMessage =
            "The Asura falls and the temple's oath is broken. Vishakanya is free.\n" +
            "All five encounters cleared. Return to the Temple Map to replay any encounter.";

        public string EndingHeading => endingHeading;
        public string EndingMessage => endingMessage;

        /// <summary>Assigns every reference at runtime, for tests and tooling.</summary>
        public void SetReferences(CampaignController newCampaign, BattleScreenView newView, BattleHUD newHud)
        {
            if (isActiveAndEnabled) Unbind();
            campaign = newCampaign;
            view = newView;
            hud = newHud;
            if (isActiveAndEnabled) Bind();
        }

        private void OnEnable() => Bind();
        private void OnDisable() => Unbind();

        private void Start()
        {
            if (campaign != null) Show(campaign.State);
        }

        private void Bind()
        {
            if (campaign == null || view == null) return;
            if (hud != null) view.SetHUD(hud);
            var map = campaign.GetComponent<TempleMapView>();
            view.BindNavigation(map != null ? (System.Action)map.ContinueCampaign : Guard(campaign.ContinueCampaign),
                                map != null ? (System.Action)map.EnterNext : Guard(campaign.NextEncounter),
                                Guard(campaign.RetryEncounter), map != null ? (System.Action)map.Open : Guard(campaign.RestartCampaign),
                                Guard(campaign.ReturnToTitle));
            view.ConfigureCampaignOptions(map != null ? (System.Action)map.Open : null,
                Guard(campaign.RestartCampaign));
            campaign.StateChanged += Show;
        }

        /// <summary>
        /// The view locks its buttons after a click until the screen changes. If the
        /// campaign refuses the request, unlock them so the player is not stuck.
        /// </summary>
        private System.Action Guard(System.Action navigate)
        {
            return () =>
            {
                CampaignState before = campaign.State;
                navigate();
                if (campaign.State == before) view.RejectNavigation("That option is not available right now.");
            };
        }

        private void Unbind()
        {
            if (campaign != null) campaign.StateChanged -= Show;
            if (view != null) view.ConfigureCampaignOptions(null, null);
            if (view != null) view.BindNavigation(null, null, null, null, null);
        }

        public void Show(CampaignState state)
        {
            if (campaign == null || view == null) return;

            int number = state == CampaignState.Title ? 0 : campaign.EncounterNumber;
            view.SetEncounter(number, campaign.EncounterCount);
            string enemy = campaign.CurrentEncounter != null ? campaign.CurrentEncounter.DisplayName : null;

            view.SetCheckpoint(campaign.HasCheckpoint, campaign.ResumeEncounter + 1,
                campaign.HighestCompletedEncounter + 1, campaign.EncounterCount, campaign.SaveFailed);
            switch (state)
            {
                case CampaignState.Title: view.ShowTitle(); break;
                case CampaignState.InBattle: view.ShowBattle(); break;
                case CampaignState.Victory: view.ShowVictory(enemy); break;
                case CampaignState.Defeat: view.ShowDefeat(enemy); break;
                case CampaignState.DemoComplete: ShowEnding(); break;
            }
        }

        /// <summary>
        /// The final victory screen: Charith's complete panel with the ending text.
        /// Its Return to Temple Map button opens the map, where every cleared encounter can be replayed.
        /// </summary>
        private void ShowEnding()
        {
            view.ShowComplete();
            SetPanelText("CompletePanel/Heading", endingHeading);
            SetPanelText("CompletePanel/Message", endingMessage);
        }

        private void SetPanelText(string path, string value)
        {
            Transform child = view.transform.Find(path);
            if (child != null && child.TryGetComponent(out Text text)) text.text = value;
        }
    }
}
