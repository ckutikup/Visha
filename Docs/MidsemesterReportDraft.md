# Mid-Semester Report

**Project:** Visha: The Last Guardian
**Course / Team:** SER594 / Team 5
**Date of Meeting:** [Enter the actual recording date]
**Zoom Recording Link:** [Add the accessible recording link]

## Demo Till Date

Since our second biweekly report, we have connected the combat systems into a playable Unity demo. The current version has three single-enemy encounters: Temple Guardian and Guardian Warden in the Courtyard, followed by a Vault Sentinel encounter introducing the Vault. This is a partial version of the planned five-encounter campaign.

All three player abilities work. Venom Strike adds poison, Serpent’s Bite consumes it for immediate damage, and Fade reduces damage during the next enemy phase. Poison resolves before the enemy attacks, and an enemy defeated by poison cannot act. Enemy intentions show the upcoming move and damage. The zero-damage turn previously called Defend is now called Recover because it does not provide armour.

The player can start from the title screen, complete encounters, advance to the next fight, retry after defeat, and reach a sealed-path ending. A temple map shows completed, available, locked and unfinished encounters. Completed encounters can be replayed; the inner Vault and Sanctum are labelled unavailable. Health, poison and ability state reset between encounters. The interface displays health, poison stacks, enemy intentions, target selection and ability availability.

The presentation now includes compact corner ability controls, simpler character sprites, separate Courtyard and Vault backgrounds, moving attacks, hit reactions, damage feedback, sound effects and a mute control. Enemy attacks visibly approach the player before applying damage. Fade’s damage feedback is labelled GUARDED so the reduction is clear.

We added a small optional mechanic called Breaking the Routine. Surviving a marked heavy attack with Fade earns one curse fragment per encounter. The next valid Venom Strike applies one additional poison stack, within the existing five-stack cap. The boon clears between attempts and encounters. This is a deliberate extension to the original pitch; it adds no new ability, inventory or permanent upgrade.

For the recorded demo, we will show:

1. The title screen, temple map, available encounters and sealed future areas.
2. Venom Strike adding poison and poison resolving before the enemy response.
3. Fade reducing a marked heavy attack, earning the one-use boon, followed by its temporary unavailability.
4. Serpent’s Bite consuming poison to finish an enemy before it can attack.
5. Victory, the next encounter, and the different enemy opening.
6. Defeat and retry, the Vault environment, and the sealed-path ending.
7. The corresponding code, tests and repository progress.

The latest code passed 162 Edit Mode checks and 23 Play Mode checks. Automated checks cover combat rules, campaign progression, reset behavior, input locking, movement, damage feedback, mute, map locks, replay, and the boon’s trigger, cap and reset behavior. Two Play Mode checks capture presentation evidence; they are not independent gameplay regressions. These checks do not replace first-time player feedback or establish the final game’s duration.

## Future Plans for the Project Till Final Presentation

| Stage | Deliverable | Main owners | Completion check |
| --- | --- | --- | --- |
| Next integration milestone | Two-enemy battles with left-to-right enemy resolution, individual poison states, target selection and accurate intent displays | Rahul, Balaji, Charith | Killing one enemy prevents its action without skipping the survivor; retry restores both targets correctly |
| Following content milestone | Complete five encounters across Courtyard, Vault and Sanctum, including the fixed-pattern boss | Anshul, Shivani | The campaign can be played from the title through the final ending with correct transitions and resets |
| Presentation milestone | Sanctum background, consistent combat feedback, sound balance and clearer area introductions | Akshat, Shivani, Charith | The three areas are distinct and the interface remains readable at supported resolutions |
| Final two project weeks | First-time playtests, balance adjustments, bug fixing and exported-build verification | All members; Akshat coordinates builds | Record observed problems, retest fixes and complete a full playthrough of the final export |

The final game will retain one playable character and three abilities. We will create deeper decisions through enemy combinations and attack timing. Equipment, upgrades, exploration, saving, a separate ultimate ability and full cutscenes remain outside the agreed scope. The final 10–15 minute duration is a playtesting target, not a measured result yet.

## Participation Summary

Complete this section with each member’s agreed summary before submission. The responsibility column below is a reminder of ownership, not a claim that every planned task is finished.

| Team member | Responsibility | Agreed work completed by this presentation |
| --- | --- | --- |
| Rahul Ganesh Marathervar | Combat flow | [Confirm completed work and relevant PRs] |
| Balaji Radhakrishnan Padmanabhan | Player abilities, poison and health | [Confirm completed work and relevant PRs] |
| Anshul Reddy Goli | Enemy patterns and encounters | [Confirm completed work and relevant PRs] |
| Charith Kutikuppala | Battle UI and integration | [Confirm UI, review and integration work; distinguish it from teammates’ original implementations] |
| Shivani Kalpak Kulkarni | Campaign flow | [Confirm completed work and relevant PRs] |
| Akshat Aggarwal | Presentation and builds | [Confirm original presentation work; distinguish subsequent integration fixes] |

Before submission, replace the placeholders, have all six members agree to the participation summary, and verify that the recording link is accessible. All six members must attend with cameras on. Follow the course’s supplied participation template if it contains additional fields.
