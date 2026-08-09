using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(Camera))]
public class ContainBackgroundCamera : MonoBehaviour
{
    [SerializeField] SpriteRenderer background;
    [SerializeField] Color barColor = Color.black;

    Camera cam;

    void OnEnable() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (background == null || cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = barColor;

        Bounds b = background.bounds;
        bool limitByWidth = cam.aspect < (b.size.x / b.size.y);

        cam.orthographicSize = limitByWidth
            ? (b.size.x / cam.aspect) * 0.5f
            : b.size.y * 0.5f;

        transform.position = new Vector3(b.center.x, b.center.y, transform.position.z);
    }
}