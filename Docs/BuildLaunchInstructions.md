# Presentation build

Rebuild the standalone app after pulling the presentation revision. An earlier `VishaPresentation.app` in the contributor's `/private/tmp` does not include these updates and is not a shared download. Keep build outputs outside the Git repository and share the actual app archive with the team.

## Launch

On the presentation Mac, copy `VishaPresentation.app` to a convenient location, then open it from Finder. If macOS blocks the app on first launch, use the standard Open action from its context menu.

For a windowed launch at a chosen size, run the player inside the app bundle:

```sh
"/path/to/VishaPresentation.app/Contents/MacOS/Visha" -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
```

Replace the width and height with `1280` and `720` for the smaller display. The game supports mouse input: choose an ability, then click the enemy; Fade applies directly. The victory and defeat panels provide the campaign navigation controls.

## Display checks

- 1920×1080 window: title, battle HUD, sprites, health bars, intention, targeting controls, and ability buttons visible.
- 1280×720 window: campaign HUD and abilities remain visible and readable.

The campaign opens first. `BattleUI_Demo` remains a separate single-battle scene in the Unity project.
