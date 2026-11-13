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

In File > Build Profiles, select the target platform and include `Campaign_Demo` as the first scene. Save build output outside the repository. Install the matching Unity platform module if it is missing.

## Tests

Open Window > General > Test Runner. Run Edit Mode tests for combat rules, UI state and campaign configuration, and Play Mode tests for scene integration and gameplay flow. Test the exported application separately before release.
