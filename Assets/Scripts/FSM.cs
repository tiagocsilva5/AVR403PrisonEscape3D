using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public enum State
{
    patrol,
    chase,
    flee,
    dead
}

public class FSM : MonoBehaviour
{
    public Transform player;

    NavMeshAgent agent;
    Animator anim;

    public float runSpeed;
    public float walkSpeed;
    public float detectionDistance;
    public float loseDistance;
    public float maxDist;

    public State state;

    bool isIdle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        anim = GetComponentInChildren<Animator>();

        Debug.Log("NPC Animator: " + anim.gameObject.name);
        Debug.Log("Animator Enabled: " + anim.enabled);
        Debug.Log("Animator Speed: " + anim.speed);
        Debug.Log("Animator Controller: " + anim.runtimeAnimatorController);
        Debug.Log("Animator Avatar: " + anim.avatar);

        anim.SetBool("isGrounded", true);

        isIdle = false;

        ChooseNewDestination();
    
    }
    // Start is called before the first frame update
/*   void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        anim.SetBool("isGrounded", true);

        isIdle = false;

        ChooseNewDestination();
    }

*/
    // Update is called once per frame
    void Update()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer = Vector3.Normalize(toPlayer);

        int mask = 1 << LayerMask.NameToLayer("Navigation");


        switch (state)
        {
            // -------------------------
            // PATROL STATE
            // -------------------------
            case State.patrol:

                if (!isIdle)
                {
                    anim.SetInteger("State", 1);

                    agent.speed = walkSpeed;

                    if (Vector3.Distance(agent.destination, transform.position) < 3f)
                    {
                        StartCoroutine(Idle());
                        ChooseNewDestination();
                    }
                }

                Vector3.Angle(toPlayer, transform.forward);


                // Player detection
                if (
                    Vector3.Distance(player.position, transform.position) < detectionDistance
                    &&
                    !Physics.Raycast(
                        transform.position,
                        toPlayer,
                        detectionDistance,
                        mask
                    )
                )
                {
                    state = State.chase;
                }

                break;


            // -------------------------
            // CHASE STATE
            // -------------------------
            case State.chase:

                anim.SetInteger("State", 2);

                agent.speed = runSpeed;

                agent.SetDestination(player.position);


                if (
                    Vector3.Distance(player.position, transform.position) > loseDistance
                    ||
                    Physics.Raycast(
                        transform.position,
                        toPlayer,
                        detectionDistance,
                        mask
                    )
                )
                {
                    state = State.patrol;

                    ChooseNewDestination();
                }

                break;


            // -------------------------
            // FLEE STATE
            // -------------------------
            case State.flee:

                anim.SetInteger("State", 2);

                agent.speed = runSpeed;

                ChooseMedpack();

                /*
                Vector3 away = transform.position - player.position;

                away = away.normalized;

                agent.SetDestination(
                    transform.position + away * 8f
                );
                */

                break;


            // -------------------------
            // DEAD STATE
            // -------------------------
            case State.dead:

                break;


            // -------------------------
            // DEFAULT
            // -------------------------
            default:

                Debug.LogError(
                    $"Current state {state} doesn't exist."
                );

                break;
        }
    }


    // -------------------------
    // CHOOSE PATROL DESTINATION
    // -------------------------
    void ChooseNewDestination()
    {
        Vector3 targetPosition;

        targetPosition = new Vector3(
            Random.Range(-50f, 10f),
            0f,
            Random.Range(-10f, 10f)
        );

        agent.SetDestination(targetPosition);
    }


    // -------------------------
    // CHOOSE MEDPACK
    // -------------------------
    void ChooseMedpack()
    {
        Vector3 targetPosition;

        GameObject[] medPacks =
            GameObject.FindGameObjectsWithTag("Medpack");

        float maxDistance = 0f;

        Vector3 furthest = Vector3.zero;


        foreach (GameObject medPack in medPacks)
        {
            float dist =
                Vector3.Distance(
                    player.position,
                    medPack.transform.position
                );


            if (
                dist > maxDistance
                &&
                Vector3.Distance(
                    transform.position,
                    medPack.transform.position
                ) < maxDist
                &&
                Vector3.Angle(
                    transform.position - player.position,
                    transform.position - medPack.transform.position
                ) > 40f
            )
            {
                furthest = medPack.transform.position;

                maxDistance = dist;
            }
        }


        Debug.Log(
            "Furthest dist: " +
            Vector3.Angle(
                transform.position - player.position,
                transform.position - furthest
            )
        );


        targetPosition = furthest;

        agent.SetDestination(targetPosition);
    }


    // -------------------------
    // IDLE COROUTINE
    // -------------------------
    IEnumerator Idle()
    {
        isIdle = true;

        anim.SetInteger("State", 0);

        agent.speed = 0;

        float waitTime = Random.Range(0, 4f);

        yield return new WaitForSeconds(waitTime);

        isIdle = false;

        yield return null;
    }


    // -------------------------
    // GIZMOS
    // -------------------------
    private void OnDrawGizmos()
    {
        // Patrol destination
        Gizmos.color = Color.yellow;

        if (agent != null)
        {
            Gizmos.DrawWireSphere(
                agent.destination,
                2f
            );
        }


        // Detection distance
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionDistance
        );


        // Lose distance
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            loseDistance
        );


        // Line to player
        Gizmos.color = Color.blue;

        if (player != null)
        {
            Gizmos.DrawLine(
                transform.position,
                player.position
            );
        }
    }
}