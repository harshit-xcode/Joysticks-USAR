# Rules checker without opening Unity

Optional: requires .NET SDK 10 and an installed Unity 6000.0.81f1. This tool compiles the actual `ProgramBlock`, `RobotSimulation`, and `TileKind` source files, then runs 70 assertions covering their behavior and two routes through a fixture matching the demo's layout. It does not validate Unity's native runtime, the scene importer, or mouse input.

From the repository root on macOS with Unity in the default location:

```sh
dotnet run --project Tools/RulesCheck/RulesCheck.csproj
```

For another location, pass the directory containing `UnityEngine.CoreModule.dll`:

```sh
dotnet run --project Tools/RulesCheck/RulesCheck.csproj -p:UnityManagedPath="/path/to/Unity/Managed/UnityEngine"
```

Expected final output: `LOOPS_ALL_TESTS_PASSED: 70 checks, including both demo routes.`

The board fixture here is deliberately independent from the runtime factory. When changing the demo layout, update its intended routes here too, then run the Unity `BuildAndTest` entry point to test the actual generated board.
