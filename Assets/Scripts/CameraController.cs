using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float zoomSensitivity, panSensitivity, minDistance, maxDistance, minAngle, maxAngle, smooth;
    public float collisionRadius = 0.3f;
    public LayerMask collisionMask = ~0; // set this in inspector to exclude the player layer

    float distance, angleY, targetDistance;

    void Start()
    {
        distance = transform.localPosition.magnitude;
        targetDistance = distance;
        angleY = Mathf.Atan2(transform.localPosition.y, Mathf.Abs(transform.localPosition.z));
    }

    void LateUpdate()
    {
        if (GameScreens.BlocksGameplay)
            return;

        // Zoom input
        targetDistance = Mathf.Clamp(
            targetDistance - Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity * Time.deltaTime,
            minDistance, maxDistance);

        // Orbit input
        if (Input.GetMouseButton(1))
        {
            float newAngleY = Mathf.Clamp(angleY + Input.GetAxis("Mouse Y") * panSensitivity * Time.deltaTime, minAngle, maxAngle);
            Vector3 dir = transform.localPosition;
            dir = Quaternion.AngleAxis(newAngleY - angleY, Vector3.right) * dir;
            transform.localPosition = dir;
            transform.Rotate(Vector3.right, newAngleY - angleY);
            angleY = newAngleY;
        }

        // Direction from pivot toward camera, at current orientation
        Vector3 direction = transform.localPosition.normalized;
        Vector3 pivot = transform.parent.position;

        // Where would we like to be, ignoring walls?
        float wantedDistance = targetDistance;

        RaycastHit hitInfo;
        if (Physics.SphereCast(pivot, collisionRadius, direction, out hitInfo, targetDistance, collisionMask))
        {
            wantedDistance = hitInfo.distance;
        }

        // Snap in instantly if blocked (prevents clipping), smooth when pulling back out
        if (wantedDistance < distance)
            distance = wantedDistance;
        else
            distance = Mathf.Lerp(distance, wantedDistance, smooth * Time.deltaTime);

        distance = Mathf.Max(distance, 0.05f); // avoid camera landing inside the pivot

        transform.localPosition = direction * distance;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
        Gizmos.DrawLine(transform.parent.position, transform.position);
    }
}
