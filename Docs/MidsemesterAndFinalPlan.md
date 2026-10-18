# Visha: The Last Guardian — Midsemester and Final Plan

## Midsemester scope

The midsemester goal is a small playable section of the game, not the finished campaign. The two Courtyard encounters form the main demo. The existing single-enemy Vault encounter is a preview of the next area, not the planned two-enemy Vault system.

- Keep the three original abilities: Venom Strike builds persistent poison, Serpent’s Bite consumes it for immediate damage, and Fade reduces the next enemy phase’s damage and cannot be used consecutively.
- Resolve turns in the original pitch order: player action, poison damage, then the surviving enemy’s action. Display the enemy’s next action before the player chooses.
- Connect the available fights with a temple map showing all five planned encounters. Completed nodes can be replayed; later nodes remain locked until reached. The inner Vault and Sanctum are unavailable in this build.
- Give each available encounter a short introduction and a clear challenge. The Guardian introduces the core rules; the Warden opens with a heavy attack; the Sentinel previews the Vault with Recover, Crush, and Shield Bash.
- Use visible approach, contact, recoil, and recovery movements. Give Strike, heavy attacks, Shield Bash, and Fade distinct feedback. Keep health, poison, intentions, and ability controls readable.
- End at a sealed path with replay and return navigation. Do not present the unfinished campaign as a completed game.
- Demonstrate retry, encounter resets, and navigation as well as combat.

### Small design extension: Breaking the Routine

This is an addition to the original pitch, not a previously promised feature. It retains the original three abilities and can be disabled independently.

Once per encounter, surviving a marked Heavy Blow or Crush while Fade is active earns a curse fragment. The fragment grants one extra poison stack on the next valid Venom Strike, capped at five total stacks. It cannot stack or be earned again in the same attempt. It resets on retry, encounter changes, and return to title. Invalid actions do not spend it.

The opportunity is labelled before the action, and the earned boon remains visible until used. A poison kill before the enemy attacks does not count as guarding an attack. Bite remains useful as an immediate poison finisher without an additional reward rule.

## What remains for the final game

| Area | Final work | Main owners |
| --- | --- | --- |
| Combat flow | Two-enemy sequencing, per-target resolution, dead-target handling, clear input locking throughout the enemy phase | Rahul |
| Abilities and balance | Tune poison, Bite, Fade, and the optional boon using playtests; verify useful choices instead of a single dominant sequence | Balaji |
| Enemies and encounters | Finish both Vault encounters with different enemy timings; implement the Sanctum boss with a readable repeating pattern | Anshul |
| Battle interface | Multi-target selection, previews for both enemies, keyboard/controller focus, contrast and resolution checks | Charith |
| Campaign | Complete the five-encounter route, area introductions, replay/reset behavior, and a separate true victory ending | Shivani |
| Presentation and builds | Sanctum atmosphere, consistent sound and visual feedback, targeted animation improvements, exported builds, playtest support | Akshat |

These are proposed assignments for the remaining work. Each member should confirm ownership, and the participation summary should credit only work actually completed by that person.

## Milestones after midsemester

1. **First integration milestone:** make one two-enemy Vault battle work end to end, with target selection, separate intentions, poison per enemy, and reliable retry.
2. **Campaign milestone:** finish the second Vault encounter and Sanctum boss, connect all five nodes, and add the true temple-reclaimed ending.
3. **Playtest milestone:** observe first-time players, record confusing decisions and deaths, measure playtime against the original 10–15 minute target, and adjust encounter patterns and balance.
4. **Final two project weeks:** fix regressions, improve accessibility and presentation, test the exported build, and prepare final demo evidence and contribution summaries.

The team should add calendar dates based on the final submission deadline. These are proposed development milestones, not additional course requirements.

## Midsemester presentation and submission

- Show the pitch and previous reports, then explain what has become playable since Report 2.
- Demonstrate the temple map, one complete fight, poison versus Bite, guarding a heavy attack, the earned boon, retry, and the next encounter’s changed challenge.
- Show the Vault preview and identify the sealed content honestly.
- Explain the remaining tasks and owners using the table above.
- Include the meeting date, an accessible Zoom recording link, and an agreed participation summary.
- All six members should attend the recorded presentation with cameras on, as required by the assignment.

The rubric allocates 10 points to the demo aligned with the reports, 5 to detailed future plans, 3 to the recording and participation on camera, and 2 to the agreed participation summary. It does not specify a percentage of the game that must be finished.
