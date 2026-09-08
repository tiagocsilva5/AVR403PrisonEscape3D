using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    public float zoomSensitivity, panSensitivity, minDistance, maxDistance, minAngle, maxAngle, smooth;


    float distance, angleY, targetDistance;

    // Start is called before the first frame update
    void Start()
    {
        distance = transform.localPosition.magnitude;
        angleY = Mathf.Atan2(transform.localPosition.y, Mathf.Abs( transform.localPosition.z));
    }

    // Update is called once per frame
    void LateUpdate()
    {


        targetDistance = Mathf.Clamp(targetDistance - Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity * Time.deltaTime, minDistance, maxDistance);


        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.parent.position, 0.5f, transform.position - transform.parent.position, out hitInfo, maxDistance))
        {
            Debug.Log("Blocked!");
            if ( distance > hitInfo.distance)
                targetDistance = hitInfo.distance;

        }
        
        distance = Mathf.Lerp(distance, targetDistance, smooth * Time.deltaTime);


        if (Input.GetMouseButton(1))
        {
            float newAngleY = Mathf.Clamp(angleY + Input.GetAxis("Mouse Y") * panSensitivity * Time.deltaTime, minAngle, maxAngle);
            // Direction from pivot to object
            Vector3 dir = transform.localPosition;

            // Rotate the direction
            dir = Quaternion.AngleAxis(newAngleY - angleY, Vector3.right) * dir;

            // Update position
            transform.localPosition = dir;

            transform.Rotate(Vector3.right, newAngleY - angleY);

            angleY = newAngleY;
        }


        transform.localPosition *= distance / transform.localPosition.magnitude;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Gizmos.DrawLine(transform.parent.position, transform.position);
    }
}
