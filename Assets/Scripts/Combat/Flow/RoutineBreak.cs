using UnityEngine;

namespace Visha.Combat.Flow
{
    public sealed class RoutineBreak : MonoBehaviour
    {
        private bool claimed;
        public bool HasBoon { get; private set; }
        public bool IsOpportunity(string intent) => isActiveAndEnabled && !claimed
            && (intent == "Heavy Blow" || intent == "Crush");

        public bool TryGuard(string intent, bool guarded, bool survived)
        {
            if (!guarded || !survived || !IsOpportunity(intent)) return false;
            claimed = true;
            HasBoon = true;
            return true;
        }

        public int ConsumeBonus()
        {
            if (!isActiveAndEnabled || !HasBoon) return 0;
            HasBoon = false;
            return 1;
        }

        public void ResetEncounter() { claimed = false; HasBoon = false; }
        private void OnDisable() => ResetEncounter();
    }
}
