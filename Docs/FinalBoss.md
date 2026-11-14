# The Asura: final boss (encounter 5)

The fifth encounter is the final boss in the Sanctum, the Asura from our concept page. It is one enemy with two phases. Every move is telegraphed a turn ahead and nothing is random, same as the other enemies. All values below are starting points for playtesting.

Code: `AsuraPattern.cs` (moves and phase rule) and `EnemyPatternRunner` (`PatternKind.Asura` plus the phase API). Data: `Assets/Data/Encounters/Encounter5_TheAsura.asset`.

## Health

| Value | Number |
| --- | --- |
| Max health | 76 (same as the concept) |
| Starting health | full |
| Phase 2 starts at | 38 HP or below (half of max, rounded down) |
| Player health | 50 (same as every other fight) |

## Phase 1

| Step | Move | Damage | Why |
| --- | --- | --- | --- |
| 1 | Recover | 0 | Free turn to start poison or Bite |
| 2 | Cursed Claw | 10 | Steady pressure |
| 3 | Ruin | 16 | The big hit. Fade halves it to 8 |

## Phase 2

Phase 2 always starts on Roar, no matter where phase 1 was in its cycle.

| Step | Move | Damage | Why |
| --- | --- | --- | --- |
| 1 | Roar | 0 | The phase change gives a free turn to counterattack, and it comes back every cycle |
| 2 | Cleave | 12 | |
| 3 | Hex | 8 | |
| 4 | Cataclysm | 22 | Biggest hit in the game. Fade halves it to 11 |

Phase 2 averages 10.5 damage a turn against 8.7 in phase 1, and its big hit goes from 16 to 22. It stays manageable for three reasons:

- There's still a free turn every cycle, and it falls right after the phase change.
- The big hit is telegraphed a full turn ahead, so you can always save Fade for it.
- Every boss attack is 8 or more, so Fading any of them earns an Awakening charge with the existing rule.

So the ways to defend and counterattack are the ones players already know: Fade the big hit, then use the free turns and the charge to hit back.

All move names are 11 characters or fewer, so the HUD intent box shows them whole. The two free turns (Recover, Roar) are Defend moves with 0 damage, so the HUD shows the shield icon and "NO ATTACK".

## Balance

I simulated the fight with the combat contract v2 rules: Venom +2 stacks (cap 5), poison 2 per stack each turn, Bite stacks x 4, Fade halves each hit (not twice in a row), and the phase check after each damage step. Temple's Blessing and Awakening are left out, since both only help the player.

| Fight | Best play | Never uses Fade |
| --- | --- | --- |
| Encounter 3, Vault Sentinel (solo) | 34 / 50 HP left | 26 / 50 |
| Encounter 4, Inner Vault (two enemies) | 21 / 50 | dies |
| Encounter 5, the Asura | 18 / 50, about 8 turns | dies |

So the boss is the hardest fight in the game but still clearly winnable. The Blessing then gives one extra safety net on top. The original concept numbers (76 HP with a single 0 / 20 / 8 / 0 / 24 cycle) left only 10 / 50 with perfect play and had no second phase, so I kept the concept's health and moved the damage into two readable phases.

Other values I tried:

| Change | Best play |
| --- | --- |
| Max health 84 | 13 / 50 |
| Ruin 18 instead of 16 | 17 / 50 |
| Phase 2 Cleave 14 / Hex 6 / Cataclysm 24 | 19 / 50 |

If playtests find it too hard, lower Cataclysm first (it's the spike in phase 2), then max health. Everything is tuned in `AsuraPattern.cs` and the encounter asset.

## Integration

Rahul (combat flow):

- Configure the boss like any other enemy: `runner.Configure(PatternKind.Asura, "The Asura")`. The encounter asset already has pattern 2 and 76 HP.
- After each step that damages the boss (the player's ability, then the poison tick), call `runner.UpdateBossPhase(health.CurrentHealth, health.MaxHealth)` before the boss acts. It returns true exactly once, on the turn phase 2 starts. If the poison tick kills the boss, the boss is dead and nothing changes.
- When it returns true, the boss's current move has already become Roar (0 damage), so that enemy phase is a free turn. Refresh the intent from `IntentLabel` / `IncomingDamage` as usual.
- Every turn, set `viewState.bossPhase = runner.BossPhase` and `viewState.bossPhaseCount = runner.BossPhaseCount`. Both are 0 for normal enemies, so it's safe to set them for every fight.
- Retry: `runner.ResetPattern()` already puts the boss back in phase 1 on Recover.
- `CombatFlowController.cs` line 620 treats "Heavy Blow" and "Crush" as heavy threats by name. Add "Ruin" and "Cataclysm" there, or check `IncomingDamage >= 9` instead so new moves are covered automatically.

Charith (HUD): the phase label already reads `bossPhase` / `bossPhaseCount`, so it will show PHASE 1 / 2 and PHASE 2 / 2 once Rahul sets them. "Cursed Claw" is the longest name (11 characters).

Akshat (presentation): `SanctumPresentation` plays the phase change when `bossPhase` goes up, which happens once, on the Roar turn.

Shivani (progression): `Encounter5_TheAsura.asset` is ready to be added after the Inner Vault. Its sprite is empty for Akshat to fill in.

Balaji (abilities): nothing in the boss depends on the Blessing. Poison, Bite and Fade work on it like any single enemy. A Bite that drops the boss from above half to far below still triggers phase 2 only once.

## Tests

`AsuraPatternTests` (Visha.Enemies.Tests) and `FinalBossEncounterTests` (Visha.Campaign.Tests) check:

- both cycles, move by move;
- phase 2 is stronger but keeps a free turn;
- every attack can earn Awakening when Faded;
- labels fit the HUD;
- the threshold is 38 for 76 HP;
- the phase changes exactly once, opens on Roar, and doesn't happen when the boss is dead;
- the telegraph matches the executed move in phase 2;
- retry and re-configure go back to phase 1;
- the encounter asset holds the values above.
