using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CarController : MonoBehaviour
{
    public float acceleration = 15f;
    public float steering = 100f;
    public float traction = 4f;
    public float spinRate;

    private Rigidbody rb;


    public Transform driverPos, exitPos;
    Transform player;
    bool isDriving;
    bool isGrounded;

    bool display;

    public float detectionDist;

    public Transform[] wheels;

    //public static Event onVehicleEnter;
    //public static Event onVehicleExit;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // lower center of mass to reduce rolling
        isDriving = false;
        display = false;
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (isDriving && Input.GetKeyDown(KeyCode.E))
            VehicleExit();

        else if (Vector3.Distance(player.position, transform.position) < 3f )
        {

            //enterText.enabled = true;
            display = true;

            if (Input.GetKeyDown(KeyCode.E))
                VehicleEnter();
        }
        else
        {
            display = false;
            //enterText.enabled = false;
        }
            

        

        foreach (Transform t in wheels)
        {
            t.Rotate(Vector3.right, spinRate* Vector3.Dot(transform.forward, rb.velocity) * Time.deltaTime);
        }

    }

    void FixedUpdate()
    {

        int layerToExclude = 6;
        int layerMask = ~(1 << layerToExclude); // Invert mask to exclude layer 6

        isGrounded = Physics.Raycast(transform.position, Vector3.down, detectionDist, layerMask);

        if (isDriving && isGrounded)
            Control();

    }

    void OnGUI()
    {
        
        if (display && !isDriving)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 32; // Adjust the size as needed
            style.alignment = TextAnchor.LowerCenter;
            style.normal.textColor = Color.white; // Optional: set the text color

            Rect rect = new Rect(0, Screen.height - 100, Screen.width, 50);
            GUI.Label(rect, "Press 'E' to Enter Vehicle", style);


        }

    }

    void Control()
    {
        float moveInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");

        // Move forward/backward
        rb.AddForce(transform.forward * moveInput * acceleration, ForceMode.Acceleration);

        // Determine if the car is moving forward or backward
        float movingDirection = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward));

        // Steering: rotate around Y-axis, scaled by current speed and movement direction
        float steerAmount = steerInput * steering * movingDirection * Time.fixedDeltaTime * Mathf.Clamp01(rb.velocity.magnitude / 5f);
        Quaternion turnRotation = Quaternion.Euler(0f, steerAmount, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);

        // Apply traction to reduce sliding
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.x = Mathf.Lerp(localVelocity.x, 0, traction * Time.fixedDeltaTime); // reduce sideways drift
        rb.velocity = transform.TransformDirection(localVelocity);
    }
    

    void VehicleEnter()
    {
        isDriving = true;

        player.GetComponent<CharacterController>().enabled = false;
        player.GetComponent<Collider>().enabled = false;
        player.GetComponent<PlayerController>().enabled = false;

        player.GetComponentInChildren<Animator>().SetBool("isDriving", true);
        player.SetParent(driverPos);
        player.localPosition = Vector3.zero;
        player.localRotation = Quaternion.identity;

        player.GetComponent<PlayerController>().child.localRotation = Quaternion.identity;
        
    }

    void VehicleExit()
    {
        isDriving = false;


        player.GetComponentInChildren<Animator>().SetBool("isDriving", false);
        player.SetParent(null);
        player.position = exitPos.position;
        player.localRotation = Quaternion.identity;
        player.GetComponent<PlayerController>().child.localRotation = Quaternion.identity;

        player.GetComponent<CharacterController>().enabled = true;
        player.GetComponent<Collider>().enabled = true;
        player.GetComponent<PlayerController>().enabled = true;


    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * detectionDist);
    }

}
