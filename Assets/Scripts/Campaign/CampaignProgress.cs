using System;
using Visha.Battle;

namespace Visha.Campaign
{
    /// <summary>
    /// The campaign's rules with no Unity dependencies: which encounter is current
    /// and which transitions are allowed from each <see cref="CampaignState"/>.
    ///
    ///   Title ──Start──▶ InBattle(0)
    ///   InBattle ──victory──▶ Victory, or DemoComplete after the last encounter
    ///   InBattle ──defeat───▶ Defeat
    ///   Victory ──Next────▶ InBattle(index + 1)
    ///   Defeat  ──Retry───▶ InBattle(same index)
    ///   any     ──Restart─▶ InBattle(0)
    ///   any     ──ReturnToTitle─▶ Title
    ///
    /// Every transition method returns false and changes nothing when called from
    /// the wrong state, so a double-clicked button cannot skip an encounter.
    /// </summary>
    public sealed class CampaignProgress
    {
        public CampaignProgress(int encounterCount)
        {
            if (encounterCount <= 0)
                throw new ArgumentException("A campaign needs at least one encounter.", nameof(encounterCount));
            EncounterCount = encounterCount;
        }

        /// <summary>Fires after every successful transition.</summary>
        public event Action<CampaignState> StateChanged;

        public CampaignState State { get; private set; } = CampaignState.Title;
        public int EncounterCount { get; }

        /// <summary>0-based index of the encounter being fought or just finished.</summary>
        public int EncounterIndex { get; private set; }

        /// <summary>1-based number for display ("Encounter 2 / 3").</summary>
        public int EncounterNumber => EncounterIndex + 1;

        public bool IsLastEncounter => EncounterIndex == EncounterCount - 1;

        public bool Start()
        {
            if (State != CampaignState.Title) return false;
            EncounterIndex = 0;
            SetState(CampaignState.InBattle);
            return true;
        }

        /// <summary>Records the battle result. Only accepted while an encounter is in progress.</summary>
        public bool ReportOutcome(BattleOutcome outcome)
        {
            if (State != CampaignState.InBattle || outcome == BattleOutcome.None) return false;

            if (outcome == BattleOutcome.Defeat) SetState(CampaignState.Defeat);
            else SetState(IsLastEncounter ? CampaignState.DemoComplete : CampaignState.Victory);
            return true;
        }

        public bool Next()
        {
            if (State != CampaignState.Victory) return false;
            EncounterIndex++;
            SetState(CampaignState.InBattle);
            return true;
        }

        public bool Retry()
        {
            if (State != CampaignState.Defeat) return false;
            SetState(CampaignState.InBattle);
            return true;
        }

        public void Restart()
        {
            EncounterIndex = 0;
            SetState(CampaignState.InBattle);
        }

        public void ReturnToTitle()
        {
            EncounterIndex = 0;
            SetState(CampaignState.Title);
        }

        private void SetState(CampaignState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
