# Midsemester build and run

Open the local Mac build at `Desktop/Visha Builds/VishaMidsemester-October18-Combat.app`. Build outputs are outside the Git repository. This application was exported from the October 18 presentation revision using Unity 6000.6.3f1.

## Unity Editor

1. Open the Desktop/Visha project in Unity 6000.6.3f1.
2. Stop Play mode, wait for import/compilation, and reopen `Assets/Scenes/Campaign_Demo.unity` from disk. Do not overwrite it with an older scene still open in memory.
3. Press Play and choose Enter the Courtyard.
4. Select Venom Strike or Bite, then click the highlighted enemy. Fade acts immediately. Esc cancels target selection. Sound On/Off is at the upper right.

## Rebuild

In File > Build Profiles, choose macOS. Include Campaign_Demo as the first scene. Build to a folder outside the repository. The editor utility `Visha.Combat.Flow.EditorTools.DemoBuild.BuildMac` also exports this scene; it reads the destination .app path from VISHA_BUILD_PATH and restores the previous display settings afterward.

The verified export starts in a 1280×720 window. The HUD was also rendered and inspected at 1920×1080. BattleUI_Demo is an independent UI demonstration, not the campaign entry point.

## Verification on October 18

- 157 Edit Mode checks passed for combat and components.
- 16 Play Mode checks passed: 15 scene regressions and one screenshot capture check. These cover campaign outcomes/reset, poison kills, attack movement, Fade expiry, input locking, interruption, environment transitions and mute.
- The exported Mac app launched. Interactive checks covered Start, Venom, Fade against Heavy Blow (9 reduced to 4), Bite consuming four stacks for 16 damage, victory, Next, and the second encounter restoring HP/poison/Fade.
- A complete first-time human playtest and a measured 4–7 minute duration have not been performed. Full defeat/retry and end-of-demo coverage was in Editor Play Mode, not manually replayed in the export.

## Known limits

The player log reports stripped/unsupported URP post-processing shaders. The displayed 2D scene and combat worked; no blocking exceptions were observed during the exported-build checks. Do not rely on Bloom or depth-of-field effects in this build. Editor setup utilities also emit deprecation warnings under Unity 6000.6.3f1.

Only three single-enemy encounters are included. Two-enemy combat, the remaining campaign content, boss, and final balancing remain semester work.
