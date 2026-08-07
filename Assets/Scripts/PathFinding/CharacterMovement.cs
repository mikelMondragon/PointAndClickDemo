using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField, Tooltip("Unidades por segundo a escala 1")]
    float speed = 3f;
    [SerializeField, Tooltip("Distancia a la que se considera 'llegado' a un waypoint")]
    float arriveThreshold = 0.05f;

    readonly List<Vector2> path = new();
    int index;

    public bool IsMoving => index < path.Count;
    public Vector2 Velocity { get; private set; }

    public void SetPath(IReadOnlyList<Vector2> newPath)
    {
        path.Clear();
        for (int i = 0; i < newPath.Count; i++) path.Add(newPath[i]);
        index = 0;
    }

    public void Stop()
    {
        path.Clear();
        index = 0;
        Velocity = Vector2.zero;
    }

    void Update()
    {
        if (!IsMoving) { Velocity = Vector2.zero; return; }

        Vector2 pos = transform.position;
        Vector2 target = path[index];

        float step = speed * Time.deltaTime;
        Vector2 next = Vector2.MoveTowards(pos, target, step);

        Velocity = Time.deltaTime > 0f ? (next - pos) / Time.deltaTime : Vector2.zero;
        transform.position = new Vector3(next.x, next.y, transform.position.z);

        if (Vector2.Distance(next, target) <= arriveThreshold) index++;
    }
}