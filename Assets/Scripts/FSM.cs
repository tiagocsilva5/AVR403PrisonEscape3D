using UnityEngine;
using UnityEngine.AI;
using Unity.Profiling;

public enum State
{
    patrol = 0,
    chase = 1,
    search = 4
}

[RequireComponent(typeof(NavMeshAgent))]
public class FSM : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator anim;

    [Tooltip("Optional eye transform. Otherwise uses Eye Height.")]
    public Transform eyes;

    [Header("Movement")]
    [Min(0.1f)] public float walkSpeed = 1.5f;
    [Min(0.1f)] public float runSpeed = 3.5f;
    [Min(1f)] public float patrolRadius = 12f;
    public Vector2 idleDuration = new Vector2(1f, 4f);
    [Min(1f)] public float maximumTravelTime = 20f;

    [Header("Perception")]
    [Min(0.1f)] public float detectionDistance = 12f;
    [Min(0.1f)] public float loseDistance = 18f;
    [Range(1f, 360f)] public float fieldOfView = 110f;
    public float eyeHeight = 1.5f;
    public float playerAimHeight = 1f;

    [Tooltip("Include walls, doors and scenery. Exclude guard colliders.")]
    public LayerMask obstacleLayers = ~0;

    [Header("Search")]
    [Min(0.1f)] public float loseSightDelay = 1.2f;
    [Min(0.5f)] public float searchDuration = 5f;
    [Range(0f, 90f)] public float searchLookAngle = 60f;

    [Header("Optimizations")]
    [Tooltip("Disable for the benchmark baseline.")]
    public bool optimizeUpdates = true;

    [Min(0.02f)] public float perceptionInterval = 0.2f;
    [Min(0.02f)] public float chasePathInterval = 0.35f;
    [Min(0.05f)] public float targetMoveThreshold = 0.5f;

    [Header("Runtime State")]
    public State state = State.patrol;

    public int PerceptionChecks { get; private set; }
    public int ChasePathRequests { get; private set; }

    private NavMeshAgent agent;
    private NavMeshPath patrolPath;
    private Vector3 homePosition;
    private Vector3 lastSeenPosition;
    private Vector3 lastRequestedPosition;

    private bool canSeePlayer;
    private bool patrolWaiting;
    private bool searchLooking;
    private bool hasRequestedPosition;
    private bool navigationReady;
    private bool initialized;

    private float lastSeenTime;
    private float nextPerceptionTime;
    private float nextChasePathTime;
    private float stateDeadline;
    private float searchStartTime;
    private float searchStartYaw;

    private State enteredState;
    private bool hasAnimationState;
    private int lastAnimationState = -1;

    private static readonly int StateHash =
        Animator.StringToHash("State");

    private static readonly int GroundedHash =
        Animator.StringToHash("isGrounded");

    private static readonly ProfilerMarker PerceptionMarker =
        new ProfilerMarker("Guard.Perception");

    private static readonly ProfilerMarker ChasePathMarker =
        new ProfilerMarker("Guard.ChasePath");

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        patrolPath = new NavMeshPath();
        homePosition = transform.position;

        if (player == null)
        {
            Debug.LogError(
                "FSM: assign the Player reference in the Inspector.",
                this);

            enabled = false;
            return;
        }

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        ConfigureAnimator();

        agent.stoppingDistance = 0.5f;
        agent.updateRotation = true;

        if (state != State.patrol &&
            state != State.chase &&
            state != State.search)
        {
            state = State.patrol;
        }

        lastSeenPosition = transform.position;
        nextPerceptionTime =
            Time.time + Random.Range(0f, perceptionInterval);

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            navigationReady = false;
            SetAnimation(0);
            return;
        }

        if (!navigationReady)
        {
            navigationReady = true;
            canSeePlayer = false;

            nextPerceptionTime =
                Time.time + Random.Range(0f, perceptionInterval);

            EnterState(state);
        }
        else if (state != enteredState)
        {
            // Also supports changing State through another script.
            EnterState(state);
        }

        if (!optimizeUpdates || Time.time >= nextPerceptionTime)
        {
            nextPerceptionTime =
                Time.time + perceptionInterval;

            using (PerceptionMarker.Auto())
            {
                PerceptionChecks++;
                canSeePlayer = CheckPlayerVisibility();
            }

            if (canSeePlayer)
            {
                lastSeenPosition = player.position;
                lastSeenTime = Time.time;

                if (state != State.chase)
                    EnterState(State.chase);
            }
        }

        switch (state)
        {
            case State.patrol:
                UpdatePatrol();
                break;

            case State.chase:
                UpdateChase();
                break;

            case State.search:
                UpdateSearch();
                break;
        }

        bool moving =
            !agent.isStopped &&
            agent.velocity.sqrMagnitude > 0.01f;

        SetAnimation(
            moving ? (state == State.chase ? 2 : 1) : 0);
    }

    private bool CheckPlayerVisibility()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            return false;

        Vector3 origin = eyes != null
            ? eyes.position
            : transform.position + Vector3.up * eyeHeight;

        Vector3 target =
            player.position + Vector3.up * playerAimHeight;

        Vector3 direction = target - origin;

        float range = state == State.chase
            ? Mathf.Max(detectionDistance, loseDistance)
            : detectionDistance;

        if (direction.sqrMagnitude > range * range)
            return false;

        // Use a horizontal viewing cone.
        Vector3 horizontalDirection = direction;
        horizontalDirection.y = 0f;

        Vector3 forward = eyes != null
            ? eyes.forward
            : transform.forward;

        forward.y = 0f;

        if (horizontalDirection.sqrMagnitude > 0.001f &&
            forward.sqrMagnitude > 0.001f)
        {
            float minimumDot =
                Mathf.Cos(fieldOfView * 0.5f * Mathf.Deg2Rad);

            float dot = Vector3.Dot(
                forward.normalized,
                horizontalDirection.normalized);

            if (dot < minimumDot)
                return false;
        }

        float distance = direction.magnitude;

        if (distance < 0.001f)
            return true;

        if (Physics.Raycast(
            origin,
            direction / distance,
            out RaycastHit hit,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            // Allows the Player layer to be included in the mask.
            return hit.transform == player ||
                   hit.transform.IsChildOf(player);
        }

        return true;
    }

    private void EnterState(State nextState)
    {
        if (nextState != State.patrol &&
            nextState != State.chase &&
            nextState != State.search)
        {
            nextState = State.patrol;
        }

        state = nextState;
        enteredState = nextState;

        StopMovement();

        patrolWaiting = false;
        searchLooking = false;
        hasRequestedPosition = false;

        switch (state)
        {
            case State.patrol:
                agent.speed = walkSpeed;

                // Spread patrol path calculations across agents.
                patrolWaiting = true;
                stateDeadline =
                    Time.time + Random.Range(0.1f, 0.6f);
                break;

            case State.chase:
                agent.speed = runSpeed;
                lastSeenTime = Time.time;
                nextChasePathTime = Time.time;
                break;

            case State.search:
                agent.speed = walkSpeed;
                stateDeadline =
                    Time.time + maximumTravelTime;

                if (!RequestDestination(lastSeenPosition))
                    BeginLookingAround();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (patrolWaiting)
        {
            if (Time.time < stateDeadline)
                return;

            patrolWaiting = false;

            if (!ChoosePatrolDestination())
            {
                BeginPatrolPause();
                return;
            }

            stateDeadline =
                Time.time + maximumTravelTime;

            return;
        }

        if (Time.time >= stateDeadline)
        {
            BeginPatrolPause();
            return;
        }

        if (!agent.pathPending &&
            (HasArrived() || HasInvalidPath()))
        {
            BeginPatrolPause();
        }
    }

    private void BeginPatrolPause()
    {
        StopMovement();
        patrolWaiting = true;

        float minimum = Mathf.Max(0.2f, idleDuration.x);
        float maximum = Mathf.Max(minimum, idleDuration.y);

        stateDeadline =
            Time.time + Random.Range(minimum, maximum);
    }

    private void UpdateChase()
    {
        if (!canSeePlayer &&
            Time.time - lastSeenTime >= loseSightDelay)
        {
            EnterState(State.search);
            return;
        }

        if (optimizeUpdates &&
            Time.time < nextChasePathTime)
        {
            return;
        }

        nextChasePathTime =
            Time.time + chasePathInterval;

        if (optimizeUpdates)
        {
            if (agent.pathPending)
                return;

            bool targetMoved =
                !hasRequestedPosition ||
                (lastSeenPosition - lastRequestedPosition)
                    .sqrMagnitude >=
                targetMoveThreshold * targetMoveThreshold;

            if (!targetMoved &&
                agent.hasPath &&
                agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                return;
            }

            if (!targetMoved && HasArrived())
                return;
        }

        // Follow the last observed position, not an unseen player.
        using (ChasePathMarker.Auto())
        {
            if (RequestDestination(lastSeenPosition))
            {
                ChasePathRequests++;
                lastRequestedPosition = lastSeenPosition;
                hasRequestedPosition = true;
            }
        }
    }

    private void UpdateSearch()
    {
        if (!searchLooking)
        {
            if (Time.time >= stateDeadline ||
                (!agent.pathPending &&
                 (HasArrived() || HasInvalidPath())))
            {
                BeginLookingAround();
            }

            return;
        }

        float elapsed = Time.time - searchStartTime;

        float yaw =
            searchStartYaw +
            Mathf.Sin(elapsed * 1.5f) * searchLookAngle;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (Time.time >= stateDeadline)
            EnterState(State.patrol);
    }

    private void BeginLookingAround()
    {
        StopMovement();
        agent.updateRotation = false;

        searchLooking = true;
        searchStartTime = Time.time;
        searchStartYaw = transform.eulerAngles.y;
        stateDeadline = Time.time + searchDuration;
    }

    private bool ChoosePatrolDestination()
    {
        NavMeshQueryFilter filter = CreateNavigationFilter();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 offset =
                Random.insideUnitCircle * patrolRadius;

            Vector3 candidate =
                homePosition +
                new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(
                candidate, out NavMeshHit hit, 2f, filter))
            {
                continue;
            }

            // Avoid sampling a different prison floor.
            if (Mathf.Abs(hit.position.y - homePosition.y) > 1.5f)
                continue;

            if ((hit.position - transform.position)
                .sqrMagnitude < 4f)
            {
                continue;
            }

            if (!agent.CalculatePath(hit.position, patrolPath))
                continue;

            if (patrolPath.status != NavMeshPathStatus.PathComplete)
                continue;

            agent.updateRotation = true;
            agent.isStopped = false;

            if (agent.SetPath(patrolPath))
                return true;
        }

        return false;
    }

    private bool RequestDestination(Vector3 destination)
    {
        if (!NavMesh.SamplePosition(
            destination,
            out NavMeshHit hit,
            2f,
            CreateNavigationFilter()))
        {
            return false;
        }

        agent.updateRotation = true;
        agent.isStopped = false;

        return agent.SetDestination(hit.position);
    }

    private NavMeshQueryFilter CreateNavigationFilter()
    {
        return new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
    }

    private bool HasArrived()
    {
        return !agent.pathPending &&
               agent.remainingDistance <=
               agent.stoppingDistance + 0.1f;
    }

    private bool HasInvalidPath()
    {
        return !agent.hasPath ||
               agent.pathStatus != NavMeshPathStatus.PathComplete;
    }

    private void StopMovement()
    {
        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
        agent.updateRotation = true;
    }

    private void ConfigureAnimator()
    {
        if (anim == null ||
            anim.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                "FSM: no Animator Controller assigned. " +
                "Navigation will still work.",
                this);

            return;
        }

        anim.applyRootMotion = false;

        foreach (AnimatorControllerParameter parameter
                 in anim.parameters)
        {
            if (parameter.name == "State" &&
                parameter.type == AnimatorControllerParameterType.Int)
            {
                hasAnimationState = true;
            }

            if (parameter.name == "isGrounded" &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool(GroundedHash, true);
            }

            if ((parameter.name == "isDriving" ||
                 parameter.name == "isFlying") &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool(parameter.name, false);
            }
        }

        if (!hasAnimationState)
        {
            Debug.LogWarning(
                "FSM: Animator needs an Integer parameter named State.",
                this);
        }
    }

    private void SetAnimation(int animationState)
    {
        if (!hasAnimationState ||
            anim == null ||
            animationState == lastAnimationState)
        {
            return;
        }

        anim.SetInteger(StateHash, animationState);
        lastAnimationState = animationState;
    }

    private void OnDisable()
    {
        StopMovement();
        navigationReady = false;

        SetAnimation(0);
        lastAnimationState = -1;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            Application.isPlaying ? homePosition : transform.position,
            patrolRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, loseDistance);

        Vector3 origin = eyes != null
            ? eyes.position
            : transform.position + Vector3.up * eyeHeight;

        Vector3 forward = eyes != null
            ? eyes.forward
            : transform.forward;

        Vector3 left =
            Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * forward;

        Vector3 right =
            Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, left * detectionDistance);
        Gizmos.DrawRay(origin, right * detectionDistance);

        if (Application.isPlaying && state != State.patrol)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(lastSeenPosition, 0.5f);
            Gizmos.DrawLine(origin, lastSeenPosition);
        }
    }
}