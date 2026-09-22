using System.Collections;
using System.Collections.Generic;
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
    public State currentState;
    NavMeshAgent agent;
    public float speed, detectionDistance, loseDistance;

    // stores the medkit the NPC is going to
    Transform targetMedpack;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }


    void Update()
    {
        agent.speed = speed;

        switch (currentState)
        {
            case State.patrol:
                if(Vector3.Distance(agent.transform.position, agent.destination) < 3f)
                {
                    ChooseDestination();
                }

                if(Vector3.Distance(player.position, transform.position) < detectionDistance)
                {
                    currentState = State.chase;
                }

                break;


            case State.chase:
                agent.SetDestination(player.position);

                if(Vector3.Distance(player.position, transform.position) > loseDistance)
                {
                    currentState = State.patrol;
                    ChooseDestination();
                }

                break;


            // The npc tries to reach a medkit, but runs away if the player gets too close
            case State.flee:

                // find a medkit
                if (targetMedpack == null)
                {
                    ChooseMedpack();
                }

                // Check how close the player is to the NPC
                float playerDistance = Vector3.Distance(
                    player.position,
                    transform.position
                );

                // if the player is too close, run away
                if (playerDistance < 5f)
                {
                    Vector3 away = transform.position - player.position;
                    away = away.normalized;

                    agent.SetDestination(
                        transform.position + away * 5f
                    );
                }

                // if the player is far enough away, continue toward the medkit
                else if (targetMedpack != null)
                {
                    agent.SetDestination(targetMedpack.position);
                }

                break;


            case State.dead:

                break;


            default:
                Debug.LogError($"Current state {currentState} doesn't exist");
                break;
        }
    }


    void ChooseDestination()
    {
        Vector3 targetPosition;

        targetPosition = new Vector3(
            Random.Range(-10f, 10f),
            0,
            Random.Range(-10f, 10f)
        );

        agent.SetDestination(targetPosition);
    }


    void ChooseMedpack()
    {
        // find "Medpack" tag
        GameObject[] medpacks = GameObject.FindGameObjectsWithTag("Medpack");

        float maxDistance = 0f;

        GameObject furthest = null;

        // check every medkit
        foreach (GameObject medpack in medpacks)
        {
            // Calculate the distance from the player to the medkit
            float distance = Vector3.Distance(
                player.position,
                medpack.transform.position
            );

            // If this medkit is further from the player, make it the new target
            if (distance > maxDistance)
            {
                maxDistance = distance;
                furthest = medpack;
            }
        }

        // save the selected medkit
        if (furthest != null)
        {
            targetMedpack = furthest.transform;
        }
    }

    private void OnDrawGizmos()
    {
        if (agent == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(agent.destination, 3f);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}