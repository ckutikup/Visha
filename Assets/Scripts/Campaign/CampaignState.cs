namespace Visha.Campaign
{
    /// <summary>
    /// Where the player is in the demo. Screens are shown from this value alone.
    /// </summary>
    public enum CampaignState
    {
        /// <summary>Start screen with the tutorial text. Waiting for Start.</summary>
        Title,
        /// <summary>An encounter is being fought.</summary>
        InBattle,
        /// <summary>The current encounter was won and another follows. Waiting for Next.</summary>
        Victory,
        /// <summary>The current encounter was lost. Waiting for Retry.</summary>
        Defeat,
        /// <summary>The final encounter was won. Waiting for Restart.</summary>
        DemoComplete
    }
}
