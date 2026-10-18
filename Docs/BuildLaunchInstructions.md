# Midsemester build and run

Open the local Mac build at `Desktop/Visha Builds/VishaMidsemester-October18-OverheadReview.app`. Build outputs are outside the Git repository. This application was exported from the October 18 presentation revision using Unity 6000.6.3f1.

## Unity Editor

1. Open the Desktop/Visha project in Unity 6000.6.3f1.
2. Stop Play mode, wait for import/compilation, and reopen `Assets/Scenes/Campaign_Demo.unity` from disk. Do not overwrite it with an older scene still open in memory.
3. Press Play and choose Enter the Temple, then Enter Encounter on the map.
4. Select Venom Strike or Bite, then click the highlighted enemy. Fade acts immediately. Esc cancels target selection. Sound On/Off is at the upper right.

## Rebuild

In File > Build Profiles, choose macOS. Include Campaign_Demo as the first scene. Build to a folder outside the repository. The editor utility `Visha.Combat.Flow.EditorTools.DemoBuild.BuildMac` also exports this scene; it reads the destination .app path from VISHA_BUILD_PATH and restores the previous display settings afterward.

The verified export starts in a 1280×720 window. The HUD was also rendered and inspected at 1920×1080. BattleUI_Demo is an independent UI demonstration, not the campaign entry point.

## Verification on October 18

- 162 Edit Mode checks and 23 Play Mode checks passed. Two Play Mode checks capture screenshots; the rest cover gameplay and navigation.
- Checks include base combat, map locks, replay progress, input locking, effects cleanup, the boon’s trigger, one-use reward, poison cap, and reset/transition behavior.
- Presentation screenshots were inspected at 1280×720 and 1920×1080.
- The exported Mac app was launched and checked through title → map → first encounter → victory → unlocked Warden node. Checks included a sealed Sanctum node, Venom, guarding Heavy Blow, earning and consuming the boon at the five-stack cap, and a Bite finish. No blocking exceptions were observed in the player log during this pass.
- A complete first-time human playtest and a measured duration have not been performed. Automated checks do not establish difficulty or final game length.

## Known limits

The player log reports stripped/unsupported URP post-processing shaders. The displayed 2D scene and combat worked; no blocking exceptions were observed during the exported-build checks. Do not rely on Bloom or depth-of-field effects in this build. Editor setup utilities also emit deprecation warnings under Unity 6000.6.3f1.

Only three single-enemy encounters are included. Two-enemy combat, the remaining campaign content, boss, and final balancing remain semester work.

## UI review revision

See LocalUIReview.md for the latest controls, five tested render sizes, visual fixes and review checklist. This UI revision was approved for pull-request review after local testing. The latest Play Mode regression run passed 27 checks, including four capture checks; the final focused visual pass covers all nine screens/states at five sizes and two two-enemy layout previews. The latter are not playable two-enemy combat.
