using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class WorldTooltipBillboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void LateUpdate()
    {
        Camera cameraToFace = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToFace == null)
        {
            return;
        }

        Vector3 horizontalDirection = cameraToFace.transform.position - transform.position;
        horizontalDirection.y = 0f;
        if (horizontalDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        // World-space UI renders from its local back face, so it must point away from the camera.
        transform.rotation = Quaternion.LookRotation(-horizontalDirection, Vector3.up);
    }
}
