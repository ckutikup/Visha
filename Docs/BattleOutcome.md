# Victory / defeat and retry milestone

This contribution implements Shivani's assigned piece: detecting when the battle
is over (victory or defeat) and resetting the encounter for a retry. It contains
no combat resolution, damage math, turn ordering, or UI. It does not modify the
shared BattlePrototype scene, BattleHUD, or any project settings — everything
lives in a new `Visha.Battle` assembly.

## What it does

`BattleOutcomeController` watches the player's and enemy's `Health` components:

- When the enemy's `Health.OnDied` fires while the player is still alive, the
  outcome is **Victory**.
- When the player's `Health.OnDied` fires, the outcome is **Defeat**.
- If both happen to go down together, **Defeat** wins the tie — going down still
  counts as losing even if the killing blow also finished the enemy.
- Once an outcome is set it is locked (`IsOver`): further deaths in the same
  encounter cannot flip the result, and `OnBattleEnded` fires exactly once.

`Retry()` resets the encounter:

- Revives both combatants (`Health.ResetHealth()`).
- Clears any poison stacks on either side (`Poison.ConsumeAllStacks()`), if a
  `Poison` component is present.
- Restarts the enemy's attack pattern (`EnemyPatternRunner.ResetPattern()`), if
  one is present, per Anshul's documented retry contract.
- Clears `Outcome` back to `None` and fires `OnRetry` so any other system (a
  turn controller, once it exists) can reset its own state.

## Integration contract

The controller is rules-agnostic: it only watches `Health.OnDied` and reports
the result. It never applies damage and never decides who goes next.

- Attach `BattleOutcomeController` next to (or anywhere in) the battle scene and
  assign the player's and enemy's `Health` in the inspector, or call
  `SetCombatants(player, enemy)` at runtime once both are spawned.
- Whatever combat controller Rahul builds should subscribe to `OnBattleEnded`
  and copy the result onto `BattleViewState.phase`
  (`Visha.UI.BattlePhase.Victory` / `.Defeat`), which Charith's `BattleHUD`
  already renders ("VICTORY" / "DEFEAT" labels).
- Wire a retry button/input to call `Retry()`; it does not assume any particular
  UI, so it can be driven from a keypress, a HUD button, or a test.

## Files

| File under Assets | Purpose |
| --- | --- |
| Scripts/Battle/BattleOutcome.cs | `None` / `Victory` / `Defeat` result enum |
| Scripts/Battle/BattleOutcomeController.cs | Watches Health.OnDied, decides the outcome, resets on retry |
| Scripts/Battle/Visha.Battle.asmdef | Runtime assembly for outcome/retry logic |
| Tests/Editor/Battle/BattleOutcomeControllerTests.cs | Victory/defeat detection, tie-break, locking, retry reset |
| Tests/Editor/Battle/Visha.Battle.Tests.asmdef | EditMode test assembly |

## Verification

Window > General > Test Runner > EditMode > Visha.Battle.Tests runs the outcome
tests: enemy death while the player is alive resolves to Victory, player death
resolves to Defeat, a simultaneous death favors Defeat, the outcome locks after
the first result and `OnBattleEnded` fires only once, `Retry()` revives both
combatants and clears poison and the enemy's attack pattern, `OnRetry` fires,
and a disabled controller does not react to death.

## Work remaining with the team

Rahul's controller decides when a hit is lethal and should call into this
controller's watched `Health` components (it already will, since it drives
`Health.TakeDamage`); it then reads `Outcome` / `OnBattleEnded` to drive
`BattleViewState.phase` and wires a retry action to `Retry()`. Akshat's
player/enemy prefabs are where `Health` (and this controller, or its
references) ultimately get placed once the scene is wired end to end.
