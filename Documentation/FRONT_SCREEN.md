# Factory front screen

The main campaign scenes now open on a purple factory title screen. It is created automatically by `FrontScreenController` when a scene contains `LevelManager`; no scene editing is required.

- **Play** starts Level 1 for a first-time player, or the saved highest unlocked level on a later launch. Winning saves solved flags and unlocks the next level automatically.
- **Sound** opens separate Music and Sound Effects sliders. Each slider has + / − buttons for five-percent steps and its own percentage display. Changes apply immediately and save automatically. Mute All sets both sliders to zero. Done returns to the title menu.
- **Exit** quits a desktop/Android player. In Unity it exits Play Mode. iOS leaves quitting to the system.
- **Menu** at the upper left of gameplay pauses the game and returns to the title. Play becomes Resume. Escape opens the menu, closes Sound, or resumes gameplay.
- Up/Down select menu buttons, Enter activates the selected button, and Left/Right adjust a selected slider. Touch controls use the same Unity UI buttons and slider.

Artwork: `Assets/Resources/FrontScreen/FactoryTitle.png`. Generated with the built-in image generation tool using the user's factory-screen reference. Controls, icons and the volume panel are actual Unity UI, separate from the artwork. The menu uses a 16:9 composition fitted inside the device safe area.

Music and sound-effects gains are stored separately under `Joysticks.MusicVolume` and `Joysticks.SfxVolume` in PlayerPrefs. Existing `Joysticks.MasterVolume` values migrate to both settings the first time; each can then be adjusted independently. These gains multiply the AudioManager’s authored music/SFX balance. Sound controls are available in the front screen only. During gameplay, choose Menu, then Sound to adjust volume.

`GameCameraController` ignores camera shortcuts while the front screen is open. Returning to the menu preserves the current board and program.
