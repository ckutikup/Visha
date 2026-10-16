# Campaign progression and retry flow

This contribution implements Shivani's midsemester piece: connecting the
individual battles into a demo that can be played from the start screen to
the end screen without stopping Play Mode. It adds no damage, ability or turn
logic. Every reset goes through the existing `BattleOutcomeController.Retry()`
and Rahul's `CombatFlowController`. Everything lives in a new `Visha.Campaign`
assembly, and the shared `BattleUI_Demo` scene is not modified.

## The flow

```
Title (start screen + how to play)
  │ Start
  ▼
Encounter 1 ──defeat──▶ Defeat panel ──Retry──▶ same encounter, from its start
  │ victory                    └──Return to Start──▶ Title
  ▼
Victory panel ──Next──▶ Encounter 2 ──▶ … ──▶ Encounter 3
                                               │ victory
                                               ▼
                                     End-of-demo screen ──Restart──▶ Encounter 1
                                               └──Return to Start──▶ Title
```

- **Start screen.** It shows the title and short instructions for enemy
  intentions, Venom Strike/poison, Serpent's Bite and Fade, plus a Start button.
- **Encounters in order.** The encounters come from the `EncounterConfig`
  assets listed on `CampaignController`.
- **Victory waits for Next.** The defeated enemy stays down until the player
  presses Next. Next is ignored in any other state, so a double click cannot
  skip an encounter.
- **Defeat retries the current encounter.** Health, poison, enemy pattern,
  Fade and turn state are reset through their own systems.
- **End of demo.** It appears after encounter three. Restart resets the
  encounter index and the campaign state and begins encounter 1 at full health.

## How each encounter starts

New encounters, retries and restarts all use the same steps:

1. `CampaignController` applies the encounter's config to the enemy:
   `EnemyPatternRunner.Configure(pattern, displayName)` and
   `Health.SetMaxHealth(maxHealth)`. If the enemy has a `SpriteRenderer`,
   it also gets the encounter's sprite.
2. It calls `BattleOutcomeController.Retry()`. That call revives both
   combatants, clears poison, rewinds the enemy pattern and clears the outcome.
3. `OnRetry` fires:
   - `CombatFlowController` clears Fade and its pending actions, then enters
     the player turn.
   - `CampaignController` sets the encounter's starting health: the
     enemy's `startingHealth`, and the player's health according to the
     carryover rule.

A retry button wired straight to `BattleOutcomeController.Retry()` behaves
the same as `CampaignController.RetryEncounter()`.

## Player health between encounters

This is the `playerHealthRule` setting on `CampaignController`.

- **RestoreToFull** (default): the player starts every encounter at full health.
- **CarryOver**: the player keeps the health they won the previous encounter
  with. A retry restores the health the encounter started with, not full.

The team still needs to confirm this rule. Both options are implemented and
tested, so changing it is one inspector setting.

## Encounter configs

`EncounterConfig` (Assets > Create > Visha > Encounter Config) holds:

- display name
- enemy type / action pattern (`EnemyPatternRunner.PatternKind`)
- maximum health
- starting health (0 means full)
- an optional enemy sprite

Damage stays in the pattern definitions, so it is tuned in one place.

`Assets/Data/Encounters` holds three placeholder configs. Only the Temple
Guardian pattern exists so far, so difficulty rises through health:

| # | Asset | Name | Max HP |
| --- | --- | --- | --- |
| 1 | Encounter1_TempleGuardian | Temple Guardian | 24 |
| 2 | Encounter2_GuardianSentinel | Guardian Sentinel | 30 |
| 3 | Encounter3_GuardianWarden | Guardian Warden | 36 |

Anshul can edit these assets or replace them with the final configs. To
use the second enemy type, add it to `PatternKind` and select it here.

## How to run

1. Open `Assets/Scenes/Campaign_Demo.unity` and press **Play**.
2. Press **Start**, then play the battle as usual.
3. Win to see the Victory panel, then press **Next**. Lose to see the Defeat
   panel, then press **Retry**.
4. After encounter three, press **Restart** on the end screen.

`Campaign_Demo` is a copy of `BattleUI_Demo` with the campaign added. To
rebuild it after the battle scene changes, use **Visha > Create Campaign Demo
Scene**. To add the campaign to whichever scene is open, use **Visha > Setup
Campaign In Open Scene**. That scene must already have the combat flow from
Visha > Setup Combat Flow. Both menu items can be run more than once.
Existing encounter assets are never overwritten.

## Files

| File under Assets | Purpose |
| --- | --- |
| Scripts/Campaign/CampaignState.cs | Title / InBattle / Victory / Defeat / DemoComplete |
| Scripts/Campaign/CampaignProgress.cs | Encounter index and allowed transitions, no Unity dependencies |
| Scripts/Campaign/CampaignController.cs | Loads encounters, starts/retries/restarts them, applies the health rule |
| Scripts/Campaign/EncounterConfig.cs | Per-encounter data asset |
| Scripts/Campaign/CampaignScreens.cs | Shows the screen for each state; buttons forward to CampaignController |
| Scripts/Campaign/Editor/CampaignSceneSetup.cs | Menu items that create the configs, screens and Campaign_Demo scene |
| Data/Encounters/*.asset | The three placeholder encounters |
| Scenes/Campaign_Demo.unity | Playable campaign scene |
| Tests/Editor/Campaign/*.cs | EditMode tests for progression rules and controller resets |
| Tests/PlayMode/Campaign/CampaignDemoPlayModeTests.cs | Plays Campaign_Demo through its real buttons |

`EnemyPatternRunner` gained one additive method, `Configure(kind,
displayName)`, so the campaign can set each encounter's enemy. Nothing existing
changed behaviour.

## Verification

In **Window > General > Test Runner**:

- **EditMode > Visha.Campaign.Tests** (25 tests):
  - encounter order
  - Next only after victory, and only once
  - Retry only after defeat, staying on the same encounter
  - retry resetting health, poison and pattern
  - pattern reset on a new encounter
  - both health rules, including retry under carryover
  - starting health below max
  - full restart after the demo
- **PlayMode > Visha.Campaign.PlayModeTests** (2 tests): loads
  `Campaign_Demo` and drives it through its real buttons and the real
  `CombatFlowController`. One test plays Start, win, Next, lose, Retry, win,
  Next, win, end screen, Restart. The other plays Return to Start from the
  Defeat panel. After every transition, the tests check that a Venom Strike is
  accepted and the enemy takes its turn.

## Work remaining with the team

- **Everyone:** confirm the health carryover rule.
- **Anshul:** final encounter configs and the second enemy type.
- **Charith:** the outcome panels are functional placeholders. Restyle them, or
  point the HUD's own Next / Retry / Return to Start buttons at
  `CampaignController.NextEncounter()`, `RetryEncounter()`,
  `RestartCampaign()` and `ReturnToTitle()`, and then remove these. The HUD
  can show the encounter number from `CampaignController.EncounterNumber` /
  `EncounterCount`.
- **Akshat:** add `Campaign_Demo` to Build Settings as the first scene for the
  standalone build.
- **Rahul:** move the campaign into the final integrated battle scene with
  Visha > Setup Campaign In Open Scene.
