# Enemy patterns and encounters

Anshul's part of the midsemester plan: enemy behavior and the encounter content
for the first three fights. Everything lives in the `Visha.Enemies` assembly
(`Assets/Scripts/Enemies`). It contains no damage, poison, turn or UI code, and it
does not touch the shared battle scenes or project settings.

## The two regular enemy types

Both types come from the team's approved concept (Guard and Vault Sentinel). Each
is a fixed, repeating cycle. Nothing is random, so every fight is something the
player learns to read.

### Temple Guardian (`PatternKind.TempleGuardian`)

| Step | Move       | Kind   | Damage | Why it is there |
| ---- | ---------- | ------ | ------ | --------------- |
| 0    | Strike     | Attack | 4      | A normal, low-pressure turn. |
| 1    | Heavy Blow | Attack | 9      | Telegraphed a turn ahead: the turn to use Fade. |
| 2    | Defend     | Defend | 0      | A safe window to stack Venom Strike. |

### Vault Sentinel (`PatternKind.VaultSentinel`)

| Step | Move          | Kind   | Damage | Why it is there |
| ---- | ------------- | ------ | ------ | --------------- |
| 0    | Defend        | Defend | 0      | Opens with a free turn to start poison. |
| 1    | Crush         | Attack | 16     | The biggest regular hit. Fade halves it to 8. |
| 2    | Shield Bash   | Attack | 8      | Comes straight after. Fade can't be used twice in a row, so only one of the two hits can be softened. |

The Guardian asks *when* to Fade. The Sentinel asks *which* hit to Fade, and
whether to Bite it down before the next Crush.

Move labels are kept to 11 characters or fewer, because the HUD's intent box
shows about that many ("Shield Bash" fits; "Crushing Blow" was cut to
"CRUSHING"). A test guards this for every enemy type.

## Defend

Decided: **Defend is a labelled idle turn.** The enemy braces and deals 0 damage.
It has no damage-reduction effect, so it needs no code in `Visha.Combat`. Fade
only reduces Attack moves, so Fade and Defend never interact. The label contains
"Defend", which is what `IntentView` checks to show the shield icon. A test guards
that for both enemies.

## The three encounters

One active enemy per encounter. The player starts every encounter at full health
(team decision), so each fight is balanced on its own.

| # | Display name    | Enemy type     | Max HP | Starting HP |
| - | --------------- | -------------- | ------ | ----------- |
| 1 | Temple Guardian | TempleGuardian | 30     | full        |
| 2 | Guardian Warden | TempleGuardian | 44     | full        |
| 3 | Vault Sentinel  | VaultSentinel  | 46     | full        |

Only one value rises at each step, as the plan asks: encounter 2 is the same enemy
with more health, and encounter 3 changes the enemy type.

Balance check, simulated with the combat contract v2 rules (player 50 HP; Venom
+2 stacks, cap 5; poison 2 per stack after the player acts; Bite stacks x 4;
Fade halves and can't be used twice in a row):

| # | Best play            | Never uses Fade      |
| - | -------------------- | -------------------- |
| 1 | 42 / 50 HP, 4 turns  | 37 / 50 HP, 4 turns  |
| 2 | 38 / 50 HP, 5 turns  | 33 / 50 HP, 5 turns  |
| 3 | 34 / 50 HP, 5 turns  | 26 / 50 HP, 4 turns  |

Each encounter is harder than the one before, and good Fade timing matters more in
each one. These are starting values for playtesting; all of them are editable.

The encounter data itself is stored in Shivani's `EncounterConfig` assets
(`Visha.Campaign`), which pick the enemy type through
`EnemyPatternRunner.PatternKind`. The values above are what those three assets
should hold.

## Integration contract

`EnemyPatternRunner` is the only component other systems touch. It decides and
reports the enemy's move; it never applies damage or reads other systems.

| Member | Used by | Purpose |
| --- | --- | --- |
| `EnemyName` | Rahul (view state) | Display name, or the encounter's override |
| `IntentLabel`, `IncomingDamage`, `IntentKind` | Rahul -> Charith's HUD | The telegraph: label, damage and Attack/Defend type |
| `NextAction`, `NextIsDefend`, `PeekAhead(n)` | Anyone | Read the upcoming move without changing it |
| `TakeTurn()` | Rahul | Returns the move to apply and advances the cycle |
| `ResetPattern()` | Shivani (retry) | Restarts the cycle from its first move |
| `Configure(kind, displayName)` | Shivani (new encounter) | Switches enemy type, restarts the cycle, sets the HUD name |
| `Kind` | Anyone | Which enemy type is active |

The value shown before the enemy acts is always the move `TakeTurn()` returns.
A test checks this over two full cycles for both enemy types.

`PatternKind` values are fixed numbers (`TempleGuardian = 0`, `VaultSentinel = 1`)
because encounter assets store the number. New types must be added at the end.

## Files

| File under Assets | Purpose |
| --- | --- |
| Scripts/Enemies/EnemyAction.cs | One known move: label, damage, Attack/Defend kind |
| Scripts/Enemies/EnemyAttackPattern.cs | Deterministic repeating cycle |
| Scripts/Enemies/TempleGuardianPattern.cs | Temple Guardian moves |
| Scripts/Enemies/VaultSentinelPattern.cs | Vault Sentinel moves |
| Scripts/Enemies/EnemyPatternRunner.cs | MonoBehaviour that exposes the telegraph |
| Tests/Editor/Enemies/EnemyPatternTests.cs | Pattern engine and Guardian tests |
| Tests/Editor/Enemies/VaultSentinelPatternTests.cs | Sentinel cycle, icon labels, differs from the Guardian |
| Tests/Editor/Enemies/EnemyPatternRunnerTests.cs | Configure, names, reset, telegraph matches executed move |

## Verification

Window > General > Test Runner > EditMode > Visha.Enemies.Tests.

## Still open with the team

- Shivani: set the three encounter assets to the values above once this is merged.
- Akshat: sprites for the Temple Guardian and the Vault Sentinel go in each
  encounter's `enemySprite`.
- The boss (The Asura) and multi-enemy fights are the next milestone.
