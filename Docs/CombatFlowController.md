# Lab 1: September 28, 2026

## Combat flow controller milestone

This contribution implements: the basic player/enemy turn sequence and
action locking. `CombatFlowController` is the orchestrator that
connects the systems the rest of the team built. It listens to Charith's
`BattleHUD`, applies the three player abilities through `Health`, `Poison` and
`VenomStrike`, runs the enemy's telegraphed move from Anshul's
`EnemyPatternRunner`, and hands victory/defeat to Shivani's
`BattleOutcomeController`. Everything lives in a new `Visha.Combat.Flow`
assembly. No existing script, assembly definition or test was modified.

## What it does

### Turn sequence

1. **Start.** `Start()` subscribes to every battle system and enters the
   player's turn.
2. **Player turn.** The phase is `PlayerTurn`. The HUD shows the enemy's next
   move (intent and incoming damage) and waits for an ability.
3. **Player acts.** `BattleHUD.AbilityRequested` reaches the controller, which
   validates and applies the ability.
4. **Enemy turn.** The phase switches to `EnemyTurn` and the HUD re-renders.
   One frame later the controller calls `EnemyPatternRunner.TakeTurn()`,
   applies the move's damage to the player (reduced by Fade if active), then
   ticks the enemy's poison.
5. **Back to the player.** If the battle is not over, control returns to step 2.
6. **End.** When either combatant dies, `BattleOutcomeController.OnBattleEnded`
   sets the phase to `Victory` or `Defeat` and the loop stops.
7. **Retry.** `BattleOutcomeController.OnRetry` resets the controller's turn
   state and starts a new player turn.

### Abilities

Ability Rules

- **Venom Strike**: Calls `VenomStrike.UseOn(enemy)`, adding `stacksApplied` (default 2) poison stacks to the enemy.
- **Serpent's Bite**: Requires at least one poison stack on the enemy. Deals `serpentsBiteDamage` (default 12) plus 1 per stack, then consumes all stacks.
- **Fade**: Guards the next enemy attack. With `fadeBlocksNextAttack` on (default), the attack deals 0. With it off, the attack is reduced by `fadeReduction`. Defend moves are unaffected.

All three balance values are inspector fields on `CombatFlowController`.

## How to run

`BattleUI_Demo` is already wired and committed, so the menu step is only
needed if the scene objects are missing or you want to wire another scene.

1. Open `Assets/Scenes/BattleUI_Demo.unity` as a scene (not in Prefab Mode).
2. Optional: click **Visha > Setup Combat Flow In Open Scene** in the menu bar
   (on macOS, the menu bar at the top of the screen). It adds Player, Enemy
   and CombatFlowController, fills in every reference and disables
   BattleHUDPreview. It is safe to run more than once.
3. If you ran the menu item, save the scene (Cmd+S / Ctrl+S).
4. Press **Play**.
5. Click the enemy to target it, then use **Venom Strike**. The venom counter
   fills, the phase switches to the enemy turn and back, and both health
   bars drop.
6. Once the enemy has poison, use **Serpent's Bite** to spend the stacks, or
   use **Fade** before the telegraphed Heavy Blow.
7. Keep playing until either side reaches 0 HP to see Victory or Defeat. To
   play again, stop and re-enter Play mode (there is no Retry button yet).

## Scene integration

`BattleUI_Demo` now runs the real turn loop instead of the preview:

- **Player**: `Health` (30 HP), `Poison`, `VenomStrike`.
- **Enemy**: `Health` (24 HP), `Poison`, `EnemyPatternRunner` (Temple Guardian).
- **CombatFlowController**: `BattleOutcomeController` (watching both `Health`
  components) and `CombatFlowController` with every reference assigned.
- **BattleHUDPreview** is disabled (not deleted). It listens to the same
  `AbilityRequested` event and renders sample data, which would conflict with
  the real controller. Re-enabling it is one checkbox.

The wiring was done by an editor-only menu item, **Visha > Setup Combat Flow In
Open Scene** (`CombatFlowSceneSetup`). It can be re-run safely: it reuses
existing objects, records every change with Undo, and only marks the scene
dirty. It lives in its own Editor assembly and is excluded from builds.
