using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loops
{
    // Shared by the runtime preview and the editor command that creates editable tile objects.
    public static class DemoLevelFactory
    {
        public static void Populate(PanelSettings panel, Func<string, Color, bool, Material> material)
        {
            var stone = material("Stone", new Color(.5f, .62f, .65f), false);
            var lava = material("Lava", new Color(1f, .24f, .025f), true);
            var goal = material("Goal", new Color(.08f, .9f, .9f), true);
            var wood = material("Wood", new Color(.4f, .22f, .09f), false);
            var white = material("Robot", new Color(.88f, .96f, 1), false);
            var dark = material("Face", new Color(.015f, .055f, .08f), false);
            var grass = material("Grass", new Color(.14f, .31f, .22f), false);

            var board = new GameObject("Level Board - edit tiles under this object").AddComponent<LevelBoard>();
            board.cellSize = 1.4f;
            board.spawn = Vector2Int.zero;
            board.spawnFacing = 0;
            // Main route: repeated move / jump / clear / move pattern, three times.
            for (int z = 0; z <= 12; z++) AddTile(board, 0, z, z == 12 ? TileKind.Goal :
                z % 4 == 2 ? TileKind.Lava : TileKind.Safe, z > 0 && z < 12 && z % 4 == 0, stone, lava, goal, wood);
            // Longer alternative route with two obstacle stops and four jumps.
            for (int x = 1; x <= 5; x++) AddTile(board, x, 0, TileKind.Safe, x == 3, stone, lava, goal, wood);
            for (int z = 1; z <= 12; z++) AddTile(board, 5, z, z == 3 || z == 6 || z == 9 ? TileKind.Lava : TileKind.Safe,
                z == 11, stone, lava, goal, wood);
            for (int x = 1; x < 5; x++) AddTile(board, x, 12, x == 2 ? TileKind.Lava : TileKind.Safe, false, stone, lava, goal, wood);
            // Cross-route and one visible dead end for path planning.
            for (int x = 1; x < 5; x++) AddTile(board, x, 5, TileKind.Safe, x == 2, stone, lava, goal, wood);
            for (int x = -1; x >= -2; x--) AddTile(board, x, 7, TileKind.Safe, false, stone, lava, goal, wood);

            var island = Primitive("Island placeholder - replace with your art", PrimitiveType.Cube, grass,
                new Vector3(2.1f, -1.2f, 8.4f), new Vector3(13.2f, 1.6f, 20.5f));
            island.transform.SetParent(board.transform, true);
            var robot = new GameObject("Robot - replace child visuals, keep this root").transform;
            robot.position = board.RobotPosition(board.spawn);
            var body = Primitive("Body", PrimitiveType.Capsule, white, robot.position, new Vector3(.55f, .42f, .55f));
            body.transform.SetParent(robot, true);
            var face = Primitive("Forward face (+Z)", PrimitiveType.Cube, dark,
                robot.position + new Vector3(0, .15f, .27f), new Vector3(.4f, .2f, .08f));
            face.transform.SetParent(robot, true);
            var eye = Primitive("Eyes", PrimitiveType.Cube, goal,
                robot.position + new Vector3(0, .16f, .32f), new Vector3(.26f, .055f, .04f));
            eye.transform.SetParent(robot, true);
            var runner = new GameObject("Robot Program Runner").AddComponent<RobotProgramRunner>();
            runner.board = board;
            runner.robot = robot;

            var ui = new GameObject("Scratch-style Block Editor");
            ui.SetActive(false);
            ui.AddComponent<UIDocument>().panelSettings = panel;
            ui.AddComponent<BlockProgramUI>().runner = runner;
            ui.SetActive(true);

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 12.5f;
            camera.rect = new Rect(.28f, 0, .72f, 1);
            camera.transform.position = new Vector3(18, 24, -12);
            camera.transform.LookAt(new Vector3(2, 0, 8));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.07f, .12f, .19f);
            camera.gameObject.AddComponent<AudioListener>();
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(.5f, .58f, .66f);
        }

        static void AddTile(LevelBoard board, int x, int z, TileKind kind, bool blocked,
            UnityEngine.Material stone, UnityEngine.Material lava, UnityEngine.Material goal, UnityEngine.Material wood)
        {
            var cell = new Vector2Int(x, z);
            var obj = new GameObject($"Tile [{x},{z}] {kind}" + (blocked ? " + obstacle" : ""));
            obj.transform.SetParent(board.transform, false);
            obj.transform.position = board.CellToWorld(cell);
            var tile = obj.AddComponent<LevelTile>();
            tile.coordinate = cell;
            tile.kind = kind;
            tile.startsBlocked = blocked;
            var surface = Primitive("Surface", PrimitiveType.Cube, kind == TileKind.Lava ? lava : kind == TileKind.Goal ? goal : stone,
                obj.transform.position - Vector3.up * .15f, new Vector3(1.3f, .3f, 1.3f));
            surface.transform.SetParent(obj.transform, true);
            if (blocked)
            {
                var log = Primitive("Obstacle - removable log", PrimitiveType.Cylinder, wood,
                    obj.transform.position + Vector3.up * .32f, new Vector3(.48f, .53f, .48f));
                log.transform.rotation = Quaternion.Euler(0, 0, 90);
                log.transform.SetParent(obj.transform, true);
                tile.obstacleVisual = log;
            }
        }

        static GameObject Primitive(string name, PrimitiveType type, UnityEngine.Material material, Vector3 position, Vector3 scale)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

    }
}
