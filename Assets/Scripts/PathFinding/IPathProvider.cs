using System.Collections.Generic;
using UnityEngine;

public interface IPathProvider
{
    bool TryGetPath(Vector2 from, Vector2 to, List<Vector2> result);
}