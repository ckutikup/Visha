# Build and run

## Unity Editor

1. Open the project with Unity 6000.6.3f1.
2. Open `Assets/Scenes/Campaign_Demo.unity`.
3. Press Play, choose Enter the Temple, and select an available encounter on the map.

The campaign scene is already configured. `BattleUI_Demo` and the two-enemy preview are separate development scenes.

## Controls

- 1: Venom Strike.
- 2: Serpent's Bite, or Twin Fang while available during Awakening.
- 3: Fade.
- 4: Guardian Awakening when charged and available.
- Click an enemy or press Enter to confirm an attack; Tab switches eligible targets.
- Escape cancels target selection.
- Hover an ability for its description. Sound can be toggled in the battle UI.

## Build

The release build uses Unity 6000.6.3f1 and the `DemoBuild.BuildMac` editor method.
It builds only `Assets/Scenes/Campaign_Demo.unity` as a windowed 1280x720 macOS
application, so the standalone build does not include the battle or UI preview
scenes. Install Unity's macOS Build Support module if it is missing.

After the team approves the candidate, run this from Terminal (update the Unity
executable path if the editor is installed elsewhere):

```sh
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity"
VISHA_BUILD_PATH="/private/tmp/VishaPresentation.app" "$UNITY_EDITOR" \
  -batchmode -quit \
  -projectPath "/Users/akshataggarwal/Documents/ChatGPT/SER594 PROJECT" \
  -executeMethod Visha.Combat.Flow.EditorTools.DemoBuild.BuildMac \
  -logFile /private/tmp/Visha-build.log
```

Launch the resulting app from Finder or run:

```sh
open "/private/tmp/VishaPresentation.app"
```

The app path above is the planned output location; no standalone app has been
built or verified yet. Before release, launch the app outside Unity and record
the tested macOS version, hardware architecture, display sizes, controls, audio,
campaign progression, save, retry, and ending results here. Do not describe the
candidate as verified until those checks pass.

## Tests

Open Window > General > Test Runner. Run Edit Mode tests for combat rules, UI state and campaign configuration, and Play Mode tests for scene integration and gameplay flow. Test the exported application separately before release.
