using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformMovement : MonoBehaviour
{
    [Range(0f, 2f)]
    public float speed;

    public GameObject platform;

    public Transform[] wayPoints;


    [HideInInspector]
    public Vector3 move;

    int index;

    public Vector3 deltaPos { get; private set; }

    private Vector3 lastPosition;


    Vector3 targetPos;
    // Start is called before the first frame update
    void Start()
    {
        index = 0;
        targetPos = wayPoints[index].localPosition;
        lastPosition = platform.transform.position;
    }

    // Update is called once per frame
    void Update()
    {

        // Save current position for later
        Vector3 currentPosition = platform.transform.position;

        // Move platform
        Vector3 direction = (targetPos - platform.transform.localPosition).normalized;
        Vector3 displacement = direction * speed * Time.deltaTime;
        platform.transform.Translate(displacement);

        // Calculate velocity based on actual displacement
        deltaPos = (platform.transform.position - lastPosition); //moved during previous frame
        lastPosition = platform.transform.position;

        //determine next waypoint
        if (Vector3.Distance(platform.transform.localPosition,targetPos) < 0.1f)
        {
            index = (index+1) % wayPoints.Length;
            targetPos = wayPoints[index].localPosition;
        }

    }



}
