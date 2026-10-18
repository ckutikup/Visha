# Midsemester demo

## Demonstrable scope

Three single-enemy encounters: Temple Guardian (30 HP), Guardian Warden (44 HP), and Vault Sentinel (46 HP). The player starts each encounter with 50 HP. Start, Next, defeat/retry, demo completion, restart and return to start are connected.

The presentation revision adds a shared character atlas, a temple courtyard backdrop, readable status panels, live ability descriptions, a short pause before the enemy response, and audio/reset fixes. It does not implement the final game.

## Combat rules in this build

Venom Strike adds 2 poison stacks, up to 5. Poison persists and deals 2 damage per stack each round. Bite consumes all poison for 4 damage per stack. Fade reduces incoming damage by 50% for one enemy phase and cannot be used consecutively. Another successful ability restores its availability.

The current integrated controller resolves the player ability, the enemy action, then poison, stopping after death. The submitted pitch described poison before the enemy action. That design difference remains a separate team decision; this presentation revision does not change it. Explain the implemented order accurately in the demo.

The Guardian teaches the repeating Strike / Heavy Blow / Defend sequence. The Warden uses that sequence with more health. The Sentinel opens with Defend, then Crush and Shield Bash. Defend currently means a zero-damage turn, not an armour buff. Enemy intentions are visible before choosing an ability.

## Suggested recording sequence

1. Show the title/tutorial and start the campaign.
2. Point out both health bars, the enemy intention, poison count, and ability descriptions.
3. Use Venom Strike, then show how poison enables Bite and changes its displayed damage.
4. Use Fade before a heavy hit and show its restriction on the following turn.
5. Finish an encounter and use Next to demonstrate the different enemy sprite and encounter number.
6. Demonstrate defeat/retry, then show completion and restart if time permits.

Do not claim final balance, five completed encounters, multi-enemy targeting or a finished boss. The presentation pass and review fixes are follow-up integration work, separate from each teammate's original contribution.

## Remaining semester work

Two-enemy targeting and turn resolution; the remaining encounters and boss; Vault and Sanctum presentation; fuller tutorial/story transitions; additional audio and feedback; balance testing and standalone release verification.

Use Unity 6000.6.3f1 and open Assets/Scenes/Campaign_Demo.unity. BattleUI_Demo remains the separate single-battle scene. Rebuild any previously shared application after pulling this revision.
