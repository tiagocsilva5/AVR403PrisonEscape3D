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

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
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
            case State.flee:
                ChooseMedpack();

                //Vector3 away = transform.position - player.position;
                //away = away.normalized;
                //agent.SetDestination(transform.position + away);

                break;
            case State.dead:

            default:
                Debug.LogError($"Current state {currentState} doesn't exist");
                break;
        }
        
    }

    void ChooseDestination()
    {
        Vector3 targetPosition;

        targetPosition = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        agent.SetDestination(targetPosition);
    }

    void ChooseMedpack()
    {
        Vector3 targetPosition;

        GameObject[] medpacks = GameObject.FindGameObjectsWithTag("Medpack");
        float maxDistance = 0f;
        Vector3 furthest = Vector3.zero;

        foreach (GameObject medpack in medpacks)
        {
            float distance = Vector3.Distance(player.position, medpack.transform.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                furthest = medpack.transform.position;
            }
        }
        agent.SetDestination(furthest);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(agent.destination, 3f);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
