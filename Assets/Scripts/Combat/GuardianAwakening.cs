namespace Visha.Combat
{
    public sealed class GuardianAwakening
    {
        public const int RequiredCharges = 3;
        public bool Unlocked { get; private set; }
        public int Charges { get; private set; }
        public int ActionsRemaining { get; private set; }
        public bool Used { get; private set; }
        public bool TwinFangUsed { get; private set; }
        public bool Active => ActionsRemaining > 0;
        public bool Ready => Unlocked && !Used && Charges == RequiredCharges;
        public bool TwinFangReady => Active && !TwinFangUsed;

        public void Reset(bool unlocked, bool introductoryEncounter = false)
        {
            Unlocked = unlocked;
            Charges = unlocked && introductoryEncounter ? RequiredCharges : 0;
            ActionsRemaining = 0; Used = false; TwinFangUsed = false;
        }
        public void EarnCharge()
        {
            if (Unlocked && !Used && !Active && Charges < RequiredCharges) Charges++;
        }
        public bool TryActivate()
        {
            if (!Ready) return false;
            Used = true; Charges = 0; ActionsRemaining = 2;
            return true;
        }
        public bool TryUseTwinFang()
        {
            if (!TwinFangReady) return false;
            TwinFangUsed = true;
            return true;
        }
        public void CompletePlayerAction() { if (Active) ActionsRemaining--; }
        public void EndBattle() { ActionsRemaining = 0; }
    }
}
