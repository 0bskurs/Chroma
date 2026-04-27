using UnityEngine;

public class ArcToTargetDistance : MonoBehaviour
{
    [Header("Detection")]
    public float rayDistance = 50f;
    public string targetTag = "Player";

    [Header("Rotation")]
    public float rotationSpeed = 5f;

    [Header("Jump")]
    public float arcHeight = 5f;
    public float delayBeforeJump = 2f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.2f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private Transform currentTarget;

    private float trackStartTime;
    private bool isTracking = false;
    private bool isJumping = false;
    float jumpEndTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isJumping)
        {
            CheckLanding();
            return;
        }

        if (!isTracking)
        {
            DetectTarget();
        }
        else
        {
            if (currentTarget == null)
            {
                ResetState();
                return;
            }

            RotateTowardsTarget();

            if (Time.time >= trackStartTime + delayBeforeJump)
            {
                LaunchToTarget(currentTarget.position);
                isJumping = true;
            }
        }
    }

    void DetectTarget()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Debug.DrawRay(transform.position, transform.forward * rayDistance, Color.red);

        if (Physics.Raycast(ray, out hit, rayDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag(targetTag))
            {
                currentTarget = hit.transform;
                isTracking = true;
                trackStartTime = Time.time;
            }
        }
    }

    void RotateTowardsTarget()
    {
        Vector3 direction = currentTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void LaunchToTarget(Vector3 targetPosition)
    {
        float gravity = Physics.gravity.y;

        Vector3 displacement = targetPosition - transform.position;
        Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);

        float timeUp = Mathf.Sqrt(-2f * arcHeight / gravity);
        float timeDown = Mathf.Sqrt(2f * (displacement.y - arcHeight) / gravity);
        float totalTime = timeUp + timeDown;

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2f * gravity * arcHeight);
        Vector3 velocityXZ = displacementXZ / totalTime;

        rb.linearVelocity = velocityXZ + velocityY;

        // 🔑 store when jump should end
        jumpEndTime = Time.time + totalTime + 0.1f;
    }

    void CheckLanding()
    {
       
        if (Time.time < jumpEndTime)
            return;

        if (Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer))
        {
            ResetState();
        }
    }

    void ResetState()
    {
        isTracking = false;
        isJumping = false;
        currentTarget = null;
        rb.linearVelocity = Vector3.zero;
    }
}
    