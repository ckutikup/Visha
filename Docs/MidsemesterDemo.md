# Midsemester demo

## What works

Three single-enemy encounters: Temple Guardian (30 HP), Guardian Warden (44 HP), and Vault Sentinel (46 HP). Each encounter starts the player at 50 HP. Start, Next, defeat/retry, demo completion, restart and return to title are connected.

The first two fights take place in the Courtyard. The third introduces the Vault, with a sealed serpent door, abandoned offerings and a broken chain. This is a partial campaign: the remaining Vault encounter and Sanctum boss are not implemented.

## Combat rules

The order is player ability, poison damage, then the surviving enemy's action. This restores the order in the submitted pitch. An enemy killed by Bite or poison cannot attack or advance its pattern.

- Venom Strike adds 2 poison stacks, capped at 5. Each stack deals 2 damage per round and persists until consumed or the encounter resets.
- Serpent's Bite consumes all poison for 4 damage per stack. Its button previews the current burst damage. Bite is unavailable at zero stacks and does not consume a turn.
- Fade halves the next enemy phase's damage, rounding down, and cannot be used consecutively. Another successful ability restores its availability.

For example, an enemy at 14 HP with 4 poison stacks can be killed immediately by Bite for 16 damage. Adding more poison instead reaches the cap and deals 10 poison damage, leaving that enemy alive to attack. This is the intended decision: preserve damage over time or spend the stacks to remove an immediate threat.

A second situation: at the five-stack cap against an enemy above 20 HP, both Venom and Fade leave 10 poison damage ticking. If Crush is incoming, Fade reduces its 16 damage to 8 while Venom adds no stacks. This makes defending useful without adding an energy system. These are rule examples, not evidence from human playtests.

Guardian: Strike (4), Heavy Blow (9), then Recover (no attack).
Warden: the same cycle begins on Heavy Blow, then Recover, then Strike. Its opening is restored on retry.
Sentinel: Recover, Crush (16), then Shield Bash (8).
Recover is a non-attacking turn; it does not grant armour. This is a label correction from the earlier report, not a new defensive rule.

## Presentation

Venom Strike uses a short wind-up and melee lunge. Bite moves Vishakanya toward the enemy and back. Enemy attacks have a brief wind-up, approach and retreat. Damage is applied at impact. Fade steps back and becomes translucent while its protection is active. Heavy enemy attacks have a longer wind-up; defeated characters become dim.

These are transform-based sprite movements, not a full set of frame-by-frame character animations. Input stays locked while a turn resolves. Returning to title or resetting an encounter cancels pending animation and damage.

A quiet synthesized drone, selection feedback, and the Sound On/Off button support the demo. Dust drifts behind the combatants. New encounters fade the environment into view, and victory shifts it toward warmer light. No camera shake is used.

The HUD uses Barlow, fixed health panels, clear action labels, current enemy intention, poison counters and damage previews. Ground shadows follow the characters. The title has an optional three-part story preview; Enter the Courtyard starts immediately.

## Recording sequence

1. Briefly show the title and story preview, then start.
2. Explain the visible enemy intention and the three abilities.
3. Use Venom Strike and show the approach and strike, poison application, poison damage and enemy response in order.
4. Use Fade before a heavy attack. Point out that it cannot be repeated immediately.
5. Build poison, then use Bite to show the approach animation and consumption of stacks.
6. Show Next and the Warden's different opening. Demonstrate retry if time permits.
7. Enter the Vault to show the environment change and Sentinel pattern.
8. Show the demo ending and explain the remaining work.

## Remaining semester work

- Rahul: two-enemy sequencing, per-target action locking and edge-case integration.
- Balaji: balance poison/Bite/Fade through playtests, test damage and reset rules, and assess whether each ability is useful.
- Anshul: the remaining Vault encounter and telegraphed Sanctum boss, with meaningful combinations and timing.
- Charith: multi-target UI, readable previews for more than one enemy, accessibility and layout checks.
- Shivani: full five-encounter progression, short area introductions, final ending and transition handling.
- Akshat: Sanctum presentation, consistent visual/audio polish, exported builds and playtest coordination.

No new ultimate, energy system, equipment or upgrades are required. The pitch explicitly limits the player to three fixed abilities; Bite is the poison-powered finisher. Full cutscenes and full character animation remain outside the submitted scope.

This presentation and integration pass should be described separately from each teammate's original contribution. All members should agree on the participation summary.

## Running the demo

Use Unity 6000.6.3f1 and Assets/Scenes/Campaign_Demo.unity. Stop Play mode and reload the saved scene after importing this revision. BattleUI_Demo is the separate single-battle scene. Rebuild any exported application before using it for the recording; editor tests do not verify an older standalone build.

## Scope and verification limits

The current third fight is a single-enemy Vault preview. It does not satisfy the final pitch's two-enemy Vault encounters. Full five-encounter progression, multi-target combat, the boss, and final balancing remain unfinished. A first-play duration of 4–7 minutes is a target to measure, not a result or course requirement.

The midsemester rubric asks for a working demo consistent with the reports, detailed future plans, an accessible Zoom recording with all six members on camera, and an agreed participation summary. Report 2 described Bite and Fade as pending; demonstrate their current implementation as progress since that report.

Suggested remaining sequence: first integrate two-enemy combat and target UI; next finish Vault combinations and the Sanctum boss; reserve the final two project weeks for balance, accessibility, regression checks and exported-build playtests. Owners remain as listed above.

The battle layout uses corner area/progress text, a small turn indicator and three separate action buttons over the environment. The full-width title band and large action-tray background have been removed. Health labels render above their bar decorations with a dark shadow.

Enemy hits now pause briefly at contact, push and tilt the player back, and show the actual health lost for 0.9 seconds. Fade hits are labelled GUARDED. Restarting clears the label and recoil. No damage values were changed by this feedback revision.
