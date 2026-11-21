# Encounter balance audit

Anshul's audit of the five encounters on main at 60190d5: are they understandable, fair and varied, and what is the smallest data change that fixes what isn't. Two values changed (encounter 1 and the Asura's health). No code, enemy types, encounters, modes or player mechanics changed.

## How this was checked

- Data: every encounter asset, the Vault config, the patterns, and the ability values actually set in `Campaign_Demo.unity` (player 50 HP, Venom +2, poison cap 5 at 2 per stack, Bite 4 per stack, Fade 50%).
- Telegraphs: a new EditMode test, `EncounterIntentionTests`, loads the shipped assets and checks for every enemy that what the HUD shows before each enemy turn is exactly what executes.
- Strategies: a turn-by-turn simulation that follows `CombatFlowController` exactly, including when poison ticks, the boss phase check, Fade's rounding and cooldown, Awakening charges and Twin Fang, Routine Break, and Temple's Blessing. For each encounter it finds the best possible play, and it also runs fixed strategies a real player might use.
- In-game playtest: I played the full campaign in the editor with the new values (Campaign_Demo, from the title screen), then replayed the Inner Vault twice to check both target orders and the no-Fade loss. Results are in the playtest section below and match the simulation exactly.
- Tests: full EditMode (317 / 317) and PlayMode (32 / 32) runs pass with these changes.

## The encounters

| # | Encounter | Enemies | HP | Opening move | Mechanic it teaches |
| - | --------- | ------- | -- | ------------ | ------------------- |
| 1 | Courtyard | Temple Guardian | 34 (was 30) | Strike 4 | Poison, Bite, Fade timing on a telegraphed Heavy Blow |
| 2 | Courtyard | Guardian Warden (offset 1) | 44 | Heavy Blow 9 | Fade on turn one; Routine Break |
| 3 | Vault | Vault Sentinel | 46 | Recover 0 | The 16 Crush; Awakening (starts with full charges) |
| 4 | Inner Vault | Vault Guardian + Vault Sentinel (offset 2) | 18 + 26 | Strike 4 + Shield Bash 8 | Target choice |
| 5 | Sanctum | The Asura, two phases | 70 (was 76) | Recover 0 | Everything, plus the phase 2 Roar window and the Blessing |

## Telegraphs match what happens

`EncounterIntentionTests` passes for all five encounters over 9 turns each: label, damage and kind shown before every enemy turn equal the move that executes, every 0-damage move shows as "no attack", and the Inner Vault's combined damage never goes above 16 on any turn.

There is one place where the shown move changes on purpose: if the player's ability or the poison tick drops the Asura to half health, its telegraph switches from the phase 1 move to Roar (0 damage) before it acts. The HUD refreshes straight away and the change is always to less damage, never more, but a player who Faded for Ruin on that turn wastes the Fade.

## Strategies (simulated)

HP left out of 50 / turns taken. W = win, L = loss.

| # | Best play | Poison only | Venom to 5, Bite, no Fade | Early Bite at 2 | Fade the 16+ hits, Bite at 5 | Fade every 9+ hit, Bite at 5 |
| - | --------- | ----------- | ------------------------- | --------------- | ---------------------------- | ---------------------------- |
| 1 (before, 30 HP) | 42 / 4 | W 37 / 4 | W 37 / 4 | W 24 / 6 | W 37 / 4 | W 42 / 4 |
| 1 (after, 34 HP) | 42 / 4 | W 33 / 5 | W 37 / 4 | W 24 / 6 | W 37 / 4 | W 42 / 4 |
| 2 | 38 / 5 | W 28 / 6 | W 28 / 5 | W 15 / 8 | W 28 / 5 | W 38 / 5 |
| 3 | 34 / 5 | W 10 / 6 | W 26 / 5 | W 2 / 8 | W 34 / 5 | W 34 / 5 |
| 5 (before, 76 HP) | 18 / 8 | W 1 / 9 | W 1 / 8 | L 0 / 8 | W 1 / 9 | W 1 / 11 |
| 5 (after, 70 HP) | 26 / 7 | W 4 / 8 | W 1 / 8 | L 0 / 9 | W 12 / 8 | W 1 / 9 |

Winning routes (best play): E1 Venom, Fade, Venom, Bite. E2 Fade, Venom, Venom, Fade, Bite. E3 Venom, Fade, Venom, Venom, Bite. E5 Venom, Venom, Fade, Venom, Venom, Fade, Bite.

What this shows:

- Poison buildup dominated the first fight. At 30 HP, never biting gave exactly the same result as Venom-then-Bite (37 HP, 4 turns), so encounter 1 didn't teach Bite. At 34, poison alone takes an extra turn and costs 4 HP. From encounter 3 on, poison alone falls far behind (10 / 50 in E3).
- Early Bite is never good. Biting at 2 stacks deals 8 and throws away the poison ticks, so it is the worst strategy everywhere and loses to the Asura. Bite pays off at 4 to 5 stacks, which the HUD's Bite preview already shows.
- Defensive timing matters, and more as the game goes on. Fading the telegraphed big hit adds 5 HP in E1, 10 in E2, 8 in E3 and 11 against the Asura (after the change). Fading every hit of 9 or more is fine in the Courtyard but too slow against the Asura, where Cursed Claw (10) isn't worth a Fade.

## Inner Vault: which enemy first

| Order | Best play | Fade the Crush, Bite at 5 | Bite at 5, no Fade |
| ----- | --------- | ------------------------- | ------------------ |
| Vault Sentinel first | 21 / 50, 6 turns | W 8 / 7 | L |
| Vault Guardian first | 13 / 50, 6 turns | W 13 / 6 | L |

Both orders are winnable, so the choice is real, but attacking the Sentinel first is better with perfect play (21 vs 13) because it removes the 16-damage Crush for good. Best route: Venom, Venom, Bite on the Sentinel, then Venom, Fade, Venom on the Guardian. With the simpler "Fade the Crush" habit the Guardian-first order actually does better (13 vs 8), because the Guardian dies fast and its Heavy Blow stops early. Not using Fade loses with either order, and the reason is visible: the 16 Crush is telegraphed a turn ahead.

I tried lowering the Sentinel to 22 or 24 HP. At 22, playing without Fade wins comfortably (16 HP), which removes the encounter's lesson, and best play doesn't change. So the Inner Vault stays as it is.

## Heavy hits and combined damage

Every big hit is telegraphed a full turn ahead, and Fade is always available for it if the player didn't Fade the turn before. Encounter 2 opens on Heavy Blow on turn one and Fade is available on turn one. In the Inner Vault the two cycles are offset so Heavy Blow (9) and Crush (16) never land together: combined damage per turn is 12 / 9 / 16 and never 25. In two-enemy fights each enemy's poison ticks just before it acts, so poison can kill the second enemy before its attack.

## The Asura

- Transition: phase 2 starts at half health (now 35) and always opens on Roar, a free turn. The phase change can happen from the poison tick, so the Roar window is often the turn right after a big poison build-up, which is a good moment to Bite.
- Heavy attacks: Ruin (16) and Cataclysm (22) are both telegraphed a turn ahead, and the moves before them (Cursed Claw, Hex) don't need a Fade, so Fade is always free when they come.
- Blessing: at 76 HP the Blessing was doing too much. Every reasonable strategy only survived on exactly 1 HP, and without the Blessing three of them lost. One more mistake meant a loss even when the player was doing the intended thing. At 70 HP, Fading the big hits leaves 12 / 50, and the Blessing is a real safety net for messier play rather than the only reason anyone survives.
- Awakening: every boss attack is 8 or more, so Fading any of them earns a charge, and a Bite at 5 poison earns one too. Twin Fang puts 2 poison back on the boss if it survives the Bite.

## Problems found outside encounter data

These are in other members' systems, so I haven't changed them. Each one can make a loss feel unexplained.

1. Awakening doesn't give two actions. The Vault hint says "Press 4 to awaken for two actions" and the HUD says "two actions", but `CombatFlowController` hands the turn to the enemies after every player action. Awakening lasts two player actions, and enemies act after each one. What it actually gives is Twin Fang and a pause in charge gain. Either the turn flow (Rahul) or the wording (Charith) should change. The simulation above uses the real behavior.
2. Balaji's two wording notes are correct (checked in code): Bite only earns a charge at full poison (5 / 5), and guarding only earns one when Fade reduces an attack of 8 or more; and Twin Fang places no poison when the Bite kills the only enemy.
3. Routine Break only triggers on "Heavy Blow" and "Crush" by name, so it never happens against the Asura. That's fine if it's meant as a Courtyard and Vault mechanic, but nothing tells the player.

## Changes

| Value | Before | After | Why |
| ----- | ------ | ----- | --- |
| Encounter 1, Temple Guardian max HP | 30 | 34 | Poison alone was exactly as good as using Bite, so the first fight didn't teach Bite. Now Bite saves a turn and 4 HP; best play is unchanged (42 / 50, 4 turns) |
| Encounter 5, the Asura max HP | 76 | 70 | Disciplined play survived on 1 HP only because of the Blessing. Now Fading the big hits leaves 12 / 50 and best play 26 / 50; careless play still needs the Blessing, so the boss stays the hardest fight to survive without mistakes |

Files: the two encounter assets, `CampaignSceneSetup` (so re-running Shivani's setup keeps 34), and the tests that check the shipped values (`EncounterAssetTests`, `FiveEncounterCampaignTests`, `CampaignDemoPlayModeTests`, `FinalBossEncounterTests`). Docs: `EnemyPattern.md`, `FinalBoss.md`.

## In-game playtest (new values)

One full campaign in the editor, played by me (I know the patterns, so this is close to best play), no retries.

| # | Encounter | Time to win | Turns | HP left | Retries | Route |
| - | --------- | ----------- | ----- | ------- | ------- | ----- |
| 1 | Temple Guardian | 0:54 | 4 | 42 / 50 | 0 | Venom, Fade (Heavy Blow), Venom (+1 from Routine Break), Bite |
| 2 | Guardian Warden | 0:51 | 5 | 38 / 50 | 0 | Fade, Venom (+1), Venom, Fade, Bite |
| 3 | Vault Sentinel | 1:16 | 5 | 34 / 50 | 0 | Awaken, Venom, Fade (Crush), Venom, Bite before the next Crush |
| 4 | Inner Vault | 1:27 | 6 | 21 / 50 | 0 | Sentinel first: Venom, Venom, Bite; then Guardian: Venom, Fade, Venom |
| 5 | The Asura | 1:33 | 7 | 26 / 50 | 0 | Venom, Venom, Fade (Ruin), Venom, Venom, Fade (Cleave), Bite. Blessing not needed |

Whole campaign from title screen to the final victory card: about 6 minutes.

Inner Vault replays:

| Order | Result | Turns | HP left |
| ----- | ------ | ----- | ------- |
| Guardian first, Fade the Crush | Win | 6 | 13 / 50 |
| Sentinel first, no Fade | Loss | 5 | 0 |

The loss is explainable from what's on screen: the telegraphed Crush (16) went unfaded and left 13 HP, the Bite killed the Sentinel, and then the Guardian's telegraphed Heavy Blow (9) landed on 9 HP while the "Fade through this heavy attack" hint was showing. Retry was offered straight away.

Also seen in the playtest:

- The Awakening problem below is real in game: after pressing 4 and then Venom, the Sentinel still acted (its intent moved from Recover to Crush) and the HUD showed "1 actions left".
- In the Asura fight the poison tick crossed 35 HP, and the shown Cursed Claw switched to Roar (0) before the boss acted, as described above.
- Small UI notes for Charith: the Inner Vault victory card says "Vault Guardian defeated" even when the Sentinel died last, and the HUD says "1 actions left" instead of "1 action left".

## Still to verify

- Time against the 10 to 15 minute target: measured at about 6 minutes for an experienced player with no retries, so it's under target for someone who knows the game. A first-time player reading hints, the map and story screens, and retrying a fight or two is likely to be closer to the target, but that is unverified until the team test.
- Unfamiliar-player test: needs the team. For each tester, record the first full campaign separately from any repeat, and per encounter note time, turns, HP left, retries, the strategy they used, and anything they said was confusing (especially Awakening).
