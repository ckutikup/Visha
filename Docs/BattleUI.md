# Battle UI

Charith owns the battle HUD, targeting, health and poison displays, enemy intention display, and reusable navigation screens. Gameplay rules and campaign progression stay with their assigned controllers.

## Try the integrated battle

Use Unity 6000.6.3f1. Open `Assets/Scenes/BattleUI_Demo.unity` and press Play. Choose Venom Strike or Serpent's Bite, then click the enemy; Escape cancels targeting. Fade targets the player. The scene uses the existing combat controller and real health/poison values. Winning or losing opens the corresponding screen. Retry resets the current battle through `BattleOutcomeController`.

This scene starts directly in battle. Campaign Start, Next, Restart and Return are connected by the campaign owner; the UI does not create encounters or advance progression.

## HUD contract for Rahul

- Supply current gameplay data through `BattleHUD.Render(BattleViewState)`.
- Subscribe to `AbilityRequested(BattleAbility, int)`; target 0 is the current enemy and -1 is self. Validate and resolve actions in the controller.
- Render EnemyTurn while resolving, then PlayerTurn with updated state. Duplicate submissions remain blocked until a phase transition.
- Call `RejectAction(reason)` when validation fails so the player can choose again. This only unlocks actions during PlayerTurn.
- Supply the actual enemy name, intention, incoming damage, poison count and Fade availability. The HUD does not calculate combat rules.
- `SetInputEnabled(false)` cancels targeting and blocks all ability requests while another screen owns input.

## Screen contract for Shivani

Reuse `Assets/Prefabs/BattleScreens.prefab`. It contains a title/tutorial panel, victory, defeat and completion panels, and an encounter counter. No campaign assembly is required.

1. In the campaign scene, disable or omit `BattleScreenPresenter` before binding your controller. That presenter only connects the existing single-battle outcome and retry.
2. Call `screens.SetHUD(hud)` and bind your handlers with `BindNavigation(start, next, retry, restart, returnToStart)`. Each argument is a parameterless Action; null disables or hides unavailable navigation.
3. Drive the view from campaign state: `ShowTitle()`, `ShowBattle()`, `ShowVictory(enemyName)`, `ShowDefeat(enemyName)`, or `ShowComplete()`.
4. Call `SetEncounter(number, total)` with the active encounter; `(0, 0)` hides the counter. The view never increments it.
5. A navigation request locks buttons until the controller changes screens. If navigation fails or remains on the same screen, call `RejectNavigation(reason)` to unlock it. Repeated renders of the same screen do not release the duplicate-click guard.
6. Unbind with `BindNavigation(null, null, null, null, null)` when your controller is disabled. Keep one navigation owner active at a time.

Start/Next/Retry/Restart callbacks must load or reset gameplay state before showing Battle. Stop combat while title/result screens are open; UI input locking alone does not pause a controller. Transition through Battle when restarting or loading another encounter.

With no Next handler, victory offers Retry Battle when a Retry handler exists. No dummy encounter progression is supplied. Campaign completion copy and encounter counts should match the playable milestone.

## Assets and verification

`BattleHUD.prefab` holds the battle interface. `BattleScreens.prefab` holds navigation overlays. `BattleScreenSetup` builds the overlays and installs them in the existing battle scene; regeneration overwrites prefab edits, so normal layout changes can be made directly in the prefab instead.

Both canvases use a 1920x1080 reference layout for 16:9. WorldHUDAnchor supports overlay and camera canvases. Keep one EventSystem with InputSystemUIInputModule in an integrated scene.

Run EditMode tests for UI request guards and navigation callbacks. Run PlayMode `Visha.UI.PlayModeTests` for actual battle victory/defeat/retry and campaign-controller handoff. The tests force lethal damage to isolate navigation from balance.

## Team integration still needed

- Rahul: connect Balaji's ability components, enforce turn/Fade rules, and gate combat during campaign transitions.
- Balaji: maintain ability/health/poison behavior and agreed balance values.
- Anshul: supply encounter enemy patterns and intention data.
- Shivani: connect the screen callbacks to encounter loading, progression, retry, restart and completion.
- Akshat: replace placeholders, add audio/visual feedback, and produce a tested build.
