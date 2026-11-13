# Victory / defeat and retry milestone

`BattleOutcomeController` detects victory and defeat and resets encounters for retry.

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
  one is present, using the configured opening offset.
- Clears `Outcome` back to `None` and fires `OnRetry` so any other system (the
  combat controller) can reset its own state.

## Integration contract

The controller is rules-agnostic: it only watches `Health.OnDied` and reports
the result. It never applies damage and never decides who goes next.

- Attach `BattleOutcomeController` next to (or anywhere in) the battle scene and
  assign the player's and enemy's `Health` in the inspector, or call
  `SetCombatants(player, enemy)` at runtime once both are spawned.
- The combat controller should subscribe to `OnBattleEnded`
  and copy the result onto `BattleViewState.phase`
  (`Visha.UI.BattlePhase.Victory` / `.Defeat`), which `BattleHUD`
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
