using UnityEngine;

public class DunyaninSonuMustafaBosstest : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform head;
    public Transform neck; 

    [Header("Rotation Settings")]
    public float bodyRotationSpeed = 5f;
    public float headLookSpeed = 5f;
    public float maxHeadYAngle = 60f;

    [Header("Distance Controls")]
    public float minLookDistance = 0.5f;
    public float headDownDistance = 3f; // Distance where head starts looking down
    public float maxHeadDownAngle = 30f; // Maximum downward angle

    [Header("Model Rotation Offsets")]
    public float modelYRotationOffset = 0f;
    public float headYRotationOffset = 0f;
    public float headDownTiltOffset = 0f; // Fine-tune down angle

    // Head rotation storage
    private Quaternion initialHeadLocalRotation;
    private Quaternion targetHeadRotation;

    void Start()
    {
        if (head != null)
        {
            initialHeadLocalRotation = head.localRotation;
        }
        
        // Initialize target rotation
        targetHeadRotation = head.rotation;
    }

    void Update()
    {
        if (player == null || head == null) return;

        // --- Body Rotation ---
        RotateBody();

        // --- Head Rotation ---
        RotateHead();
    }

    void RotateBody()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude >= minLookDistance * minLookDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            lookRotation *= Quaternion.Euler(0, modelYRotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, bodyRotationSpeed * Time.deltaTime);
        }
    }

    void RotateHead()
    {
        // Calculate direction to player
        Vector3 directionToPlayer = player.position - head.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Calculate horizontal direction
        Vector3 horizontalDir = directionToPlayer;
        horizontalDir.y = 0;

        // Only rotate head if we have a valid direction
        if (horizontalDir.sqrMagnitude > 0.001f)
        {
            // Calculate base head rotation (horizontal)
            Quaternion targetHorizontalRot = Quaternion.LookRotation(horizontalDir);
            targetHorizontalRot *= Quaternion.Euler(0, headYRotationOffset, 0);

            // Apply horizontal rotation limits
            Vector3 horizontalEuler = targetHorizontalRot.eulerAngles;
            float yAngle = Mathf.DeltaAngle(transform.eulerAngles.y, horizontalEuler.y);
            yAngle = Mathf.Clamp(yAngle, -maxHeadYAngle, maxHeadYAngle);
            Quaternion clampedHorizontalRot = Quaternion.Euler(0, transform.eulerAngles.y + yAngle, 0);

            // Calculate vertical look angle
            float verticalAngle = 0f;
            if (distanceToPlayer < headDownDistance)
            {
                // Calculate how close player is (0 at start distance, 1 at min distance)
                float downFactor = 1 - Mathf.Clamp01(distanceToPlayer / headDownDistance);

                // Calculate look down angle based on player position
                float heightDifference = head.position.y - player.position.y;
                float lookDownAngle = Mathf.Atan2(heightDifference, distanceToPlayer) * Mathf.Rad2Deg;

                // Apply limits and factor
                verticalAngle = Mathf.Clamp(lookDownAngle, 0, maxHeadDownAngle) * downFactor;
            }

            // Combine rotations: horizontal + vertical + offset
            targetHeadRotation = clampedHorizontalRot * Quaternion.Euler(verticalAngle + headDownTiltOffset, 0, 0);
            
            if(neck != null) neck.rotation = Quaternion.Slerp(neck.rotation, targetHeadRotation, headLookSpeed * Time.deltaTime);
        }

        // Apply head rotation smoothly
        head.rotation = Quaternion.Slerp(head.rotation, targetHeadRotation, headLookSpeed * Time.deltaTime);
    }
}