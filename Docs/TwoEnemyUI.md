# Two-enemy battle UI

## Scope

This change supplies the UI and input contract for two enemies. It does not implement multi-enemy combat, enemy patterns, campaign progression, or damage. The existing campaign continues using the single-enemy snapshot.

## Review locally

1. Open this project with Unity 6000.6.3f1.
2. Choose **Visha > UI > Open Two Enemy Preview**. Save any current scene changes when prompted.
3. Press Play. This is a UI-only sample, not a campaign encounter.
4. Choose Venom Strike and click an enemy or its intention indicator. A request locks input while a presentation-only enemy sequence plays. No damage is applied.
5. Press R to reset. Choose Bite: the HUD focuses the poisoned Sentinel, but still requires confirmation. The unpoisoned Guardian rejects Bite.
6. Use Tab to change the highlighted eligible target and Enter to confirm. Escape cancels the pending ability. Keys 1, 2 and 3 choose abilities.
7. K marks the highlighted enemy defeated; T toggles the turn phase; L toggles a long enemy name. R restores the sample.
8. Check 1280x720, 1920x1080 and 1024x768 in the Game view.
9. Stop Play and reopen Assets/Scenes/Campaign_Demo.unity to run the actual game. Do not add the preview to Build Profiles.

Space previews enemies stepping forward in turn. The shared detail panel follows the actor, then restores the selected target. R safely cancels the sequence. The sample does not apply damage or advance campaign state. Record it as UI demonstration evidence until the combat integration is complete.

## Combat integration — Rahul

Populate `BattleViewState.enemies` with up to two `EnemyViewState` entries. A null array preserves legacy single-enemy behavior; an empty array means no available targets.

Each entry contains `targetId`, `name`, `health`, `maxHealth`, `poison`, `poisonCap`, `intention`, and `incomingDamage`. IDs must be unique and non-negative, stay stable during the encounter, and map to the controller's combatants. Do not renumber when an enemy dies. Dead entries can remain visible with zero health.

The existing `AbilityRequested(BattleAbility, int)` event now returns the chosen enemy ID. Fade still returns -1. Validate the request against live combat state; call `RejectAction(reason)` if rejected without consuming a turn. Render phase transitions to release the submission lock for the next player turn. Repeated snapshots in the same phase do not unlock a submitted action.

Set `actingEnemyId` during `EnemyTurn` to show the acting enemy in the shared detail panel. It does not overwrite `SelectedEnemyId`. Reset it to -1 outside the enemy phase.

The HUD copies enemy snapshots and does not mutate them. Only living enemies can be targeted. Bite is offered when any living enemy has poison, but confirmation is checked against the chosen enemy. Campaign overlays should continue calling `SetInputEnabled(false)`.

## Other integrations

- Balaji: supply per-enemy poison values; agree Fade's full enemy-phase behavior with Rahul. The HUD does not apply those rules.
- Anshul: provide two enemy records with the same stable IDs used by combat and their individual intentions.
- Shivani: supply a fresh snapshot on encounter start/retry and use the existing overlay input lock.
- Akshat: one shared detail panel and two compact intention indicators occupy the upper-right band. The preview demonstrates front/back staging using current campaign sprites. Integrate movement with the combat controller’s actual action sequence; keep target identity independent of sprite position. `EnemyTargetAnchor` provides character clicking and a focus ring. The preview animation is not production combat choreography.

## Checks

Editor tests cover target identity, per-target Bite availability, dead targets, input locking, rejected targets, keyboard selection, empty rosters, duplicate IDs, and snapshot ownership. The PlayMode check renders both panels at three resolutions and exercises the actual card buttons. Set VISHA_UI_CAPTURE_DIR before running PlayMode tests to save optional PNG evidence outside the project.
