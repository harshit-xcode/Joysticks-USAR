# Level designer guide

## Make an editable scene

Open the demo, leave Play Mode, then choose **Loops → Create or Rebuild Demo Level**. This creates actual scene objects with all references connected. Save a copy with **File → Save As** before making a new level. Add the new scene in Build Profiles if you want it in a player build.

The committed preview scene uses `DemoBootstrap` to spawn the demo only when you press Play. Do not add your custom scene objects beside that bootstrap: generate an editable scene first, or remove the bootstrap and assemble the components below yourself.

## Board and coordinates

The `Level Board` object has `LevelBoard`. All playable `LevelTile` objects must be active children of this board.

- `cellSize`: distance between neighboring tile centers. Default 1.4 Unity units.
- `spawn`: starting tile coordinate `(x, z)` represented by a Vector2Int. Its Inspector Y field means grid Z.
- `spawnFacing`: 0 = +Z, 1 = +X, 2 = −Z, 3 = −X, relative to the board.
- `robotHeight`: height of the robot root above the tile's walking surface.

Use a board scale of `(1,1,1)`. Tile roots sit at walking-surface height. Their child meshes may extend below that root. Coordinates, not colliders or mesh positions, define game rules.

For each tile:

1. Duplicate an existing tile under the board, or make an empty child and add `LevelTile`.
2. Give it a unique `coordinate`.
3. Open the component's context menu → **Snap Tile To Board Grid**.
4. Set its type and obstacle fields.
5. Replace child meshes with your art. Keep the root and the component.

The runner checks for duplicate coordinates, tiles away from their assigned positions, an invalid spawn, and missing/extra goals. It reports setup errors in the coding panel instead of starting a broken level.

## Tile types and obstacle rules

| Setting | Behavior |
| --- | --- |
| Safe | Robot may move onto it if unblocked. |
| Lava | Walking onto it fails. Jump from the tile before it, landing two cells ahead. |
| Goal | First arrival wins immediately. Exactly one per board. |
| Starts Blocked | A removable tree/log on a **safe** tile. Clear from the adjacent tile while facing it. |
| Obstacle Visual | Assign the removable child model so clearing hides it; reset shows it again. |

`Clear obstacle` removes only the obstacle directly ahead; it does not move the robot. A missing obstacle is a failed command. Jump requires one lava tile ahead and a safe or goal landing beyond it. It cannot jump a tree, missing tile, or two consecutive lava tiles.

Keep obstacles on safe tiles. Do not assign the tile root as its obstacle visual. Obstacles on goal/lava tiles are rejected. Avoid multiple grid layers at the same coordinate: bridges and stacked routes need an expanded coordinate system and are outside this prototype.

## Robot, runner, and visual coding panel

`RobotProgramRunner` needs references to the board and the robot root Transform. Do not add a dynamic Rigidbody or another movement script to that root. The runner animates movement directly. Keep the model facing local +Z. Adjust `robotHeight`, `commandDuration`, and `jumpHeight` for your art.

The coding panel object needs:

- `UIDocument` with the `LoopsPanel` PanelSettings asset created by the editor demo menu.
- `BlockProgramUI` with its `runner` field assigned.

The UI is built in C# and uses the built-in UI Toolkit event system; an EventSystem object is not required for this starter. The theme is `Assets/Loops/Resources/LoopsTheme.tss`. The demo camera leaves space on the left for the block editor.

## Make harder, fairer puzzles

Repeat an obstacle sequence several times to make loops useful. Add alternate routes that meet at the same goal, then compare their command counts. Put safe turning tiles before intersections. Add dead ends where failure teaches a readable rule.

Test every intended route manually. The grid checks validate the configuration but do not automatically prove that an arbitrary custom level is solvable.

## Sharing

Save the scene before committing. Include new assets and their `.meta` files in the same commit. Pull your teammates' changes before editing; push yours when finished. Scene merges can be difficult, so assign different scene files to different designers.
