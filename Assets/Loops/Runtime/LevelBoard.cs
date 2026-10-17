using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loops
{
    [DisallowMultipleComponent]
    public sealed class LevelBoard : MonoBehaviour
    {
        [Min(.1f)] public float cellSize = 1.4f;
        public Vector2Int spawn;
        [Range(0, 3), Tooltip("0 = +Z, 1 = +X, 2 = -Z, 3 = -X (board local axes)")]
        public int spawnFacing;
        public float robotHeight = .55f;

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return transform.TransformPoint(new Vector3(cell.x * cellSize, 0, cell.y * cellSize));
        }

        public Vector3 RobotPosition(Vector2Int cell)
        {
            return CellToWorld(cell) + transform.up * robotHeight;
        }

        public Dictionary<Vector2Int, LevelTile> ReadTiles()
        {
            if (cellSize <= 0) throw new ArgumentException("Board cell size must be positive.");
            var tiles = new Dictionary<Vector2Int, LevelTile>();
            int goals = 0;
            foreach (var tile in GetComponentsInChildren<LevelTile>(false))
            {
                if (!tiles.TryAdd(tile.coordinate, tile))
                    throw new ArgumentException($"Duplicate tile coordinate {tile.coordinate}: {tile.name}.");
                if (!Enum.IsDefined(typeof(TileKind), tile.kind))
                    throw new ArgumentException($"Unknown tile type at {tile.coordinate}.");
                if (Vector3.Distance(tile.transform.position, CellToWorld(tile.coordinate)) > .05f)
                    throw new ArgumentException($"Tile {tile.name} is off-grid. Use Snap Tile To Board Grid.");
                if (tile.kind == TileKind.Goal) goals++;
                if (tile.kind != TileKind.Safe && tile.startsBlocked)
                    throw new ArgumentException("Place obstacles only on safe tiles.");
            }
            if (!tiles.TryGetValue(spawn, out var start) || start.kind != TileKind.Safe || start.startsBlocked)
                throw new ArgumentException("Spawn must be an unblocked safe tile.");
            if (goals != 1) throw new ArgumentException("The level needs exactly one goal tile.");
            return tiles;
        }
    }
}
