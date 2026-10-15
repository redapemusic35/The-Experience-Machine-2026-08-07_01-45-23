using UnityEngine;

/// <summary>Keeps a world-space name label turned toward the active camera.</summary>
public class FaceCamera : MonoBehaviour
{
    private Camera cam;

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }
    }
}
