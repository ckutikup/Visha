# Enemy attack pattern milestone

This contribution implements Anshul's assigned piece: one enemy attack pattern
(the Temple Guardian) and the "next-action" information the UI uses to telegraph
it. It contains no combat resolution, health/poison rules, turn ordering, UI, art,
or result screens. It does not modify the shared BattlePrototype scene or any
project settings — everything lives in a new `Visha.Enemies` assembly.

## What it does

The Temple Guardian follows a fixed, repeating three-move cycle. Nothing about an
enemy turn is random, so the fight is something the player learns to read:

| Step | Move       | Kind   | Damage | Why it is there |
| ---- | ---------- | ------ | ------ | --------------- |
| 0    | Strike     | Attack | 4      | A normal, low-pressure turn. |
| 1    | Heavy Blow | Attack | 9      | Telegraphed a full turn ahead — this is the turn to use Fade. Teaches Fade timing. |
| 2    | Defend     | Defend | 0      | The Guardian braces and deals no damage — a safe window to stack Venom Strike (build poison). |

The cycle then repeats. Because the upcoming move is known before the player acts,
the player always has a real decision: Fade the big hit, or spend the quiet Defend
turn building poison for a later Serpent's Bite.

Damage values are a starting point for playtesting, not final balance. They are all
in one place (`TempleGuardianPattern`) and easy to tune.

## Integration contract

The pattern is UI- and rules-agnostic. It only decides and reports the enemy's
intended move; it never applies damage or reads other systems.

- `EnemyPatternRunner` is a MonoBehaviour for the enemy placeholder. During the
  player's turn the controller reads `IntentLabel` and `IncomingDamage` and copies
  them into `BattleViewState.intention` and `BattleViewState.incomingDamage`. These
  are the exact fields Charith's `IntentView` already renders, so no UI change is
  needed.
- The Defend move's label contains "Defend", which is what `IntentView` looks for to
  show the shield icon rather than the attack icon. A test guards this coupling.
- When the enemy turn resolves, the controller calls `TakeTurn()`: it returns the
  move to apply (Rahul's flow / Balaji's damage) and advances the cycle so the next
  telegraph is ready.
- `ResetPattern()` restarts the cycle for a new encounter or a retry (Shivani's flow).
- `PeekAhead(n)` can preview further down the cycle if a later UI milestone wants a
  two-turn forecast; the basic telegraph does not need it.

## Files

| File under Assets | Purpose |
| --- | --- |
| Scripts/Enemies/EnemyAction.cs | One known move: label, damage, attack/defend kind |
| Scripts/Enemies/EnemyAttackPattern.cs | Deterministic repeating cycle; Current / Peek / Advance / TakeTurn / Reset |
| Scripts/Enemies/TempleGuardianPattern.cs | The Temple Guardian's concrete three-move cycle |
| Scripts/Enemies/EnemyPatternRunner.cs | MonoBehaviour that exposes the telegraph to the controller |
| Scripts/Enemies/Visha.Enemies.asmdef | Runtime assembly for enemy logic |
| Tests/Editor/Enemies/EnemyPatternTests.cs | Cycle order, determinism, peek-does-not-mutate, reset, guards |
| Tests/Editor/Enemies/Visha.Enemies.Tests.asmdef | EditMode test assembly |

## Verification

Window > General > Test Runner > EditMode > Visha.Enemies.Tests runs the pattern
tests: cycle order (Strike -> Heavy Blow -> Defend -> repeat), determinism across
two fresh copies, that reading the telegraph does not advance the cycle, TakeTurn
returning-then-advancing, Reset, and the defend-icon label guard.

## Work remaining with the team

Rahul's controller drives when the enemy acts and copies the telegraph into the
view state; Balaji's code applies the reported damage and Fade reduction; Akshat's
enemy prefab can carry the `EnemyPatternRunner` component. A second enemy type and
two-target support are later milestones.
