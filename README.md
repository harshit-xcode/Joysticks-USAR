# Joysticks USAR — LOOPS game jam

Unity **6000.0.81f1**. A 3D tile-based robot programming game built around repeating command sequences.

## Open the project

1. Clone this repository, or download its ZIP and extract it.
2. Unity Hub → **Add project from disk** → select the repository root containing `Assets`, `Packages`, and `ProjectSettings`.
3. Use Unity **6000.0.81f1**. Allow packages and TextMesh Pro assets to import.
4. Open `Assets/GameJamLevel .unity` and press Play. (The scene filename currently includes a space before `.unity`.)

`Assets/Loops/Scenes/LoopsDemo.unity` is the earlier standalone prototype. The current custom game uses the components in `Assets/script`.

## Current game controls

- Click **Walk** or **Jump** to append it to **YOUR PROGRAM**. The list grows and scrolls automatically.
- **Start** and **Stop at goal** are fixed boundaries, outside the editable commands.
- Click a row's **×**, or right-click it, to remove that command.
- **Run** repeats the chosen commands until the robot reaches the End tile.
- A lava mistake respawns the robot and automatically retries the program.
- **Stop** cancels the current loop on the last completed tile and keeps your commands. Run starts the command list again from that tile.
- **Reset** stops execution, cancels pending respawns, clears the victory state, and returns the robot to Start while keeping the commands.

The Jump command retains its legacy internal name `Lava` to preserve existing scene and prefab references. Do not change serialized enum values or remove script `.meta` files.

## Work with a friend

Each person should clone their own local copy and use the same Unity version. Pull before working, create a feature branch, commit changes, then push and open a pull request. Avoid simultaneous edits to the same scene.

Commit **Assets and their `.meta` files**, **Packages**, and **ProjectSettings**. Unity regenerates `Library`, `Temp`, and `Logs`; these are intentionally excluded. Save scene changes in Unity before committing.

For direct pushes, the repository owner must add the friend under **Settings → Collaborators**. Otherwise, the friend can fork the repository and submit pull requests.

## Code and level setup

The custom game scripts are in `Assets/script`, with command prefabs in `Assets/Prefabs`.

A scene needs an active TileManager, a robot with PlayerController and PlayerRespawn, a LevelManager with its robot/UI references, a CodeExecutor, and a UIManager with its command prefab references. There must be exactly one active manager of each type. Assign unique tile grid coordinates and provide StartTile and EndTile components.

Use **Tools → Joysticks → Repair Command Panel** in Edit Mode if the original custom panel layout needs rebuilding, then save the scene. The growing program list and Reset button are built when entering Play Mode.

## Verification

Changed custom scripts were compiled against Unity 6000.0.81f1 assemblies. Simulated checks covered growing command lists, fixed endpoints, repeated execution, goal completion, respawn retries, reset, and Jump labels. Before the jam submission, verify the actual scene in Play Mode and make a player build for the target platform.
