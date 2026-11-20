# Presentation asset inventory and credits

This inventory covers the presentation files currently in the Unity project. The
repository history and Unity import metadata identify where the files are stored,
but do not record a creator, upstream source, or license for the project art and
sound files. Their provenance and redistribution terms therefore remain
unverified; this document does not claim that they are original or attribution-free.
Confirm those details with the contributors before making a distributable release.

| Asset group | Project files | Creator/source and license status |
| --- | --- | --- |
| Character art | `Assets/Art/Characters/` (Vishakanya, Temple Guardian, Guardian Warden, Vault Sentinel, Asura, and the Temple/Courtyard atlases) | Not recorded in the repository; confirm with the contributor. |
| Environment art | `Assets/Art/Environment/` (Temple Courtyard, Courtyard Dusk, Temple Vault, Inner Vault, Temple Sanctum) | Not recorded in the repository; confirm with the contributor. |
| Combat effects | `Assets/Art/Effects/` (Poison Mark, Fade Aura, Bite Flash) | Not recorded in the repository; confirm with the contributor. |
| UI art | `Assets/Art/UI/` (Diamond, Guardian Helm) | Not recorded in the repository; confirm with the contributor. |
| Sound effects | `Assets/Audio/` (Hit, Venom Strike, Serpent's Bite, Fade, Victory, Defeat, Menu Confirm) | Not recorded in the repository; confirm with the contributor. |
| Fonts | `Assets/Fonts/Barlow-Regular.ttf`, `Assets/Fonts/Barlow-SemiBold.ttf` | Barlow Project Authors; upstream source is recorded as `https://github.com/jpt/barlow`. The SIL Open Font License 1.1 notice is retained in `Assets/Fonts/Barlow-License.txt`; keep it with redistributed font files. |

The temple drone is synthesized at runtime by `DemoSoundControl`; dust and ground
shadows are drawn at runtime by the project scripts. They do not use imported
image or audio files.

The imported art and audio files remain in the project. No external creator,
source, or license terms have been added where the repository does not provide
them.
