using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class InmateWander : MonoBehaviour
{
    [Header("Movement")]
    [Min(1f)] public float wanderRadius = 8f;
    [Min(0.1f)] public float walkSpeed = 1.5f;
    [Min(1f)] public float maximumWalkTime = 20f;

    [Header("Inmate Behavior")]
    public Vector2 pauseDuration = new Vector2(3f, 7f);
    [Range(0f, 1f)] public float lookAroundChance = 0.65f;
    [Min(1f)] public float lookTurnSpeed = 60f;

    [Header("Animation")]
    public Animator animator;

    [Tooltip("Must match the Animator's State condition for Idle.")]
    public int idleState = 0;

    [Tooltip("Must match the Animator's State condition for Walk.")]
    public int walkState = 1;

    private NavMeshAgent agent;
    private NavMeshPath candidatePath;
    private Vector3 homePosition;
    private Quaternion lookRotation;

    private bool walking;
    private bool lookingAround;
    private float stateTimer;

    private bool hasStateParameter;
    private bool hasGroundedParameter;

    private static readonly int StateHash =
        Animator.StringToHash("State");

    private static readonly int GroundedHash =
        Animator.StringToHash("isGrounded");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        candidatePath = new NavMeshPath();
        homePosition = transform.position;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        hasStateParameter = HasParameter(
            "State", AnimatorControllerParameterType.Int);

        hasGroundedParameter = HasParameter(
            "isGrounded", AnimatorControllerParameterType.Bool);

        if (animator != null)
        {
            animator.applyRootMotion = false;

            if (hasGroundedParameter)
                animator.SetBool(GroundedHash, true);

            if (HasParameter(
                "isDriving", AnimatorControllerParameterType.Bool))
            {
                animator.SetBool("isDriving", false);
            }

            if (HasParameter(
                "isFlying", AnimatorControllerParameterType.Bool))
            {
                animator.SetBool("isFlying", false);
            }

            if (!hasStateParameter)
            {
                Debug.LogWarning(
                    "InmateWander needs an Integer Animator parameter " +
                    "named 'State'.",
                    this);
            }
        }

        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.35f;
    }

    private void OnEnable()
    {
        walking = false;
        lookingAround = false;
        stateTimer = Random.Range(0.5f, 2f);

        UpdateAnimation();
    }

    private void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            walking = false;
            lookingAround = false;
            UpdateAnimation();
            return;
        }

        stateTimer -= Time.deltaTime;

        if (walking)
        {
            if (!agent.pathPending)
            {
                bool arrived =
                    agent.remainingDistance <=
                    agent.stoppingDistance + 0.1f;

                bool invalidPath =
                    !agent.hasPath ||
                    agent.pathStatus != NavMeshPathStatus.PathComplete;

                if (arrived || invalidPath || stateTimer <= 0f)
                    BeginPause();
            }
            else if (stateTimer <= 0f)
            {
                BeginPause();
            }
        }
        else
        {
            agent.isStopped = true;
            agent.updateRotation = false;

            if (lookingAround)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    lookRotation,
                    lookTurnSpeed * Time.deltaTime);
            }

            if (stateTimer <= 0f)
            {
                if (!BeginWalk())
                    BeginPause();
            }
        }

        UpdateAnimation();
    }

    private bool BeginWalk()
    {
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 offset =
                Random.insideUnitCircle * wanderRadius;

            Vector3 candidate = homePosition +
                new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(
                candidate, out NavMeshHit hit, 2f, filter))
            {
                continue;
            }

            // Avoid destinations on another floor.
            if (Mathf.Abs(hit.position.y - homePosition.y) > 1.5f)
                continue;

            // Avoid choosing a destination too close to the inmate.
            if (Vector3.Distance(
                transform.position, hit.position) < 1f)
            {
                continue;
            }

            if (!agent.CalculatePath(hit.position, candidatePath))
                continue;

            if (candidatePath.status != NavMeshPathStatus.PathComplete)
                continue;

            agent.updateRotation = true;
            agent.isStopped = false;

            if (!agent.SetPath(candidatePath))
            {
                agent.isStopped = true;
                continue;
            }

            walking = true;
            lookingAround = false;
            stateTimer = maximumWalkTime;

            return true;
        }

        return false;
    }

    private void BeginPause()
    {
        walking = false;

        agent.isStopped = true;
        agent.ResetPath();
        agent.updateRotation = false;

        float minimum = Mathf.Max(0.5f, pauseDuration.x);
        float maximum = Mathf.Max(minimum, pauseDuration.y);

        stateTimer = Random.Range(minimum, maximum);
        lookingAround = Random.value < lookAroundChance;

        lookRotation = Quaternion.Euler(
            0f,
            transform.eulerAngles.y + Random.Range(-110f, 110f),
            0f);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        // Follow the behavior state instead of measured velocity.
        if (hasStateParameter)
        {
            animator.SetInteger(
                StateHash,
                walking ? walkState : idleState);
        }

        if (hasGroundedParameter)
            animator.SetBool(GroundedHash, true);
    }

    private bool HasParameter(
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null ||
            string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updateRotation = true;
        }

        walking = false;
        UpdateAnimation();
    }
}