# Castle Battle – Main Menu UI for Unity

Every piece is a separate PNG, so nothing is glued together. You get each button in 4 states, the panels, the ribbons and the title, a painted castle background in parallax layers, fonts, and working scripts.

## Quick setup (about 1 minute)
1. Drag the whole **CastleBattleUI** folder into your Unity project's `Assets` folder.
2. Open (or create) your menu scene.
3. In the top menu bar, click **Tools > Castle Battle > 2. Build Main Menu In Open Scene**.
   - This fixes all sprite import settings automatically. It then builds the full menu: background, title, panel, the 4 buttons, and the How To Play and Settings screens, with every button already wired up.
4. Select **CastleBattle_MainMenu** in the Hierarchy. On **MainMenuController**, set **Game Scene Name** to your game scene's name (default `Game`).
5. Make sure both scenes are listed in **File > Build Settings** (Build Profiles in Unity 6).
6. Press **Play**.

| Button | What it does |
|---|---|
| START | Loads your game scene |
| HOW TO PLAY | Opens the How To Play panel (Back / X returns) |
| SETTINGS | Music volume, sound-effects volume and fullscreen, saved automatically |
| EXIT | Quits the game (stops Play Mode in the editor) |

Escape also goes back to the main menu (with the old Input Manager).

## What's in the folder
```
Sprites/Buttons/     Button_Start / HowToPlay / Settings / Exit / Back / Close
                     each _Normal, _Highlighted, _Pressed, _Disabled
                     + Button_Blank_Blue / Emerald / Crimson (no text, for your own labels)
Sprites/Panels/      Panel_Main, Panel_Wide, Ribbon_MainMenu / HowToPlay / Settings / Blank,
                     Title_CastleBattle (logo), Divider
Sprites/Widgets/     Slider_Track, Slider_Fill, Slider_Handle, Toggle_Box, Toggle_Check
Sprites/Background/  BG_MainMenu (1920 x 1080 castle background)
Fonts/               Cardo-Bold (titles), CrimsonText-SemiBold (body text) – free OFL fonts
Scripts/             MainMenuController.cs, MenuButtonFX.cs (hover grow / press squash + optional sounds)
Scripts/Editor/      CastleBattleMenuBuilder.cs (the Tools menu commands)
```

## Doing it by hand instead
- Import settings: Texture Type **Sprite (2D and UI)**, Mesh Type Full, Pixels Per Unit 100. Set `Slider_Track` and `Slider_Fill` borders to L/R 22 in the Sprite Editor.
- Canvas Scaler: **Scale With Screen Size**, 1920 x 1080, Match 0.5.
- Button: Transition **Sprite Swap**. Image = `_Normal`, Highlighted = `_Highlighted`, Pressed = `_Pressed`, Selected = `_Normal`, Disabled = `_Disabled`.
- OnClick: drag the object with MainMenuController in and pick `StartGame`, `OpenHowToPlay`, `OpenSettings` or `ExitGame`.

## Layout (1920 x 1080, from the centre)
Title (0, 380) · Ribbon (0, 168) · START (0, 35) · HOW TO PLAY (0, -85) · SETTINGS (0, -195) · EXIT (0, -305) · Divider (0, -405)

## Victory / Defeat screen
1. Open your **game** scene and click **Tools > Castle Battle > 3. Build Result Screen (Victory - Defeat) In Open Scene**.
2. It hides itself when the game starts. When a King falls, call it from your game code:
```csharp
FindObjectOfType<ResultScreen>().ShowVictory(ResultScreen.Team.Red);  // you won, Red King is the winner
FindObjectOfType<ResultScreen>().ShowDefeat(ResultScreen.Team.Blue);  // you lost, Blue King is the winner
```
3. To try it without writing code: press Play, right-click the **ResultScreen** component header, and pick a **Test** option.
4. Set **Main Menu Scene Name** to your menu scene. PLAY AGAIN reloads the current scene unless you set **Play Again Scene Name**.

Effects: the sun rays spin, the title pops in and pulses on Victory, and the confetti falls on Victory only.

Sprites are in `Sprites/Results/`: BG_Battlefield, Title_Victory, Title_Defeat, Rays_Victory, Rays_Defeat,
Ribbon_Winner_Red, Ribbon_Winner_Blue, King_Red_Winner, King_Blue_Winner, Pedestal, Panel_Buttons, Confetti_Overlay.
Buttons are in `Sprites/Buttons/`: Button_PlayAgain / Button_MainMenu / Button_ExitGame, each in 4 states.

## Animated Kings
Frames are in `Sprites/King/<Red|Blue>/<Idle|Walk|Cheer|Fall>/`, one PNG per frame, all facing right.
1. Click **Tools > Castle Battle > 4. Add Animated Kings To Scene**. This creates `King_Red` (facing right) and `King_Blue` (facing left), already animating.
   - Import is automatic: Pixels Per Unit 320 (about 2 grid blocks tall), with the pivot at his feet.
2. From your code:
```csharp
var king = GetComponent<KingAnimator>();
king.Play(KingAnimator.Anim.Walk);   // Idle, Walk, Cheer, Fall
king.PlayFall();                      // plays once and stays on the ground
king.faceLeft = true;                 // turn around
```
The Victory / Defeat screen now shows the winning King playing his Cheer animation.

## Castle building screen (build grid + resources)
Click **Tools > Castle Battle > 5. Build Castle Building Screen** and pick **8 x 8**, **12 x 12**, **12 x 10** or **16 x 16**.
- **Left:** the see-through build grid inside a gold frame, with a "BUILD AREA" ribbon. The `BuildGrid` object receives clicks, for your building code.
- **Right:** the RESOURCES panel:
  - WOOD / STONE / METAL / SPECIAL tabs
  - 12 piece slots, each with a red count badge
  - the big round badge showing how many pieces are left (starts at 11, set `Pieces Left`)
  - the piece name and description
  - a **READY!** button

`ResourcePanel` events for your game code:
```csharp
panel.onPieceSelected.AddListener((category, index) => { /* show that piece on the grid */ });
panel.UseSelected();               // the player placed it: counts go down
panel.ReturnPiece(category, index); // the player removed it: counts go up
panel.onReady.AddListener(StartBattle);
```
Piece counts and names are editable on the ResourcePanel component (Categories).
Sprites are in `Sprites/Build/`. Slot icons are in `Sprites/Build/Icons/<Wood|Stone|Metal|Special>/`.

## Pause menu
Open your game scene and click **Tools > Castle Battle > 6. Add Pause Menu To Scene**.
- During play, a round **II** button sits in the top-right corner. **Esc** also opens the menu.
- The menu shows **PAUSED**, then RESUME, RESTART, OPTIONS, MAIN MENU and EXIT GAME, and "PRESS [ESC] TO RESUME" at the bottom.
- The game freezes while paused (`Time.timeScale = 0`). OPTIONS has music and sound volume plus fullscreen, and Esc goes back.
- Set **Main Menu Scene Name** on the PauseMenu component. Set `canPause = false` while the Victory/Defeat screen is up.
- The background is the battlefield picture, dimmed. To show your frozen game behind it instead, delete `PauseScreen/Background`.
