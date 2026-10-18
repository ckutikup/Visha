# Combat flow controller

`CombatFlowController` (`Assets/Scripts/Combat/Flow`) is the battle's rules
engine. It owns turn order and action locking and connects the team's systems.
It holds no damage numbers of its own:

| System | Owner | What the controller calls |
| --- | --- | --- |
| `VenomStrike`, `SerpentsBite`, `Fade`, `Poison`, `Health` | Balaji | `TryUseOn`, `CanUseOn`, `TryUse`, `ReduceDamage`, `EndEnemyPhase`, `OnOtherAbilityUsed`, `ResetFade`, `Tick` |
| `EnemyPatternRunner` | Anshul | `TakeTurn`, `EnemyName`, `IntentLabel`, `IncomingDamage` |
| `BattleOutcomeController` | Shivani | `OnBattleEnded`, `OnRetry`, `IsOver` |
| `CampaignController` | Shivani | `State`, `StateChanged` (optional) |
| `BattleHUD` | Charith | `AbilityRequested`, `Render(BattleViewState)`, `RejectAction` |

## Turn order (combat contract v2)

1. **Player turn.** One ability. A refused ability does not use the turn: the
   controller calls `BattleHUD.RejectAction(reason)` and the player picks again.
2. **Enemy turn.** One frame later the telegraphed move from `TakeTurn()` hits
   the player. An Attack goes through `Fade.ReduceDamage` (halved while Fade is
   active). Defend deals 0.
3. **Poison tick.** `Poison.Tick()` on the enemy, 2 damage per stack, after the
   enemy has acted.
4. **Fade window ends** (`Fade.EndEnemyPhase()`), then back to step 1.

The turn stops the moment either side dies: no enemy turn after a killing
Bite, no poison tick after the player dies, and no new player turn after
either. Anshul's balance table in `Docs/EnemyPattern.md` (player 50 HP)
comes out the same with this timing.

## Abilities

- **Venom Strike**: `VenomStrike.TryUseOn(enemy)`, +2 stacks (cap 5).
- **Serpent's Bite**: refused unless `SerpentsBite.CanUseOn(enemy)` (the enemy
  has poison and is alive). Consumes all stacks, 4 damage per stack.
- **Fade**: `Fade.TryUse()`. Halves the next enemy Attack. Can't be used two
  turns in a row: Venom Strike or Serpent's Bite calls
  `Fade.OnOtherAbilityUsed()`, which makes it available again.

Balance values live on the components (Player object) and the enemy patterns,
not on the controller. If the ability fields are empty, the controller finds
the components on the Player.

## Locking and gating

- A submission locks the turn at once; repeats are ignored until the next
  player turn. After Victory/Defeat nothing is accepted until Retry.
- With a `CampaignController` in the scene (assigned, or found automatically),
  actions are accepted only while the campaign is `InBattle`. Title, Victory,
  Defeat and end screens show phase Waiting or the result. `SetCombatActive(bool)`
  is public for any other owner. Without a campaign (BattleUI_Demo), combat is
  always active.

## Retry and new encounters

`BattleOutcomeController.OnRetry` runs for a retry, a new encounter and a
restart. The controller stops any pending enemy turn, clears the turn lock,
calls `Fade.ResetFade()`, passes through Waiting (so the HUD drops any pending
submission) and starts a player turn. Health, poison and the pattern are reset
by `BattleOutcomeController.Retry()` and `CampaignController`.

## HUD state

Every change re-renders `BattleViewState` with real values: both health bars,
enemy poison stacks and cap (enables Bite), enemy name, intention and incoming
damage, `fadeAvailable = Fade.CanUse`, and the phase.

## Scene setup

**Visha > Setup Combat Flow In Open Scene** adds or reuses Player (Health 50,
Poison, VenomStrike, SerpentsBite, Fade), Enemy (Health, Poison,
EnemyPatternRunner) and the controller object, assigns every reference
(including the CampaignController if the scene has one) and disables
BattleHUDPreview. Safe to run again. Save the scene afterwards.

## Tests

EditMode > `Visha.Combat.Flow.Tests` covers each rule above: ability
resolution through the components, refused Bite/Fade keeping the turn, Fade
halving and its cooldown, poison timing, death stopping the turn, duplicate
submissions, retry reset, campaign gating and the HUD values.
