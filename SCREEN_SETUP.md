# Prison Escape screens

The UI is created automatically in Play mode by GameScreens: a dark prison-bar
background, amber accents, and keyboard/mouse buttons. There is no Canvas to wire.

- Launching the game shows Prison Escape, controls, the escape objective, and
  START GAME / QUIT GAME buttons. Gameplay is paused until START GAME is pressed.
  Start then shows a short preparation screen. In the Editor, Start uses the
  currently open level; a built game starts with the first Build Settings scene.
  Retries and level changes do not repeat the title screen.
- Captures one and two pause gameplay and show CAUGHT with remaining chances.
  RETURN TO CELL teleports the player and clears jump/platform movement.
- Capture three returns the player to the cell and shows LOCKDOWN with retry/quit.
- PlayerCapture.Die(reason) shows YOU DIED. The existing project has no health
  system; call this from future health logic or use PlayerDeathZone.
- LevelExit shows BLOCK CLEARED, then loads the next level asynchronously with
  a progress bar. The final level shows YOU ESCAPED instead.
- Retry reloads the level, resetting captures. Input, time scale, and cursor are
  restored after transitions. Captures reset on each newly loaded level.

## Unity setup

1. Wait for compilation. Confirm the player has PlayerCapture and a collider or
   CharacterController; guards need NPCCapturePlayer as before.
2. Build Settings now contains Scene 1 and Scene 2, in that order. Ensure Scene 2
   has the intended player, guard, spawn and baked NavMesh setup.
3. Choose Tools > Prison Escape > Create Level Exit. Move/size its Box Collider
   across the escape doorway in Scene 1. Set Next Scene Name to Scene 2, or leave
   it empty to use Build Settings order. Keep the trigger outside the cell.
4. Add another Level Exit to Scene 2. With no subsequent level, its empty scene
   field shows the escape-complete screen.
5. Choose Tools > Prison Escape > Create Death Zone. Position it below the map
   or around a lethal hazard. Adjust its Box Collider and Reason text.
6. Save each scene. Do not manually add a second GameScreens/EventSystem canvas.

The create-zone menus place zones at the selected object's position, or at the
origin if nothing is selected. They do not guess which door/hazard is intended.

## Verification in Unity

- Confirm Play mode opens the title screen and guards/player stay still until
  START GAME. Start should dismiss the preparation screen and restore gameplay.
- Preview the title and modal designs in Play mode using Tools > Prison Escape >
  Preview Screens. These are previews; game-over/death preview retry reloads.
- Capture twice: each return should preserve capture count and clear momentum.
- Capture a third time: retry must restart with no captures and resume movement.
- Enter a death zone: movement stops; retry restores normal time and input.
- Enter Scene 1's exit: continue shows progress and loads Scene 2, once only.
- Enter Scene 2's final exit: escape-complete screen offers a fresh playthrough.
- Check UI sizing at 1280x720 and 1920x1080; keyboard Tab/Enter and mouse should
  operate buttons. Check there is one active EventSystem and no Console errors.

Compilation can be checked independently, but these gameplay/visual checks
require running the project in Unity.
