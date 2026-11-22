# Campaign progression

`Assets/Scenes/Campaign_Demo.unity` is the main scene and starts at the title screen.

| Encounter | Area | Opponents |
| --- | --- | --- |
| 1 | Courtyard | Temple Guardian |
| 2 | Courtyard | Guardian Warden |
| 3 | Vault | Vault Sentinel |
| 4 | Inner Vault | Vault Guardian and Vault Sentinel |
| 5 | Sanctum | The Asura |

Victory unlocks the next encounter. Next Encounter enters it directly; Temple Map allows replay of unlocked encounters. Both enemies must be defeated in the Inner Vault. Defeating Asura opens the ending and makes the full map replayable.

## Encounter state

The player starts each encounter at full health. Retry restores health, poison, enemy patterns and temporary ability state. Guardian Awakening becomes available in the Vault; the first Vault fight introduces it ready to use. Temple's Blessing is restricted to Asura and refreshes on retry.

CampaignController owns encounter progress and connects battle outcomes to CampaignProgress. CampaignScreens binds the menu, victory, defeat and ending buttons. TempleMapView handles map selection and chapter introductions. VaultCampaignLink enables the second opponent only for encounter four.

## Checkpoints

CampaignSave stores the highest cleared encounter and the encounter to resume in `campaign.json` under Application.persistentDataPath. Continue starts the furthest unlocked encounter at full health; replaying an earlier fight does not move that checkpoint backward. After completion, Replay Finale starts the Asura fight again. Individual turns are not saved. Returning to the title preserves progress; New Game requires confirmation before replacing it. Invalid or unreadable saves show a warning on the title before Start Game replaces them. Write failures are shown on result screens and in the title status; progress remains available for the current session.

## Verification

Campaign tests cover unlocking, defeat, retry, replay, the ending, checkpoint reload and new-game confirmation. The normal-ability playthrough test completes all five fights without directly changing health. Test saves use temporary directories so they do not replace player progress.
