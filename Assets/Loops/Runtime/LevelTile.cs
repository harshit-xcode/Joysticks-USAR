using UnityEngine;

namespace Loops
{
    [DisallowMultipleComponent]
    public sealed class LevelTile : MonoBehaviour
    {
        public Vector2Int coordinate;
        public TileKind kind;
        [Tooltip("A tree/log on this tile. Clear it from the adjacent tile before moving here.")]
        public bool startsBlocked;
        [Tooltip("Optional child model to hide when the obstacle is cleared. Never assign the tile itself.")]
        public GameObject obstacleVisual;

        public void ShowObstacle(bool blocked)
        {
            if (obstacleVisual != null && obstacleVisual != gameObject &&
                obstacleVisual.transform.IsChildOf(transform))
                obstacleVisual.SetActive(blocked);
        }

        [ContextMenu("Snap Tile To Board Grid")]
        public void SnapToGrid()
        {
            var board = GetComponentInParent<LevelBoard>();
            if (board != null) transform.position = board.CellToWorld(coordinate);
        }

        void OnDrawGizmos()
        {
            var board = GetComponentInParent<LevelBoard>();
            float size = board != null ? board.cellSize : 1f;
            Gizmos.color = kind == TileKind.Lava ? new Color(1, .25f, .05f) :
                kind == TileKind.Goal ? Color.cyan : startsBlocked ? Color.yellow : new Color(.2f, 1, .5f, .5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size * .9f, .15f, size * .9f));
        }
    }
}
