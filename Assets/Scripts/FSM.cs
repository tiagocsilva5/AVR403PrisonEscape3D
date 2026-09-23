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
    public float runSpeed, walkSpeed, detectionDistance, loseDistance, maxDist;
    public State state;
    Animator anim;
    bool isIdle;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        anim.SetBool("isGrounded", true);
        isIdle = false;
        
        ChooseNewDestination();
    }

    // Update is called once per frame
    void Update()
    {


        Vector3 toPlayer = player.position - transform.position;
        toPlayer = Vector3.Normalize(toPlayer);

        int mask = 1 << LayerMask.NameToLayer("Navigation");

        switch (state)
        {

            case State.patrol:

                if(!isIdle)
                {
                    anim.SetInteger("State", 1);
                    agent.speed = walkSpeed;
                    if (Vector3.Distance(agent.destination, transform.position) < 3f)
                    {
                        StartCoroutine(Idle());
                        ChooseNewDestination();

                    }
                }


                Debug.Log(Vector3.Angle(toPlayer, transform.forward));
                //player detection
                if (Vector3.Distance(player.position, transform.position) < detectionDistance &&
                    !Physics.Raycast(transform.position, toPlayer, detectionDistance, mask))
                {
                    state = State.chase;
                }

                break;
            case State.chase:
                anim.SetInteger("State", 2);
                agent.speed = runSpeed;
                agent.SetDestination(player.position);

                if (Vector3.Distance(player.position, transform.position) > loseDistance ||
                    Physics.Raycast(transform.position, toPlayer, detectionDistance, mask))
                {
                    state = State.patrol;
                    ChooseNewDestination();  
                }


                break;
            case State.flee:
                anim.SetInteger("State", 2);
                agent.speed = runSpeed;
                ChooseMedpack();

                //Vector3 away = transform.position - player.position;
                //away = away.normalized;

                //agent.SetDestination( transform.position + away * 8f );


                break;
            case State.dead:



                break;

            default:

                Debug.LogError($"Current state {state} doesn't exist.");
                break;

        }


    }


    void ChooseNewDestination()
    {
        Vector3 targetPosition;

        targetPosition = new Vector3(Random.Range(-50f, 10f), 0f, Random.Range(-10f, 10f));

        agent.SetDestination(targetPosition);

    }


    void ChooseMedpack()
    {

        Vector3 targetPosition;

        GameObject[] medPacks = GameObject.FindGameObjectsWithTag("Medpack");
        float maxDistance = 0f;
        Vector3 furthest = Vector3.zero;

        foreach (GameObject medPack in medPacks)
        {
            float dist = Vector3.Distance(player.position, medPack.transform.position);
            if (dist > maxDistance && Vector3.Distance(transform.position, medPack.transform.position) < maxDist 
                && Vector3.Angle(transform.position - player.position , transform.position - medPack.transform.position) >40f )
            {
                furthest = medPack.transform.position;
                maxDistance = dist;
            }

        }
        Debug.Log("Furthest dist:" + Vector3.Angle(transform.position - player.position, transform.position - furthest) );
        targetPosition = furthest;    

        agent.SetDestination(targetPosition);

    }

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



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (agent != null)
            Gizmos.DrawWireSphere(agent.destination, 2f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, loseDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, player.position);

    }
}

