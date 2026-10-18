using UnityEngine;
using UnityEngine.InputSystem;

namespace Visha.UI
{
    [RequireComponent(typeof(BattleHUD))]
    public sealed class BattleShortcuts : MonoBehaviour
    {
        private BattleHUD hud;
        private void Awake() => hud = GetComponent<BattleHUD>();
        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.digit1Key.wasPressedThisFrame) hud.ChooseAbility(BattleAbility.VenomStrike);
            if (keys.digit2Key.wasPressedThisFrame) hud.ChooseAbility(BattleAbility.SerpentsBite);
            if (keys.digit3Key.wasPressedThisFrame) hud.ChooseAbility(BattleAbility.Fade);
            if (keys.escapeKey.wasPressedThisFrame) hud.CancelTargeting();
            if (keys.enterKey.wasPressedThisFrame) hud.SelectTarget();
        }
    }
}
