using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GridPathfinder : MonoBehaviour, IPathProvider
{
    [Header("Área transitable")]
    [SerializeField] PolygonCollider2D walkableArea;

    [Header("Rejilla")]
    [SerializeField, Tooltip("Tamaño de cada celda en unidades de mundo. Con PPU 100, 0.25 = celdas de 25px")]
    float cellSize = 0.25f;

    [SerializeField, Tooltip("Se autocalcula con el botón de abajo desde el Collider, pero puedes ajustarlo a mano después")]
    Bounds gridBounds;

    [SerializeField, Tooltip("Margen extra alrededor del collider al autocalcular")]
    float boundsPadding = 0.5f;

    [Header("Personaje")]
    [SerializeField, Tooltip("Radio del personaje: engorda los obstáculos para que no camine pegado a paredes/esquinas")]
    float agentRadius = 0.2f;

    bool[,] rawWalkable;    // tal cual el collider, sin erosionar — para el snap del click
    bool[,] walkable;       // erosionado — lo que usa el A*
    int cols, rows;
    Vector2 origin;

    static readonly Vector2Int[] Neighbours =
    {
        new(1,0), new(-1,0), new(0,1), new(0,-1),
        new(1,1), new(1,-1), new(-1,1), new(-1,-1)
    };

    [ContextMenu("Auto-calcular Bounds desde el Collider")]
    void ComputeBoundsFromCollider()
    {
        if (walkableArea == null)
        {
            Debug.LogWarning("Asigna Walkable Area antes de autocalcular.", this);
            return;
        }
        Bounds b = walkableArea.bounds;
        b.Expand(boundsPadding * 2f);
        gridBounds = b;
    }

    void Awake() => Bake();
    void OnValidate() => Bake();

    [ContextMenu("Bake")]
    void Bake()
    {
        if (walkableArea == null)
        {
            Debug.LogWarning("No hay Walkable Area asignado, no se puede bakear.", this);
            return;
        }
        if (gridBounds.size == Vector3.zero) ComputeBoundsFromCollider();

        origin = gridBounds.min;
        cols = Mathf.Max(1, Mathf.CeilToInt(gridBounds.size.x / cellSize));
        rows = Mathf.Max(1, Mathf.CeilToInt(gridBounds.size.y / cellSize));

        rawWalkable = new bool[cols, rows];
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                rawWalkable[x, y] = walkableArea.OverlapPoint(CellToWorld(x, y));

        Erode();
    }

    void Erode()
    {
        walkable = (bool[,])rawWalkable.Clone();

        int r = Mathf.CeilToInt(agentRadius / cellSize);
        if (r <= 0) return;

        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                if (!rawWalkable[x, y]) continue;

                for (int dx = -r; dx <= r && walkable[x, y]; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        bool blocked = nx < 0 || ny < 0 || nx >= cols || ny >= rows || !rawWalkable[nx, ny];
                        if (blocked) { walkable[x, y] = false; break; }
                    }
            }
    }

    Vector2 CellToWorld(int x, int y)
        => origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

    Vector2Int WorldToCell(Vector2 p) => new(
        Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / cellSize), 0, cols - 1),
        Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / cellSize), 0, rows - 1));

    /// <summary>Busca la celda erosionada-transitable más cercana a `from` por anillos crecientes.</summary>
    bool FindNearestWalkable(Vector2Int from, out Vector2Int found)
    {
        int maxR = Mathf.Max(cols, rows);
        for (int r = 0; r <= maxR; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    // Solo el borde del anillo, no todo el cuadrado, para no repetir trabajo.
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;

                    int nx = from.x + dx, ny = from.y + dy;
                    if (nx < 0 || ny < 0 || nx >= cols || ny >= rows) continue;
                    if (walkable[nx, ny]) { found = new Vector2Int(nx, ny); return true; }
                }
        found = from;
        return false;
    }

    public bool TryGetPath(Vector2 from, Vector2 to, List<Vector2> result)
    {
        result.Clear();
        if (walkable == null) return false;

        Vector2Int start = WorldToCell(from);
        if (!walkable[start.x, start.y] && !FindNearestWalkable(start, out start)) return false;

        // El click se valida contra el punto pedido, pero la búsqueda usa la celda
        // erosionada más cercana si el click cayó en el margen de seguridad del agente.
        Vector2Int goal = WorldToCell(to);
        if (!walkable[goal.x, goal.y] && !FindNearestWalkable(goal, out goal)) return false;

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [start] = 0f };
        var open = new List<Vector2Int> { start };
        var closed = new HashSet<Vector2Int>();

        while (open.Count > 0)
        {
            int best = 0;
            float bestF = float.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                float f = gScore[open[i]] + Heuristic(open[i], goal);
                if (f < bestF) { bestF = f; best = i; }
            }

            Vector2Int current = open[best];
            if (current == goal) { Reconstruct(cameFrom, current, result); Simplify(result); return true; }

            open.RemoveAt(best);
            closed.Add(current);

            foreach (var dir in Neighbours)
            {
                Vector2Int n = current + dir;
                if (n.x < 0 || n.y < 0 || n.x >= cols || n.y >= rows) continue;
                if (!walkable[n.x, n.y] || closed.Contains(n)) continue;

                // No cortar esquinas: si es diagonal, ambas celdas ortogonales deben ser libres.
                if (dir.x != 0 && dir.y != 0 &&
                    (!walkable[current.x + dir.x, current.y] || !walkable[current.x, current.y + dir.y]))
                    continue;

                float tentative = gScore[current] + (dir.x != 0 && dir.y != 0 ? 1.414f : 1f);
                if (gScore.TryGetValue(n, out float g) && tentative >= g) continue;

                cameFrom[n] = current;
                gScore[n] = tentative;
                if (!open.Contains(n)) open.Add(n);
            }
        }
        return false;
    }

    static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (1.414f - 2f) * Mathf.Min(dx, dy);
    }

    void Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, List<Vector2> result)
    {
        var cells = new List<Vector2Int> { current };
        while (cameFrom.TryGetValue(current, out current)) cells.Add(current);
        cells.Reverse();
        for (int i = 1; i < cells.Count; i++) result.Add(CellToWorld(cells[i].x, cells[i].y));
    }

    /// <summary>String pulling: quita waypoints intermedios con línea de visión directa.</summary>
    void Simplify(List<Vector2> pts)
    {
        int i = 0;
        while (i < pts.Count - 2)
        {
            if (HasLineOfSight(pts[i], pts[i + 2])) pts.RemoveAt(i + 1);
            else i++;
        }
    }

    bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / (cellSize * 0.5f));
        for (int i = 1; i < steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            Vector2Int c = WorldToCell(p);
            if (!walkable[c.x, c.y]) return false;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(gridBounds.center, gridBounds.size);

        if (walkable == null) return;
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                Gizmos.color = walkable[x, y] ? new Color(0f, 1f, 0f, 0.25f) : new Color(1f, 0f, 0f, 0.25f);
                Gizmos.DrawCube(CellToWorld(x, y), Vector3.one * cellSize * 0.9f);
            }
    }
}