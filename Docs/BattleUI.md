# Battle UI milestone

This contribution implements Charith's assigned interface: skill buttons, target
selection, health displays, poison indicators, and enemy intention displays.
It contains no combat resolution, enemy patterns, world art, animation, campaign
progression, retry logic, or victory screen. The original BattlePrototype scene is
unchanged. The demo uses its existing player and enemy placeholders.

## Open and demonstrate

Use Unity 6000.6.3f1. Open Assets/Scenes/BattleUI_Demo.unity and press Play.
No extra packages or manual wiring are required for this scene.

1. Click Venom Strike. The guardian's target marker appears. Escape cancels.
2. Click the guardian. The HUD sends an action request and disables further input.
   The preview switches to an enemy-phase sample; it does not perform an attack.
3. Press F1 (fn+F1 if macOS uses the key for brightness) to show sample controls.
4. Player sample displays 22/30 player HP, 16/24 enemy HP and three poison stacks.
   Serpent's Bite is available; Fade is marked unavailable.
5. Enemy, Victory and Defeat samples demonstrate status labels and input blocking.
   These are HUD states, not enemy logic or campaign/result screens.
6. Reset preview restores the initial sample. Fade sends a self-targeted request.

F1 controls are available only in the Editor or a development build. Sample values
are demonstration inputs, not agreed damage or balance values.

## Integration contract

The dependency direction is controller -> BattleViewState -> BattleHUD.Render.
The UI sends intent back through BattleHUD.AbilityRequested(BattleAbility, int).
The snapshot is display data, not a second gameplay state model.

- Subscribe to AbilityRequested before the first Render; without a subscriber the
  ability controls are disabled. Unsubscribe when the controller is disabled.
- Target index 0 is the single enemy; -1 is self for Fade. Two-enemy UI support is a
  later milestone to coordinate with Rahul and Anshul.
- The owning controller validates requests and calculates all health, poison,
  Fade restrictions, targeting validity, turn order, and deaths.
- The HUD prevents duplicate clicks immediately after sending one request.
  Re-rendering the same phase cannot reopen input. A phase transition unlocks it.
- Render EnemyTurn when the accepted action resolves, then PlayerTurn with the
  next actual state. For a new encounter or a rejected action, render Waiting
  before the current PlayerTurn snapshot. SetFeedback can explain rejection.
- Serpent's Bite uses the supplied poison count for availability; Fade uses the
  supplied fadeAvailable flag. These UI hints do not replace rule validation.
- Health and poison are clamped only for display. Render does not mutate input.

Add Assets/Prefabs/BattleHUD.prefab to the integration scene. Use one EventSystem
with InputSystemUIInputModule. For the four WorldHUDAnchor components, assign the
scene camera and the appropriate player/enemy Transform; the demo contains wired
examples. Offsets are world units and may need adjustment when art changes.
The prefab uses a 1920x1080 reference Canvas and is intended for 16:9 screens.
Do not copy PreviewControls or BattleHUDPreview into the real combat scene.

## Files and responsibilities

| File under Assets | Purpose |
| --- | --- |
| Scripts/UI/BattleHUD.cs | Display snapshot, labels, availability, ability-first targeting and request event |
| Scripts/UI/AbilityButtonView.cs | Hover/selected/disabled UI feedback |
| Scripts/UI/TargetIndicator.cs | Eligible-target marker and hover emphasis |
| Scripts/UI/HealthBarView.cs | Display interpolation and delayed damage trail |
| Scripts/UI/PoisonIndicatorView.cs | Five visual segments with numerical count retained in the HUD |
| Scripts/UI/IntentView.cs | Short action label, icon and incoming value |
| Scripts/UI/WorldHUDAnchor.cs | Attach the UI to supplied world transforms |
| Scripts/Preview/BattleHUDPreview.cs | Fixed sample states for testing without a gameplay controller |
| Prefabs/BattleHUD.prefab | Reusable, wired interface |
| Scenes/BattleUI_Demo.unity | Separate demonstration scene with existing placeholders |
| Art/UI/Diamond.png, GuardianHelm.png | Simple placeholder UI icons; replace during art integration |
| Tests/Editor/BattleHUDTests.cs | Input guards, targeting, snapshot integrity and scene-reference checks |

Assembly definitions separate runtime UI, preview controls and Editor tests.
Unity .meta files accompany the assets so references survive cloning.

## Work remaining with the team

Rahul owns combat flow and authoritative action/target handling; Balaji owns
abilities, health changes and poison/Fade rules; Anshul supplies enemy intention
data and encounters; Shivani owns campaign, retries and result screens; Akshat
owns sprites, backgrounds, sound, hit feedback and builds.

Our initial integration target is one real player/enemy exchange using this HUD.
Later UI milestones can add two-target support, actual-state integration tests,
readability/accessibility improvements and final art compatibility. The semester
reports should describe completed, demonstrable work rather than future features.

## Verification

Window > General > Test Runner > EditMode > Visha.UI.Tests runs the HUD tests.
Use Play mode to check ability selection, Escape cancellation, target confirmation,
poison/Fade availability, F1 sample states and duplicate-click locking.
