# Report 3 — Courtyard to Vault

## Implemented locally

Charith's contribution extends the existing campaign with the Vault arrival screen, Vault armor, Guardian Awakening, an empowered Bite, UI feedback and regression tests. The original three abilities remain available in the Courtyard.

- Entering encounter 3 introduces Guardian Awakening. The arrival can be cancelled; it is not repeated on defeat/retry.
- Vault armor replaces the base outfit. Returning to the title or replaying the Courtyard restores the base appearance.
- Awakening activates with 4 or its button during the player's turn without consuming an action. It lasts two successful player actions and can be activated once per encounter.
- The first Vault encounter starts charged, including retries, so the unlock can be learned immediately. Subsequent encounters start empty: maximum-poison Bite earns one charge, and Fade reducing an attack of at least 8 base damage earns one. Three charges are required. Charge cannot accumulate after activation.
- During Awakening, Bite becomes Twin Fang once. It deals normal Bite damage, consumes poison, then leaves 2 poison on a living target. The integration helper transfers these stacks to a supplied second living enemy instead when one is provided.
- The temporary aura ends when the two actions finish or the battle ends. Invalid actions do not consume its duration.
- The two-enemy focused HUD and front/back movement remain a separate presentation preview. The live campaign still resolves one enemy per encounter.

## Review locally

Use Unity 6000.6.3f1 with this project. Stop Play, then select **Visha > Report 3 > Play Vault Review**. This editor-only shortcut clears the two earlier encounters in memory and opens the real Vault arrival screen. It does not edit the saved scene or change the shipped campaign path.

Enter the Vault. Press 4, use Venom Strike on the enemy, then use Twin Fang on the next turn. Watch the action counter, poison residue and return to the normal armored appearance. For the complete path, stop Play and run Campaign_Demo normally.

Use **Visha > UI > Open Two Enemy Preview** separately to review target selection and attack staging. That preview does not apply damage.

## Report evidence

Show the Vault arrival, base versus armored appearance, activation, Twin Fang and its poison residue, expiration, and defeat/retry. Label the two-enemy movement sequence as a presentation preview until the combat integration is finished. Do not report the boss, blessing or difficulty options as completed.

## Teammates building on this work

- Rahul: integrate living enemy rosters, acting IDs and sequential attacks with the focused HUD. Award heavy-guard charge at most once per enemy phase when multiple enemies are present. Preserve the Awakening action count across the entire player action.
- Balaji: review ability balance and use TwinFang.ApplyResidue(struckEnemy, otherEnemy) for a second living enemy. Keep per-enemy poison independent; do not add another damage multiplier without updating previews and tests.
- Anshul: author and balance the two-enemy Inner Vault encounter. Keep enemy intentions accurate and make target selection meaningful.
- Shivani: integrate the additional encounter and replace the current encounter-3 Vault boundary with explicit area configuration as campaign content expands. Verify retry, replay and title resets.
- Akshat: integrate front/back staging with real combat callbacks, maintain sprite proportions, and refine the transformation and attack audio/visual timing.

## Next milestone — preserved design

1. Complete the Inner Vault two-enemy battle and playtest Awakening charge pacing.
2. Build the Sanctum boss with clearly announced phases.
3. Introduce the final guardian appearance at the Sanctum entrance.
4. Grant the Temple's Blessing before the boss: survive one lethal hit at 1 HP, with a visible consumed state. Reset it on a fresh boss attempt; skip repeated introductions on retries.
5. Add brief area-restoration and victory moments without long repeated cutscenes.
6. Consider Standard and Assist difficulty only after the full campaign is balanced. Intent previews must match adjusted damage.

Keep one temporary transformation and one empowered move. Weapon inventory, outfit selection menus and upgrade trees remain outside this milestone.
