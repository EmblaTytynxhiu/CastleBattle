# CASTLE BATTLE – complete Unity project pack

Everything for a playable game: art, menus, animated Kings, sounds, music and all the gameplay code.

```
CastleBattle/
  Art/        castle pieces, cannons, ammo, defenses, effects, grids (from the asset pack)
  UI/         main menu, build screen, pause menu, victory/defeat, animated King frames, fonts, menu scripts
  Game/       gameplay scripts, editor tools, sounds + music
  Previews~/  preview pictures (Unity ignores this folder)
```

## Setup (about 5 minutes)
Use **Unity 2021.3 or newer** (Unity 6 works too). Make a **2D** project (any "2D" template).

1. **Import:** drag the whole `CastleBattle` folder into your project's `Assets` folder and wait for it to finish.
2. **Create the game pieces:** in the top menu click **Tools > Castle Battle > 7. Create Game Prefabs + Assets**.
   This sets up every picture, then creates the physics prefabs for each piece (red + blue), the Kings and `Game/Generated/GameAssets.asset`.
   The first run can take a few minutes while Unity re-imports the pictures.
3. **The game scene:**
   - File > New Scene (Basic 2D or Empty).
   - Click **Tools > Castle Battle > 8. Build Game Scene > 12 x 10 (recommended)**. You can also pick 8 x 8, 12 x 12 or 16 x 16.
   - Save the scene as **`Game`**.
4. **The main menu scene:**
   - File > New Scene.
   - Click **Tools > Castle Battle > 2. Build Main Menu In Open Scene**.
   - Save the scene as **`MainMenu`**.
5. **File > Build Settings** (Build Profiles in Unity 6): add **MainMenu** first, then **Game**.
6. Open **MainMenu**, press **Play**, then click **START**.

## How to play
**1. Build** (the Red King builds first)
- Pick a piece in the RESOURCES panel (WOOD / STONE / METAL / SPECIAL tabs).
- **Left-click** on the grid to place it. It snaps to the squares.
- The preview turns **green** where you're allowed to place and **red** where you're not (outside the grid, overlapping another piece, or floating in the air).
- **Right-click** a piece to take it back. This only works if nothing is resting on top of it.
- Your **King** is placed for you in the middle of the bottom row. Build around him to protect him.
- The big round badge shows how many pieces you may still place. Press **READY!** when you're done.

**2. Battle**
- Physics switches on and your castle has to stand on its own.
- Each cannon stands on the ground just in front of its castle. A dotted arc always shows where your shot will land.
- On your turn, aim in whichever way you like:
  - **Aim panel** (under your castle): hold **–** / **+** to change the **ANGLE** (0–80°) and **POWER** (0–100%), or drag the sliders. Then press **FIRE!**
  - **Mouse:** press anywhere on the battlefield, **drag BACK** away from the enemy like a slingshot, and **let go** to fire.
  - **Keyboard:** **W/S** or **Up/Down** = angle, **A/D** or **Left/Right** = power, **Space** = fire.
- Ammo: click the buttons at the bottom or press **1–6**.

| Key | Ammo | What it does |
|---|---|---|
| 1 | Cannonball | Unlimited, normal damage |
| 2 | Stone ball | Heavy, smashes wood |
| 3 | Fire shot | Sets wood on fire, and fire spreads. Water barrels put it out |
| 4 | Explosive barrel | Blows up: area damage and throws pieces around |
| 5 | Chain shot | Spins and shreds thin (1-wide) pieces |
| 6 | Split shot | Click while it flies to split it into 3 |

**3. Winning:** a King loses if he's knocked over, runs out of health or falls off the map. The winner cheers, the loser falls over, and the Victory or Defeat screen appears.

- **Esc** or the round **II** button pauses the game.

## Settings to try (on the `GameManager` object in the Game scene)
- **Mode:** `VsComputer` (you are Red) or `TwoPlayers`. Two players share one computer: both build, then take turns.
- **Grid Size:** 8x8, 12x12, 12x10 or 16x16.
- **Castle Gap:** the distance between the castles.
- **Cannon Distance:** how far in front of its castle each cannon stands.
- **Aiming:** on each `Cannon_Red` / `Cannon_Blue`, set `Min/Max Angle`, `Min/Max Power` and how fast the buttons turn (`Angle Speed`, `Power Speed`).
- **AIPlayer:** `Start Error` and `Improve Per Turn` control how accurate the computer is.
- **Cannons:** `Ammo` counts and the power range.
- **Pieces:** weight and health per material are in `Game/Scripts/Block.cs` (MaterialStats). The block prefabs are in `Game/Generated/Prefabs`.
- **Volume:** music and sound effects are in Options, in the main menu and the pause menu.

## If something goes wrong
- **"Scene 'Game' is not in Build Settings":** see step 5.
- **Pieces look huge or tiny:** run step 7 again. It fixes the import settings.
- **Nothing happens when you click:** make sure the scene has an EventSystem. The builder adds one, so don't delete it.
- **Red errors in the Console:** these scripts were written and compile-checked outside of Unity, but they have **not been run inside Unity yet**, so small fixes may be needed. Copy the first red error and send it to me.

## Licences
- Fonts: Cardo and Crimson Text, both SIL Open Font License (see `UI/Fonts`).
- Sounds and music were generated for this project and are free to use.
- `UI/Sprites/Background/BG_MainMenu.png` is the castle painting you supplied, which is someone else's artwork. Swap it before publishing the game.
