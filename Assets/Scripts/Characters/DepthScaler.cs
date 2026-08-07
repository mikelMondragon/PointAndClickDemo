using UnityEngine;

public class DepthScaler : MonoBehaviour
{
    [Header("Referencias en Y (unidades de mundo)")]
    [SerializeField, Tooltip("Y del fondo del escenario (personaje más lejano/pequeño)")]
    float yFar = 2f;
    [SerializeField, Tooltip("Y del frente (personaje más cercano/grande)")]
    float yNear = -3f;

    [Header("Escalas")]
    [SerializeField] float scaleFar = 0.5f;
    [SerializeField] float scaleNear = 1.2f;

    void LateUpdate()
    {
        float t = Mathf.InverseLerp(yFar, yNear, transform.position.y);
        float s = Mathf.Lerp(scaleFar, scaleNear, t);
        transform.localScale = new Vector3(s, s, 1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-50f, yFar), new Vector3(50f, yFar));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-50f, yNear), new Vector3(50f, yNear));
    }
}