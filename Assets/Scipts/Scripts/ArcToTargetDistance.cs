using UnityEngine;
using UnityEngine.AI;

public class ArcToTargetDistance : MonoBehaviour
{
    #region Variables

    [Header("Patrol")]
    public Transform patrolPointA;
    public Transform patrolPointB;
    public float patrolWaitTime = 1f;

    [Header("Detection")]
    public float detectionDistance = 15f;
    public float loseTargetDelay = 3f;
    public string targetTag = "Player";

    [Header("Chase")]
    public float attackDistance = 4f;

    [Header("Jump")]
    public float arcHeight = 5f;
    public float jumpCooldown = 2f;
    public float jumpDelay = 1.5f;

    [Header("Avoidance")]
    public float obstacleCheckDistance = 2f;
    public float avoidanceStrength = 2f;

    public float rotationSpeed = 6f;

    private NavMeshAgent agent;
    private Rigidbody rb;

    private Transform player;
    private Transform currentPatrolTarget;

    private float waitTimer;
    private float lastSeenTime;
    private float lastJumpTime;
    private float jumpStartTime;
    private float jumpEndTime;

    private bool hasSeenPlayer = false;
    private bool isJumping = false;
    private bool isPreparingJump = false;

    #endregion

    #region Unity Methods

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        agent.updateRotation = false;

        currentPatrolTarget = patrolPointA;
        agent.SetDestination(currentPatrolTarget.position);
    }

    void Update()
    {
        if (isJumping)
        {
            CheckLanding();
            return;
        }

        DetectPlayer();

        Vector3 moveTarget;

        if (player != null)
        {
            hasSeenPlayer = true;
            lastSeenTime = Time.time;

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackDistance && Time.time >= lastJumpTime + jumpCooldown)
            {
                if (!isPreparingJump)
                {
                    isPreparingJump = true;
                    jumpStartTime = Time.time;

                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                RotateTowards(player.position);

                if (Time.time >= jumpStartTime + jumpDelay)
                {
                    isPreparingJump = false;
                    StartJump(player.position);
                }

                return;
            }

            isPreparingJump = false;
            moveTarget = player.position;
        }
        else
        {
            if (hasSeenPlayer && Time.time < lastSeenTime + loseTargetDelay)
            {
                moveTarget = currentPatrolTarget.position;
            }
            else
            {
                hasSeenPlayer = false;

                if (currentPatrolTarget == null) return;

                float distance = Vector3.Distance(transform.position, currentPatrolTarget.position);

                if (distance < 1f)
                {
                    if (Time.time < waitTimer + patrolWaitTime)
                        return;

                    waitTimer = Time.time;
                    currentPatrolTarget = currentPatrolTarget == patrolPointA ? patrolPointB : patrolPointA;
                }

                moveTarget = currentPatrolTarget.position;
            }
        }

        agent.isStopped = false;
        agent.SetDestination(moveTarget);

        HandleRotationWithAvoidance();
    }

    #endregion

    #region Detection

    void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionDistance);

        player = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag(targetTag))
            {
                player = hit.transform;
                break;
            }
        }
    }

    #endregion

    #region Movement & Rotation

    void RotateTowards(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rot,
            rotationSpeed * Time.deltaTime
        );
    }

    void HandleRotationWithAvoidance()
    {
        if (agent.velocity.sqrMagnitude < 0.1f)
            return;

        Vector3 desiredDir = agent.desiredVelocity.normalized;
        desiredDir.y = 0f;

        Vector3 avoidance = Vector3.zero;

        Vector3[] dirs =
        {
        transform.forward,
        Quaternion.Euler(0, 30, 0) * transform.forward,
        Quaternion.Euler(0, -30, 0) * transform.forward
    };

        foreach (var dir in dirs)
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, dir, out hit, obstacleCheckDistance))
            {
                if (hit.collider.CompareTag("Wall"))
                {
                    Vector3 normal = hit.normal;
                    normal.y = 0f;

                    float strength = (obstacleCheckDistance - hit.distance) / obstacleCheckDistance;
                    avoidance += normal * strength * avoidanceStrength;
                }
            }

            Debug.DrawRay(transform.position, dir * obstacleCheckDistance, Color.yellow);
        }

        Vector3 finalDir = (desiredDir + avoidance).normalized;

        if (finalDir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(finalDir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Jump

    void StartJump(Vector3 targetPos)
    {
        isJumping = true;
        lastJumpTime = Time.time;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updatePosition = false;
        agent.updateRotation = false;

        JumpToTarget(targetPos);
    }

    void JumpToTarget(Vector3 targetPos)
    {
        float gravity = Physics.gravity.y;

        Vector3 displacement = targetPos - transform.position;
        Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);

        float timeUp = Mathf.Sqrt(-2f * arcHeight / gravity);
        float timeDown = Mathf.Sqrt(2f * Mathf.Abs(displacement.y - arcHeight) / -gravity);
        float totalTime = timeUp + timeDown;

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2f * gravity * arcHeight);
        Vector3 velocityXZ = displacementXZ / totalTime;

        rb.linearVelocity = velocityXZ + velocityY;

        jumpEndTime = Time.time + totalTime;
    }

    void CheckLanding()
    {
        if (Time.time < jumpEndTime)
            return;

        isJumping = false;

        rb.linearVelocity = Vector3.zero;

        agent.Warp(transform.position);
        agent.updatePosition = true;
        agent.updateRotation = false;
        agent.isStopped = false;

        if (player != null)
            agent.SetDestination(player.position);
        else
            agent.SetDestination(currentPatrolTarget.position);
    }

    #endregion
}


