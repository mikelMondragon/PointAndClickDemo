using System.Collections.Generic;
using UnityEngine;

namespace PointAndClickDemo.Pathfinding
{
    /// <summary>
    /// A* over a grid rasterised from the walkable area.
    /// The grid is eroded by the agent radius so the character does not
    /// walk flush against the walls.
    /// </summary>
    [ExecuteAlways]
    public class GridPathfinder : MonoBehaviour, IPathProvider
    {
        private const float DiagonalCost = 1.414f;

        private static readonly Vector2Int[] Neighbours =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        [Header("Walkable area")]
        [SerializeField] private PolygonCollider2D walkableArea;

        [Header("Grid")]
        [SerializeField]
        [Tooltip("Cell size in world units. At 100 PPU, 0.25 means 25px cells")]
        private float cellSize = 0.25f;

        [SerializeField]
        [Tooltip("Computed from the collider via the context menu, but can be tweaked by hand")]
        private Bounds gridBounds;

        [SerializeField]
        [Tooltip("Extra margin around the collider when auto-computing")]
        private float boundsPadding = 0.5f;

        [Header("Character")]
        [SerializeField]
        [Tooltip("Character radius: inflates obstacles so it does not hug walls and corners")]
        private float agentRadius = 0.2f;

        /// <summary>Straight from the collider, not eroded.</summary>
        private bool[,] rawWalkable;

        /// <summary>Eroded by the agent radius: this is what A* uses.</summary>
        private bool[,] walkable;

        private int columns;
        private int rows;
        private Vector2 origin;

        public bool TryGetPath(Vector2 from, Vector2 to, List<Vector2> result)
        {
            result.Clear();
            if (walkable == null) return false;

            Vector2Int start = WorldToCell(from);
            if (!walkable[start.x, start.y] && !FindNearestWalkable(start, out start)) return false;

            // When the click lands inside the agent's safety margin, snap to the nearest
            // eroded cell instead of discarding the destination.
            Vector2Int goal = WorldToCell(to);
            if (!walkable[goal.x, goal.y] && !FindNearestWalkable(goal, out goal)) return false;

            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            Dictionary<Vector2Int, float> costSoFar = new() { [start] = 0f };
            List<Vector2Int> open = new() { start };
            HashSet<Vector2Int> closed = new();

            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    float score = costSoFar[open[i]] + Heuristic(open[i], goal);
                    if (score >= bestScore) continue;

                    bestScore = score;
                    bestIndex = i;
                }

                Vector2Int current = open[bestIndex];
                if (current == goal)
                {
                    Reconstruct(cameFrom, current, result);
                    Simplify(result);
                    return true;
                }

                open.RemoveAt(bestIndex);
                closed.Add(current);

                foreach (Vector2Int direction in Neighbours)
                {
                    Vector2Int neighbour = current + direction;
                    if (!IsInsideGrid(neighbour)) continue;
                    if (!walkable[neighbour.x, neighbour.y] || closed.Contains(neighbour)) continue;

                    bool isDiagonal = direction.x != 0 && direction.y != 0;

                    // No corner cutting: on a diagonal both orthogonal cells must be free.
                    if (isDiagonal &&
                        (!walkable[current.x + direction.x, current.y] ||
                         !walkable[current.x, current.y + direction.y]))
                        continue;

                    float tentativeCost = costSoFar[current] + (isDiagonal ? DiagonalCost : 1f);
                    if (costSoFar.TryGetValue(neighbour, out float knownCost) && tentativeCost >= knownCost) continue;

                    cameFrom[neighbour] = current;
                    costSoFar[neighbour] = tentativeCost;
                    if (!open.Contains(neighbour)) open.Add(neighbour);
                }
            }

