# Local UI review — October 18

This revision was reviewed locally and approved for publication on a pull request. The merge remains the project owner's decision.

## Run the Mac app

Open `Desktop/Visha Builds/VishaMidsemester-October18-OverheadReview.app`. Use this version rather than the earlier Combat or Temple exports.

Choose **Enter the Temple**, select the first available encounter on the map, then choose **Enter Encounter**.

## Run in Unity

1. Open `/Users/charithkutikuppala/Desktop/Visha` with Unity **6000.6.3f1**.
2. Stop Play mode and wait for asset import and script compilation. Preserve any unrelated unsaved work before reloading a scene.
3. Open `Assets/Scenes/Campaign_Demo.unity` from the Project panel so the saved scene revision is loaded.
4. Open the Game view and select 1280×720 or 1920×1080. Use Fit to view the whole frame.
5. Press Play. Follow the same title → map → encounter route as the app.
6. For layout review, change the Game view resolution to the tested sizes below. The UI uses viewport anchors and fitted canvas scaling; the camera keeps the battlefield in view.

The scene is already configured. You do not need to run setup menus. If rebuilding the presentation deliberately, use **Visha → Apply Battle UI Polish**. The older presentation setup alone does not include this latest pass.

## Controls and a quick review route

- **1:** select Venom Strike.
- **2:** select Serpent's Bite. It is unavailable without poison.
- **3:** use Fade immediately when available.
- **Click the enemy or press Enter:** confirm the selected attack in the current single-enemy encounters.
- **Esc:** cancel a pending target selection.
- Hover an ability to read its description. Selection uses a jade ring; disabled skills retain their name and explain their requirement.
- Sound On/Off is at the upper right.

For a short first-fight review:

1. Use Venom Strike and confirm the enemy. Check the approach, impact, poison counter and enemy response.
2. Use Fade against Heavy Blow. Check reduced damage, the earned curse fragment, and Fade becoming unavailable.
3. Use Venom again. The fragment adds one extra stack, within the five-stack cap, then clears.
4. Use Bite to finish the guardian. Return to the map and verify that the Warden unlocks while later nodes stay locked.
5. Resize the window and hover each skill. Watch the skill names, health values, intent text and explanation panel.

## Visual changes

- A staggered circular skill cluster replaces the row of identical buttons. The skills form a compact shallow arc, with Bite slightly raised and safe margins around all names and state labels.
- Original mesh-drawn poisoned blade, fang and evasive crescent symbols use restrained brass and jade outlines. No artwork or icons were copied from the reference game.
- Full skill names, number shortcuts and brief availability/damage labels remain visible.
- Explanations occupy a bounded lower-left panel instead of following the mouse off-screen.
- Character names wrap within their health panels; HP remains numeric, with a red low-health bar and delayed damage trail.
- Enemy intentions allow multiple lines without crossing the damage value. Poison counts use larger text alongside the five stack markers.
- Courtyard and Vault color grading is restrained, and backgrounds cover the tested aspect ratios while keeping the floor aligned.
- Title, map and outcome text use the same font family and color palette. The map blocks keyboard focus from reaching the screen underneath it.

## Verification

| Render size | Aspect | States rendered |
| --- | --- | --- |
| 1280×720 | 16:9 | Title, map, battle, disabled Bite explanation, long-name/full-poison/low-health selection, victory, defeat, retry, sealed ending |
| 1920×1080 | 16:9 | Same states |
| 1440×900 | 16:10 | Same states |
| 1024×768 | 4:3 | Same states |
| 2560×1080 | Ultrawide | Same states |

The final visual pass produced 45 state/resolution renders plus two two-enemy layout previews. These use the actual Unity scene and UI rendered into buffers of the stated sizes. They are not static mockups and are not a performance benchmark.

The regression run passed 162 Edit Mode checks and 27 Play Mode checks. The latter comprises 23 gameplay/navigation/input checks and four capture checks. A final focused render check was repeated after the visual corrections. Pointer raycasts confirm that skill artwork does not block clicks; an invalid enemy index does not consume a turn. Existing tests cover animation input locks, effect cancellation, victory/defeat/retry, poison, Fade, map progression and the optional boon.

Observed issues fixed during review: missing HP numbers from undersized labels, clipped long intentions, a divider crossing the damage number, dimmed explanation text caused by draw order, a near-edge skill name, and an uncovered strip of background at 4:3.

## Limits

- The playable build still has three single-enemy encounters. Two-enemy screenshots are explicitly labelled layout previews. They do not establish two-enemy targeting, damage resolution or gameplay readiness.
- Inner Vault and Sanctum remain unavailable. No new campaign content or fourth ability was added in this UI pass.
- Portrait windows and sizes below those tested are not covered by this review. There is no touch/controller-specific layout yet.
- Motion uses sprite movement, recoil and effects, not a full frame-by-frame animation set. No performance or accessibility certification is claimed.
- The older URP shader-stripping and editor deprecation warnings remain; this pass does not depend on Bloom or depth-of-field effects.

## Exported app check

The Mac export was checked through title, map, first battle, keyboard attack selection and confirmation, and Fade against Heavy Blow. Health changed to 46/50 after Strike and 42/50 after the guarded heavy hit; poison and availability labels updated correctly. Expanding the native window preserved the layout but exposed insufficient clearance between the poison counter and Venom ring. The cluster was lowered slightly and the five-resolution render check repeated. The inspected player log contained no exception or error entries.

The final skill refinement keeps all three existing abilities. Labels and status text sit below the icons, with a shallow arc and original blade, fang and crescent silhouettes. Review screenshots are in Desktop/Visha Builds/UI Review Screenshots.

## Overhead status revision

Names and health panels now sit above their characters. Enemy poison stays with enemy health; intentions occupy a separate panel above status. Positions derive from each sprite's initial head height with canvas-space gaps, and remain stable during attack movement. Skill controls have more bottom and right clearance. The five-resolution capture check was repeated successfully after these changes, including long names, low health, full poison, and the two-enemy layout preview. The preview is not playable two-enemy combat.

The placement applies proximity and visible-state principles, with dark backplates for contrast. Above-character health is a project layout decision, not a universal usability requirement. References: https://www.nngroup.com/articles/closeness-of-actions-and-objects-gui/ and https://gameaccessibilityguidelines.com/provide-high-contrast-between-text-ui-and-background/ .
