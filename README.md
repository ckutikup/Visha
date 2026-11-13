# Visha: The Last Guardian

A 2D turn-based combat game built in Unity and C# for **SER594 — Game Programming and Design, Team 5**.

Play as Vishakanya, a poison specialist reclaiming a cursed temple. Read enemy intentions, build poison and decide when to defend or consume your stacks to finish an enemy.

## Gameplay

- **Venom Strike:** adds poison that deals damage each round.
- **Serpent’s Bite:** consumes poison for immediate damage.
- **Fade:** reduces damage during the next enemy phase. Cannot be used on consecutive turns.
- **Guardian Awakening:** a temporary state introduced in the Vault that empowers Bite into **Twin Fang** for one use.
- **Twin Fang:** consumes poison for damage and leaves poison on another living enemy, or on the original target if it survives and no other enemy remains.

Enemies display their next action before you choose. In two-enemy battles, each enemy has its own health, poison and attack pattern.

## Current Progress

The playable campaign includes four encounters across the Courtyard and Vault, including a two-enemy Inner Vault battle. It includes a temple map, encounter progression, retry, attack feedback and the Vault Awakening introduction.

The Sanctum boss, Temple’s Blessing and final ending are in development. Final balancing and full-campaign playtesting remain in progress.

## Run the Game

1. Clone this repository.
2. Add the project folder in Unity Hub.
3. Open it with **Unity 6000.6.3f1**.
4. Open `Assets/Scenes/Campaign_Demo.unity`.
5. Press **Play**, choose **Enter the Temple**, and select an available encounter.

`Campaign_Demo` is the main game scene. Other demo and preview scenes are for development.

## Controls

| Input | Action |
| --- | --- |
| **1** | Select Venom Strike |
| **2** | Select Serpent’s Bite or Twin Fang |
| **3** | Use Fade |
| **4** | Activate Guardian Awakening when available |
| **Click an enemy / Enter** | Confirm an attack target |
| **Tab** | Switch eligible targets |
| **Esc** | Cancel target selection |

Hover over an ability to read its description. Use the **Sound On/Off** button to toggle audio.

## Build and Test

Open **File → Build Profiles**, select the target platform and include `Campaign_Demo` as the first scene. Save build output outside the repository.

Tests are available through **Window → General → Test Runner**:

- **Edit Mode:** combat rules, ability behavior, UI state and campaign configuration.
- **Play Mode:** scene integration, combat flow, targeting and encounter resets.

See [Build and Run Instructions](Docs/BuildLaunchInstructions.md) for details.

## Team

- Akshat Aggarwal 
- Anshul Reddy Goli 
- Balaji Radhakrishnan Padmanabhan 
- Charith Kutikuppala 
- Rahul Ganesh Marathervar 
- Shivani Kalpak Kulkarni 


## Asset Credits

See [Presentation Asset Credits](Docs/PresentationAssetCredits.md) for asset sources and licensing information.
