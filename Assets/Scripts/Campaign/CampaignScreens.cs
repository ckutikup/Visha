using UnityEngine;
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
            view.BindNavigation(Guard(campaign.StartCampaign), Guard(campaign.NextEncounter),
                                Guard(campaign.RetryEncounter), Guard(campaign.RestartCampaign),
                                Guard(campaign.ReturnToTitle));
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
            if (view != null) view.BindNavigation(null, null, null, null, null);
        }

        public void Show(CampaignState state)
        {
            if (campaign == null || view == null) return;

            int number = state == CampaignState.Title ? 0 : campaign.EncounterNumber;
            view.SetEncounter(number, campaign.EncounterCount);
            string enemy = campaign.CurrentEncounter != null ? campaign.CurrentEncounter.DisplayName : null;

            switch (state)
            {
                case CampaignState.Title: view.ShowTitle(); break;
                case CampaignState.InBattle: view.ShowBattle(); break;
                case CampaignState.Victory: view.ShowVictory(enemy); break;
                case CampaignState.Defeat: view.ShowDefeat(enemy); break;
                case CampaignState.DemoComplete: view.ShowComplete(); break;
            }
        }
    }
}
