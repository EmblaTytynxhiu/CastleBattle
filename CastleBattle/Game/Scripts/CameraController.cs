using UnityEngine;

/// <summary>Smoothly moves/zooms the orthographic camera: close-up while building, whole field during battle.</summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public float smooth = 3f;
    Camera cam;
    Vector3 targetPos;
    float targetSize;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        targetPos = transform.position;
        targetSize = cam.orthographicSize;
    }

    /// <summary>Frame a build grid on the LEFT part of the screen (the RESOURCES panel sits on the right).</summary>
    public void ShowBuild(BuildManager bm)
    {
        float size = Mathf.Max(bm.rows * 0.5f + 1.6f, (bm.cols + 2f) / (cam.aspect * 0.9f));
        float viewW = size * 2f * cam.aspect;
        Vector2 c = bm.Center;
        targetPos = new Vector3(c.x + viewW * 0.24f, size - 1.2f, -10f);
        targetSize = size;
    }

    /// <summary>Show both castles and the field between them.</summary>
    public void ShowBattle(BuildManager left, BuildManager right)
    {
        float minX = left.Origin.x - 2f, maxX = right.Origin.x + right.cols + 2f;
        float width = maxX - minX;
        float tallest = Mathf.Max(left.rows, right.rows) + 6f;
        // leave a strip of ground under the castles for the aiming panel (bottom of the view = y -4.6)
        const float below = 4.6f;
        float size = Mathf.Max(width / (2f * cam.aspect), (tallest - 1.5f + below) * 0.5f);
        targetPos = new Vector3((minX + maxX) * 0.5f, size - below, -10f);
        targetSize = size;
    }

    public void SnapNow()
    {
        transform.position = targetPos;
        cam.orthographicSize = targetSize;
    }

    void LateUpdate()
    {
        float k = 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, k);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, k);
    }
}