            return false;
        }

        private void Awake() => Bake();

        private void OnValidate() => Bake();

        [ContextMenu("Auto-compute Bounds from Collider")]
        private void ComputeBoundsFromCollider()
        {
            if (walkableArea == null)
            {
                Debug.LogWarning("Assign Walkable Area before auto-computing.", this);
                return;
            }

            Bounds bounds = walkableArea.bounds;
            bounds.Expand(boundsPadding * 2f);
            gridBounds = bounds;
        }

        [ContextMenu("Bake")]
        private void Bake()
        {
            if (walkableArea == null)
            {
                Debug.LogWarning("No Walkable Area assigned, cannot bake.", this);
                return;
            }

            if (gridBounds.size == Vector3.zero) ComputeBoundsFromCollider();

            origin = gridBounds.min;
            columns = Mathf.Max(1, Mathf.CeilToInt(gridBounds.size.x / cellSize));
            rows = Mathf.Max(1, Mathf.CeilToInt(gridBounds.size.y / cellSize));

            rawWalkable = new bool[columns, rows];
            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                rawWalkable[x, y] = walkableArea.OverlapPoint(CellToWorld(x, y));

            Erode();
        }

        /// <summary>Marks as blocked every cell with an obstacle within the agent radius.</summary>
        private void Erode()
        {
            walkable = (bool[,])rawWalkable.Clone();

            int radiusInCells = Mathf.CeilToInt(agentRadius / cellSize);
            if (radiusInCells <= 0) return;

            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (!rawWalkable[x, y]) continue;

                for (int dx = -radiusInCells; dx <= radiusInCells && walkable[x, y]; dx++)
                for (int dy = -radiusInCells; dy <= radiusInCells; dy++)
                {
                    Vector2Int probe = new(x + dx, y + dy);
                    if (IsInsideGrid(probe) && rawWalkable[probe.x, probe.y]) continue;

                    walkable[x, y] = false;
                    break;
                }
            }
        }

        private bool IsInsideGrid(Vector2Int cell)
            => cell.x >= 0 && cell.y >= 0 && cell.x < columns && cell.y < rows;

        private Vector2 CellToWorld(int x, int y)
            => origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

        private Vector2Int WorldToCell(Vector2 worldPoint) => new(
            Mathf.Clamp(Mathf.FloorToInt((worldPoint.x - origin.x) / cellSize), 0, columns - 1),
            Mathf.Clamp(Mathf.FloorToInt((worldPoint.y - origin.y) / cellSize), 0, rows - 1));

        /// <summary>Finds the walkable cell closest to <paramref name="from"/> by expanding rings.</summary>
        private bool FindNearestWalkable(Vector2Int from, out Vector2Int found)
        {
            int maxRing = Mathf.Max(columns, rows);
            for (int ring = 0; ring <= maxRing; ring++)
            for (int dx = -ring; dx <= ring; dx++)
            for (int dy = -ring; dy <= ring; dy++)
            {
                // Only the ring border, not the whole square, to avoid redoing work.
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;

                Vector2Int candidate = new(from.x + dx, from.y + dy);
                if (!IsInsideGrid(candidate) || !walkable[candidate.x, candidate.y]) continue;

                found = candidate;
                return true;
            }

            found = from;
            return false;
        }

        /// <summary>Octile distance: consistent with the costs used by the A*.</summary>
        private static float Heuristic(Vector2Int from, Vector2Int to)
        {
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);
            return dx + dy + (DiagonalCost - 2f) * Mathf.Min(dx, dy);
        }

        private void Reconstruct(
            Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, List<Vector2> result)
        {
            List<Vector2Int> cells = new() { current };
            while (cameFrom.TryGetValue(current, out current)) cells.Add(current);

            cells.Reverse();
            for (int i = 1; i < cells.Count; i++) result.Add(CellToWorld(cells[i].x, cells[i].y));
        }

        /// <summary>String pulling: drops intermediate waypoints with direct line of sight.</summary>
        private void Simplify(List<Vector2> points)
        {
            int i = 0;
            while (i < points.Count - 2)
            {
                if (HasLineOfSight(points[i], points[i + 2])) points.RemoveAt(i + 1);
                else i++;
            }
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / (cellSize * 0.5f));
            for (int i = 1; i < steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)steps);
                Vector2Int cell = WorldToCell(point);
                if (!walkable[cell.x, cell.y]) return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(gridBounds.center, gridBounds.size);

            if (walkable == null) return;

            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                Gizmos.color = walkable[x, y]
                    ? new Color(0f, 1f, 0f, 0.25f)
                    : new Color(1f, 0f, 0f, 0.25f);
                Gizmos.DrawCube(CellToWorld(x, y), Vector3.one * (cellSize * 0.9f));
            }
        }
    }
}
