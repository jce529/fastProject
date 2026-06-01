using UnityEngine;

/// <summary>
/// Direct LateUpdate camera follow -- no Cinemachine, no lead-ahead (per D-11, D-12, D-13).
/// Attach to Main Camera. Assign target = Player Transform in Inspector.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}
