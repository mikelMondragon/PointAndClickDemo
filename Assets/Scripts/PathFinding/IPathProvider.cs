using System.Collections.Generic;
using UnityEngine;

namespace PointAndClickDemo.Pathfinding
{
    /// <summary>Source of walkable paths between two points of the scene.</summary>
    public interface IPathProvider
    {
        /// <summary>
        /// Fills <paramref name="result"/> with the path waypoints.
        /// Returns false when the destination is unreachable.
        /// </summary>
        bool TryGetPath(Vector2 from, Vector2 to, List<Vector2> result);
    }
}
