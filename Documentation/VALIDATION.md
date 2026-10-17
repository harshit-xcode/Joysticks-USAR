# Validation record

Target: Unity 6000.0.81f1.

- All runtime and editor C# sources were compiled with Roslyn against the installed **6000.0.81f1 Unity assemblies**.
- **70 assertions passed.** Gameplay rules and program expansion were exercised outside the Unity editor using .NET and Unity's managed Vector2Int type. Checks cover repeat expansion, nested repeats, instruction and depth limits, circular programs, invalid jump/landing, lava, obstacles, off-grid movement, reset, stop, incomplete programs, and victory. Both a short route and a longer alternative through the demo's coordinate layout were checked.
- Asset/metadata references and whitespace were checked statically.

**Not yet verified:** Unity import, Play Mode, actual mouse drag/drop, visual layout, and a player build. The installed Unity editor exits before opening the project because it reports that Rosetta 2 is missing. A successful C# compile and isolated rules checks do not establish those runtime behaviors.

Before using this in the jam:

1. Open the project in Unity 6000.0.81f1 and check the Console for import/compile errors.
2. Open LoopsDemo and press Play; enter the README solution and verify the robot wins.
3. Try lava/off-grid failures, Stop during a jump, Reset after clearing a log, and Run twice.
4. Drag commands between the main list and nested Repeat blocks, reorder them, change counts, and remove a selected Repeat.
5. Use the Loops menu to create editable tile objects and run the rules smoke tests.
6. Make a test player build for the jam's target platform.

The portable checker is committed under `Tools/RulesCheck`; its README documents how to reproduce those checks.
