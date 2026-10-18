# Joysticks USAR — LOOPS game jam

Unity **6000.0.81f1**. A 3D tile-based robot programming game built around repeating command sequences.

## Open the project

1. Clone this repository, or download its ZIP and extract it.
2. Unity Hub → **Add project from disk** → select the repository root containing `Assets`, `Packages`, and `ProjectSettings`.
3. Use Unity **6000.0.81f1**. Allow packages and TextMesh Pro assets to import.
4. Open `Assets/GameJamLevel  1.unity` and press Play. (The filename has **two spaces before `1`**.) This is the single scene in Build Settings and includes AudioManager with all nine clips assigned: background music and eight sound effects.

The older `Assets/GameJamLevel .unity` is a legacy scene without AudioManager; use the scene above for playing and building the game.

`Assets/Loops/Scenes/LoopsDemo.unity` is the earlier standalone prototype. The current custom game uses the components in `Assets/script`.

## Current game controls

- Click the compact **level number/name beside CAM** to open the replay dropdown. Cleared levels are selectable for replay, and the highest unlocked unfinished level is available as **Continue** when returning from a replay. Future levels stay locked. Switching levels saves and restores each level’s circuit (blocks, positions, endpoints, wires, and palette stock) without changing solved flags or unlocks. Circuits persist between launches; undo history is retained separately per level during the current session. The robot starts the selected puzzle at Start. Click outside the list to close it.

- Click **Walk** or **Jump** to append it to **YOUR PROGRAM**. The list grows and scrolls automatically.
- **Start** and **Stop at goal** are fixed boundaries, outside the editable commands.
- Click a row's **×**, or right-click it, to remove that command.
- **Run** repeats the chosen commands until the robot reaches the End tile.
- A hazard death stops the program after the first failure. The robot respawns at Start while your circuit stays editable. A red command highlight, tile outline, and persistent message identify the pass, node, beat, location, and cause. Edit the circuit or press Run to retry.
- **Stop** cancels the current loop on the last completed tile and keeps your commands. Run starts the command list again from that tile.
- **Reset** stops execution, cancels pending respawns, clears the victory state, and returns the robot to Start while keeping the commands.

The Jump command retains its legacy internal name `Lava` to preserve existing scene and prefab references. Do not change serialized enum values or remove script `.meta` files.

## Saved progress

First-time players begin at Level 1. Winning saves that level's solved flag and unlocks the next level immediately using PlayerPrefs. Future launches resume the highest unlocked level; the scene's editor preview cannot choose the starting level. Replaying a level, resetting a puzzle, or choosing Play Again does not erase completed levels.

The highest unlocked level is stored as a one-based number under `Joysticks.Progress.HighestUnlockedLevel`. Solved flags use `Joysticks.Progress.SolvedLevel.1` through `.9`. Progress is stored locally for each platform/application profile.

## Work with a friend

Each person should clone their own local copy and use the same Unity version. Pull before working, create a feature branch, commit changes, then push and open a pull request. Avoid simultaneous edits to the same scene.

Commit **Assets and their `.meta` files**, **Packages**, and **ProjectSettings**. Unity regenerates `Library`, `Temp`, and `Logs`; these are intentionally excluded. Save scene changes in Unity before committing.

For direct pushes, the repository owner must add the friend under **Settings → Collaborators**. Otherwise, the friend can fork the repository and submit pull requests.

## Code and level setup

The custom game scripts are in `Assets/script`, with command prefabs in `Assets/Prefabs`.

A scene needs an active TileManager, a robot with PlayerController and PlayerRespawn, a LevelManager with its robot/UI references, a CodeExecutor, and a UIManager with its command prefab references. There must be exactly one active manager of each type. Assign unique tile grid coordinates and provide StartTile and EndTile components.

Use **Tools → Joysticks → Repair Command Panel** in Edit Mode if the original custom panel layout needs rebuilding, then save the scene. The growing program list and Reset button are built when entering Play Mode.

## Verification

Changed custom scripts were compiled against Unity 6000.0.81f1 assemblies. Simulated checks covered growing command lists, fixed endpoints, repeated execution, goal completion, first-death stops, reset, and Jump labels. Before the jam submission, verify the actual scene in Play Mode and make a player build for the target platform.
