# The Vault: two-enemy encounter

This configures the Vault encounter, the first fight where two enemies act in the
same round. It uses the two existing enemy types (no new enemies) and sets the
starting health and opening offsets for a fight that is harder than the solo
encounters but still winnable. These are playtest starting values, not final
balance. Every knob lives in one place (`VaultEncounter.ApplyDefaultVault`) and in
the asset.

## The two enemies

| Slot | Target id | Enemy (type) | Max HP | Opening offset | Opens by telegraphing |
| ---- | --------- | ------------ | ------ | -------------- | --------------------- |
| 0 | 0 | Vault Guardian (Temple Guardian) | 18 | 0 | Strike (4) |
| 1 | 1 | Vault Sentinel (Vault Sentinel) | 26 | 2 | Shield Bash (8) |

Both start at full health. Target ids are stable and never renumber, including after
an enemy dies.

The Guardian is the lighter target (its Heavy Blow is only 9). The Sentinel is the
bigger threat (Crush hits 16) but has more health, so the player has to choose: race
the Sentinel down, or drop the Guardian first for tempo and then manage the Sentinel
with Fade and poison.

## Why the opening offset matters

Each enemy telegraphs its move a turn ahead, so with both acting every round the real
design question is whether their big hits land together. The Sentinel's offset of 2
staggers its cycle so its Crush (16) never shares a turn with the Guardian's Heavy
Blow (9). Combined incoming damage per turn settles into a readable rhythm:

| Turn | Guardian | Sentinel | Combined | With Fade (each hit halved, rounded down) |
| ---- | -------- | -------- | -------- | ----------------------------------------- |
| 1 | Strike (4) | Shield Bash (8) | 12 | 6 |
| 2 | Heavy Blow (9) | Recover (0) | 9 | 4 |
| 3 | Recover (0) | Crush (16) | 16 | 8 |

The cycle repeats (12 / 9 / 16). The worst single turn is the lone Crush at 16, never
9 + 16 = 25. Offset 0 was rejected because Heavy Blow and Crush land together (25 on
turn 2), and offset 1 opens with Strike plus Crush (20 on turn 1, then 17).

## Difficulty

The player has 50 HP in the campaign scenes. One full cycle deals 37 raw damage, so
doing nothing is lethal during the second cycle. Fade can't be used two turns in a
row, so it can soften one big hit per cycle, and killing an enemy early removes its
damage for the rest of the fight.

Simulated with the combat contract v2 rules (Venom +2 stacks, cap 5; poison 2 per
stack each turn; Bite stacks x 4; Fade halves, not twice in a row; Twin Fang and
awakening left out):

| Setup | Best play | Never uses Fade |
| ----- | --------- | --------------- |
| Vault: Guardian 18 + Sentinel 26 | 21 / 50 HP left, 7 turns | dies |
| For reference: solo Vault Sentinel, 46 HP | 34 / 50 HP left, 5 turns | 26 / 50 HP left |

So the Vault is clearly harder than the last solo fight, and Fade timing is required
to survive it. Higher health was tried and makes it close to unwinnable (Guardian 24 +
Sentinel 32 leaves 9 / 50 with perfect play).

Tuning knobs, in order of effect: each enemy's max HP (how long the pressure lasts),
the opening offsets (keep the big hits apart), and starting health (for a softer
opening). If playtests find it too hard, lower the Sentinel's HP first; if too easy,
raise it. Enemy move damage is deliberately not changed here. It lives in
`TempleGuardianPattern` / `VaultSentinelPattern` and is shared with the solo fights.

## Integration contract

`VaultEncounter` is data only. It applies no damage and runs no turns.

- Rahul (combat flow): spawn one combatant per `EnemyPlacement`, keyed by `TargetId`.
  Each player turn, copy each enemy's live runner telegraph (`IntentLabel`,
  `IncomingDamage`) into the matching `BattleViewState.enemies` entry
  (`EnemyViewState` with the same id, name and max health). Configure each
  `EnemyPatternRunner` with the placement's `Pattern`, `ResolvedName` and
  `OpeningOffset`; the runner already supports the offset. A placement's
  `BuildPattern()` returns the same pattern positioned at its opening move.
- Charith (HUD): renders those entries; ids stay stable after a death.
- Balaji (abilities): per-enemy poison and Bite are already separate; Twin Fang's 2
  leftover poison fits two targets here.
- Shivani (progression): reset both enemies to `StartingHealth` on start and retry,
  and place the Vault on the temple map after the solo fights.
- Akshat (art): assign each placement's `EnemySprite`.

## Files

| File under Assets | Purpose |
| --- | --- |
| Scripts/Campaign/VaultEncounter.cs | Two-enemy encounter config: placements, ids, health, offsets, opening intents |
| Scripts/Campaign/Editor/VaultEncounterSetup.cs | "Visha/Setup/Create Vault Encounter Asset" menu |
| Tests/Editor/Campaign/VaultEncounterTests.cs | Locks the agreed values, intentions and the staggering |
| Scripts/Enemies/EnemyPatternRunner.cs | Added `CreatePattern(kind)` so the config builds patterns the same way the runner does |

## Verification

Window > General > Test Runner > EditMode > Visha.Campaign.Tests >
`VaultEncounterTests`. The tests check the two enemy types, the agreed health, the
opening intentions, that each enemy telegraphs its big hit, that the big hits land on
different turns (12 / 9 / 16, worst turn 16), and that one cycle (37) is survivable
but two are lethal for the 50 HP player.

To create the asset: Visha > Setup > Create Vault Encounter Asset (writes
`Assets/Data/Encounters/VaultEncounter_GuardianAndSentinel.asset`). It stays inert
until the two-enemy combat flow consumes `VaultEncounter`.
